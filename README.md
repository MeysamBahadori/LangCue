# LangCue

**[نسخه فارسی راهنما / Persian README](README.fa.md)**

A lightweight Windows utility that shows the active keyboard language near the insertion caret so multilingual users can notice the selected language **before typing**. It never reads, stores, or transmits typed content.

**Version:** `0.2.0-preview.2` — an early pre-release, not 1.0.

## Features

- **Multiple keyboard languages:** ISO language codes such as EN, DE, FR, AR, HE, UR, RU, ZH, JA, and KO. Persian retains the familiar **فا** label. Unknown custom Windows language IDs are displayed as hexadecimal IDs.
- **RTL/LTR aware:** Ahead/Behind placement uses the Windows language's right-to-left property (not just Persian).
- **Position controls:** Above (default), Ahead, Behind or Below the caret; customizable 0–80 pixel gap.
- **Smoother badge:** Per-pixel-alpha antialiased rounded corners, independently adjustable **background opacity** (40–100%, default 78%); text stays opaque.
- **Three caret detection paths:** Win32 system caret, MSAA OBJID_CARET, and UI Automation with collapsed-range validation and parent traversal, supporting more Chromium/Electron controls.
- **Popup handling:** Reasserts topmost Z-order above ordinary menus/dropdowns when Windows permits without stealing keyboard focus.
- **Custom application/tray icon**, Settings and Exit tray commands, single-instance handling.
- **Privacy:** No keyboard hook, text capture, telemetry, or background internet requests.

## Install on Windows

After the release workflow succeeds, get the self-contained Windows 10/11 x64 installer from **[GitHub Releases](https://github.com/MeysamBahadori/LangCue/releases)**.

Asset: `LangCue-Setup-0.2.0-preview.2-win-x64.exe`.

Per-user installation requires no administrator rights or separately installed .NET runtime. Start at Windows sign-in is an **optional, initially unchecked** installer task. Uninstall from Windows Settings > Installed Apps.

The pre-release installer is **unsigned**; Windows SmartScreen may warn about an unrecognized publisher.

**Release availability:** A commit is not an installable release. The Windows GitHub Actions job must successfully run to create the release and attach its installer.

## Build and run from source

Requires Windows 10/11, .NET 10 SDK and NuGet access on initial restore.

```powershell
git clone https://github.com/MeysamBahadori/LangCue.git
cd LangCue
dotnet restore LangCue.slnx
dotnet build LangCue.slnx -c Release
dotnet run --project src/LangCue
```

To build the installer locally, install Inno Setup 6, then:

```powershell
dotnet publish src/LangCue/LangCue.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer\LangCue.iss"
```

Output: `artifacts/installer/LangCue-Setup-0.2.0-preview.2-win-x64.exe`.

## One-command installer and manual-release fallback

If Actions cannot start a Windows job, build the installer yourself using the included PowerShell script (requires **.NET 10 SDK + Inno Setup 6**):

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\Build-Installer.ps1
```

After verifying the installer, you can publish the first pre-release from your own Windows machine using **GitHub CLI**, authenticated with `gh auth login`:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\Build-Installer.ps1 -Publish
```

The publishing option creates the GitHub Release and uploads the setup file; it will refuse to overwrite an existing release.

## Settings

Right-click LangCue in the system tray, then choose **Settings...**

| Setting | Description |
| --- | --- |
| Above | Default: above caret |
| Ahead / Behind | Takes keyboard language's RTL/LTR direction into account |
| Below | Below caret |
| Gap | 0–80 physical pixels; default 14 |
| Background opacity | 40–100%; default 78, text stays solid |

Saved in `%LOCALAPPDATA%\LangCue\settings.json`; previous settings are retained when upgrading.

## Manual smoke tests

1. Exit any prior instance using the tray menu, then launch the new executable.
2. Try Notepad, Chrome's address bar and page text boxes, and a ChatGPT contenteditable field.
3. Try VS Code Editor, Search, Terminal and Chat and a Claude chat field.
4. Switch Windows keyboard languages (English, Persian, German, Arabic, etc.) and move the caret.
5. Adjust position, gap and opacity; restart to test settings persistence.
6. Try editable dropdowns and menus.

## How it works

A dedicated MTA worker reads the **foreground input layout** from Windows. It requests caret geometry in this order:

1. `GetGUIThreadInfo` — system caret;
2. `AccessibleObjectFromWindow(OBJID_CARET)` — Active Accessibility;
3. UI Automation `TextPattern2.GetCaretRange` and collapsed `TextPattern.GetSelection` on the focused element or its parents;
4. An **approximate input boundary** only for certain focused Edit controls.

No text is read. The badge is a non-activating, click-through, layered window with per-pixel transparency.

## Known limitations

- Some VS Code/Monaco editors and custom chat widgets do **not** expose exact caret geometry. Display may be approximate or absent; app-specific adapters may be needed later.
- Some Windows system menus and elevated/secure windows can occlude the overlay; access across UAC secure desktops is not supported.
- IME submodes such as Japanese Kana/Latin may change without changing the Windows keyboard language. The badge shows the Windows input language, **not** the IME's exact conversion mode or paragraph direction.
- Behavior depends on each application's accessibility implementation; universal coverage is not guaranteed.

## Project structure

`LangCue.slnx` contains **one** .NET 10 WinForms project in `src/LangCue`. The `assets` folder contains the app icon; `installer` holds the Inno Setup script; `.github/workflows` defines build and release automation.

## License

LangCue is released under the **MIT License**. You may use, modify, distribute, and contribute to the project under the terms of the license. See the [LICENSE](LICENSE) file for the full text.

Copyright © 2026 Meysam Bahadori.
