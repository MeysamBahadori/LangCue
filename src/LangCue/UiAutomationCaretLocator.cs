using System.Drawing;
using System.Runtime.InteropServices;
using Interop.UIAutomationClient;

namespace LangCue;

/// <summary>
/// Caret geometry from a UIA provider must belong to a focused, visible text
/// control. An ancestor's saved selection is not proof that it still has focus.
/// All methods run on the MTA caret-monitoring thread.
/// </summary>
internal sealed class UiAutomationCaretLocator
{
    private const int TextPatternId = 10014;
    private const int TextPattern2Id = 10024;
    private const int EditControlTypeId = 50004;
    private const int ComboBoxControlTypeId = 50003;

    private IUIAutomation? _automation;

    public bool TryLocate(IntPtr foreground, KeyboardLanguage language, out Rectangle caret)
    {
        caret = Rectangle.Empty;
        try
        {
            if (!TryGetFocus(foreground, out IUIAutomationElement focused) ||
                !TryGetVisibleClip(focused, out Rectangle clip))
            {
                return false;
            }

            IUIAutomationElement? element = focused;
            IUIAutomationTreeWalker walker = _automation!.RawViewWalker;

            // TextPattern2.GetCaretRange tells us whether its own caret is
            // active. TextPattern.GetSelection does NOT, so it is accepted
            // ONLY on the actual focused element, never on an ancestor.
            for (int depth = 0; depth < 6 && element is not null; depth++)
            {
                if (TryGetTextCaret(element, isFocusedElement: depth == 0,
                        out Rectangle candidate) &&
                    CaretVisibility.IsWithinElementClip(candidate, clip))
                {
                    caret = candidate;
                    return NativeMethods.GetForegroundWindow() == foreground;
                }

                element = walker.GetParentElement(element);
            }

            // Approximate the edge ONLY for an actual, focused Edit control.
            // Do not borrow a stale selection from a parent editor/document.
            if (focused.CurrentControlType == EditControlTypeId)
            {
                var bounds = focused.CurrentBoundingRectangle;
                int width = bounds.right - bounds.left;
                int height = bounds.bottom - bounds.top;

                if (width > 12 && width < 3000 && height > 12 && height < 400)
                {
                    int x = language.IsRightToLeft ? bounds.right - 10 : bounds.left + 10;
                    int y = bounds.top + Math.Max(2, (height - 18) / 2);
                    Rectangle approximate = new(x, y, 2, 18);

                    if (CaretVisibility.IsWithinElementClip(approximate, clip))
                    {
                        caret = approximate;
                        return NativeMethods.GetForegroundWindow() == foreground;
                    }
                }
            }
        }
        catch (COMException)
        {
            _automation = null;
        }
        catch (InvalidCastException)
        {
            _automation = null;
        }
        catch (InvalidOperationException)
        {
            _automation = null;
        }

        return false;
    }

    /// <summary>
    /// Validate Win32/MSAA candidates against the *current* UIA focus and its
    /// visible ancestors. A stale MSAA caret must not keep the badge alive
    /// after the user clicks a toolbar or closes the editor.
    /// </summary>
    public bool IsFocusedCaretVisible(IntPtr foreground, Rectangle caret)
    {
        try
        {
            if (!TryGetFocus(foreground, out IUIAutomationElement focused) ||
                !LooksLikeTextInput(focused) ||
                !TryGetVisibleClip(focused, out Rectangle clip))
            {
                return false;
            }

            return CaretVisibility.IsWithinElementClip(caret, clip) &&
                   NativeMethods.GetForegroundWindow() == foreground;
        }
        catch (COMException)
        {
            _automation = null;
            // If a provider has no UIA support at all, the independently
            // validated Win32/MSAA system caret can still be used.
            return true;
        }
        catch (InvalidCastException)
        {
            _automation = null;
            return true;
        }
        catch (InvalidOperationException)
        {
            _automation = null;
            return true;
        }
    }

    private bool TryGetFocus(IntPtr foreground, out IUIAutomationElement focused)
    {
        _automation ??= CreateAutomation();
        focused = _automation.GetFocusedElement();

        return focused is not null &&
               focused.CurrentHasKeyboardFocus != 0 &&
               focused.CurrentIsOffscreen == 0 &&
               NativeMethods.GetForegroundWindow() == foreground;
    }

