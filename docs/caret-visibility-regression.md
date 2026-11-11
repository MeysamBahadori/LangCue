# Caret visibility / focus regression checklist

This fix is intended to address two observed failures:

1. **Scrollable browser page:** A caret remains logically focused while its textbox scrolls entirely out of view. The floating badge must **disappear**, not stick at the top of the screen.
2. **VS Code / Electron:** After clicking another control or closing an editor, the badge must **disappear**, not stay at the previous text position.

## Manual test matrix (Windows)

These are manual tests, **not** automated or already verified.

| Scenario | Expected behavior |
| --- | --- |
| Chrome, visible textbox, EN/FA keyboard | Badge appears at visible caret |
| Chrome, scroll textbox partly out of view | Badge stays only while caret is within the visible input viewport |
| Chrome, scroll textbox fully outside page viewport | Badge hides instead of sticking to viewport edge |
| Chrome, scroll textbox back into view | Badge reappears once the focused caret is visible |
| VS Code search → editor or sidebar | Badge follows the new focused editable control (when that provider exposes a caret) |
| VS Code edit → Explorer, toolbar or noneditable area | Badge hides |
| VS Code editor tab closed | Badge hides rather than remaining at the last editor coordinate |
| VS Code chat / Claude chat | Badge appears only when a visible input caret is reported |
| Classic Notepad and WinForms TextBox | Previous behavior preserved |
| Settings dialog or tray menu open | Badge does not overlay settings while focus is elsewhere |
| Chrome → another application | Old badge disappears immediately |
| Language changed via Win+Space | Badge updates; no keystroke content is inspected |

## Implementation notes

- UIA selection geometry is accepted only when it belongs to the **focused** element; text patterns on ancestors are accepted only if **TextPattern2** reports an **active** caret.
- The caret's screen coordinates must remain within the foreground window's client area and the visible UI Automation focus/ancestor intersection.
- Cross-process focus and foreground WinEvent notifications invalidate snapshots immediately, and the monitor binds observations to a focus revision.
- Missing or inconsistent accessibility geometry is handled by hiding the badge rather than rendering it at a guessed screen edge.

## Limitations

No universal caret geometry API exists for every VS Code/Monaco editor state. If a provider reports neither a visible active caret nor valid legacy coordinates, the correct fallback is to hide the badge. A manual Windows build and browser/editor regression pass are needed before considering this verified.
