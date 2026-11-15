using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DisplayFlip;

internal sealed class FluentDialogButton : Button
{
    public bool IsPrimary { get; set; }
    public char? Glyph { get; set; }
    private bool _hovered;
    private bool _pressed;

    public FluentDialogButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = FluentTheme.BodyFont;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var pal = FluentTheme.Current;
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? pal.Background);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = FluentMenu.RoundedRect(rect, FluentTheme.RadiusItem);

        Color fill;
        Color fg;
        if (!Enabled)
        {
            fill = pal.Divider;
            fg = pal.TextSecondary;
        }
        else if (IsPrimary)
        {
            fill = pal.Accent;
            fg = pal.Accent.GetBrightness() > 0.55f ? Color.Black : Color.White;
        }
        else
        {
            fill = pal.Surface;
            fg = pal.Text;
        }

        using (var bgBrush = new SolidBrush(fill))
            g.FillPath(bgBrush, path);

        if (Enabled && (_pressed || _hovered))
        {
            Color overlay = _pressed
                ? Color.FromArgb(36, IsPrimary ? Color.Black : pal.Text)
                : Color.FromArgb(18, IsPrimary ? Color.Black : pal.Text);
            using var ovBrush = new SolidBrush(overlay);
            g.FillPath(ovBrush, path);
        }

        if (!IsPrimary)
        {
            Color borderCol = Color.FromArgb(40, pal.Text);
            using var pen = new Pen(borderCol, 1);
            g.DrawPath(pen, path);
        }

        if (Focused && ShowFocusCues)
        {
            var focusRect = new Rectangle(2, 2, Width - 5, Height - 5);
            using var focusPath = FluentMenu.RoundedRect(focusRect, 2);
            using var focusPen = new Pen(IsPrimary ? fg : pal.Accent, 1);
            g.DrawPath(focusPen, focusPath);
        }

        if (Glyph is char gch)
        {
            var sz = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, Height),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            int iconSz = 14;
            int totalW = iconSz + (string.IsNullOrEmpty(Text) ? 0 : 6 + sz.Width);
            int startX = Math.Max(6, (Width - totalW) / 2);
            g.DrawImage(FluentIcons.Render(gch, iconSz, fg), startX, (Height - iconSz) / 2);
            if (!string.IsNullOrEmpty(Text))
            {
                TextRenderer.DrawText(g, Text, Font,
                    new Rectangle(startX + iconSz + 4, 0, Width - (startX + iconSz + 6), Height), fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(4, 0, Width - 8, Height), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }
}

/// <summary>
/// Checkbox kustom bergaya Windows 11 Fluent (tanpa kotak putih WinForms klasik).
/// </summary>
internal sealed class FluentCheckBox : CheckBox
{
    private bool _hovered;
    private bool _pressed;

