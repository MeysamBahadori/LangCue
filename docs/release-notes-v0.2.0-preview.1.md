# LangCue 0.2.0-preview.1 — Windows x64

First **pre-release** of LangCue. This is an early tester preview, not version 1.0.

- Windows keyboard languages displayed as ISO language codes (Persian: فا).
- RTL-aware placement; settings for position, gap, and opacity.
- Antialiased translucent click-through badge and custom application/tray icon.
- Win32, MSAA, and UI Automation caret-location strategies.
- UI Automation parent traversal and stricter collapsed-range checks for Chromium and Electron editors.
- Single-project .NET 10 WinForms app and self-contained Windows x64 Inno Setup installer.

## Known limitations

Some custom editors (including Monaco/VS Code) may still not expose usable caret geometry. Certain system menus, elevated windows and secure prompts can remain above the overlay. Windows input language is not the same as an IME's internal composition mode.

## Install

Download the installer asset named LangCue-Setup-0.2.0-preview.1-win-x64.exe from this release. It installs for the current user, needs no administrator permission, and includes the .NET runtime. You can opt into launching LangCue at sign-in.

This preview installer is **unsigned**. Windows SmartScreen may show an unrecognized publisher warning. Verify the release source before running it.

[راهنمای فارسی](../README.fa.md)
