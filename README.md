# LangCue

A lightweight Windows utility that shows the active keyboard language beside the text caret.

LangCue helps multilingual users notice the selected keyboard language **before** typing. It does not read typed text, install a keyboard hook, or change the keyboard layout.

## MVP 1

- Shows a small **EN** / **فا** indicator beside the caret in applications exposing the standard Windows caret.
- Updates when the caret moves or the active keyboard language changes (150 ms polling).
- Hides when no supported caret is available.
- Runs in the Windows notification area; right-click the tray icon to exit.
- Does not steal keyboard focus or intercept keystrokes.
- Prevents multiple instances in the same Windows session.

## Requirements

- Windows 10/11
- .NET 10 SDK (to build and run from source)

## Run

```powershell
dotnet restore LangCue.slnx
dotnet run --project src/LangCue/LangCue.csproj
```

Build a Release executable:

```powershell
dotnet build LangCue.slnx --configuration Release
```

The resulting application is a Windows GUI process, so it has no console window. Find the tray icon (possibly under the hidden icons arrow) and choose **Exit LangCue** to quit.

## Manual smoke test

1. Add both English and Persian keyboards in Windows settings.
2. Start LangCue from a terminal; verify that a tray icon appears.
3. Focus a classic Win32 edit control (e.g., a Windows Forms `TextBox`; Notepad where the Windows caret is exposed).
4. Check that an **EN** or **فا** badge appears next to the insertion caret.
5. Switch input language with **Win+Space** or **Alt+Shift**; verify the badge updates.
6. Type or move the caret; verify the badge follows it.
7. Focus a non-editable control or switch away; verify the badge disappears.
8. Right-click the tray icon and select **Exit LangCue**.

## MVP 1 limitations

This first version intentionally uses only the Win32 `GetGUIThreadInfo` system-caret API and `GetKeyboardLayout`. Custom-drawn carets in some modern applications (including some versions of Notepad, browsers, editors, and terminals) may not be detected. MSAA/UI Automation fallbacks are planned for a later MVP. It is not expected to work on secure desktops, UAC prompts, or all elevated applications.

The indicator is a non-activating, click-through overlay. It never records text or sends any input. There are no external NuGet dependencies.

## Project structure

- `LangCue.slnx` — .NET 10 solution
- `src/LangCue` — single WinForms project (tray host, Win32 caret monitor, overlay)

## License

A license has not been selected yet; the repository is currently private.