    public FluentCheckBox()
    {
        Cursor = Cursors.Hand;
        Font = FluentTheme.BodyFont;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var pal = FluentTheme.Current;
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        int boxSize = 18;
        int boxY = (Height - boxSize) / 2;
        var boxRect = new Rectangle(1, boxY, boxSize, boxSize);
        using var boxPath = FluentMenu.RoundedRect(boxRect, 4);

        if (Checked)
        {
            Color fill = Enabled ? pal.Accent : pal.Divider;
            using (var b = new SolidBrush(fill))
                g.FillPath(b, boxPath);
            if (_hovered && Enabled)
            {
                using var ov = new SolidBrush(Color.FromArgb(24, Color.White));
                g.FillPath(ov, boxPath);
            }
            Color checkFg = pal.Accent.GetBrightness() > 0.55f ? Color.Black : Color.White;
            g.DrawImage(FluentIcons.Render(FluentIcons.Check, 11, checkFg), boxRect.X + 4, boxRect.Y + 4);
        }
        else
        {
            Color uncheckedBg = _pressed
                ? pal.Press
                : (_hovered ? pal.Hover : Color.FromArgb(28, pal.Background));
            using (var b = new SolidBrush(uncheckedBg))
                g.FillPath(b, boxPath);
            Color borderCol = _hovered && Enabled ? pal.Text : pal.TextSecondary;
            using (var p = new Pen(borderCol, 1.2f))
                g.DrawPath(p, boxPath);
        }

        Color textFg = Enabled ? ForeColor : pal.TextSecondary;
        int textX = boxRect.Right + 8;
        TextRenderer.DrawText(g, Text, Font,
            new Rectangle(textX, 0, Math.Max(0, Width - textX), Height),
            textFg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// Dropdown select button kustom bergaya Windows 11 Fluent menggunakan <see cref="FluentMenu"/>
/// sehingga bebas dari border/tombol putih bawaan Win32 ComboBox pada Dark Mode.
/// </summary>
internal sealed class FluentSelectButton : Control
{
    private readonly List<string> _items = new();
    private int _selectedIndex = -1;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler? SelectedIndexChanged;

    public IList<string> Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = (value >= 0 && value < _items.Count) ? value : -1;
            if (_selectedIndex != clamped)
            {
                _selectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Invalidate();
            }
        }
    }

    public string? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public FluentSelectButton()
    {
        Cursor = Cursors.Hand;
        Font = FluentTheme.CaptionFont;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
    }

    public void SetItems(IEnumerable<string> items, int selectedIndex = 0)
    {
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = (_items.Count > 0 && selectedIndex >= 0 && selectedIndex < _items.Count)
            ? selectedIndex
            : (_items.Count > 0 ? 0 : -1);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled)
        {
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_pressed)
        {
            _pressed = false;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (!Enabled || _items.Count == 0)
            return;

        int menuWidth = Math.Max(Width, 160);
        var menu = new FluentMenu(menuWidth);
        for (int i = 0; i < _items.Count; i++)
        {
            int capturedIdx = i;
            var item = new FluentMenuItem(_items[i], 30)
            {
                Glyph = i == _selectedIndex ? FluentIcons.Check : null,
                Width = menuWidth - FluentTheme.MenuPadding * 2
            };
            item.Click += (_, _) => SelectedIndex = capturedIdx;
            menu.AddItem(item);
        }
        menu.Closed += (_, _) => BeginInvoke(new Action(menu.Dispose));
        menu.Show(this, new Point(0, Height + 2));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var pal = FluentTheme.Current;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? pal.Surface);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = FluentMenu.RoundedRect(rect, FluentTheme.RadiusItem);

        Color fill = !Enabled
            ? pal.Divider
            : (_pressed ? pal.Press : (_hovered ? pal.Hover : pal.Background));
        using (var bg = new SolidBrush(fill))
            g.FillPath(bg, path);

        Color borderCol = _hovered && Enabled ? pal.Accent : Color.FromArgb(45, pal.Text);
        using (var pen = new Pen(borderCol, 1))
            g.DrawPath(pen, path);

        Color fg = Enabled ? pal.Text : pal.TextSecondary;
        string label = SelectedItem ?? string.Empty;
        TextRenderer.DrawText(g, label, Font,
            new Rectangle(8, 0, Math.Max(0, Width - 28), Height), fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        g.DrawImage(FluentIcons.Render(FluentIcons.ChevronDown, 10, pal.TextSecondary),
            Width - 18, (Height - 10) / 2);
    }
}

public class FluentDialogBase : Form
{
    private bool _useFallbackRegion;

    protected FluentDialogBase()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.None;
        Width = FluentTheme.DialogWidth;
        Font = FluentTheme.BodyFont;
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!FluentMenu.DwmRoundCorners(Handle))
        {
            _useFallbackRegion = true;
            UpdateFallbackRegion();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (_useFallbackRegion)
            UpdateFallbackRegion();
    }

    private void UpdateFallbackRegion()
    {
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;
        Region?.Dispose();
        using var path = FluentMenu.RoundedRect(ClientRectangle, FluentTheme.RadiusMenu);
        Region = new Region(path);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}

/// <summary>
/// Dialog Simpan &amp; Setup Profil (mendukung nama profil, pintasan shortcut custom, dan opsi Primary).
/// </summary>
public sealed class SaveProfileDialog : FluentDialogBase
{
    private readonly HashSet<string> _existing;
    private readonly string? _editingOriginalName;
    private readonly TextBox _input;
    private readonly TextBox _shortcutInput;
    private readonly FluentSelectButton? _primaryCombo;
    private readonly List<DisplayFlip.Core.DisplayMonitor> _monitors;
    private readonly Panel _cautionBar;
    private readonly FluentDialogButton _saveBtn;
    private readonly Panel _bodyPanel;
    private readonly Panel _footerPanel;

    private const int Pad = 24;
    private const int ContentWidth = FluentTheme.DialogWidth - Pad * 2;

    public string ProfileName => _input.Text.Trim();
    public string ShortcutText => _shortcutInput.Text.Trim();
    public string? SelectedPrimaryKey { get; private set; }

