using System.Drawing;
using System.Drawing.Text;

namespace DisplayFlip;

/// <summary>
/// Ikon glyph Segoe Fluent Icons (fallback Segoe MDL2 Assets), digambar per ukuran
/// 16/20/24px - tidak di-upscale dari bitmap. Kode sudah diverifikasi ke font
/// terpasang (lihat docs/review/glyph-sheet.png).
/// </summary>
public static class FluentIcons
{
    public const char Swap = '\uE8AB';          // Tukar kiri-kanan
    public const char Save = '\uE74E';          // Simpan
    public const char Play = '\uE768';          // Terapkan profil
    public const char Close = '\uE8BB';         // Keluar
    public const char ChevronRight = '\uE76C';  // Submenu
    public const char Check = '\uE73E';         // Centang checkbox
    public const char StarOutline = '\uE734';   // Jadikan primary
    public const char StarSolid = '\uE735';     // Badge primary
    public const char Profile = '\uE77B';       // Nav Profil (Fase B)
    public const char Settings = '\uE713';      // Nav Umum (Fase B)
    public const char Info = '\uE946';          // Nav Tentang (Fase B)
    public const char Add = '\uE710';           // Tambah (Fase B)
    public const char More = '\uE712';          // Opsi lain (Fase B)
    public const char Warning = '\uE7BA';       // Peringatan (Fase C)

    private static FontFamily? s_fluent;
    private static FontFamily? s_mdl2;
    private static readonly Dictionary<(char, int, int), Bitmap> s_cache = new();

    private static FontFamily FluentFamily()
    {
        if (s_fluent == null)
        {
            foreach (var f in FontFamily.Families)
            {
                if (f.Name == "Segoe Fluent Icons")
                    s_fluent = f;
                else if (f.Name == "Segoe MDL2 Assets")
                    s_mdl2 ??= f;
            }
        }
        return s_fluent ?? s_mdl2
            ?? throw new InvalidOperationException("Tidak ada font ikon Segoe terpasang.");
    }

    private static FontFamily Mdl2Family() => s_mdl2 ?? FluentFamily();

    /// <summary>Gambar satu glyph terpusat dalam bitmap sizexsize.</summary>
    public static Bitmap Render(char glyph, int size, Color color, bool preferMdl2 = false)
    {
        int argb = color.ToArgb();
        var key = (glyph, size, argb);
        lock (s_cache)
        {
            if (s_cache.TryGetValue(key, out var hit))
                return hit;
        }
        var family = preferMdl2 ? Mdl2Family() : FluentFamily();
        // Ukuran em ~80% kotak agar glyph 16px tidak terpotong.
        using var font = new Font(family, size * 0.8f, FontStyle.Regular, GraphicsUnit.Pixel);
        var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            var sf = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var brush = new SolidBrush(color);
            g.DrawString(glyph.ToString(), font, brush,
                new RectangleF(0, 0, size, size), sf);
        }
        lock (s_cache)
        {
            s_cache[key] = bmp;
        }
        return bmp;
    }
}
