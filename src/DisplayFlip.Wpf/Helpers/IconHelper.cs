using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DisplayFlip.UI.Helpers;

/// <summary>
/// Helper untuk pemuatan ikon multi-resolusi DisplayFlip beresolusi tinggi,
/// mencegah ikon buram atau pixelated di seluruh tingkatan DPI dan dimensi tampilan.
/// Mendukung rendering vektor GDI+ dinamis untuk gaya fluent, accent, dan minimal.
/// </summary>
public static class IconHelper
{
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    static IconHelper()
    {
        EnsurePackScheme();
    }

    private static void EnsurePackScheme()
    {
        try
        {
            if (!UriParser.IsKnownScheme("pack"))
            {
                _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            }
        }
        catch
        {
            // Abaikan jika sudah teregistrasi
        }
    }

    /// <summary>
    /// Membuka stream file .ico aplikasi baik dari resource WPF maupun filesystem.
    /// </summary>
    public static Stream? OpenIconStream()
    {
        EnsurePackScheme();

        // 1. Coba dari Application Resource Stream
        try
        {
            var uri = new Uri("pack://application:,,,/DisplayFlip.ico", UriKind.Absolute);
            var sri = Application.GetResourceStream(uri);
            if (sri != null)
                return sri.Stream;
        }
        catch
        {
            // Fall through ke alternatif
        }

        // 2. Coba dari pack URI dengan nama assembly eksplisit
        try
        {
            var uri = new Uri("pack://application:,,,/DisplayFlip.Wpf;component/DisplayFlip.ico", UriKind.Absolute);
            var sri = Application.GetResourceStream(uri);
            if (sri != null)
                return sri.Stream;
        }
        catch
        {
            // Fall through ke filesystem
        }

        // 3. Coba dari direktori aplikasi atau direktori kerja
        try
        {
            string[] probePaths =
            [
                Path.Combine(AppContext.BaseDirectory, "DisplayFlip.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DisplayFlip.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DisplayFlip.Wpf", "DisplayFlip.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DisplayFlip", "DisplayFlip.ico"),
                "DisplayFlip.ico"
            ];

            foreach (var p in probePaths)
            {
                var full = Path.GetFullPath(p);
                if (File.Exists(full))
                {
                    return File.OpenRead(full);
                }
            }
        }
        catch
        {
            // Fall through
        }

        return null;
    }

    /// <summary>
    /// Mengambil frame resolusi tertinggi (mis. 256x256) dari file .ico aplikasi.
    /// </summary>
    public static BitmapSource GetHighResIconFrame()
    {
        return GetBestIconFrame(256);
    }

    /// <summary>
    /// Mengambil frame terbaik dari file .ico aplikasi atau me-render langsung
    /// secara presisi pada ukuran pixel yang diminta (anti-buram di semua DPI).
    /// </summary>
    public static BitmapSource GetBestIconFrame(int requestedPixelSize, string style = "fluent")
    {
        if (!string.Equals(style, "fluent", StringComparison.OrdinalIgnoreCase))
        {
            return GetIconStylePreview(style, requestedPixelSize);
        }

        try
        {
            using var stream = OpenIconStream();
            if (stream != null)
            {
                var decoder = new IconBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                if (decoder.Frames.Count > 0)
                {
                    // Cari frame dengan ukuran persis jika ada
                    var exact = decoder.Frames.FirstOrDefault(f => f.PixelWidth == requestedPixelSize);
                    if (exact != null)
                        return exact;

                    // Cari frame terkecil yang >= requestedPixelSize
                    var candidate = decoder.Frames
                        .Where(f => f.PixelWidth >= requestedPixelSize)
                        .OrderBy(f => f.PixelWidth)
                        .FirstOrDefault();

                    if (candidate != null)
                        return candidate;

                    // Bila semua frame < requestedPixelSize, pilih frame terbesar
                    return decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
                }
            }
        }
        catch
        {
            // Fallback ke generator vektor jika decode .ico gagal
        }

        return GetIconStylePreview("fluent", requestedPixelSize);
    }