    public SaveProfileDialog(
        IEnumerable<string> existingNames,
        string defaultName,
        string initialShortcut = "",
        IReadOnlyList<DisplayFlip.Core.DisplayMonitor>? monitors = null,
        string? initialPrimaryKey = null,
        string? editingOriginalName = null)
    {
        _existing = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        _editingOriginalName = editingOriginalName;
        _monitors = monitors?.ToList() ?? new List<DisplayFlip.Core.DisplayMonitor>();
        var pal = FluentTheme.Current;
        bool isEdit = !string.IsNullOrWhiteSpace(editingOriginalName);
        Text = isEdit ? "DisplayFlip - Edit & Setup Profil" : "DisplayFlip - Simpan & Setup Profil";
        BackColor = pal.Surface;

        _bodyPanel = new Panel
        {
            Location = new Point(0, 0),
            Width = FluentTheme.DialogWidth,
            BackColor = pal.Surface
        };

        var title = new Label
        {
            Text = isEdit ? "Edit & setup profil" : "Simpan & setup profil",
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(Pad, Pad),
            Size = new Size(ContentWidth, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var fieldLabel = new Label
        {
            Text = "Nama profil",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(Pad, Pad + 28 + 14),
            Size = new Size(ContentWidth, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var inputBoxHost = new Panel
        {
            Location = new Point(Pad, fieldLabel.Bottom + 4),
            Size = new Size(ContentWidth, 32),
            BackColor = pal.Surface
        };
        inputBoxHost.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            using var borderPen = new Pen(Color.FromArgb(30, pal.Text), 1);
            g.DrawRectangle(borderPen, 0, 0, inputBoxHost.Width - 1, inputBoxHost.Height - 1);
            using var accentBrush = new SolidBrush(pal.Accent);
            g.FillRectangle(accentBrush, 0, inputBoxHost.Height - 2, inputBoxHost.Width, 2);
        };

        _input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = FluentTheme.BodyFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Text = defaultName,
            MaxLength = 100,
            Location = new Point(10, 8),
            Width = ContentWidth - 20
        };
        _input.TextChanged += (_, _) => UpdateDuplicateState();
        inputBoxHost.Controls.Add(_input);

        int nextTop = inputBoxHost.Bottom + 12;

        if (_monitors.Count > 0)
        {
            var priLabel = new Label
            {
                Text = "Layar utama (Primary) untuk profil ini",
                Font = FluentTheme.BodyFont,
                ForeColor = pal.TextSecondary,
                BackColor = pal.Surface,
                Location = new Point(Pad, nextTop),
                Size = new Size(ContentWidth, 16),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _primaryCombo = new FluentSelectButton
            {
                Font = FluentTheme.BodyFont,
                Location = new Point(Pad, priLabel.Bottom + 4),
                Size = new Size(ContentWidth, 30)
            };
            int selIdx = 0;
            for (int i = 0; i < _monitors.Count; i++)
            {
                var m = _monitors[i];
                string disp = m.FriendlyName.StartsWith("\\\\.\\", StringComparison.Ordinal)
                    ? m.FriendlyName[4..]
                    : m.FriendlyName;
                _primaryCombo.Items.Add($"Layar {i + 1}: {disp} ({m.Position.Width}x{m.Position.Height})");
                if (!string.IsNullOrEmpty(initialPrimaryKey) && m.StableKey == initialPrimaryKey)
                    selIdx = i;
                else if (string.IsNullOrEmpty(initialPrimaryKey) && m.Position.X == 0 && m.Position.Y == 0)
                    selIdx = i;
            }
            _primaryCombo.SelectedIndex = selIdx;
            SelectedPrimaryKey = _monitors[selIdx].StableKey;
            _primaryCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_primaryCombo.SelectedIndex >= 0 && _primaryCombo.SelectedIndex < _monitors.Count)
                    SelectedPrimaryKey = _monitors[_primaryCombo.SelectedIndex].StableKey;
            };
            _bodyPanel.Controls.Add(priLabel);
            _bodyPanel.Controls.Add(_primaryCombo);
            nextTop = _primaryCombo.Bottom + 12;
        }

        var scLabel = new Label
        {
            Text = "Pintasan keyboard (Shortcut custom - klik lalu tekan tombol)",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(Pad, nextTop),
            Size = new Size(ContentWidth, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scBoxHost = new Panel
        {
            Location = new Point(Pad, scLabel.Bottom + 4),
            Size = new Size(ContentWidth - 76, 32),
            BackColor = pal.Surface
        };
        scBoxHost.Paint += (_, pe) =>
        {
            var g = pe.Graphics;
            using var borderPen = new Pen(Color.FromArgb(30, pal.Text), 1);
            g.DrawRectangle(borderPen, 0, 0, scBoxHost.Width - 1, scBoxHost.Height - 1);
        };

        _shortcutInput = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = FluentTheme.MonoFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            ReadOnly = true,
            Text = initialShortcut ?? string.Empty,
            PlaceholderText = "Mis. Ctrl+Alt+1 (opsional)",
            Location = new Point(10, 8),
            Width = scBoxHost.Width - 20
        };
        _shortcutInput.KeyDown += (_, ke) =>
        {
            ke.SuppressKeyPress = true;
            ke.Handled = true;
            if (ke.KeyCode is Keys.Back or Keys.Delete)
            {
                _shortcutInput.Text = string.Empty;
                return;
            }
            var formatted = HotkeyParser.FormatKeyEvent(ke);
            if (!string.IsNullOrEmpty(formatted))
                _shortcutInput.Text = formatted;
        };
        scBoxHost.Controls.Add(_shortcutInput);

        var clearScBtn = new FluentDialogButton
        {
            Text = "Hapus",
            IsPrimary = false,
            Location = new Point(Pad + ContentWidth - 68, scLabel.Bottom + 4),
            Size = new Size(68, 32)
        };
        clearScBtn.Click += (_, _) => _shortcutInput.Text = string.Empty;

        _bodyPanel.Controls.Add(scLabel);
        _bodyPanel.Controls.Add(scBoxHost);
        _bodyPanel.Controls.Add(clearScBtn);

        int cautionTop = scBoxHost.Bottom + 12;
        _cautionBar = new Panel
        {
            Location = new Point(Pad, cautionTop),
            Size = new Size(ContentWidth, 40),
            BackColor = pal.Surface,
            Visible = false
        };
        _cautionBar.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, _cautionBar.Width - 1, _cautionBar.Height - 1);
            using var path = FluentMenu.RoundedRect(r, FluentTheme.RadiusItem);
            using var brush = new SolidBrush(pal.CautionBackground);
            pe.Graphics.FillPath(brush, path);
        };

        var warnIcon = new PictureBox
        {
            Size = new Size(16, 16),
            Location = new Point(12, 12),
            BackColor = pal.CautionBackground,
            Image = FluentIcons.Render(FluentIcons.Warning, 16, pal.CautionIcon)
        };
        var warnLabel = new Label
        {
            Text = "Nama sudah ada \u2014 akan ditimpa",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.Text,
            BackColor = pal.CautionBackground,
            Location = new Point(36, 0),
            Size = new Size(ContentWidth - 36 - 76, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var overwriteBtn = new FluentDialogButton
        {
            Text = "Timpa",
            IsPrimary = false,
            Location = new Point(ContentWidth - 8 - 64, 6),
            Size = new Size(64, 28)
        };
        overwriteBtn.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(ProfileName))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        _cautionBar.Controls.Add(warnIcon);
        _cautionBar.Controls.Add(warnLabel);
        _cautionBar.Controls.Add(overwriteBtn);

        _bodyPanel.Controls.Add(title);
        _bodyPanel.Controls.Add(fieldLabel);
        _bodyPanel.Controls.Add(inputBoxHost);
        _bodyPanel.Controls.Add(_cautionBar);

        _footerPanel = new Panel
        {
            Width = FluentTheme.DialogWidth,
            Height = 80,
            BackColor = pal.Background
        };

        int btnW = (ContentWidth - 8) / 2;
        var cancelBtn = new FluentDialogButton
        {
            Text = "Batal",
            IsPrimary = false,
            Location = new Point(Pad, 24),
            Size = new Size(btnW, 32),
            DialogResult = DialogResult.Cancel
        };
        cancelBtn.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _saveBtn = new FluentDialogButton
        {
            Text = "Simpan",
            IsPrimary = true,
            Location = new Point(Pad + btnW + 8, 24),
            Size = new Size(ContentWidth - btnW - 8, 32),
            DialogResult = DialogResult.OK
        };
        _saveBtn.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(ProfileName))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        _footerPanel.Controls.Add(cancelBtn);
        _footerPanel.Controls.Add(_saveBtn);

        Controls.Add(_bodyPanel);
        Controls.Add(_footerPanel);

        AcceptButton = _saveBtn;
        CancelButton = cancelBtn;

        UpdateDuplicateState();
        Shown += (_, _) =>
        {
            _input.Focus();
            _input.SelectAll();
        };
    }

