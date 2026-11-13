# Contract: CLI

**Feature**: 001-displayflip-monitor-layout
**Assembly**: `DisplayFlip.exe`

## Syntax

```text
DisplayFlip.exe --mirror
DisplayFlip.exe --apply "<ProfileName>"
DisplayFlip.exe --list [--json]           # daftar profil untuk diagnosa (helper)
```

## Semantics

- `--mirror` = operasi Mirror primary-preserving (FR-001). Tidak membaca/menulis profil. Settles secara asinkron; proses keluar setelah Apply berhasil diproses (atau gagal dengan pesan). Exit 0 = berhasil.
- `--apply "<name>"` = ApplyProfile named (FR-003/FR-008). Name case-insensitive, harus persis cocok dengan profil tersimpan. Bila nama tidak ditemukan: exit 3, pesan ke stderr. Bila monitor hilang dan user membatalkan: exit 4.
- `--list` (diagnostik, bukan requirement spec tapi berguna untuk test) = cetak daftar profil; `--json` menghasilkan JSON array sesuai Data Model.

## Exit codes

| Code | Arti |
|------|------|
| 0 | Berhasil (Mirror/Apply sudah Applied) |
| 2 | Validasi gagal (`ERROR_BAD_CONFIGURATION`) — lihat pesan validate |
| 3 | Profil tidak ditemukan |
| 4 | Dibatalkan karena monitor hilang / input invalid |
| 5 | Gagal QE bertopologi (CCD error lain) |

## Examples

```powershell
.\DisplayFlip.exe --mirror
.\DisplayFlip.exe --apply "Primary Kanan"
.\DisplayFlip.exe --list --json
```

Kompatibilitas: CLI dan tray memakai implementasi Core yang sama (D7); tidak ada perbedaan semantik.

## Error messages (stderr)

- `Profile not found: '<name>' (see --list)`
- `MissingMonitor: <StableKey> not connected. Abort.`
- `Validation failed: ERROR_BAD_CONFIGURATION — see quickstart`
