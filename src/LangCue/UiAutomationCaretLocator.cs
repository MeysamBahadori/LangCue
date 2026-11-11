using System.Drawing;
using System.Runtime.InteropServices;
using Interop.UIAutomationClient;

namespace LangCue;

/// <summary>
/// Resolves custom carets in modern apps (especially Chromium) using Windows UI Automation.
/// Construct and call this class only from the monitoring worker, never from the UI thread.
/// </summary>
internal sealed class UiAutomationCaretLocator
{
    private const int TextPatternId = 10014;
    private const int TextPattern2Id = 10024;
    private const int EditControlTypeId = 50004;

    private IUIAutomation? _automation;

    public bool TryLocate(IntPtr foreground, string language, out Rectangle caret)
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

            // Unsupported patterns are normal. Not every UIA provider offers TextPattern2.
            // Do not let a missing pattern prevent the Edit-control fallback.
            object? pattern = GetOptionalPattern(focused, TextPattern2Id);
            if (pattern is IUIAutomationTextPattern2 text2)
            {
                try
                {
                    IUIAutomationTextRange range = text2.GetCaretRange(out int isActive);
                    if (isActive == 0)
                    {
                        return false;
                    }

                    if (TryGetCaretRectangle(range, out caret))
                    {
                        return true;
                    }
                }
                catch (COMException)
                {
                    // Caret geometry not implemented, try the next strategy.
                }
            }

            pattern = GetOptionalPattern(focused, TextPatternId);
            if (pattern is IUIAutomationTextPattern text)
            {
                try
                {
                    IUIAutomationTextRangeArray selection = text.GetSelection();
                    if (selection is not null && selection.Length > 0 &&
                        TryGetCaretRectangle(selection.GetElement(0), out caret))
                    {
                        return true;
                    }
                }
                catch (COMException)
                {
                    // Some controls expose TextPattern without selection geometry.
                }
            }

            // Conservative fallback for an empty or unsupported Edit field (e.g. omnibox).
            // We place the badge near the input boundary, not at an invented exact caret.
            if (focused.CurrentControlType == EditControlTypeId &&
                focused.CurrentHasKeyboardFocus != 0)
            {
                var rect = focused.CurrentBoundingRectangle;
                if (rect.right > rect.left && rect.bottom > rect.top)
                {
                    int x = language == "فا" ? rect.right - 10 : rect.left + 10;
                    int y = rect.top + Math.Max(2, (rect.bottom - rect.top - 18) / 2);
                    caret = new Rectangle(x, y, 2, 18);
                    return true;
                }
            }
        }
        catch (COMException)
        {
            // Accessibility providers can disappear or reject cross-process queries.
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

        // Some UIA providers return no rectangle for a zero-length caret. Request
        // the geometry of one adjacent character without requesting its contents.
        IUIAutomationTextRange following = range.Clone();
        if (following.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_End,
                                         TextUnit.TextUnit_Character, 1) > 0 &&
            TryConvertRange(following.GetBoundingRectangles(), useRightEdge: false, out caret))
        {
            return true;
        }

        IUIAutomationTextRange previous = range.Clone();
        if (previous.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
                                        TextUnit.TextUnit_Character, -1) < 0 &&
            TryConvertRange(previous.GetBoundingRectangles(), useRightEdge: true, out caret))
        {
            return true;
        }

        caret = Rectangle.Empty;
        return false;
    }

    private static bool TryConvertRange(
        double[]? coordinates,
        bool useRightEdge,
        out Rectangle caret)
    {
        caret = Rectangle.Empty;
        if (coordinates is not { Length: >= 4 })
        {
            return false;
        }

        double x = coordinates[0];
        double y = coordinates[1];
        double width = coordinates[2];
        double height = coordinates[3];

        // Ignore selections, off-screen ranges, and broken provider geometry.
        if (!double.IsFinite(x) || !double.IsFinite(y) ||
            !double.IsFinite(width) || !double.IsFinite(height) ||
            width < 0 || width > 80 || height < 2 || height > 150 ||
            Math.Abs(x) > 100000 || Math.Abs(y) > 100000)
        {
            return false;
        }

        int caretX = (int)Math.Round(useRightEdge ? x + width : x);
        caret = new Rectangle(caretX, (int)Math.Round(y), 2, (int)Math.Round(height));
        return true;
    }
}
