using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DisplayFlip;

/// <summary>
/// ContextMenuStrip ala Fluent: sudut 8px, bayangan sistem, item digambar kustom
/// via renderer (hover/press/separator radius 4/8, ikon 16px, baris monitor 44px).
/// Acrylic dicoba sekali via SetWindowCompositionAttribute; bila gagal atau
/// tidak stabil, fallback ke permukaan solid + shadow (jawaban owner 12).
/// </summary>
public sealed class FluentMenu : ContextMenuStrip
{
    public FluentMenu(int width)
    {
        Padding = new Padding(FluentTheme.MenuPadding);
        ShowImageMargin = false;
        ShowCheckMargin = false;
        Renderer = new FluentStripRenderer();
        BackColor = FluentTheme.Current.Surface;
        // Paksa lebar jendela: auto-size dropdown WinForms mengabaikan Size item
        // (terbukti: item 248px tapi jendela 118px). Tinggi tetap otomatis.
        MinimumSize = new Size(width, 0);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        // Samakan warna backing dengan tema aktif agar tepi Region tidak berbingkai hitam.
        BackColor = FluentTheme.Current.Surface;
        base.OnVisibleChanged(e);
    }

    public void AddItem(FluentMenuItem item) => Items.Add(item);

    public void ClearItems()
    {
        while (Items.Count > 0)
        {
            var item = Items[0];
            Items.RemoveAt(0);
            item.Dispose();
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int CS_DROPSHADOW = 0x00020000;
            var cp = base.CreateParams;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }

    private bool _useFallbackRegion;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Windows 11: biarkan DWM memotong sudut + menggambar bayangan secara native.
        // Fallback Region hanya bila atribut DWM tidak didukung (pra-Win11).
        if (!DwmRoundCorners(Handle))
        {
            _useFallbackRegion = true;
            UpdateRegion();
        }
        // Acrylic sudah dicoba satu kali via Acrylic.TryEnable: atribut terpasang
        // tanpa error tetapi tidak ada efek blur yang terlihat pada dropdown
        // non-layered, dan WS_EX_LAYERED berisiko mengulang ketidakstabilan menu.
        // Keputusan: tetap permukaan solid + shadow (jawaban owner 12).
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        // Ukuran dropdown final setelah layout; Region pralayout memotong submenu.
        if (_useFallbackRegion)
            UpdateRegion();
    }

    protected override void OnClosed(ToolStripDropDownClosedEventArgs e)
    {
        Region?.Dispose();
        Region = null;
        base.OnClosed(e);
    }

    private void UpdateRegion()
    {
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;
        Region?.Dispose();
        using var path = RoundedRect(ClientRectangle, FluentTheme.RadiusMenu);
        Region = new Region(path);
    }

