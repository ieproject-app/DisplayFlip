using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DisplayFlip;

/// <summary>
/// Ikon glyph Segoe Fluent Icons (fallback Segoe MDL2 Assets), digambar per ukuran
/// 16/20/24px - tidak di-upscale dari bitmap. Plus generator ikon modern multi-monitor.
/// </summary>
public static class FluentIcons
{
    public const char Swap = '\uE8AB';          // Tukar kiri-kanan
    public const char Save = '\uE74E';          // Simpan
    public const char Play = '\uE768';          // Terapkan profil
    public const char Close = '\uE8BB';         // Keluar
    public const char ChevronRight = '\uE76C';  // Submenu
    public const char ChevronDown = '\uE70D';   // Dropdown
    public const char Check = '\uE73E';         // Centang checkbox
    public const char StarOutline = '\uE734';   // Jadikan primary
    public const char StarSolid = '\uE735';     // Badge primary
    public const char Profile = '\uE77B';       // Ikon profil user lama
    public const char Display = '\uE7F4';       // Monitor / Layar
    public const char DualDisplay = '\uE7F8';   // Multi-monitor
    public const char Project = '\uE7F8';       // Proyeksi / Multi-layar
    public const char Settings = '\uE713';      // Nav Umum
    public const char Info = '\uE946';          // Nav Tentang
    public const char Add = '\uE710';           // Tambah
    public const char More = '\uE712';          // Opsi lain
    public const char Warning = '\uE7BA';       // Peringatan
    public const char Keyboard = '\uE765';      // Pintasan keyboard
    public const char Identify = '\uE7B3';      // Identifikasi layar
    public const char Edit = '\uE70F';          // Edit profil / susunan
    public const char Refresh = '\uE72C';       // Segarkan
    public const char Rotate = '\uE7AD';        // Orientasi / putar
    public const char Align = '\uE8E4';         // Perataan vertikal
    public const char Undo = '\uE7A7';          // Reset susunan
    public const char Sparkle = '\uE790';       // Tema / gaya ikon
    public const char Delete = '\uE74D';        // Hapus
    public const char Move = '\uE759';          // Pindah posisi
    public const char Scale = '\uE740';         // Skala / resolusi

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
            using var sf = new StringFormat(StringFormat.GenericTypographic)
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