    private void UpdateDuplicateState()
    {
        string trimmed = ProfileName;
        bool hasName = !string.IsNullOrWhiteSpace(trimmed);
        bool isSameAsEditing = !string.IsNullOrWhiteSpace(_editingOriginalName)
                               && string.Equals(trimmed, _editingOriginalName, StringComparison.OrdinalIgnoreCase);
        bool duplicate = hasName && !isSameAsEditing && _existing.Contains(trimmed);
        _saveBtn.Enabled = hasName;
        _cautionBar.Visible = duplicate;
        int bodyHeight = duplicate ? _cautionBar.Bottom + Pad : _cautionBar.Top - 12 + Pad;
        _bodyPanel.Height = bodyHeight;
        _footerPanel.Location = new Point(0, bodyHeight);
        ClientSize = new Size(FluentTheme.DialogWidth, bodyHeight + _footerPanel.Height);
    }

    public static string? Prompt(IEnumerable<string> existingNames, string defaultName = "Primary Kanan")
    {
        using var dlg = new SaveProfileDialog(existingNames, defaultName);
        return dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.ProfileName)
            ? dlg.ProfileName
            : null;
    }

    public static (string Name, string Shortcut, string? PrimaryKey)? PromptSetup(
        IEnumerable<string> existingNames,
        string defaultName,
        string initialShortcut,
        IReadOnlyList<DisplayFlip.Core.DisplayMonitor> monitors,
        string? initialPrimaryKey = null,
        string? editingOriginalName = null)
    {
        using var dlg = new SaveProfileDialog(
            existingNames, defaultName, initialShortcut, monitors, initialPrimaryKey, editingOriginalName);
        return dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.ProfileName)
            ? (dlg.ProfileName, dlg.ShortcutText, dlg.SelectedPrimaryKey)
            : null;
    }
}

