# Contract: Profile Store (JSON)

**Feature**: 001-displayflip-monitor-layout
**Store**: `%LocalAppData%\DisplayFlip\profiles.json`

## File schema

```json
{
  "schemaVersion": 1,
  "profiles": [
    {
      "name": "Primary Kanan",
      "primaryStableKey": "PCI\\VEN_10DE&DEV_2F04&SUBSYS_...&REV_A1\\4&...#DELF06B#0#...",
      "monitors": [
        {
          "stableKey": "PCI\\VEN_10DE...#DELF06B#0#...",
          "friendlyName": "DELL P2419H",
          "position": { "x": 0, "y": 0, "width": 1920, "height": 1080 },
          "resolution": { "width": 1920, "height": 1080 },
          "orientation": "Landscape",
          "isEnabled": true
        }
      ],
      "createdAt": "2026-11-12T23:20:00Z",
      "updatedAt": "2026-11-12T23:20:00Z"
    }
  ]
}
```

## Rules (dari data-model.md)

- `schemaVersion` wajib; reader menolak versi > yang didukung.
- `name` unik case-insensitive; `primaryStableKey` harus ada di `monitors` profil yang sama.
- Tidak ada `adapterId` / ordinal DISPLAY tersimpan — hanya StableKey (D2).
- File ditulis atomik (tulis ke `profiles.json.tmp` lalu rename). File tidak ada = store kosong.

## Access API (untuk DisplayFlip.Windows)

```csharp
ProfileStore Load();                        // toleran file missing / corrupt (error dengan pesan)
void Save(LayoutProfile profile);           // upsert by name, tulis atomik
List<string> ListNames();
LayoutProfile? Find(string name);          // case-insensitive
void Delete(string name);
```

Upsert/Delete memperbarui `UpdatedAt` dan mempertahankan `CreatedAt` bila overwrite.