    /// <summary>
    /// Gambar bitmap ikon aplikasi modern (fluent / accent / minimal) pada ukuran tertentu.
    /// </summary>
    public static Bitmap RenderModernAppBitmap(string style, int size, Color? customAccent = null)
    {
        var accent = customAccent ?? FluentTheme.Current.Accent;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        float s = size / 64f;
        string mode = (style ?? "fluent").ToLowerInvariant();

        // Latar squircle modern
        var bgRect = new RectangleF(2 * s, 2 * s, 60 * s, 60 * s);
        float bgRad = Math.Max(2f, 14f * s);
        using (var bgPath = RoundedRectF(bgRect, bgRad))
        {
            Color c1 = mode == "minimal"
                ? Color.FromArgb(0x18, 0x1A, 0x1F)
                : Color.FromArgb(0x14, 0x18, 0x24);
            Color c2 = mode == "minimal"
                ? Color.FromArgb(0x26, 0x2A, 0x32)
                : Color.FromArgb(0x22, 0x2B, 0x3C);
            using (var bgBrush = new LinearGradientBrush(bgRect, c1, c2, 45f))
                g.FillPath(bgBrush, bgPath);

            Color borderCol = mode == "accent"
                ? Color.FromArgb(140, accent)
                : Color.FromArgb(90, 0x94, 0xA3, 0xB8);
            using (var borderPen = new Pen(borderCol, Math.Max(1f, 1.4f * s)))
                g.DrawPath(borderPen, bgPath);
        }

        // Layar belakang (Monitor 2 - kanan atas)
        var backMon = new RectangleF(24 * s, 11 * s, 29 * s, 20 * s);
        float monRad = Math.Max(1.5f, 4f * s);
        using (var backPath = RoundedRectF(backMon, monRad))
        {
            Color backFill = mode switch
            {
                "minimal" => Color.FromArgb(0x38, 0x3E, 0x4A),
                "accent" => Color.FromArgb(0x32, 0x36, 0x44),
                _ => Color.FromArgb(0x1E, 0x3A, 0x5F)
            };
            using (var b = new SolidBrush(backFill))
                g.FillPath(b, backPath);

            Color backStroke = mode switch
            {
                "minimal" => Color.FromArgb(0x94, 0xA3, 0xB8),
                "accent" => Color.FromArgb(180, 0xCB, 0xD5, 0xE1),
                _ => Color.FromArgb(0x38, 0xBD, 0xF8)
            };
            using (var p = new Pen(backStroke, Math.Max(1f, 1.5f * s)))
                g.DrawPath(p, backPath);
        }

        // Layar depan utama (Monitor 1 - kiri bawah)
        var frontMon = new RectangleF(10 * s, 23 * s, 31 * s, 21 * s);
        using (var frontPath = RoundedRectF(frontMon, monRad))
        {
            Color f1, f2;
            if (mode == "accent")
            {
                f1 = ControlPaint.Light(accent, 0.15f);
                f2 = accent;
            }
            else if (mode == "minimal")
            {
                f1 = Color.FromArgb(0xF8, 0xFA, 0xFC);
                f2 = Color.FromArgb(0xE2, 0xE8, 0xF0);
            }
            else
            {
                f1 = Color.FromArgb(0x38, 0xBD, 0xF8);
                f2 = Color.FromArgb(0x02, 0x84, 0xC7);
            }
            using (var fb = new LinearGradientBrush(frontMon, f1, f2, 35f))
                g.FillPath(fb, frontPath);

            Color frontBorder = mode == "minimal"
                ? Color.White
                : Color.FromArgb(220, 255, 255, 255);
            using (var fp = new Pen(frontBorder, Math.Max(1f, 1.4f * s)))
                g.DrawPath(fp, frontPath);
        }

        // Kaki penyangga monitor depan (stand)
        if (size >= 20)
        {
            using var standPen = new Pen(Color.FromArgb(200, 0xE2, 0xE8, 0xF0), Math.Max(1f, 2f * s))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            float cx = frontMon.X + frontMon.Width / 2f;
            float topY = frontMon.Bottom + 1.5f * s;
            float botY = frontMon.Bottom + 5.5f * s;
            g.DrawLine(standPen, cx, topY, cx, botY);
            g.DrawLine(standPen, cx - 6.5f * s, botY, cx + 6.5f * s, botY);
        }

        // Lencana panah flip modern di kanan-bawah
        var badgeRect = new RectangleF(35 * s, 34 * s, 19 * s, 19 * s);
        using (var badgePath = new GraphicsPath())
        {
            badgePath.AddEllipse(badgeRect);
            Color badgeFill = mode == "accent"
                ? Color.FromArgb(0x18, 0x18, 0x20)
                : (mode == "minimal" ? Color.FromArgb(0x0F, 0x17, 0x2A) : Color.FromArgb(0xF5, 0x9E, 0x0B));
            using (var bb = new SolidBrush(badgeFill))
                g.FillPath(bb, badgePath);

            Color badgeRing = mode == "accent" ? accent : Color.White;
            using (var bp = new Pen(badgeRing, Math.Max(1f, 1.5f * s)))
                g.DrawPath(bp, badgePath);
        }

        // Gambar panah tukar kiri-kanan di dalam lencana
        Color arrowCol = mode == "fluent" ? Color.FromArgb(0x11, 0x18, 0x27) : Color.White;
        using (var arrowPen = new Pen(arrowCol, Math.Max(1f, 1.8f * s))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        })
        {
            float bx = badgeRect.X + badgeRect.Width / 2f;
            float by = badgeRect.Y + badgeRect.Height / 2f;
            float dx = 4.2f * s;
            float dy = 2.4f * s;
            // Panah kanan (atas)
            g.DrawLine(arrowPen, bx - dx, by - dy, bx + dx, by - dy);
            g.DrawLine(arrowPen, bx + dx - 2.2f * s, by - dy - 2f * s, bx + dx, by - dy);
            // Panah kiri (bawah)
            g.DrawLine(arrowPen, bx + dx, by + dy, bx - dx, by + dy);
            g.DrawLine(arrowPen, bx - dx + 2.2f * s, by + dy + 2f * s, bx - dx, by + dy);
        }