/// <summary>
/// Dialog Monitor hilang (mockup 05): penjelasan monitor hilang + Batal / Lanjutkan dengan yang tersisa.
/// </summary>
public sealed class MissingMonitorDialog : FluentDialogBase
{
    private const int Pad = 24;
    private const int ContentWidth = FluentTheme.DialogWidth - Pad * 2;

    public MissingMonitorDialog(
        string profileName,
        int totalCount,
        IReadOnlyList<string> missingNames,
        IReadOnlyList<string> remainingNames)
    {
        var pal = FluentTheme.Current;
        Text = "DisplayFlip - Monitor hilang";
        BackColor = pal.Surface;

        string missingText = missingNames.Count > 0 ? string.Join(", ", missingNames) : "monitor";
        string remainingText = remainingNames.Count > 0 ? string.Join(", ", remainingNames) : "(tidak ada)";

        string primaryMsg =
            $"Profil \u201C{profileName}\u201D memakai {totalCount} monitor, tetapi {missingText} tidak terdeteksi saat ini.";
        string secondaryMsg = remainingNames.Count > 0
            ? $"Melanjutkan hanya menerapkan susunan untuk {remainingText}."
            : "Tidak ada monitor dari profil ini yang terdeteksi saat ini.";

        int h1 = MeasureWrappedHeight(primaryMsg, FluentTheme.SubtitleFont, ContentWidth);
        int h2 = MeasureWrappedHeight(secondaryMsg, FluentTheme.SubtitleFont, ContentWidth);

        var bodyPanel = new Panel
        {
            Location = new Point(0, 0),
            Width = FluentTheme.DialogWidth,
            BackColor = pal.Surface
        };

        var title = new Label
        {
            Text = "Monitor hilang",
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(Pad, Pad),
            Size = new Size(ContentWidth, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body1 = new Label
        {
            Text = primaryMsg,
            Font = FluentTheme.SubtitleFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(Pad, title.Bottom + 12),
            Size = new Size(ContentWidth, h1)
        };

        var body2 = new Label
        {
            Text = secondaryMsg,
            Font = FluentTheme.SubtitleFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(Pad, body1.Bottom + 8),
            Size = new Size(ContentWidth, h2)
        };

        int bodyHeight = body2.Bottom + Pad;
        bodyPanel.Height = bodyHeight;
        bodyPanel.Controls.Add(title);
        bodyPanel.Controls.Add(body1);
        bodyPanel.Controls.Add(body2);

        var footerPanel = new Panel
        {
            Location = new Point(0, bodyHeight),
            Size = new Size(FluentTheme.DialogWidth, 80),
            BackColor = pal.Background
        };

        int cancelW = (int)Math.Round((ContentWidth - 8) / 2.8);
        int continueW = ContentWidth - 8 - cancelW;

        var cancelBtn = new FluentDialogButton
        {
            Text = "Batal",
            IsPrimary = false,
            Location = new Point(Pad, 24),
            Size = new Size(cancelW, 32),
            DialogResult = DialogResult.Cancel
        };
        cancelBtn.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var continueBtn = new FluentDialogButton
        {
            Text = "Lanjutkan dengan yang tersisa",
            IsPrimary = true,
            Location = new Point(Pad + cancelW + 8, 24),
            Size = new Size(continueW, 32),
            DialogResult = DialogResult.OK,
            Enabled = remainingNames.Count > 0
        };
        continueBtn.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        footerPanel.Controls.Add(cancelBtn);
        footerPanel.Controls.Add(continueBtn);

        Controls.Add(bodyPanel);
        Controls.Add(footerPanel);
        ClientSize = new Size(FluentTheme.DialogWidth, bodyHeight + footerPanel.Height);

        AcceptButton = continueBtn;
        CancelButton = cancelBtn;
    }

    private static int MeasureWrappedHeight(string text, Font font, int width)
    {
        var sz = TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        return Math.Max(20, sz.Height + 4);
    }

    public static bool Confirm(
        string profileName,
        int totalCount,
        IReadOnlyList<string> missingNames,
        IReadOnlyList<string> remainingNames)
    {
        using var dlg = new MissingMonitorDialog(profileName, totalCount, missingNames, remainingNames);
        return dlg.ShowDialog() == DialogResult.OK;
    }
}

/// <summary>
/// Parser &amp; formatter pintasan keyboard global (Ctrl, Alt, Shift, Win + Key).
/// </summary>
public static class HotkeyParser
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public static string FormatKeyEvent(KeyEventArgs e)
    {
        var key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin or Keys.None)
            return string.Empty;

        bool ctrl = e.Control;
        bool alt = e.Alt;
        bool shift = e.Shift;
        if (!ctrl && !alt && !shift && (key < Keys.F1 || key > Keys.F12))
            return string.Empty;

        var parts = new List<string>(4);
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        parts.Add(FormatKeyCode(key));
        return string.Join("+", parts);
    }

