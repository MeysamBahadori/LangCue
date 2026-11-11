using System.Drawing;
using System.Runtime.InteropServices;
using Interop.UIAutomationClient;

namespace LangCue;

/// <summary>
/// Finds custom carets through UI Automation, without ever reading text content.
/// Called exclusively on the background MTA monitoring thread.
/// </summary>
internal sealed class UiAutomationCaretLocator
{
    private const int TextPatternId = 10014;
    private const int TextPattern2Id = 10024;
    private const int EditControlTypeId = 50004;

    private IUIAutomation? _automation;

    public bool TryLocate(IntPtr foreground, KeyboardLanguage language, out Rectangle caret)
    {
        caret = Rectangle.Empty;

        try
        {
            _automation ??= CreateAutomation();
            IUIAutomationElement focused = _automation.GetFocusedElement();

            if (focused is null || focused.CurrentIsOffscreen != 0 ||
                NativeMethods.GetForegroundWindow() != foreground)
            {
                return false;
            }

            // Electron / Monaco can put the actual text pattern on a parent
            // rather than on the initially focused leaf element.
            IUIAutomationElement? element = focused;
            IUIAutomationTreeWalker walker = _automation.RawViewWalker;

            for (int depth = 0; depth < 5 && element is not null; depth++)
            {
                if (TryGetTextCaret(element, out caret))
                {
                    return NativeMethods.GetForegroundWindow() == foreground;
                }

                element = walker.GetParentElement(element);
            }

            // Final approximation ONLY for a focused Edit control. The input
            // boundary is not an exact caret; never treat a large selection
            // rectangle as the caret (the old left-edge VS Code bug).
            if (focused.CurrentControlType == EditControlTypeId &&
                focused.CurrentHasKeyboardFocus != 0)
            {
                var rect = focused.CurrentBoundingRectangle;
                if (rect.right > rect.left && rect.bottom > rect.top &&
                    rect.right - rect.left < 3000 &&
                    rect.bottom - rect.top < 400)
                {
                    int x = language.IsRightToLeft ? rect.right - 10 : rect.left + 10;
                    int y = rect.top + Math.Max(2, (rect.bottom - rect.top - 18) / 2);
                    caret = new Rectangle(x, y, 2, 18);
                    return NativeMethods.GetForegroundWindow() == foreground;
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

    private static bool TryGetTextCaret(IUIAutomationElement element, out Rectangle caret)
    {
        caret = Rectangle.Empty;

        object? pattern = GetOptionalPattern(element, TextPattern2Id);
        if (pattern is IUIAutomationTextPattern2 text2)
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
                // Not implemented by this accessibility provider.
            }
        }

        pattern = GetOptionalPattern(element, TextPatternId);
        if (pattern is IUIAutomationTextPattern text)
        {
            try
            {
                IUIAutomationTextRangeArray selection = text.GetSelection();
                if (selection is null || selection.Length != 1)
                {
                    return false;
                }

                IUIAutomationTextRange range = selection.GetElement(0);
                // A selected word or paragraph is not a caret. It previously
                // caused indicators to appear at the left edge of chat editors.
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
                // Selection geometry is optional; fall back to another provider.
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

        // Some UIA providers return no bounds for a degenerate range. Only
        // adjacent character geometry is requested, not the character's text.
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
            (int)Math.Round(y),
            2,
            (int)Math.Round(height));
        return true;
    }
}