    private bool TryGetVisibleClip(IUIAutomationElement focused, out Rectangle clip)
    {
        clip = Rectangle.Empty;
        bool foundRectangle = false;

        IUIAutomationElement? current = focused;
        IUIAutomationTreeWalker walker = _automation!.RawViewWalker;

        // Parent bounding rectangles constrain visible scroll viewports.
        // Do not let carets outside a scrolled-off textbox survive at the
        // edge of the browser or desktop.
        for (int depth = 0; depth < 10 && current is not null; depth++)
        {
            if (current.CurrentIsOffscreen != 0)
            {
                return false;
            }

            var bound = current.CurrentBoundingRectangle;
            if (bound.right > bound.left + 4 && bound.bottom > bound.top + 4 &&
                bound.left > -100000 && bound.top > -100000 &&
                bound.right < 100000 && bound.bottom < 100000)
            {
                Rectangle rectangle = Rectangle.FromLTRB(
                    bound.left, bound.top, bound.right, bound.bottom);

                clip = foundRectangle ? Rectangle.Intersect(clip, rectangle) : rectangle;
                foundRectangle = true;

                if (clip.IsEmpty)
                {
                    return false;
                }
            }

            current = walker.GetParentElement(current);
        }

        return foundRectangle && !clip.IsEmpty;
    }

    private static bool LooksLikeTextInput(IUIAutomationElement focused)
    {
        int controlType = focused.CurrentControlType;
        if (controlType is EditControlTypeId or ComboBoxControlTypeId)
        {
            return true;
        }

        // A text editor/terminal can expose a Document or custom control
        // rather than Edit. Trust TextPattern2 only when it reports its own
        // active caret, not an old collapsed selection.
        if (GetOptionalPattern(focused, TextPattern2Id) is IUIAutomationTextPattern2 text2)
        {
            try
            {
                text2.GetCaretRange(out int isActive);
                return isActive != 0;
            }
            catch (COMException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryGetTextCaret(
        IUIAutomationElement element,
        bool isFocusedElement,
        out Rectangle caret)
    {
        caret = Rectangle.Empty;

        if (GetOptionalPattern(element, TextPattern2Id) is IUIAutomationTextPattern2 text2)
        {
            try
            {
                IUIAutomationTextRange range = text2.GetCaretRange(out int isActive);
                if (isActive != 0 && TryGetCaretRectangle(range, out caret))
                {
                    return true;
                }
            }
            catch (COMException)
            {
                // The provider does not implement the caret geometry.
            }
        }

        if (!isFocusedElement)
        {
            return false;
        }

        if (GetOptionalPattern(element, TextPatternId) is IUIAutomationTextPattern text)
        {
            try
            {
                IUIAutomationTextRangeArray selection = text.GetSelection();
                if (selection is null || selection.Length != 1)
                {
                    return false;
                }

                IUIAutomationTextRange range = selection.GetElement(0);
                if (range.CompareEndpoints(
                        TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
                        range,
                        TextPatternRangeEndpoint.TextPatternRangeEndpoint_End) != 0)
                {
                    return false;
                }

                return TryGetCaretRectangle(range, out caret);
            }
            catch (COMException)
            {
                return false;
            }
        }

        return false;
    }

    private static object? GetOptionalPattern(IUIAutomationElement element, int id)
    {
        try
        {
            return element.GetCurrentPattern(id);
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static IUIAutomation CreateAutomation()
    {
        try
        {
            return new CUIAutomation8();
        }
        catch (COMException)
        {
            return new CUIAutomation();
        }
    }

    private static bool TryGetCaretRectangle(IUIAutomationTextRange range, out Rectangle caret)
    {
        if (TryConvertRange(range.GetBoundingRectangles(), useRightEdge: false, out caret))
        {
            return true;
        }

        // Request only adjacent character geometry, never the text itself.
        IUIAutomationTextRange following = range.Clone();
        if (following.MoveEndpointByUnit(
                TextPatternRangeEndpoint.TextPatternRangeEndpoint_End,
                TextUnit.TextUnit_Character, 1) > 0 &&
            TryConvertRange(following.GetBoundingRectangles(), useRightEdge: false, out caret))
        {
            return true;
        }

        IUIAutomationTextRange previous = range.Clone();
        if (previous.MoveEndpointByUnit(
                TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
                TextUnit.TextUnit_Character, -1) < 0 &&
            TryConvertRange(previous.GetBoundingRectangles(), useRightEdge: true, out caret))
        {
            return true;
        }

        caret = Rectangle.Empty;
        return false;
    }

    private static bool TryConvertRange(double[]? bounds, bool useRightEdge, out Rectangle caret)
    {
        caret = Rectangle.Empty;
        if (bounds is not { Length: 4 })
        {
            return false;
        }

        double x = bounds[0];
        double y = bounds[1];
        double width = bounds[2];
        double height = bounds[3];

        if (!double.IsFinite(x) || !double.IsFinite(y) ||
            !double.IsFinite(width) || !double.IsFinite(height) ||
            width < 0 || width > 80 || height < 2 || height > 150 ||
            Math.Abs(x) > 100000 || Math.Abs(y) > 100000)
        {
            return false;
        }

        caret = new Rectangle(
            (int)Math.Round(useRightEdge ? x + width : x),
            (int)Math.Round(y), 2, (int)Math.Round(height));
        return true;
    }
}