    public static string FormatKeyCode(Keys key)
    {
        if (key is >= Keys.D0 and <= Keys.D9)
            return ((int)(key - Keys.D0)).ToString();
        if (key is >= Keys.NumPad0 and <= Keys.NumPad9)
            return "Num" + (int)(key - Keys.NumPad0);
        return key.ToString();
    }

    public static bool TryParse(string? shortcut, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;
        if (string.IsNullOrWhiteSpace(shortcut))
            return false;

        var tokens = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return false;

        Keys parsedKey = Keys.None;
        foreach (var raw in tokens)
        {
            if (raw.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || raw.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_CONTROL;
                continue;
            }
            if (raw.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_ALT;
                continue;
            }
            if (raw.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_SHIFT;
                continue;
            }
            if (raw.Equals("Win", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_WIN;
                continue;
            }

            if (parsedKey != Keys.None)
                return false;

            if (raw.Length == 1 && char.IsDigit(raw[0]))
            {
                parsedKey = Keys.D0 + (raw[0] - '0');
            }
            else if (raw.Length == 1 && char.IsLetter(raw[0]))
            {
                parsedKey = (Keys)char.ToUpperInvariant(raw[0]);
            }
            else if (raw.Length == 4
                     && raw.StartsWith("Num", StringComparison.OrdinalIgnoreCase)
                     && char.IsDigit(raw[3]))
            {
                parsedKey = Keys.NumPad0 + (raw[3] - '0');
            }
            else if (!int.TryParse(raw, out _)
                     && Enum.TryParse<Keys>(raw, ignoreCase: true, out var k)
                     && Enum.IsDefined(typeof(Keys), k))
            {
                parsedKey = k;
            }
            else
            {
                return false;
            }
        }

        if (parsedKey == Keys.None)
            return false;
        if (modifiers == 0 && (parsedKey < Keys.F1 || parsedKey > Keys.F12))
            return false;

        vk = (uint)parsedKey;
        return true;
    }
}

/// <summary>
/// Dialog perekam pintasan keyboard custom (Shortcut Recorder).
/// </summary>
public sealed class ShortcutRecorderDialog : FluentDialogBase
{
    private const int Pad = 24;
    private const int ContentWidth = FluentTheme.DialogWidth - Pad * 2;
    private readonly TextBox _box;

    public string ShortcutValue => _box.Text.Trim();

    public ShortcutRecorderDialog(string actionTitle, string currentShortcut)
    {
        var pal = FluentTheme.Current;
        Text = "DisplayFlip - Atur Pintasan";
        BackColor = pal.Surface;

        var bodyPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(FluentTheme.DialogWidth, 210),
            BackColor = pal.Surface
        };

        var title = new Label
        {
            Text = "Atur pintasan keyboard",
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(Pad, Pad),
            Size = new Size(ContentWidth, 28)
        };

        var sub = new Label
        {
            Text = $"Target: {actionTitle}\nTekan kombinasi tombol (mis. Ctrl+Alt+F, Ctrl+Alt+1, atau F9):",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(Pad, title.Bottom + 8),
            Size = new Size(ContentWidth, 36)
        };

