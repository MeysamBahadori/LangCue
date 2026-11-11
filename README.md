# LangCue

A lightweight Windows utility that shows the active keyboard language near the text caret.

LangCue helps multilingual users notice the selected keyboard language **before** typing. It does not read typed text, install a keyboard hook, or change the keyboard layout.

## Features

- **EN** / **فا** indicator near the insertion caret.
- Four indicator positions: **Above** (default), **Ahead**, **Behind**, or **Below** the caret.
- Configurable gap of 0–80 pixels; settings survive app restarts.
- Direction-aware Ahead/Behind positioning (Persian = RTL, other layouts = LTR).
- Classic Win32 caret support, plus Windows UI Automation fallback for modern controls, including Chromium browsers.
- Runs in the Windows system tray; right-click for **Settings...** or **Exit LangCue**.
- Does not steal focus, intercept keystrokes, or inspect typed text.
- Prevents multiple instances in the same Windows session.

## Requirements

- Windows 10/11
- .NET 10 SDK (to build and run from source)
- Internet during first restore to download the single NuGet dependency, \`Interop.UIAutomationClient\`.

## Run

```powershell
git pull
dotnet restore LangCue.slnx
dotnet run --project src/LangCue/LangCue.csproj
```

Build a Release executable:

\`\`\`powershell
dotnet build LangCue.slnx --configuration Release
\`\`\`

The executable has no console window. Find the tray icon (possibly under the hidden-icons arrow), right-click it, and open **Settings...** or **Exit LangCue**. Restart the old running instance before trying a new build; only one instance runs at a time.

### Settings

- **Above caret:** indicator is centered over the insertion point, separated by the configured gap (default: 14px).
- **Ahead:** right side in LTR, left side in RTL.
- **Behind:** left side in LTR, right side in RTL.
- **Below caret:** centered below the insertion point.
- **Gap:** 0–80 physical pixels between the caret and indicator.

Settings are stored in \`%LOCALAPPDATA%\LangCue\settings.json\`. Delete this file to restore the defaults.

## Manual smoke test

1. Add both English and Persian keyboards in Windows Settings.
2. Start LangCue; verify the system tray icon and **Settings...** option.
3. In Notepad or another standard edit box, move the caret; verify the badge appears **above** it.
4. Switch layouts with **Win+Space**; verify the badge changes between **EN** and **فا**.
5. Open Chrome (version 138+ recommended). Click the **address bar** (Ctrl+L), type, and move the caret; verify the badge.
6. In Chrome, test a search box, a \`textarea\`, and a \`contenteditable\` field, in both LTR and RTL input modes.
7. Change the position and gap in Settings; verify the indicator moves and settings persist after restart.
8. Switch to a non-editable area; the badge should disappear. Use **Exit LangCue** to close the app.

## How caret detection works

1. \`GetGUIThreadInfo\` retrieves traditional system-caret bounds.
2. When that fails, a dedicated background MTA thread uses Windows UI Automation \`TextPattern2.GetCaretRange\` to retrieve the focused text control's caret bounds.
3. If the provider returns an empty caret range, LangCue asks for an adjacent character's **geometry only** (never its contents). If an Edit field has no usable caret geometry, it falls back to the focused field's edge; the badge will be near the field, not necessarily at the exact insertion point.
4. If focus or geometry cannot be verified, the indicator is hidden instead of guessing.

The UI thread never waits on the accessibility provider, so unresponsive applications cannot freeze the settings window or tray menu.

## Limitations

Browser caret support depends on the UI Automation capabilities of the installed browser, version, and focused element. Chrome 138+ enables native UIA by default, but providers can still omit caret geometry for specific controls. Some custom editors, secure desktops, UAC prompts, elevated apps, and games are not supported. Direction for Ahead/Behind is inferred from the selected keyboard language rather than the document's paragraph direction.

## Project structure

- \`LangCue.slnx\` — .NET 10 solution
- \`src/LangCue\` — one WinForms project, including the tray host, Win32/UIA monitor, settings, and overlay
- \`.github/workflows/build.yml\` — Windows build workflow

## License

A license has not been selected yet; the repository is currently private.