    internal static bool DwmRoundCorners(IntPtr hwnd)
    {
        try
        {
            int preference = 2; // DWMWCP_ROUND
            int hr = DwmSetWindowAttribute(hwnd, 33, ref preference, sizeof(int)); // DWMWA_WINDOW_CORNER_PREFERENCE
            return hr == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    internal static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var p = new GraphicsPath();
        int d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

internal static class Acrylic
{
    private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    /// <summary>
    /// Coba aktifkan acrylic; kembalikan false bila tidak didukung/gagal.
    /// Tidak melempar - menu tetap jalan solid.
    /// </summary>
    public static bool TryEnable(IntPtr hwnd)
    {
        try
        {
            var policy = new AccentPolicy
            {
                AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 0,
                GradientColor = unchecked((int)0x99000000),
                AnimationId = 0
            };
            int size = Marshal.SizeOf(policy);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, ptr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = 19, // WCA_ACCENT_POLICY
                    Data = ptr,
                    SizeOfData = size
                };
                return SetWindowCompositionAttribute(hwnd, ref data) != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Item menu Fluent: baris biasa (ikon + label), checkbox, atau radio monitor
/// (2 baris + badge primary). Digambar oleh <see cref="FluentStripRenderer"/>.
/// </summary>
public sealed class FluentMenuItem : ToolStripMenuItem
{
    public char? Glyph { get; set; }
    public string? SecondaryLine { get; set; }
    public string? ShortcutHint { get; set; }
    public bool CheckBox { get; set; }
    public bool Radio { get; set; }
    public bool RadioChecked { get; set; }
    public bool BadgePrimary { get; set; }
    internal bool IsMouseDown { get; private set; }

    public FluentMenuItem(string text, int height)
    {
        Text = text;
        AutoSize = false;
        Height = height;
    }

    public FluentMenuItem(string text) : this(text, FluentTheme.ItemHeight)
    {
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled)
        {
            IsMouseDown = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (IsMouseDown)
        {
            IsMouseDown = false;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (IsMouseDown)
        {
            IsMouseDown = false;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }
}

internal sealed class FluentStripRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var pal = FluentTheme.Current;
        using var brush = new SolidBrush(pal.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // Sudut ditangani Region; tanpa border agar tidak ada garis keras.
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var pal = FluentTheme.Current;
        var r = e.Item.Bounds;
        int y = r.Y + r.Height / 2;
        using var pen = new Pen(pal.Divider, 1);
        e.Graphics.DrawLine(pen, r.X + 2, y, r.Right - 2, y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        // Chevron digambar sendiri oleh OnRenderItemText (glyph E76C).
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // Checkbox digambar sendiri oleh OnRenderItemText.
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var pal = FluentTheme.Current;
        var r = new Rectangle(Point.Empty, e.Item.Size);
        if (e.Item.Selected && e.Item.Enabled)
        {
            var fill = IsPressed(e.Item) ? pal.Press : pal.Hover;
            using var path = FluentMenu.RoundedRect(r, FluentTheme.RadiusItem);
            using var brush = new SolidBrush(fill);
            e.Graphics.FillPath(brush, path);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item is not FluentMenuItem item)
        {
            base.OnRenderItemText(e);
            return;
        }
        var pal = FluentTheme.Current;
        var g = e.Graphics;
        var bounds = new Rectangle(Point.Empty, item.Size);
        Color fg = item.Enabled ? pal.Text : pal.TextSecondary;
        int x = bounds.X + 6;
        int cy = bounds.Y + bounds.Height / 2;

        if (item.CheckBox)
        {
            var box = new Rectangle(x, cy - 8, 16, 16);
            if (item.Checked)
            {
                using var path = FluentMenu.RoundedRect(box, 4);
                using var brush = new SolidBrush(pal.Accent);
                g.FillPath(brush, path);
                Color checkFg = pal.Accent.GetBrightness() > 0.55f ? Color.Black : Color.White;
                g.DrawImage(FluentIcons.Render(FluentIcons.Check, 10, checkFg),
                    box.X + 3, box.Y + 3);
            }
            else
            {
                using var path = FluentMenu.RoundedRect(box, 4);
                using var pen = new Pen(pal.TextSecondary, 1);
                g.DrawPath(pen, path);
            }
            x += 16 + 8;
            DrawSingleLine(g, item.Text ?? string.Empty, FluentTheme.BodyFont, fg,
                new Rectangle(x, bounds.Y, bounds.Right - 6 - x, bounds.Height));
            return;
        }

        if (item.Radio)
        {
            var circle = new Rectangle(x, cy - 8, 16, 16);
            using (var pen = new Pen(pal.RadioBorder, 1))
                g.DrawEllipse(pen, circle);
            if (item.RadioChecked)
            {
                using var brush = new SolidBrush(pal.Accent);
                g.FillEllipse(brush, circle.X + 4, circle.Y + 4, 8, 8);
            }
            x += 16 + 8;
            TextRenderer.DrawText(g, item.Text, FluentTheme.BodyFont,
                new Point(x, bounds.Y + 5), fg,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, item.SecondaryLine ?? string.Empty, FluentTheme.CaptionFont,
                new Point(x, bounds.Y + 22), pal.TextSecondary,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            if (item.BadgePrimary)
            {
                const string badge = "primary";
                var badgeSize = TextRenderer.MeasureText(g, badge, FluentTheme.CaptionFont,
                    new Size(0, 0), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                int bx = bounds.Right - 6 - badgeSize.Width;
                g.DrawImage(FluentIcons.Render(FluentIcons.StarSolid, 11, pal.BadgeAccent),
                    bx - 15, cy - 6);
                TextRenderer.DrawText(g, badge, FluentTheme.CaptionFont,
                    new Point(bx, cy - 7), pal.TextSecondary,
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
            return;
        }

        if (item.Glyph is char glyph)
        {
            g.DrawImage(FluentIcons.Render(glyph, 16, fg), x, cy - 8);
            x += 16 + 8;
        }
        int right = bounds.Right - 6;
        if (item.HasDropDownItems)
            right -= FluentTheme.SmallIconSize + 4;
        if (!string.IsNullOrWhiteSpace(item.ShortcutHint) && !item.HasDropDownItems)
        {
            var hintSize = TextRenderer.MeasureText(g, item.ShortcutHint, FluentTheme.CaptionFont,
                new Size(120, bounds.Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            int hx = right - hintSize.Width;
            TextRenderer.DrawText(g, item.ShortcutHint, FluentTheme.CaptionFont,
                new Rectangle(hx, bounds.Y, hintSize.Width, bounds.Height),
                pal.TextSecondary, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPrefix);
            right = hx - 8;
        }
        if (!string.IsNullOrEmpty(item.SecondaryLine))
        {
            // Baris dua teks (mis. subteks "Perlu 2+ monitor"): label di atas, subteks di bawah.
            TextRenderer.DrawText(g, item.Text ?? string.Empty, FluentTheme.BodyFont,
                new Point(x, bounds.Y + 4), fg,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, item.SecondaryLine, FluentTheme.CaptionFont,
                new Rectangle(x, bounds.Y + 22, right - x, bounds.Height - 22),
                pal.TextSecondary, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
        else
        {
            DrawSingleLine(g, item.Text ?? string.Empty, FluentTheme.BodyFont, fg,
                new Rectangle(x, bounds.Y, right - x, bounds.Height));
        }
        if (item.HasDropDownItems)
        {
            g.DrawImage(FluentIcons.Render(FluentIcons.ChevronRight,
                FluentTheme.SmallIconSize, pal.TextSecondary),
                bounds.Right - 6 - FluentTheme.SmallIconSize, cy - FluentTheme.SmallIconSize / 2);
        }
    }

    private static void DrawSingleLine(Graphics g, string text, Font font, Color color, Rectangle rect)
    {
        TextRenderer.DrawText(g, text, font, rect, color,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
    }

    private static bool IsPressed(ToolStripItem item)
    {
        return item is FluentMenuItem fi && fi.IsMouseDown;
    }
}