        var boxHost = new Panel
        {
            Location = new Point(Pad, sub.Bottom + 10),
            Size = new Size(ContentWidth, 38),
            BackColor = pal.Background
        };
        boxHost.Paint += (_, pe) =>
        {
            using var pen = new Pen(pal.Accent, 2);
            pe.Graphics.DrawRectangle(pen, 0, 0, boxHost.Width - 1, boxHost.Height - 1);
        };

        _box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Font = new Font("Consolas", 14, FontStyle.Bold, GraphicsUnit.Pixel),
            ForeColor = pal.Text,
            BackColor = pal.Background,
            TextAlign = HorizontalAlignment.Center,
            Text = currentShortcut ?? string.Empty,
            PlaceholderText = "Tekan kombinasi tombol...",
            Location = new Point(10, 9),
            Width = ContentWidth - 20
        };
        _box.KeyDown += (_, ke) =>
        {
            ke.SuppressKeyPress = true;
            ke.Handled = true;
            if (ke.KeyCode is Keys.Back or Keys.Delete)
            {
                _box.Text = string.Empty;
                return;
            }
            var formatted = HotkeyParser.FormatKeyEvent(ke);
            if (!string.IsNullOrEmpty(formatted))
                _box.Text = formatted;
        };
        boxHost.Controls.Add(_box);

        // Preset cepat
        string[] presets = { "Ctrl+Alt+F", "Ctrl+Alt+P", "Ctrl+Alt+1", "Ctrl+Alt+2" };
        int px = Pad;
        int py = boxHost.Bottom + 12;
        foreach (var preset in presets)
        {
            string captured = preset;
            var pBtn = new FluentDialogButton
            {
                Text = captured,
                IsPrimary = false,
                Location = new Point(px, py),
                Size = new Size(82, 26)
            };
            pBtn.Click += (_, _) => _box.Text = captured;
            bodyPanel.Controls.Add(pBtn);
            px += 88;
        }

        var clearBtn = new FluentDialogButton
        {
            Text = "Kosongkan",
            IsPrimary = false,
            Location = new Point(Pad + ContentWidth - 82, py),
            Size = new Size(82, 26)
        };
        clearBtn.Click += (_, _) => _box.Text = string.Empty;
        bodyPanel.Controls.Add(clearBtn);

        bodyPanel.Controls.Add(title);
        bodyPanel.Controls.Add(sub);
        bodyPanel.Controls.Add(boxHost);

        var footerPanel = new Panel
        {
            Location = new Point(0, bodyPanel.Height),
            Size = new Size(FluentTheme.DialogWidth, 76),
            BackColor = pal.Background
        };

        int btnW = (ContentWidth - 8) / 2;
        var cancelBtn = new FluentDialogButton
        {
            Text = "Batal",
            IsPrimary = false,
            Location = new Point(Pad, 22),
            Size = new Size(btnW, 32),
            DialogResult = DialogResult.Cancel
        };
        cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var okBtn = new FluentDialogButton
        {
            Text = "Simpan Pintasan",
            IsPrimary = true,
            Location = new Point(Pad + btnW + 8, 22),
            Size = new Size(ContentWidth - btnW - 8, 32),
            DialogResult = DialogResult.OK
        };
        okBtn.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        footerPanel.Controls.Add(cancelBtn);
        footerPanel.Controls.Add(okBtn);

        Controls.Add(bodyPanel);
        Controls.Add(footerPanel);
        ClientSize = new Size(FluentTheme.DialogWidth, bodyPanel.Height + footerPanel.Height);

        Shown += (_, _) => _box.Focus();
    }

    public static string? Prompt(string actionTitle, string currentShortcut)
    {
        using var dlg = new ShortcutRecorderDialog(actionTitle, currentShortcut);
        return dlg.ShowDialog() == DialogResult.OK ? dlg.ShortcutValue : null;
    }
}

/// <summary>
/// Jendela pesan tersembunyi untuk menerima WM_HOTKEY dari RegisterHotKey Win32.
/// </summary>
internal sealed class GlobalHotkeyHost : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 0x2000;
    private bool _disposed;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public GlobalHotkeyHost()
    {
        CreateHandle(new CreateParams());
    }

    public void ClearAll()
    {
        if (Handle != IntPtr.Zero)
        {
            foreach (var id in _handlers.Keys)
                UnregisterHotKey(Handle, id);
        }
        _handlers.Clear();
    }

    public bool Register(string? shortcut, Action callback)
    {
        if (_disposed || Handle == IntPtr.Zero)
            return false;
        if (!HotkeyParser.TryParse(shortcut, out uint mods, out uint vk))
            return false;

        int id = _nextId++;
        if (RegisterHotKey(Handle, id, mods | HotkeyParser.MOD_NOREPEAT, vk))
        {
            _handlers[id] = callback;
            return true;
        }
        return false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (_handlers.TryGetValue(id, out var cb))
            {
                try { cb(); } catch { }
                return;
            }
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ClearAll();
        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}

