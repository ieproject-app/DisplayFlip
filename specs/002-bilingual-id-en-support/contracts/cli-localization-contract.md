# Contract: Command-Line Interface (`CliRunner`) Localization

## Command-Line Flags
- `--lang <id|en>` or `--lang=<id|en>` (case-insensitive)
  - Optional flag applicable to any CLI command (`--help`, `--list`, `--swap`, `--identify`, `--solo`, `--extend`, `--apply`).
  - Overrides the effective CLI language for that process invocation only; MUST NOT modify `%LocalAppData%\DisplayFlip\profiles.json`.
  - If an unknown `--lang <value>` is passed, logs a localized warning or falls back gracefully to default resolution (`auto`) without crashing.

## Localized Outputs
- **Help (`--help`, `-h`, `/?`)**: Outputs localized section headers, usage syntax, and option descriptions (including `--lang <id|en>`).
- **Profile Listing (`--list`, without `--json`)**: Outputs localized header (`"Saved Profiles (N):"` / `"Daftar Profil Tersimpan (N):"`), shortcut label, and monitor count label. `--list --json` outputs valid JSON untouched (`LayoutProfile` field names and profile names remain invariant).
- **Actions (`--swap`, `--identify`, `--solo`, `--extend`, `--apply`)**: Console status and error lines use the resolved language (`[DisplayFlip]` / `[DisplayFlip ERROR]`), while preserving profile names and numeric exit codes (`0`, `1`, `4`, `5`).
- **Missing Action / Unknown Command**: Outputs localized help hint to `stderr` with exit code `1`.