        return bmp;
    }

    /// <summary>
    /// Bangun objek Icon dari gaya modern yang dipilih.
    /// </summary>
    public static Icon CreateModernAppIcon(string style, int size = 32, Color? customAccent = null)
    {
        var bytes = BuildMultiSizeIcoBytes(style, customAccent);
        using var ms = new MemoryStream(bytes);
        return new Icon(ms, size, size);
    }

    /// <summary>
    /// Bangun file .ICO multi-size (256, 128, 64, 48, 32, 24, 20, 16) berformat PNG-in-ICO.
    /// Ukuran terbesar (256px) ditaruh di awal agar decoder default (misal WPF BitmapDecoder)
    /// selalu memilih resolusi tertinggi untuk downscaling tajam & anti-buram.
    /// </summary>
    public static byte[] BuildMultiSizeIcoBytes(string style, Color? customAccent = null)
    {
        int[] sizes = { 256, 128, 64, 48, 32, 24, 20, 16 };
        var pngBlobs = new List<byte[]>(sizes.Length);
        foreach (int sz in sizes)
        {
            using var bmp = RenderModernAppBitmap(style, sz, customAccent);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            pngBlobs.Add(ms.ToArray());
        }

        using var outStream = new MemoryStream();
        using var bw = new BinaryWriter(outStream);
        bw.Write((ushort)0); // Reserved
        bw.Write((ushort)1); // Type = 1 (ICO)
        bw.Write((ushort)sizes.Length);

        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            byte dim = sz >= 256 ? (byte)0 : (byte)sz;
            int len = pngBlobs[i].Length;
            bw.Write(dim);       // Width
            bw.Write(dim);       // Height
            bw.Write((byte)0);   // ColorCount
            bw.Write((byte)0);   // Reserved
            bw.Write((ushort)1); // Planes
            bw.Write((ushort)32);// BitCount
            bw.Write(len);       // BytesInRes
            bw.Write(offset);    // ImageOffset
            offset += len;
        }

        for (int i = 0; i < pngBlobs.Count; i++)
            bw.Write(pngBlobs[i]);

        bw.Flush();
        return outStream.ToArray();
    }

    public static void SaveModernIconPreviewSheet(string outputPngPath)
    {
        var styles = new (string Key, string Name, string Sub)[]
        {
            ("fluent", "1. Fluent Glass Dual (Default)", "Cyan-Blue Layered Glass + Gold Flip Badge"),
            ("accent", "2. Windows 11 Dynamic Accent", "Mengikuti Warna Aksen Aktif Sistem Windows 11"),
            ("minimal", "3. Minimalist Precision", "Monokrom Kontras Tinggi untuk Taskbar Bersih")
        };

        using var sheet = new Bitmap(960, 360, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(sheet);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.FromArgb(0x18, 0x1A, 0x20));

        for (int i = 0; i < styles.Length; i++)
        {
            int cardX = 24 + i * 308;
            var cardRect = new RectangleF(cardX, 20, 296, 320);
            using var path = RoundedRectF(cardRect, 12f);
            using (var bg = new SolidBrush(Color.FromArgb(0x24, 0x27, 0x31)))
                g.FillPath(bg, path);
            using (var pen = new Pen(Color.FromArgb(0x38, 0x3E, 0x4C), 1.5f))
                g.DrawPath(pen, path);

            using var bmp160 = RenderModernAppBitmap(styles[i].Key, 160);
            g.DrawImage(bmp160, cardX + 24, 36, 160, 160);

            using var bmp48 = RenderModernAppBitmap(styles[i].Key, 48);
            g.DrawImage(bmp48, cardX + 208, 48, 48, 48);

            using var bmp32 = RenderModernAppBitmap(styles[i].Key, 32);
            g.DrawImage(bmp32, cardX + 216, 114, 32, 32);

            using var bmp16 = RenderModernAppBitmap(styles[i].Key, 16);
            g.DrawImage(bmp16, cardX + 224, 162, 16, 16);

            TextRenderer.DrawText(g, styles[i].Name, FluentTheme.BodySemiboldFont,
                new Rectangle(cardX + 16, 218, 264, 24), Color.White,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, styles[i].Sub, FluentTheme.CaptionFont,
                new Rectangle(cardX + 16, 244, 264, 48), Color.FromArgb(0xC7, 0xCD, 0xD8),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }

        var dir = Path.GetDirectoryName(outputPngPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        sheet.Save(outputPngPath, ImageFormat.Png);
    }

    private static GraphicsPath RoundedRectF(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        float d = Math.Min(Math.Min(r.Width, r.Height), radius * 2f);
        if (d <= 0.5f)
        {
            p.AddRectangle(r);
            return p;
        }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