/// <summary>
/// Overlay identifikasi nomor layar ala Windows 11 System &gt; Display (Identify).
/// </summary>
public sealed class IdentifyOverlayForm : Form
{
    private readonly string _numberText;
    private readonly string _subtitleText;
    private readonly bool _isPrimary;

    private IdentifyOverlayForm(Rectangle monitorBounds, string numberText, string subtitleText, bool isPrimary)
    {
        _numberText = numberText;
        _subtitleText = subtitleText;
        _isPrimary = isPrimary;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        Size = new Size(220, 128);

        int x = monitorBounds.X + 48;
        int y = monitorBounds.Bottom - Height - 72;
        if (y < monitorBounds.Y + 24)
            y = monitorBounds.Y + (monitorBounds.Height - Height) / 2;
        Location = new Point(x, y);

        BackColor = Color.FromArgb(0x1E, 0x1E, 0x24);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int CS_DROPSHADOW = 0x00020000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!FluentMenu.DwmRoundCorners(Handle))
        {
            using var path = FluentMenu.RoundedRect(ClientRectangle, 8);
            Region = new Region(path);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var pal = FluentTheme.Current;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = FluentMenu.RoundedRect(rect, 8);
        using var pen = new Pen(_isPrimary ? pal.Accent : Color.FromArgb(120, Color.White), _isPrimary ? 2.5f : 1.5f);
        g.DrawPath(pen, path);

        using var numFont = new Font("Segoe UI Variable Display", 48, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, _numberText, numFont,
            new Rectangle(0, 8, Width, 70), Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(g, _subtitleText, FluentTheme.BodySemiboldFont,
            new Rectangle(8, 82, Width - 16, 32), Color.FromArgb(220, 220, 230),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private static readonly List<IdentifyOverlayForm> s_activeOverlays = new();

    public static void ShowForMonitors(IReadOnlyList<DisplayFlip.Core.DisplayMonitor> monitors)
    {
        if (monitors.Count == 0)
            return;

        lock (s_activeOverlays)
        {
            foreach (var old in s_activeOverlays)
            {
                try
                {
                    if (!old.IsDisposed)
                    {
                        old.Close();
                        old.Dispose();
                    }
                }
                catch { }
            }
            s_activeOverlays.Clear();
        }

        var forms = new List<IdentifyOverlayForm>();
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            string num = ExtractDisplayNumber(m.StableKey, m.FriendlyName, i + 1);
            string cleanName = m.FriendlyName.StartsWith("\\\\.\\", StringComparison.Ordinal)
                ? m.FriendlyName[4..]
                : m.FriendlyName;
            bool isPrimary = m.Position.X == 0 && m.Position.Y == 0;
            string sub = isPrimary ? $"{cleanName} \u2022 Utama" : cleanName;
            var bounds = new Rectangle(m.Position.X, m.Position.Y, m.Position.Width, m.Position.Height);
            var f = new IdentifyOverlayForm(bounds, num, sub, isPrimary);
            forms.Add(f);
            f.Show();
        }

        lock (s_activeOverlays)
        {
            s_activeOverlays.AddRange(forms);
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(2200);
            foreach (var f in forms)
            {
                try
                {
                    if (!f.IsDisposed && f.IsHandleCreated)
                        f.BeginInvoke(new Action(() => { f.Close(); f.Dispose(); }));
                }
                catch { }
            }
        });
    }

    public static string ExtractDisplayNumber(string? stableKey, string? friendlyName, int fallbackIndex)
    {
        string numFromStable = ExtractFromText(stableKey);
        if (!string.IsNullOrEmpty(numFromStable))
            return numFromStable;

        string numFromFriendly = ExtractFromText(friendlyName);
        if (!string.IsNullOrEmpty(numFromFriendly))
            return numFromFriendly;

        return fallbackIndex.ToString();
    }

    public static string ExtractDisplayNumber(string? friendlyName, int fallbackIndex)
    {
        return ExtractDisplayNumber(null, friendlyName, fallbackIndex);
    }

    private static string ExtractFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var mDisp = System.Text.RegularExpressions.Regex.Match(
            text, @"(?:DISPLAY|Layar|Monitor|Screen)\s*(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (mDisp.Success && int.TryParse(mDisp.Groups[1].Value, out int dVal) && dVal > 0 && dVal <= 32)
            return dVal.ToString();

        var mWord = System.Text.RegularExpressions.Regex.Match(
            text, @"\b([1-9]|[12]\d|3[0-2])\b");
        if (mWord.Success)
            return mWord.Groups[1].Value;

        return string.Empty;
    }
}
