using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DisplayFlip.UI.Helpers;

/// <summary>
/// Helper untuk pemuatan ikon multi-resolusi DisplayFlip beresolusi tinggi,
/// mencegah ikon buram atau pixelated di seluruh tingkatan DPI dan dimensi tampilan.
/// </summary>
public static class IconHelper
{
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
    /// Sangat cocok untuk tampilan besar seperti halaman Tentang atau Taskbar preview.
    /// </summary>
    public static BitmapSource GetHighResIconFrame()
    {
        return GetBestIconFrame(256);
    }

    /// <summary>
    /// Mengambil frame terbaik dari file .ico aplikasi yang mendekati atau sama dengan
    /// ukuran pixel target, lalu siap di-render dengan HighQuality bicubic filtering.
    /// </summary>
    public static BitmapSource GetBestIconFrame(int requestedPixelSize)
    {
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
            // Fallback default
        }

        EnsurePackScheme();
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri("pack://application:,,,/DisplayFlip.ico", UriKind.Absolute);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    /// <summary>
    /// Membuat handle HICON native dari resource .ico untuk ukuran tertentu (misal 16x16 tray icon).
    /// </summary>
    public static IntPtr CreateNativeIconHandle(int preferredSize = 16)
    {
        try
        {
            using var stream = OpenIconStream();
            if (stream != null)
            {
                using var ico = new System.Drawing.Icon(stream, preferredSize, preferredSize);
                return ico.Handle;
            }
        }
        catch
        {
            // Fallback ke system application icon
        }

        return System.Drawing.SystemIcons.Application.Handle;
    }
}
