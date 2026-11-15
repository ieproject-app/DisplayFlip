# Quickstart Validation: Bilingual Indonesian & English Localization

## Prerequisites
- Windows 10 / 11 (x64) with [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Repository root: `D:\GitHub\multi-monitor`

## 1. Automated Parity, Persistence & UI Tests
Run the full xUnit suite (includes dictionary 100% key parity, placeholder parity, first-launch OS locale defaults, persistence roundtrip, live `Loc` switching, unapplied canvas state preservation, and CLI `--lang` output):
```powershell
dotnet test DisplayFlip.slnx
```
Expected result: 100% passed, 0 failed, 0 skipped.

## 2. Command-Line Interface (CLI) Bilingual Check
Test English and Indonesian CLI outputs without modifying saved configuration:
```powershell
dotnet run --project src\DisplayFlip.Wpf\DisplayFlip.Wpf.csproj -- --help --lang en
dotnet run --project src\DisplayFlip.Wpf\DisplayFlip.Wpf.csproj -- --help --lang id
dotnet run --project src\DisplayFlip.Wpf\DisplayFlip.Wpf.csproj -- --list --lang en
dotnet run --project src\DisplayFlip.Wpf\DisplayFlip.Wpf.csproj -- --list --lang id
```
Expected result:
- Help headings, descriptions, and `--list` labels render cleanly in English for `--lang en` and Bahasa Indonesia for `--lang id`.
- `--list --json` continues to output valid JSON with unaltered profile names.

## 3. Live UI, Canvas, Tray & Persistence Verification
1. Build or launch `DisplayFlip.Wpf`.
2. Verify initial launch language follows Windows locale (`id*` -> Bahasa Indonesia, other locales -> English) when no language preference was previously saved.
3. Drag a monitor on **Displays** (`Tampilan`) without clicking **Apply** so `HasUnappliedChanges` is active.
4. Navigate to **General & Icons**, select **English** in the **Language** selector:
   - Navigation sidebar, headers, theme cards, icon cards, and tray context menu update immediately without restarting.
   - Return to **Displays**: all labels/badges (`★ Primary`, `Apply Changes`, `Reset`) are in English **and** the pending unapplied monitor move on the 2D canvas is still preserved.
5. Open **About & CLI** and verify CLI cards update to English and the official website link points to `https://displayflip.snipgeek.com`.
6. Restart `DisplayFlip.exe` and verify the app launches directly in English (`profiles.json` preserves `"language": "en"`).