    /// <summary>
    /// Menghasilkan pratinjau BitmapSource tajam dari gaya ikon yang dipilih
    /// (fluent, accent, minimal) pada ukuran pixel yang diminta.
    /// </summary>
    public static BitmapSource GetIconStylePreview(string style, int size, System.Drawing.Color? customAccent = null)
    {
        int targetSize = Math.Max(16, size);
        using var bmp = RenderAppBitmap(style, targetSize, customAccent);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = ms;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }

    /// <summary>
    /// Me-render bitmap aplikasi DisplayFlip modern secara presisi berbasis vektor GDI+.
    /// </summary>
    public static Bitmap RenderAppBitmap(string style, int size, System.Drawing.Color? customAccent = null)
    {
        int sz = Math.Max(16, size);
        var accent = customAccent ?? System.Drawing.Color.FromArgb(0x00, 0x67, 0xB8);
        var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(System.Drawing.Color.Transparent);

        float s = sz / 64f;
        string mode = (style ?? "fluent").ToLowerInvariant();

        // 1. Latar squircle modern
        var bgRect = new RectangleF(2 * s, 2 * s, 60 * s, 60 * s);
        float bgRad = Math.Max(2f, 14f * s);
        using (var bgPath = RoundedRectF(bgRect, bgRad))
        {
            System.Drawing.Color c1 = mode == "minimal"
                ? System.Drawing.Color.FromArgb(0x18, 0x1A, 0x1F)
                : System.Drawing.Color.FromArgb(0x14, 0x18, 0x24);
            System.Drawing.Color c2 = mode == "minimal"
                ? System.Drawing.Color.FromArgb(0x26, 0x2A, 0x32)
                : System.Drawing.Color.FromArgb(0x22, 0x2B, 0x3C);
            using (var bgBrush = new LinearGradientBrush(bgRect, c1, c2, 45f))
                g.FillPath(bgBrush, bgPath);

            System.Drawing.Color borderCol = mode == "accent"
                ? System.Drawing.Color.FromArgb(140, accent)
                : System.Drawing.Color.FromArgb(90, 0x94, 0xA3, 0xB8);
            using (var borderPen = new Pen(borderCol, Math.Max(1f, 1.4f * s)))
                g.DrawPath(borderPen, bgPath);
        }

        // 2. Layar belakang (Monitor 2 - kanan atas)
        var backMon = new RectangleF(24 * s, 11 * s, 29 * s, 20 * s);
        float monRad = Math.Max(1.5f, 4f * s);
        using (var backPath = RoundedRectF(backMon, monRad))
        {
            System.Drawing.Color backFill = mode switch
            {
                "minimal" => System.Drawing.Color.FromArgb(0x38, 0x3E, 0x4A),
                "accent" => System.Drawing.Color.FromArgb(0x32, 0x36, 0x44),
                _ => System.Drawing.Color.FromArgb(0x1E, 0x3A, 0x5F)
            };
            using (var b = new SolidBrush(backFill))
                g.FillPath(b, backPath);

            System.Drawing.Color backStroke = mode switch
            {
                "minimal" => System.Drawing.Color.FromArgb(0x94, 0xA3, 0xB8),
                "accent" => System.Drawing.Color.FromArgb(180, 0xCB, 0xD5, 0xE1),
                _ => System.Drawing.Color.FromArgb(0x38, 0xBD, 0xF8)
            };
            using (var p = new Pen(backStroke, Math.Max(1f, 1.5f * s)))
                g.DrawPath(p, backPath);
        }

        // 3. Layar depan utama (Monitor 1 - kiri bawah)
        var frontMon = new RectangleF(10 * s, 23 * s, 31 * s, 21 * s);
        using (var frontPath = RoundedRectF(frontMon, monRad))
        {
            System.Drawing.Color f1, f2;
            if (mode == "accent")
            {
                f1 = LightenColor(accent, 0.15f);
                f2 = accent;
            }
            else if (mode == "minimal")
            {
                f1 = System.Drawing.Color.FromArgb(0xF8, 0xFA, 0xFC);
                f2 = System.Drawing.Color.FromArgb(0xE2, 0xE8, 0xF0);
            }
            else
            {
                f1 = System.Drawing.Color.FromArgb(0x38, 0xBD, 0xF8);
                f2 = System.Drawing.Color.FromArgb(0x02, 0x84, 0xC7);
            }
            using (var fb = new LinearGradientBrush(frontMon, f1, f2, 35f))
                g.FillPath(fb, frontPath);

            System.Drawing.Color frontBorder = mode == "minimal"
                ? System.Drawing.Color.White
                : System.Drawing.Color.FromArgb(220, 255, 255, 255);
            using (var fp = new Pen(frontBorder, Math.Max(1f, 1.4f * s)))
                g.DrawPath(fp, frontPath);
        }

        // 4. Kaki penyangga monitor depan (stand)
        if (sz >= 20)
        {
            using var standPen = new Pen(System.Drawing.Color.FromArgb(200, 0xE2, 0xE8, 0xF0), Math.Max(1f, 2f * s))
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

        // 5. Lencana panah flip modern di kanan-bawah
        var badgeRect = new RectangleF(35 * s, 34 * s, 19 * s, 19 * s);
        using (var badgePath = new GraphicsPath())
        {
            badgePath.AddEllipse(badgeRect);
            System.Drawing.Color badgeFill = mode == "accent"
                ? System.Drawing.Color.FromArgb(0x18, 0x18, 0x20)
                : (mode == "minimal" ? System.Drawing.Color.FromArgb(0x0F, 0x17, 0x2A) : System.Drawing.Color.FromArgb(0xF5, 0x9E, 0x0B));
            using (var bb = new SolidBrush(badgeFill))
                g.FillPath(bb, badgePath);

            System.Drawing.Color badgeRing = mode == "accent" ? accent : System.Drawing.Color.White;
            using (var bp = new Pen(badgeRing, Math.Max(1f, 1.5f * s)))
                g.DrawPath(bp, badgePath);
        }

        // 6. Gambar panah tukar kiri-kanan di dalam lencana
        System.Drawing.Color arrowCol = mode == "fluent" ? System.Drawing.Color.FromArgb(0x11, 0x18, 0x27) : System.Drawing.Color.White;
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
    /// Membuat handle HICON native yang persisten dan valid untuk ukuran DPI tertentu.
    /// Handle yang dikembalikan HARUS dibebaskan dengan DestroyNativeIcon saat selesai.
    /// </summary>
    public static IntPtr CreateNativeIconHandle(int preferredSize = 0, string style = "fluent")
    {
        int size = preferredSize;
        if (size <= 0)
        {
            try
            {
                size = GetSystemMetrics(SM_CXSMICON);
            }
            catch
            {
                size = 16;
            }
        }

        if (size <= 0)
            size = 16;

        try
        {
            using var bmp = RenderAppBitmap(style, size);
            IntPtr hIcon = bmp.GetHicon();
            if (hIcon != IntPtr.Zero)
                return hIcon;
        }
        catch
        {
            // Fallback ke system icon jika GDI+ gagal
        }

        return System.Drawing.SystemIcons.Application.Handle;
    }

    /// <summary>
    /// Membebaskan handle HICON native yang dialokasikan oleh CreateNativeIconHandle.
    /// </summary>
    public static void DestroyNativeIcon(IntPtr hIcon)
    {
        if (hIcon != IntPtr.Zero && hIcon != System.Drawing.SystemIcons.Application.Handle)
        {
            try
            {
                DestroyIcon(hIcon);
            }
            catch
            {
                // Abaikan kesalahan deallocasi
            }
        }
    }

    private static GraphicsPath RoundedRectF(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2f;
        if (d > rect.Width) d = rect.Width;
        if (d > rect.Height) d = rect.Height;

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static System.Drawing.Color LightenColor(System.Drawing.Color color, float percentage)
    {
        int r = (int)(color.R + (255 - color.R) * percentage);
        int g = (int)(color.G + (255 - color.G) * percentage);
        int b = (int)(color.B + (255 - color.B) * percentage);
        return System.Drawing.Color.FromArgb(color.A, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }
}
