using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DisplayFlip.Core;
using DisplayFlip.Windows;

namespace DisplayFlip;

/// <summary>
/// Jendela Pengaturan DisplayFlip (Windows 11 System &gt; Display style):
/// - Kanvas interaktif drag-and-drop untuk menggeser susunan monitor secara bebas (2D + magnetic snap)
/// - Panel multi-fungsi monitor terpilih (Primary, Perataan, Resolusi, Orientasi, Mode Proyeksi)
/// - Setup &amp; edit profil langsung dari kanvas + pintasan keyboard custom (Global Hotkeys)
/// - Pilihan 3 rekomendasi ikon modern Windows 11 secara live.
/// </summary>
public sealed class FluentSettingsForm : Form
{
    private const int FormWidth = 980;
    private const int FormHeight = 720;
    private const int MinWidth = 680;
    private const int MinHeight = 560;
    private const int Edge = 5;
    private const int RootWidth = FormWidth - Edge * 2;
    private const int RootHeight = FormHeight - Edge * 2;
    private const int TitleBarHeight = 34;
    private const int NavWidth = 216;
    private const int StatusBarHeight = 28;
    private const int ContentPadLeft = 24;
    private const int ContentPadRight = 28;
    private const int ContentInnerWidth = FormWidth - Edge * 2 - NavWidth - ContentPadLeft - ContentPadRight;

    private readonly TrayHost _host;
    private readonly Panel _titleBar;
    private readonly PictureBox _titleLogo;
    private readonly Button _minBtn;
    private readonly CaptionButton _maxBtn;
    private readonly Button _closeBtn;
    private readonly Panel _navPanel;
    private readonly Panel _contentHost;
    private readonly Panel _statusBar;
    private readonly Panel _profilePage;
    private readonly Panel _root;
    private readonly ToolTip _navToolTip = new() { ShowAlways = true };
    private Label? _monHeader, _monSubHeader, _profHeader, _noteLabel;
    private bool _layoutReady;
    private int _lastProfileRowWidth = -1;
    private readonly Panel _shortcutPage;
    private readonly Panel _generalPage;
    private readonly Panel _aboutPage;

    // Elemen halaman Profil & Layar
    private readonly Panel _monitorCanvas;
    private readonly FluentDialogButton _identifyBtn;
    private readonly FluentDialogButton _mirrorBtn;
    private readonly FluentDialogButton _alignBtn;
    private readonly FluentDialogButton _multiScreenBtn;
    private readonly FluentDialogButton _refreshBtn;
    private readonly FluentDialogButton _resetCanvasBtn;
    private readonly FluentDialogButton _applyCanvasBtn;
    private readonly FluentCheckBox _autoApplyCheck;
    private readonly Panel _singleDisplayBanner;
    private readonly Label _singleDisplayLabel;
    private readonly FluentDialogButton _restoreExtendBtn;

    // Elemen 3 SettingsCard Windows 11
    private readonly Panel _cardIdentity;
    private readonly Label _selectedMonTitle;
    private readonly Label _selectedMonSub;
    private readonly FluentCheckBox _makePrimaryCheck;
    private readonly FluentDialogButton _soloDisplayBtn;

    private readonly Panel _cardQuickMove;
    private readonly Label _quickMoveTitle;
    private readonly Label _quickMoveSub;
    private readonly FluentDialogButton _moveLeftBtn;
    private readonly FluentDialogButton _moveRightBtn;
    private readonly FluentDialogButton _moveTopBtn;
    private readonly FluentDialogButton _moveBottomBtn;

    private readonly Panel _cardResOrient;
    private readonly Label _resOrientTitle;
    private readonly Label _resOrientSub;
    private readonly FluentSelectButton _resolutionCombo;
    private readonly FluentSelectButton _orientationCombo;

    // Elemen Daftar Profil
    private readonly Panel _profileListPanel;
    private readonly FluentDialogButton _newProfileBtn;
    private readonly Label _emptyProfileLabel;

    // Elemen halaman Pintasan
    private readonly Label _scTitle;
    private readonly Label _scSub;
    private readonly Panel _shortcutListPanel;

    // Elemen halaman Umum & Ikon
    private readonly Label _genTitle;
    private readonly Label _iconSectionTitle;
    private readonly Panel _iconCardFluent;
    private readonly Panel _iconCardAccent;
    private readonly Panel _iconCardMinimal;
    private readonly Panel _startupCard;
    private readonly FluentCheckBox _startupCheck;
    private readonly Label _startupSub;
    private readonly FluentCheckBox _autoApplyGeneralCheck;
    private readonly Label _autoApplySub;
    private readonly Panel _storageCard;
    private readonly Label _storageTitle;
    private readonly Label _storagePath;
    private readonly FluentDialogButton _openFolderBtn;
    private readonly Dictionary<string, Panel> _iconCards = new();

    // Elemen halaman Tentang
    private readonly Label _aboutTitle;
    private readonly Panel _aboutCard;
    private readonly Label _aboutName;
    private readonly Label _aboutDesc;
    private readonly Panel _cliCard;
    private readonly Label _cliTitle;
    private readonly Label _cliText;

    private string _activeTab = "Profil";
    private string _statusMessage = string.Empty;
    private bool _allowClose;
    private bool _updatingUi;

    private List<DisplayMonitor> _liveMonitors = new();
    private List<DisplayMonitor> _canvasMonitors = new();
    private string? _canvasPrimaryKey;
    private string? _selectedMonitorKey;
    private string? _loadedProfileName;
    private bool _hasUnappliedCanvasChanges;

    // State drag-and-drop kanvas monitor
    private string? _draggingMonitorKey;
    private Point _dragMouseStart;
    private Position _dragMonitorOriginalPos;
    private Rectangle _dragFloatingRect;
    private Position _dragSnappedDesktopPos;
    private bool _isDraggingMonitor;
    private double _lastCanvasScale = 0.08;
    private int _lastCanvasMinX;
    private int _lastCanvasMinY;
    private int _lastCanvasOffsetX;
    private int _lastCanvasOffsetY;

    private List<LayoutProfile> _profiles = new();
    private readonly List<(Rectangle Bounds, DisplayMonitor Monitor, int DisplayNumber)> _monitorHitBoxes = new();
    private string? _hoveredMonitorKey;
    private bool _useFallbackRegion;
    private bool _inSizeMove;
    private Size _lastHostSize = Size.Empty;
    private readonly List<(Point Start, Point End)> _activeGuideLines = new();
    private bool _trackingNcMouseLeave;

    private enum WindowBreakpoint { Compact, Normal, Wide }
    private static WindowBreakpoint GetBreakpoint(int width) =>
        width < 850 ? WindowBreakpoint.Compact : (width >= 1160 ? WindowBreakpoint.Wide : WindowBreakpoint.Normal);

    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONUP = 0x00A2;
    private const int WM_NCMOUSELEAVE = 0x02A2;
    private const int WM_ENTERSIZEMOVE = 0x0231;
    private const int WM_EXITSIZEMOVE = 0x0232;

    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTMAXBUTTON = 9;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TRACKMOUSEEVENT
    {
        public int cbSize;
        public uint dwFlags;
        public IntPtr hwndTrack;
        public uint dwHoverTime;
    }

    [DllImport("user32.dll")]
    private static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

    private const uint TME_LEAVE = 0x00000002;
    private const uint TME_NONCLIENT = 0x00000010;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    private readonly Dictionary<string, NavButton> _navButtons = new();

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr hWnd, int bar, bool show);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

    private static void ApplyScrollTheme(Control c)
    {
        void Apply()
        {
            try { SetWindowTheme(c.Handle, FluentTheme.IsDarkMode() ? "DarkMode_Explorer" : "Explorer", null); }
            catch { /* tema scrollbar bawaan */ }
        }
        if (c.IsHandleCreated) Apply();
        c.HandleCreated += (_, _) => Apply();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public FluentSettingsForm(TrayHost host, Icon? appIcon)
    {
        _host = host;

        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(FormWidth, FormHeight);
        MinimumSize = new Size(MinWidth, MinHeight);
        Padding = Padding.Empty;
        MaximizeBox = true;
        MinimizeBox = true;
        Text = "DisplayFlip";
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        KeyPreview = true;
        if (appIcon != null)
            Icon = appIcon;

        var pal = FluentTheme.Current;
        BackColor = pal.Background;
        Font = FluentTheme.BodyFont;

        // 1. Custom Titlebar (34px)
        _titleBar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(RootWidth, TitleBarHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = pal.ShellBackground
        };
        _titleBar.DoubleClick += (_, _) => ToggleMaximize();
        _titleBar.MouseDown += OnTitleBarMouseDown;

        _titleLogo = new PictureBox
        {
            Location = new Point(14, 7),
            Size = new Size(20, 20),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = pal.ShellBackground
        };
        UpdateAppIcon(appIcon);
        _titleLogo.MouseDown += OnTitleBarMouseDown;

        var titleText = new Label
        {
            Text = "DisplayFlip \u2014 Pengaturan Layar & Profil",
            UseMnemonic = false,
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.ShellBackground,
            Location = new Point(42, 0),
            Size = new Size(340, TitleBarHeight),
            TextAlign = ContentAlignment.MiddleLeft
        };
        titleText.MouseDown += OnTitleBarMouseDown;

        _minBtn = CreateCaptionButton("\uE921", false, RootWidth - 46 * 3);
        _minBtn.Click += (_, _) => WindowState = FormWindowState.Minimized;

        _maxBtn = (CaptionButton)CreateCaptionButton("\uE922", false, RootWidth - 46 * 2);
        _maxBtn.Click += (_, _) => ToggleMaximize();

        _closeBtn = CreateCaptionButton("\uE8BB", true, RootWidth - 46);
        _closeBtn.Click += (_, _) => Hide();

        _titleBar.Controls.Add(_titleLogo);
        _titleBar.Controls.Add(titleText);
        _titleBar.Controls.Add(_minBtn);
        _titleBar.Controls.Add(_maxBtn);
        _titleBar.Controls.Add(_closeBtn);

        // 2. Status Bar (28px bottom)
        _statusBar = new BufferedPanel
        {
            Location = new Point(0, RootHeight - StatusBarHeight),
            Size = new Size(RootWidth, StatusBarHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = pal.ShellBackground
        };
        _statusBar.Paint += PaintStatusBar;

        // 3. Left NavigationView
        _navPanel = new Panel
        {
            Location = new Point(0, TitleBarHeight),
            Size = new Size(NavWidth, RootHeight - TitleBarHeight - StatusBarHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = pal.ShellBackground
        };

        AddNavItem("Profil", "Layar & Profil", FluentIcons.Display, 6);
        AddNavItem("Pintasan", "Pintasan Keyboard", FluentIcons.Keyboard, 50);
        AddNavItem("Umum", "Umum & Ikon", FluentIcons.Settings, 94);
        AddNavItem("Tentang", "Tentang & CLI", FluentIcons.Info, 138);

        // 4. Right Content Area (Mica Alt with top-left rounded corner)
        _contentHost = new BufferedPanel
        {
            Location = new Point(NavWidth, TitleBarHeight),
            Size = new Size(RootWidth - NavWidth, RootHeight - TitleBarHeight - StatusBarHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = pal.Background
        };

        // =====================================================================
        // PAGE 1: PROFIL & LAYAR (Windows 11 System > Display Experience)
        // =====================================================================
        _profilePage = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = pal.Background
        };

        var monHeader = _monHeader = new Label
        {
            Text = "Tata Letak Layar",
            UseMnemonic = false,
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            Location = new Point(ContentPadLeft, 6),
            Size = new Size(260, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var monSubHeader = _monSubHeader = new Label
        {
            Text = "Pilih layar untuk mengatur pengaturannya. Geser (drag) layar untuk mengatur posisi.",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            Location = new Point(ContentPadLeft, 34),
            Size = new Size(ContentInnerWidth, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _singleDisplayBanner = CreateSurfaceCard(ContentPadLeft, 54, ContentInnerWidth, 34);
        _singleDisplayBanner.Visible = false;

        _singleDisplayLabel = new Label
        {
            Text = "Sedang dalam mode layar tunggal.",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(12, 6),
            Size = new Size(Math.Max(100, ContentInnerWidth - 180), 22),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _restoreExtendBtn = new FluentDialogButton
        {
            Text = "Perluas Semua Layar",
            Glyph = FluentIcons.DualDisplay,
            IsPrimary = true,
            Location = new Point(ContentInnerWidth - 170, 4),
            Size = new Size(160, 26)
        };
        _restoreExtendBtn.Click += (_, _) =>
        {
            SetStatusMessage(_host.TriggerProjectionMode("extend"));
            RefreshData(resetCanvasToLive: true);
        };

        _singleDisplayBanner.Controls.Add(_singleDisplayLabel);
        _singleDisplayBanner.Controls.Add(_restoreExtendBtn);

        // Kanvas Interaktif Monitor dengan Inside-Well Toolbar
        _monitorCanvas = new BufferedPanel
        {
            Location = new Point(ContentPadLeft, 54),
            Size = new Size(ContentInnerWidth, 218),
            BackColor = pal.WellBackground
        };
        _monitorCanvas.Paint += PaintMonitorCanvas;
        _monitorCanvas.MouseDown += OnMonitorCanvasMouseDown;
        _monitorCanvas.MouseMove += OnMonitorCanvasMouseMove;
        _monitorCanvas.MouseUp += OnMonitorCanvasMouseUp;
        _monitorCanvas.MouseLeave += (_, _) =>
        {
            if (!_isDraggingMonitor && _hoveredMonitorKey != null)
            {
                _hoveredMonitorKey = null;
                _monitorCanvas.Cursor = Cursors.Default;
                _monitorCanvas.Invalidate();
            }
        };

        _autoApplyCheck = new FluentCheckBox
        {
            Text = "Terapkan langsung saat geser",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.WellBackground,
            Location = new Point(14, 180),
            Size = new Size(210, 24),
            AutoCheck = true
        };
        _autoApplyCheck.CheckedChanged += OnAutoApplyCanvasCheckedChanged;

        _applyCanvasBtn = new FluentDialogButton
        {
            Text = "Terapkan Susunan",
            Glyph = FluentIcons.Check,
            IsPrimary = true,
            Visible = false,
            Location = new Point(ContentInnerWidth - 148, 178),
            Size = new Size(136, 28)
        };
        _applyCanvasBtn.Click += async (_, _) => await ApplyCanvasArrangementAsync();

        _resetCanvasBtn = new FluentDialogButton
        {
            Text = "Reset",
            Glyph = FluentIcons.Undo,
            IsPrimary = false,
            Visible = false,
            Enabled = false,
            Location = new Point(_applyCanvasBtn.Left - 6 - 72, 178),
            Size = new Size(72, 28)
        };
        _resetCanvasBtn.Click += (_, _) =>
        {
            ResetCanvasToLive();
            SetStatusMessage("Kanvas dikembalikan ke posisi monitor aktif saat ini.");
        };

        _identifyBtn = new FluentDialogButton
        {
            Text = "Identifikasi",
            Glyph = FluentIcons.Identify,
            IsPrimary = false,
            Location = new Point(100, 178),
            Size = new Size(96, 28)
        };
        _identifyBtn.Click += (_, _) =>
        {
            _host.TriggerIdentifyMonitors();
            SetStatusMessage("Menampilkan nomor identifikasi pada setiap layar fisik...");
        };

        _mirrorBtn = new FluentDialogButton
        {
            Text = "Tukar Kiri-Kanan",
            Glyph = FluentIcons.Swap,
            IsPrimary = false,
            Location = new Point(200, 178),
            Size = new Size(124, 28)
        };
        _mirrorBtn.Click += async (_, _) =>
        {
            _mirrorBtn.Enabled = false;
            var msg = await _host.TriggerMirrorAsync();
            SetStatusMessage(msg);
            RefreshData(resetCanvasToLive: true);
        };

        _alignBtn = new FluentDialogButton
        {
            Text = "Sejajarkan",
            Glyph = FluentIcons.Align,
            IsPrimary = false,
            Location = new Point(330, 178),
            Size = new Size(96, 28)
        };
        _alignBtn.Click += OnAlignClickedAsync;

        _multiScreenBtn = new FluentDialogButton
        {
            Text = "Multi-layar \u25BE",
            Glyph = FluentIcons.Project,
            IsPrimary = false,
            Location = new Point(430, 178),
            Size = new Size(144, 28)
        };
        _multiScreenBtn.Click += (_, _) => ShowMultiScreenMenu();

        _refreshBtn = new FluentDialogButton
        {
            Text = "Segarkan",
            Glyph = FluentIcons.Refresh,
            IsPrimary = false,
            Visible = false,
            Location = new Point(0, 0),
            Size = new Size(90, 28)
        };
        _refreshBtn.Click += (_, _) =>
        {
            RefreshData(resetCanvasToLive: true);
            SetStatusMessage("Topologi layar disegarkan dari sistem.");
        };

        _monitorCanvas.Controls.Add(_autoApplyCheck);
        _monitorCanvas.Controls.Add(_resetCanvasBtn);
        _monitorCanvas.Controls.Add(_applyCanvasBtn);
        _monitorCanvas.Controls.Add(_identifyBtn);
        _monitorCanvas.Controls.Add(_mirrorBtn);
        _monitorCanvas.Controls.Add(_alignBtn);
        _monitorCanvas.Controls.Add(_multiScreenBtn);

        // 3 SettingsCard Windows 11
        // Card 1: Identitas & Layar Utama
        _cardIdentity = CreateSettingsCardPanel(FluentIcons.Display);
        _selectedMonTitle = new Label
        {
            Text = "Pengaturan layar terpilih",
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _selectedMonSub = new Label
        {
            Text = "Resolusi dan koordinat monitor aktif",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _makePrimaryCheck = new FluentCheckBox
        {
            Text = "Layar utama (Primary)",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            AutoCheck = false
        };
        _makePrimaryCheck.Click += async (_, _) =>
        {
            if (_updatingUi || string.IsNullOrEmpty(_selectedMonitorKey))
                return;
            if (_makePrimaryCheck.Checked)
            {
                SetStatusMessage("Layar ini sudah menjadi layar utama (Primary).");
                return;
            }
            _canvasPrimaryKey = _selectedMonitorKey;
            _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _canvasPrimaryKey);
            _hasUnappliedCanvasChanges = true;
            _resetCanvasBtn.Enabled = true;
            _monitorCanvas.Invalidate();
            UpdateInspectorUi();
            if (_autoApplyCheck.Checked)
                await ApplyCanvasArrangementAsync();
            else
                SetStatusMessage("Layar utama diubah di kanvas. Klik Terapkan Susunan atau Simpan Profil.");
        };

        _soloDisplayBtn = new FluentDialogButton
        {
            Text = "Hanya Layar Ini",
            Glyph = FluentIcons.Display,
            IsPrimary = false
        };
        _soloDisplayBtn.Click += (_, _) =>
        {
            if (string.IsNullOrEmpty(_selectedMonitorKey))
                return;
            var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
            if (sel == null)
                return;
            int idx = _canvasMonitors.IndexOf(sel) + 1;
            string numStr = IdentifyOverlayForm.ExtractDisplayNumber(sel.StableKey, sel.FriendlyName, idx);
            int targetDisplay = int.TryParse(numStr, out int dn) && (dn == 1 || dn == 2) ? dn : (idx <= 1 ? 1 : 2);
            SetStatusMessage(_host.TriggerSoloDisplay(targetDisplay));
            RefreshData(resetCanvasToLive: true);
        };

        _cardIdentity.Controls.Add(_selectedMonTitle);
        _cardIdentity.Controls.Add(_selectedMonSub);
        _cardIdentity.Controls.Add(_makePrimaryCheck);
        _cardIdentity.Controls.Add(_soloDisplayBtn);

        // Card 2: Pindah Cepat Posisi Layar
        _cardQuickMove = CreateSettingsCardPanel(FluentIcons.Move);
        _quickMoveTitle = new Label
        {
            Text = "Pindah cepat posisi layar",
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _quickMoveSub = new Label
        {
            Text = "Pindahkan relatif terhadap layar lainnya tanpa drag manual",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _moveLeftBtn = CreateQuickPosButton("Ke Kiri", 0, 0, 66, "left");
        _moveRightBtn = CreateQuickPosButton("Ke Kanan", 0, 0, 74, "right");
        _moveTopBtn = CreateQuickPosButton("Ke Atas", 0, 0, 68, "top");
        _moveBottomBtn = CreateQuickPosButton("Ke Bawah", 0, 0, 76, "bottom");

        _cardQuickMove.Controls.Add(_quickMoveTitle);
        _cardQuickMove.Controls.Add(_quickMoveSub);
        _cardQuickMove.Controls.Add(_moveLeftBtn);
        _cardQuickMove.Controls.Add(_moveRightBtn);
        _cardQuickMove.Controls.Add(_moveTopBtn);
        _cardQuickMove.Controls.Add(_moveBottomBtn);

        // Card 3: Resolusi & Orientasi Tampilan
        _cardResOrient = CreateSettingsCardPanel(FluentIcons.Scale);
        _resOrientTitle = new Label
        {
            Text = "Resolusi & orientasi tampilan",
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _resOrientSub = new Label
        {
            Text = "Sesuaikan ukuran piksel dan orientasi putaran",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _resolutionCombo = CreateFluentSelect(0, 0, 180);
        _resolutionCombo.SelectedIndexChanged += OnResolutionComboChanged;

        _orientationCombo = CreateFluentSelect(0, 0, 126);
        _orientationCombo.SetItems(new[]
        {
            "Lanskap (0\u00B0)",
            "Potret (90\u00B0)",
            "Lanskap Terbalik",
            "Potret Terbalik"
        }, 0);
        _orientationCombo.SelectedIndexChanged += OnOrientationComboChanged;

        _cardResOrient.Controls.Add(_resOrientTitle);
        _cardResOrient.Controls.Add(_resOrientSub);
        _cardResOrient.Controls.Add(_resolutionCombo);
        _cardResOrient.Controls.Add(_orientationCombo);

        // Header & Daftar Profil Tersimpan
        var profHeader = _profHeader = new Label
        {
            Text = "Profil tersimpan",
            UseMnemonic = false,
            Font = FluentTheme.SubtitleFont,
            ForeColor = pal.Text,
            Location = new Point(ContentPadLeft, 356),
            Size = new Size(340, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _newProfileBtn = new FluentDialogButton
        {
            Text = "+ Simpan Profil",
            Glyph = FluentIcons.Add,
            IsPrimary = true,
            Location = new Point(ContentPadLeft + ContentInnerWidth - 132, 354),
            Size = new Size(132, 30)
        };
        _newProfileBtn.Click += (_, _) =>
        {
            if (_host.TriggerSaveNewProfile(out var savedName, _canvasMonitors, _canvasPrimaryKey, _loadedProfileName))
            {
                _loadedProfileName = savedName;
                SetStatusMessage($"Profil disimpan dari susunan kanvas: {savedName}");
            }
            RefreshData(resetCanvasToLive: false);
        };

        _profileListPanel = new Panel
        {
            Location = new Point(ContentPadLeft, 390),
            Size = new Size(ContentInnerWidth, 178),
            AutoScroll = false,
            BackColor = pal.Background
        };

        _emptyProfileLabel = new Label
        {
            Text = "Belum ada profil tersimpan.\nAtur susunan monitor di kanvas atas lalu klik \"+ Simpan Profil\".",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(0, 0),
            Size = new Size(ContentInnerWidth, 70),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var noteLabel = _noteLabel = new Label
        {
            Text = "Tips: Klik tombol [Kanvas] pada profil untuk mengedit susunan profil langsung di kanvas tanpa mengubah layar dulu.",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            Location = new Point(ContentPadLeft, 572),
            Size = new Size(ContentInnerWidth, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _profilePage.Controls.Add(monHeader);
        _profilePage.Controls.Add(monSubHeader);
        _profilePage.Controls.Add(_singleDisplayBanner);
        _profilePage.Controls.Add(_monitorCanvas);
        _profilePage.Controls.Add(_cardIdentity);
        _profilePage.Controls.Add(_cardQuickMove);
        _profilePage.Controls.Add(_cardResOrient);
        _profilePage.Controls.Add(profHeader);
        _profilePage.Controls.Add(_newProfileBtn);
        _profilePage.Controls.Add(_profileListPanel);
        _profilePage.Controls.Add(noteLabel);

        // =====================================================================
        // PAGE 2: PINTASAN KEYBOARD (Custom Shortcuts)
        // =====================================================================
        _shortcutPage = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = pal.Background,
            Visible = false
        };

        var scTitle = _scTitle = new Label
        {
            Text = "Pintasan keyboard (Custom Shortcuts)",
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            Location = new Point(ContentPadLeft, 16),
            Size = new Size(ContentInnerWidth, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var scSub = _scSub = new Label
        {
            Text = "Pintasan bekerja secara global di Windows kapan saja (termasuk saat DisplayFlip berjalan di system tray).",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            Location = new Point(ContentPadLeft, 48),
            Size = new Size(ContentInnerWidth, 22),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _shortcutListPanel = new Panel
        {
            Location = new Point(ContentPadLeft, 80),
            Size = new Size(ContentInnerWidth, 496),
            AutoScroll = true,
            BackColor = pal.Background
        };

        _shortcutPage.Controls.Add(scTitle);
        _shortcutPage.Controls.Add(scSub);
        _shortcutPage.Controls.Add(_shortcutListPanel);

        // =====================================================================
        // PAGE 3: UMUM & REKOMENDASI IKON MODERN
        // =====================================================================
        _generalPage = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = pal.Background,
            Visible = false
        };

        var genTitle = _genTitle = new Label
        {
            Text = "Pengaturan umum & gaya ikon",
            UseMnemonic = false,
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            Location = new Point(ContentPadLeft, 16),
            Size = new Size(ContentInnerWidth, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var iconSectionTitle = _iconSectionTitle = new Label
        {
            Text = "Pilih gaya ikon modern DisplayFlip (berlaku untuk Taskbar, Titlebar & System Tray):",
            UseMnemonic = false,
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            Location = new Point(ContentPadLeft, 54),
            Size = new Size(ContentInnerWidth, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };

        int cardW = (ContentInnerWidth - 16) / 3;
        var iconCardFluent = _iconCardFluent = CreateIconStyleCard(
            "fluent",
            "Fluent Glass Dual",
            "Dua layar berlapis biru-sian modern + lencana flip emas.",
            ContentPadLeft, 80, cardW);
        var iconCardAccent = _iconCardAccent = CreateIconStyleCard(
            "accent",
            "Windows 11 Accent",
            "Mengikuti warna aksen Windows 11 Anda secara dinamis.",
            ContentPadLeft + cardW + 8, 80, cardW);
        var iconCardMinimal = _iconCardMinimal = CreateIconStyleCard(
            "minimal",
            "Minimalist Precision",
            "Kontras tinggi monokrom tajam untuk taskbar bersih.",
            ContentPadLeft + (cardW + 8) * 2, 80, ContentInnerWidth - (cardW + 8) * 2);

        var startupCard = _startupCard = CreateSurfaceCard(ContentPadLeft, 228, ContentInnerWidth, 118);
        _startupCheck = new FluentCheckBox
        {
            Text = "Jalan otomatis saat Windows start",
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(16, 14),
            Size = new Size(ContentInnerWidth - 32, 24),
            AutoCheck = true
        };
        _startupCheck.CheckedChanged += (_, _) =>
        {
            if (_updatingUi)
                return;
            _host.SetStartupEnabled(_startupCheck.Checked);
            SetStatusMessage(_startupCheck.Checked
                ? "Auto-start diaktifkan (--tray)."
                : "Auto-start dinonaktifkan.");
        };
        var startupSub = _startupSub = new Label
        {
            Text = "Mulai tenang di system tray setiap login Windows.",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(42, 38),
            Size = new Size(ContentInnerWidth - 58, 18)
        };

        _autoApplyGeneralCheck = new FluentCheckBox
        {
            Text = "Langsung terapkan perubahan saat selesai menggeser layar di kanvas",
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(16, 66),
            Size = new Size(ContentInnerWidth - 32, 24),
            AutoCheck = true
        };
        _autoApplyGeneralCheck.CheckedChanged += (_, _) =>
        {
            if (_updatingUi)
                return;
            _host.SetAutoApplyDrag(_autoApplyGeneralCheck.Checked);
            _updatingUi = true;
            _autoApplyCheck.Checked = _autoApplyGeneralCheck.Checked;
            _updatingUi = false;
            SetStatusMessage(_autoApplyGeneralCheck.Checked
                ? "Auto-apply drag diaktifkan."
                : "Auto-apply drag dinonaktifkan (gunakan tombol Terapkan Susunan).");
        };
        var autoApplySub = _autoApplySub = new Label
        {
            Text = "Bebas geser layar tanpa dialog konfirmasi berulang seperti bawaan Windows 11.",
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(42, 90),
            Size = new Size(ContentInnerWidth - 58, 18)
        };

        startupCard.Controls.Add(_startupCheck);
        startupCard.Controls.Add(startupSub);
        startupCard.Controls.Add(_autoApplyGeneralCheck);
        startupCard.Controls.Add(autoApplySub);

        var storageCard = _storageCard = CreateSurfaceCard(ContentPadLeft, 360, ContentInnerWidth, 88);
        var storageTitle = _storageTitle = new Label
        {
            Text = "Penyimpanan profil & pintasan JSON",
            UseMnemonic = false,
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(16, 14),
            Size = new Size(340, 20)
        };
        var storagePath = _storagePath = new Label
        {
            Text = _host.ProfilesFilePath,
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(16, 38),
            Size = new Size(ContentInnerWidth - 130, 34)
        };
        var openFolderBtn = _openFolderBtn = new FluentDialogButton
        {
            Text = "Buka folder",
            IsPrimary = false,
            Location = new Point(ContentInnerWidth - 108, 28),
            Size = new Size(92, 32)
        };
        openFolderBtn.Click += (_, _) =>
        {
            try
            {
                var dir = Path.GetDirectoryName(_host.ProfilesFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                    Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Gagal buka folder: {ex.Message}");
            }
        };
        storageCard.Controls.Add(storageTitle);
        storageCard.Controls.Add(storagePath);
        storageCard.Controls.Add(openFolderBtn);

        _generalPage.Controls.Add(genTitle);
        _generalPage.Controls.Add(iconSectionTitle);
        _generalPage.Controls.Add(iconCardFluent);
        _generalPage.Controls.Add(iconCardAccent);
        _generalPage.Controls.Add(iconCardMinimal);
        _generalPage.Controls.Add(startupCard);
        _generalPage.Controls.Add(storageCard);

        // =====================================================================
        // PAGE 4: TENTANG & CLI
        // =====================================================================
        _aboutPage = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = pal.Background,
            Visible = false
        };

        var aboutTitle = _aboutTitle = new Label
        {
            Text = "Tentang DisplayFlip",
            Font = FluentTheme.TitleFont,
            ForeColor = pal.Text,
            Location = new Point(ContentPadLeft, 16),
            Size = new Size(ContentInnerWidth, 32),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var aboutCard = _aboutCard = CreateSurfaceCard(ContentPadLeft, 60, ContentInnerWidth, 118);
        var aboutName = _aboutName = new Label
        {
            Text = "DisplayFlip \u2014 Multi-Monitor Layout & Profile Studio",
            UseMnemonic = false,
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(16, 16),
            Size = new Size(ContentInnerWidth - 32, 22)
        };
        var aboutDesc = _aboutDesc = new Label
        {
            Text = "Utilitas Windows 11 modern untuk menggeser susunan monitor secara bebas (drag-and-drop 2D), mengatur layar utama, resolusi, orientasi, setup profil langsung, serta pintasan keyboard global.",
            Font = FluentTheme.BodyFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(16, 44),
            Size = new Size(ContentInnerWidth - 32, 56)
        };
        aboutCard.Controls.Add(aboutName);
        aboutCard.Controls.Add(aboutDesc);

        var cliCard = _cliCard = CreateSurfaceCard(ContentPadLeft, 192, ContentInnerWidth, 136);
        var cliTitle = _cliTitle = new Label
        {
            Text = "Dukungan Command Line (CLI)",
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(16, 14),
            Size = new Size(ContentInnerWidth - 32, 20)
        };
        var cliText = _cliText = new Label
        {
            Text = "DisplayFlip.exe --mirror              Tukar susunan kiri-kanan\n"
                 + "DisplayFlip.exe --apply \"<Nama>\"      Terapkan profil tersimpan\n"
                 + "DisplayFlip.exe --list [--json]       Daftar profil tersimpan\n"
                 + "DisplayFlip.exe --tray                Mulai langsung di system tray",
            Font = FluentTheme.MonoFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(16, 40),
            Size = new Size(ContentInnerWidth - 32, 82)
        };
        cliCard.Controls.Add(cliTitle);
        cliCard.Controls.Add(cliText);

        _aboutPage.Controls.Add(aboutTitle);
        _aboutPage.Controls.Add(aboutCard);
        _aboutPage.Controls.Add(cliCard);

        _contentHost.Controls.Add(_profilePage);
        _contentHost.Controls.Add(_shortcutPage);
        _contentHost.Controls.Add(_generalPage);
        _contentHost.Controls.Add(_aboutPage);

        _root = new Panel { Size = new Size(RootWidth, RootHeight), BackColor = pal.ShellBackground };
        _root.Controls.Add(_titleBar);
        _root.Controls.Add(_navPanel);
        _root.Controls.Add(_contentHost);
        _root.Controls.Add(_statusBar);
        Controls.Add(_root);
        _root.Dock = DockStyle.Fill;

        ApplyScrollTheme(_profilePage);
        ApplyScrollTheme(_shortcutListPanel);
        Resize += (_, _) => { if (_layoutReady) UpdateShellLayout(); };
        _layoutReady = true;
        SwitchTab("Profil");
        RefreshData(resetCanvasToLive: true);
        UpdateShellLayout();
    }

    // =========================================================================
    // RESPONSIVE: jendela dapat di-resize / maximize; halaman Layar menata ulang diri.
    // =========================================================================
    private int ProfileRowWidth()
    {
        return Math.Max(300, _profileListPanel.ClientSize.Width);
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE)
        {
            if (m.WParam != IntPtr.Zero)
            {
                if (WindowState == FormWindowState.Maximized || IsZoomed(Handle))
                {
                    var hMon = MonitorFromWindow(Handle, MONITOR_DEFAULTTONEAREST);
                    var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    if (GetMonitorInfo(hMon, ref mi))
                    {
                        var rc = Marshal.PtrToStructure<RECT>(m.LParam);
                        rc.Left = mi.rcWork.Left;
                        rc.Top = mi.rcWork.Top;
                        rc.Right = mi.rcWork.Right;
                        rc.Bottom = mi.rcWork.Bottom;
                        Marshal.StructureToPtr(rc, m.LParam, false);
                    }
                }
            }
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_ENTERSIZEMOVE)
        {
            _inSizeMove = true;
            base.WndProc(ref m);
            return;
        }

        if (m.Msg == WM_EXITSIZEMOVE)
        {
            _inSizeMove = false;
            base.WndProc(ref m);
            UpdateShellLayout();
            return;
        }

        if (m.Msg == WM_NCLBUTTONUP && m.WParam.ToInt32() == HTMAXBUTTON)
        {
            ToggleMaximize();
            m.Result = IntPtr.Zero;
            return;
        }

        if (m.Msg == WM_NCMOUSELEAVE)
        {
            _trackingNcMouseLeave = false;
            if (_maxBtn != null) _maxBtn.Hovered = false;
            base.WndProc(ref m);
            return;
        }

        if (m.Msg == WM_NCHITTEST)
        {
            long lp = m.LParam.ToInt64();
            int screenX = unchecked((short)(lp & 0xFFFF));
            int screenY = unchecked((short)((lp >> 16) & 0xFFFF));
            var pt = PointToClient(new Point(screenX, screenY));

            int hit = HitTestPoint(pt);
            if (_maxBtn != null)
            {
                bool isMaxHover = (hit == HTMAXBUTTON);
                _maxBtn.Hovered = isMaxHover;
                if (isMaxHover && !_trackingNcMouseLeave)
                {
                    _trackingNcMouseLeave = true;
                    var tme = new TRACKMOUSEEVENT
                    {
                        cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>(),
                        dwFlags = TME_NONCLIENT | TME_LEAVE,
                        hwndTrack = Handle
                    };
                    TrackMouseEvent(ref tme);
                }
            }
            if (hit != HTCLIENT)
            {
                m.Result = (IntPtr)hit;
                return;
            }
        }

        base.WndProc(ref m);
    }

    private int HitTestPoint(Point pt)
    {
        int w = _root != null ? _root.ClientSize.Width : ClientSize.Width;
        int h = _root != null ? _root.ClientSize.Height : ClientSize.Height;
        bool isMax = WindowState == FormWindowState.Maximized || IsZoomed(Handle);

        if (!isMax)
        {
            const int grip = 8;
            bool left = pt.X < grip;
            bool right = pt.X >= w - grip;
            bool top = pt.Y < grip;
            bool bottom = pt.Y >= h - grip;

            if (top && left) return HTTOPLEFT;
            if (top && right) return HTTOPRIGHT;
            if (bottom && left) return HTBOTTOMLEFT;
            if (bottom && right) return HTBOTTOMRIGHT;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (bottom) return HTBOTTOM;
            if (top)
            {
                // Top border: if over buttons, only top 4px is resize grip,
                // allowing buttons full usability. Everywhere else on titlebar, full 8px grip.
                bool overButtons = _minBtn != null && pt.X >= _minBtn.Left;
                if (!overButtons || pt.Y < 4)
                    return HTTOP;
            }
        }

        if (_titleBar != null && pt.Y >= 0 && pt.Y < TitleBarHeight)
        {
            if (_closeBtn != null && pt.X >= _closeBtn.Left && pt.X < _closeBtn.Right)
                return HTCLIENT;
            if (_maxBtn != null && pt.X >= _maxBtn.Left && pt.X < _maxBtn.Right)
                return HTMAXBUTTON;
            if (_minBtn != null && pt.X >= _minBtn.Left && pt.X < _minBtn.Right)
                return HTCLIENT;

            return HTCAPTION;
        }

        return HTCLIENT;
    }

    private void UpdateShellLayout()
    {
        if (!_layoutReady) return;

        int clientW = _root != null ? _root.ClientSize.Width : ClientSize.Width;
        int clientH = _root != null ? _root.ClientSize.Height : ClientSize.Height;
        if (clientW <= 0 || clientH <= 0) return;

        var bp = GetBreakpoint(clientW);
        bool isCompact = bp == WindowBreakpoint.Compact;
        bool isWide = bp == WindowBreakpoint.Wide;
        int navW = isCompact ? 52 : (isWide ? 232 : 216);

        _titleBar.SetBounds(0, 0, clientW, TitleBarHeight);
        _closeBtn.Location = new Point(clientW - 46, 0);
        _maxBtn.Location = new Point(clientW - 46 * 2, 0);
        _minBtn.Location = new Point(clientW - 46 * 3, 0);

        _statusBar.SetBounds(0, clientH - StatusBarHeight, clientW, StatusBarHeight);

        int navH = clientH - TitleBarHeight - StatusBarHeight;
        _navPanel.SetBounds(0, TitleBarHeight, navW, navH);

        foreach (var (key, btn) in _navButtons)
        {
            btn.CompactMode = isCompact;
            int btnW = isCompact ? 40 : (navW - 12);
            int btnX = isCompact ? (navW - btnW) / 2 : 6;
            btn.SetBounds(btnX, btn.Top, btnW, 40);
            _navToolTip.SetToolTip(btn, isCompact ? btn.Label : null);
        }

        int hostW = clientW - navW;
        int hostH = clientH - TitleBarHeight - StatusBarHeight;
        _contentHost.SetBounds(navW, TitleBarHeight, hostW, hostH);

        if (!_inSizeMove && hostW > 16 && hostH > 16 && (_lastHostSize.Width != hostW || _lastHostSize.Height != hostH))
        {
            _lastHostSize = new Size(hostW, hostH);
            using var path = new GraphicsPath();
            path.AddArc(0, 0, 16, 16, 180, 90);
            path.AddLine(8, 0, hostW, 0);
            path.AddLine(hostW, 0, hostW, hostH);
            path.AddLine(hostW, hostH, 0, hostH);
            path.AddLine(0, hostH, 0, 8);
            path.CloseFigure();
            _contentHost.Region?.Dispose();
            _contentHost.Region = new Region(path);
        }

        if (_inSizeMove)
        {
            switch (_activeTab)
            {
                case "Profil": LayoutProfilePage(); break;
                case "Pintasan": LayoutShortcutPage(); break;
                case "Umum": LayoutGeneralPage(); break;
                case "Tentang": LayoutAboutPage(); break;
            }
        }
        else
        {
            LayoutProfilePage();
            LayoutShortcutPage();
            LayoutGeneralPage();
            LayoutAboutPage();
        }
    }

    private void LayoutProfilePage()
    {
        if (!_layoutReady || _monHeader == null || _monSubHeader == null || _profHeader == null
            || _noteLabel == null)
            return;

        int pageW = _profilePage.ClientSize.Width;
        int pageH = _profilePage.ClientSize.Height;
        if (pageW <= 0 || pageH <= 0)
            return;

        int padL = ContentPadLeft;
        int inner = Math.Max(380, pageW - padL - ContentPadRight);
        int clientW = _root != null ? _root.ClientSize.Width : ClientSize.Width;
        var bp = GetBreakpoint(clientW);
        bool isWide = bp == WindowBreakpoint.Wide;
        bool isCompact = bp == WindowBreakpoint.Compact;

        _profilePage.SuspendLayout();

        int canvasW = isWide ? (int)(inner * 0.57) : inner;
        int canvasH = isWide ? 264 : (isCompact ? 196 : 218);
        int barY = canvasH - 42;
        int btnH = 28;
        int btnY = barY + 7;

        // Auto-apply checkbox on left: adapt label to available canvas width
        string checkText;
        int checkW;
        if (canvasW >= 740)
        {
            checkText = "Terapkan langsung saat geser";
            checkW = 195;
        }
        else if (canvasW >= 560)
        {
            checkText = "Terapkan langsung";
            checkW = 132;
        }
        else
        {
            checkText = "Auto-terapkan";
            checkW = 106;
        }
        _autoApplyCheck.Text = checkText;
        _autoApplyCheck.SetBounds(14, barY + 9, checkW, 24);
        int leftEnd = _autoApplyCheck.Right;

        if (_hasUnappliedCanvasChanges)
        {
            _resetCanvasBtn.Visible = true;
            _applyCanvasBtn.Visible = true;
            int resetW = 58;
            int applyW = canvasW < 600 ? 76 : (canvasW < 750 ? 88 : 124);
            _resetCanvasBtn.Text = "Reset";
            _applyCanvasBtn.Text = canvasW < 750 ? "Terapkan" : "Terapkan Susunan";
            _resetCanvasBtn.SetBounds(leftEnd + 6, btnY, resetW, btnH);
            _applyCanvasBtn.SetBounds(_resetCanvasBtn.Right + 6, btnY, applyW, btnH);
            leftEnd = _applyCanvasBtn.Right;
        }
        else
        {
            _resetCanvasBtn.Visible = false;
            _applyCanvasBtn.Visible = false;
        }

        // Remaining space for right action buttons
        int availRightSpace = (canvasW - 12) - (leftEnd + 8);
        bool cloned = _canvasMonitors.Count >= 2 &&
            _canvasMonitors[0].Position.X == _canvasMonitors[1].Position.X &&
            _canvasMonitors[0].Position.Y == _canvasMonitors[1].Position.Y;

        int multiW, alignW, mirrorW, identW;
        if (availRightSpace >= 470)
        {
            multiW = 144;
            _multiScreenBtn.Text = _canvasMonitors.Count <= 1 ? "1 Layar Terdeteksi" : (cloned ? "Multi-layar: Duplikat \u25BE" : "Multi-layar: Perluas \u25BE");
            alignW = 96;
            _alignBtn.Text = "Sejajarkan";
            mirrorW = 124;
            _mirrorBtn.Text = "Tukar Kiri-Kanan";
            identW = 96;
            _identifyBtn.Text = "Identifikasi";
        }
        else if (availRightSpace >= 340)
        {
            multiW = 106;
            _multiScreenBtn.Text = _canvasMonitors.Count <= 1 ? "1 Layar" : (cloned ? "Duplikat \u25BE" : "Multi-layar \u25BE");
            alignW = 80;
            _alignBtn.Text = "Sejajarkan";
            mirrorW = 64;
            _mirrorBtn.Text = "Tukar";
            identW = 84;
            _identifyBtn.Text = "Identifikasi";
        }
        else
        {
            multiW = 82;
            _multiScreenBtn.Text = cloned ? "Duplikat \u25BE" : "Perluas \u25BE";
            alignW = 32;
            _alignBtn.Text = string.Empty;
            mirrorW = 32;
            _mirrorBtn.Text = string.Empty;
            identW = 32;
            _identifyBtn.Text = string.Empty;
        }

        int rx = canvasW - 12;
        rx -= multiW;
        _multiScreenBtn.SetBounds(rx, btnY, multiW, btnH);

        rx -= (alignW + 5);
        _alignBtn.SetBounds(rx, btnY, alignW, btnH);

        rx -= (mirrorW + 5);
        _mirrorBtn.SetBounds(rx, btnY, mirrorW, btnH);

        rx -= (identW + 5);
        _identifyBtn.SetBounds(rx, btnY, identW, btnH);

        int listNeededH = Math.Max(70, _profiles.Count * 64 + (_profiles.Count == 0 ? 0 : 4));

        if (isWide)
        {
            // === 2-KOLOM (MODE WIDE >= 1160px) ===
            int leftW = canvasW;
            int rightW = inner - leftW - 20;
            int rxCol = padL + leftW + 20;

            // Kolom Kiri
            _monHeader.SetBounds(padL, 8, leftW, 28);
            _monSubHeader.SetBounds(padL, 38, leftW, 18);
            _monSubHeader.Text = "Pilih layar untuk mengubah pengaturannya. Mode layar lebar menampilkan tata letak dan daftar profil secara berdampingan.";

            int ly = 60;
            if (_singleDisplayBanner.Visible)
            {
                _singleDisplayBanner.SetBounds(padL, ly, leftW, 34);
                _singleDisplayLabel.SetBounds(12, 6, Math.Max(100, leftW - 180), 22);
                _restoreExtendBtn.SetBounds(leftW - 170, 4, 160, 26);
                ly += 34 + 8;
            }

            _monitorCanvas.SetBounds(padL, ly, leftW, canvasH);
            ly += canvasH + 12;

            bool compactCards = leftW < 580;
            _cardIdentity.SetBounds(padL, ly, leftW, 60);
            LayoutCardIdentity(leftW, isCompact: compactCards);
            ly += 60 + 6;

            _cardQuickMove.SetBounds(padL, ly, leftW, 60);
            LayoutCardQuickMove(leftW, isCompact: compactCards);
            ly += 60 + 6;

            _cardResOrient.SetBounds(padL, ly, leftW, 60);
            LayoutCardResOrient(leftW, isCompact: compactCards);
            ly += 60;

            // Kolom Kanan
            int ry = 8;
            _profHeader.Text = "Profil tersimpan & setup cepat";
            _profHeader.SetBounds(rxCol, ry, rightW - 136, 28);
            _newProfileBtn.SetBounds(rxCol + rightW - 132, ry - 2, 132, 30);
            ry += 38;

            _profileListPanel.SetBounds(rxCol, ry, rightW, listNeededH);
            ry += listNeededH + 10;

            _noteLabel.Text = "\uE946 Tips: Klik [Kanvas] pada profil untuk memodifikasi susunan monitor profil di kanvas kiri tanpa mengubah layar fisik Anda terlebih dahulu.";
            _noteLabel.SetBounds(rxCol, ry, rightW, 48);
            ry += 48 + 12;

            int totalH = Math.Max(ly, ry) + 16;
            _profilePage.AutoScrollMinSize = new Size(0, totalH);
        }
        else
        {
            // === 1-KOLOM (MODE NORMAL / COMPACT < 1160px) ===
            int colW = inner;
            _monHeader.SetBounds(padL, 8, colW, 28);
            _monSubHeader.SetBounds(padL, 38, colW, 18);
            _monSubHeader.Text = isCompact
                ? "Geser layar di kanvas untuk mengubah susunan posisi."
                : "Pilih layar untuk mengubah pengaturannya. Geser (drag) layar untuk mengatur posisi.";

            int y = 60;
            if (_singleDisplayBanner.Visible)
            {
                _singleDisplayBanner.SetBounds(padL, y, colW, 34);
                _singleDisplayLabel.SetBounds(12, 6, Math.Max(100, colW - 180), 22);
                _restoreExtendBtn.SetBounds(colW - 170, 4, 160, 26);
                y += 34 + 8;
            }

            _monitorCanvas.SetBounds(padL, y, colW, canvasH);
            y += canvasH + 10;

            bool compactCards = colW < 580 || isCompact;
            _cardIdentity.SetBounds(padL, y, colW, 58);
            LayoutCardIdentity(colW, compactCards);
            y += 58 + 6;

            _cardQuickMove.SetBounds(padL, y, colW, 58);
            LayoutCardQuickMove(colW, compactCards);
            y += 58 + 6;

            _cardResOrient.SetBounds(padL, y, colW, 58);
            LayoutCardResOrient(colW, compactCards);
            y += 58 + 16;

            _profHeader.Text = "Profil tersimpan";
            _profHeader.SetBounds(padL, y + 2, colW - 136, 26);
            _newProfileBtn.SetBounds(padL + colW - 132, y - 2, 132, 30);
            y += 36;

            _profileListPanel.SetBounds(padL, y, colW, listNeededH);
            y += listNeededH + 10;

            _noteLabel.Text = "Tips: Klik tombol [Kanvas] pada profil untuk mengedit susunan profil langsung di kanvas tanpa mengubah layar dulu.";
            _noteLabel.SetBounds(padL, y, colW, 24);
            y += 24 + 16;

            _profilePage.AutoScrollMinSize = new Size(0, y);
        }

        _profilePage.HorizontalScroll.Visible = false;
        _profilePage.HorizontalScroll.Enabled = false;
        if (_profilePage.IsHandleCreated) ShowScrollBar(_profilePage.Handle, 0, false);
        _profilePage.ResumeLayout(true);
        _monitorCanvas.Invalidate();

        int rw = ProfileRowWidth();
        if (rw != _lastProfileRowWidth)
        {
            bool oldCompact = _lastProfileRowWidth < 520;
            bool newCompact = rw < 520;
            if (oldCompact != newCompact && !_inSizeMove)
            {
                RebuildProfileRows();
            }
            else
            {
                RelayoutProfileRows(rw);
            }
        }
        else if (_profiles.Count == 0)
        {
            _emptyProfileLabel.Width = Math.Max(100, _profileListPanel.ClientSize.Width);
        }
    }

    private void RelayoutProfileRows(int rowWidth)
    {
        _lastProfileRowWidth = rowWidth;
        bool compactRow = rowWidth < 520;
        _profileListPanel.SuspendLayout();
        foreach (Control row in _profileListPanel.Controls)
        {
            if (row == _emptyProfileLabel)
            {
                row.Width = Math.Max(100, _profileListPanel.ClientSize.Width);
                continue;
            }
            row.Width = rowWidth;

            var moreBtn = row.Controls.OfType<MoreButton>().FirstOrDefault();
            var dialogBtns = row.Controls.OfType<FluentDialogButton>().ToList();
            var applyBtn = dialogBtns.FirstOrDefault(b => b.Text == "Apply");
            var loadCanvasBtn = dialogBtns.FirstOrDefault(b => b.Text == "Kanvas");
            var scBtn = dialogBtns.FirstOrDefault(b => b != applyBtn && b != loadCanvasBtn);
            var labels = row.Controls.OfType<Label>().ToList();

            if (moreBtn != null)
            {
                moreBtn.Left = rowWidth - 36;
                if (applyBtn != null)
                {
                    applyBtn.Left = moreBtn.Left - 6 - applyBtn.Width;
                    if (loadCanvasBtn != null)
                    {
                        int canvasBtnW = compactRow ? 68 : 76;
                        loadCanvasBtn.SetBounds(applyBtn.Left - 6 - canvasBtnW, 13, canvasBtnW, 28);
                        int rightBoundary = loadCanvasBtn.Left;
                        if (scBtn != null)
                        {
                            scBtn.Visible = !compactRow;
                            if (!compactRow)
                            {
                                scBtn.Left = loadCanvasBtn.Left - 6 - scBtn.Width;
                                rightBoundary = scBtn.Left;
                            }
                        }
                        int textMaxW = Math.Max(80, rightBoundary - 14 - 8);
                        foreach (var lbl in labels)
                        {
                            lbl.Width = textMaxW;
                        }
                    }
                }
            }
        }
        _profileListPanel.ResumeLayout(true);
    }

    private void LayoutCardIdentity(int cardW, bool isCompact)
    {
        const int cardH = 58;
        int soloW = isCompact ? 116 : 126;
        int checkW = isCompact ? 116 : 160;
        _soloDisplayBtn.SetBounds(cardW - 14 - soloW, (cardH - 28) / 2, soloW, 28);
        _makePrimaryCheck.SetBounds(_soloDisplayBtn.Left - 8 - checkW, (cardH - 24) / 2, checkW, 24);
        _makePrimaryCheck.Text = isCompact ? "Layar utama" : "Layar utama (Primary)";
        int textW = Math.Max(100, _makePrimaryCheck.Left - 50);
        _selectedMonTitle.SetBounds(44, 10, textW, 20);
        _selectedMonSub.SetBounds(44, 30, textW, 18);
    }

    private void LayoutCardQuickMove(int cardW, bool isCompact)
    {
        const int cardH = 58;
        int bw1 = isCompact ? 50 : 66;
        int bw2 = isCompact ? 58 : 74;
        int bw3 = isCompact ? 50 : 68;
        int bw4 = isCompact ? 58 : 76;
        _moveLeftBtn.Text = isCompact ? "Kiri" : "Ke Kiri";
        _moveRightBtn.Text = isCompact ? "Kanan" : "Ke Kanan";
        _moveTopBtn.Text = isCompact ? "Atas" : "Ke Atas";
        _moveBottomBtn.Text = isCompact ? "Bawah" : "Ke Bawah";

        int rx = cardW - 14;
        int btnY = (cardH - 28) / 2;
        rx -= bw4; _moveBottomBtn.SetBounds(rx, btnY, bw4, 28);
        rx -= (5 + bw3); _moveTopBtn.SetBounds(rx, btnY, bw3, 28);
        rx -= (5 + bw2); _moveRightBtn.SetBounds(rx, btnY, bw2, 28);
        rx -= (5 + bw1); _moveLeftBtn.SetBounds(rx, btnY, bw1, 28);

        _quickMoveTitle.Text = isCompact ? "Pindah cepat posisi" : "Pindah cepat posisi layar";
        _quickMoveSub.Text = isCompact ? "Relatif terhadap layar lain" : "Pindahkan relatif terhadap layar lainnya tanpa drag manual";
        int textW = Math.Max(100, rx - 50);
        _quickMoveTitle.SetBounds(44, 10, textW, 20);
        _quickMoveSub.SetBounds(44, 30, textW, 18);
    }

    private void LayoutCardResOrient(int cardW, bool isCompact)
    {
        const int cardH = 58;
        int oriW = isCompact ? 104 : 126;
        int resW = isCompact ? 136 : 180;
        int dropY = (cardH - 28) / 2;

        int oriX = cardW - 14 - oriW;
        _orientationCombo.SetBounds(oriX, dropY, oriW, 28);
        int resX = oriX - 8 - resW;
        _resolutionCombo.SetBounds(resX, dropY, resW, 28);

        _resOrientTitle.Text = isCompact ? "Resolusi & orientasi" : "Resolusi & orientasi tampilan";
        _resOrientSub.Text = isCompact ? "Ukuran piksel & putaran" : "Sesuaikan ukuran piksel dan orientasi putaran";
        int textW = Math.Max(100, resX - 50);
        _resOrientTitle.SetBounds(44, 10, textW, 20);
        _resOrientSub.SetBounds(44, 30, textW, 18);
    }

    private void LayoutShortcutPage()
    {
        if (!_layoutReady || _scTitle == null || _scSub == null || _shortcutListPanel == null)
            return;

        int pageW = _shortcutPage.ClientSize.Width;
        int pageH = _shortcutPage.ClientSize.Height;
        if (pageW <= 0 || pageH <= 0)
            return;

        int padL = ContentPadLeft;
        int inner = Math.Max(380, pageW - padL - ContentPadRight);

        _scTitle.SetBounds(padL, 16, inner, 30);
        _scSub.SetBounds(padL, 48, inner, 22);

        int listH = Math.Max(200, pageH - 80 - 16);
        _shortcutListPanel.SetBounds(padL, 80, inner, listH);

        int cardW = Math.Max(300, _shortcutListPanel.ClientSize.Width - 4);
        foreach (Control c in _shortcutListPanel.Controls)
        {
            if (c is Panel card)
            {
                card.Width = cardW;
                foreach (Control sub in card.Controls)
                {
                    if (sub is FluentDialogButton btn)
                        btn.Location = new Point(cardW - btn.Width - 12, 12);
                    else if (sub is Label lbl && lbl.Location.X <= 20)
                        lbl.Width = Math.Max(120, cardW - 140);
                }
            }
            else if (c is Label hLbl)
            {
                hLbl.Width = cardW;
            }
        }
    }

    private void LayoutGeneralPage()
    {
        if (!_layoutReady || _genTitle == null || _iconSectionTitle == null
            || _iconCardFluent == null || _iconCardAccent == null || _iconCardMinimal == null
            || _startupCard == null || _storageCard == null)
            return;

        int pageW = _generalPage.ClientSize.Width;
        if (pageW <= 0) return;

        int padL = ContentPadLeft;
        int inner = Math.Max(380, pageW - padL - ContentPadRight);

        _genTitle.SetBounds(padL, 16, inner, 30);
        _iconSectionTitle.SetBounds(padL, 54, inner, 20);

        int y;
        if (inner >= 660)
        {
            int cardW = (inner - 16) / 3;
            _iconCardFluent.SetBounds(padL, 80, cardW, 134);
            _iconCardAccent.SetBounds(padL + cardW + 8, 80, cardW, 134);
            _iconCardMinimal.SetBounds(padL + (cardW + 8) * 2, 80, inner - (cardW + 8) * 2, 134);
            y = 80 + 134 + 14;
        }
        else
        {
            _iconCardFluent.SetBounds(padL, 80, inner, 76);
            _iconCardAccent.SetBounds(padL, 164, inner, 76);
            _iconCardMinimal.SetBounds(padL, 248, inner, 76);
            y = 248 + 76 + 14;
        }

        _startupCard.SetBounds(padL, y, inner, 118);
        _startupCheck.SetBounds(16, 14, inner - 32, 24);
        _startupSub.SetBounds(42, 38, inner - 58, 18);
        _autoApplyGeneralCheck.SetBounds(16, 66, inner - 32, 24);
        _autoApplySub.SetBounds(42, 90, inner - 58, 18);
        y += 118 + 14;

        _storageCard.SetBounds(padL, y, inner, 88);
        _storageTitle.SetBounds(16, 14, Math.Max(100, inner - 130), 20);
        _storagePath.SetBounds(16, 38, Math.Max(100, inner - 130), 34);
        _openFolderBtn.Location = new Point(inner - 108, 28);
        y += 88 + 14;

        _generalPage.AutoScrollMinSize = new Size(0, y);
    }

    private void LayoutAboutPage()
    {
        if (!_layoutReady || _aboutTitle == null || _aboutCard == null || _cliCard == null)
            return;

        int pageW = _aboutPage.ClientSize.Width;
        if (pageW <= 0) return;

        int padL = ContentPadLeft;
        int inner = Math.Max(380, pageW - padL - ContentPadRight);

        _aboutTitle.SetBounds(padL, 16, inner, 32);
        _aboutCard.SetBounds(padL, 60, inner, 118);
        _aboutName.SetBounds(16, 16, inner - 32, 22);
        _aboutDesc.SetBounds(16, 44, inner - 32, 56);

        _cliCard.SetBounds(padL, 192, inner, 136);
        _cliTitle.SetBounds(16, 14, inner - 32, 20);
        _cliText.SetBounds(16, 40, inner - 32, 82);

        _aboutPage.AutoScrollMinSize = new Size(0, 192 + 136 + 16);
    }

    private static Panel CreateSettingsCardPanel(char glyph)
    {
        var p = new BufferedPanel
        {
            BackColor = FluentTheme.Current.Surface
        };
        p.Paint += (_, pe) =>
        {
            var pal = FluentTheme.Current;
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            using var path = FluentMenu.RoundedRect(r, 6);
            using var brush = new SolidBrush(pal.Surface);
            using var pen = new Pen(pal.Divider, 1);
            g.FillPath(brush, path);
            g.DrawPath(pen, path);

            var iconBmp = FluentIcons.Render(glyph, 16, pal.TextSecondary);
            int iconY = (p.Height - 16) / 2;
            g.DrawImage(iconBmp, 14, iconY);
        };
        return p;
    }

    private void OnTitleBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, 0);
        }
    }

    public void UpdateAppIcon(Icon? appIcon)
    {
        if (appIcon == null)
            return;
        Icon = appIcon;
        using var sized = new Icon(appIcon, 20, 20);
        var old = _titleLogo.Image;
        _titleLogo.Image = sized.ToBitmap();
        old?.Dispose();
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int CS_DROPSHADOW = 0x00020000;
            const int WS_THICKFRAME = 0x00040000;
            const int WS_MAXIMIZEBOX = 0x00010000;
            const int WS_MINIMIZEBOX = 0x00020000;
            var cp = base.CreateParams;
            cp.ClassStyle |= CS_DROPSHADOW;
            cp.Style |= WS_THICKFRAME | WS_MAXIMIZEBOX | WS_MINIMIZEBOX;
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
        TryEnableMica();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        bool max = WindowState == FormWindowState.Maximized;
        if (_maxBtn != null)
            _maxBtn.Glyph = max ? "\uE923" : "\uE922";
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

    private void TryEnableMica()
    {
        try
        {
            int dark = FluentTheme.IsDarkMode() ? 1 : 0;
            DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
            int mica = 2;
            DwmSetWindowAttribute(Handle, 38, ref mica, sizeof(int));
            int noBorder = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE: tanpa garis aksen
            DwmSetWindowAttribute(Handle, 34, ref noBorder, sizeof(int));
        }
        catch
        {
            // Fallback ke latar solid bila DWM Mica tidak didukung.
        }
    }

    public void RefreshData(bool resetCanvasToLive = true)
    {
        try
        {
            _liveMonitors = _host.GetCurrentMonitors();
            var settings = _host.GetSettings();
            _profiles = settings.Profiles;

            if (resetCanvasToLive || _canvasMonitors.Count == 0)
                ResetCanvasToLive();

            _mirrorBtn.Enabled = _liveMonitors.Count >= 2;
            _alignBtn.Enabled = _canvasMonitors.Count >= 2;

            bool wasBannerVisible = _singleDisplayBanner.Visible;
            _singleDisplayBanner.Visible = _liveMonitors.Count == 1;
            if (_singleDisplayBanner.Visible != wasBannerVisible)
                LayoutProfilePage();

            _updatingUi = true;
            _startupCheck.Checked = StartupRegistry.IsEnabled();
            _autoApplyCheck.Checked = settings.AutoApplyDrag;
            _autoApplyGeneralCheck.Checked = settings.AutoApplyDrag;
            _updatingUi = false;

            foreach (var kv in _iconCards)
                kv.Value.Invalidate();

            RebuildProfileRows();
            RebuildShortcutRows();
            UpdateInspectorUi();
            _monitorCanvas.Invalidate();
            _statusBar.Invalidate();
        }
        catch (Exception ex)
        {
            SetStatusMessage($"Gagal memuat data: {ex.Message}");
        }
    }

    private void ShowMultiScreenMenu()
    {
        var menu = new FluentMenu(188);

        bool isMultiActive = _liveMonitors.Count >= 2;
        bool isClone = false;
        if (isMultiActive)
        {
            var first = _liveMonitors[0];
            isClone = _liveMonitors.Skip(1).All(m => m.Position.X == first.Position.X && m.Position.Y == first.Position.Y);
        }
        bool isExtend = isMultiActive && !isClone;
        bool isSolo1 = false;
        bool isSolo2 = false;
        if (_liveMonitors.Count == 1)
        {
            var single = _liveMonitors[0];
            int dispNum = int.TryParse(IdentifyOverlayForm.ExtractDisplayNumber(single.StableKey, single.FriendlyName, 1), out int dn) ? dn : 1;
            if (dispNum == 2 || single.FriendlyName.Contains("DISPLAY2", StringComparison.OrdinalIgnoreCase) || single.StableKey.Contains("DISPLAY2", StringComparison.OrdinalIgnoreCase))
                isSolo2 = true;
            else
                isSolo1 = true;
        }

        var extendItem = new FluentMenuItem("Perluas (Extend)", 30)
        {
            Glyph = FluentIcons.DualDisplay,
            Width = 176,
            Radio = true,
            RadioChecked = isExtend
        };
        extendItem.Click += (_, _) =>
        {
            SetStatusMessage(_host.TriggerProjectionMode("extend"));
            RefreshData(resetCanvasToLive: true);
        };
        menu.AddItem(extendItem);

        var solo1Item = new FluentMenuItem("Hanya Layar 1", 30)
        {
            Width = 176,
            Radio = true,
            RadioChecked = isSolo1
        };
        solo1Item.Click += (_, _) =>
        {
            SetStatusMessage(_host.TriggerSoloDisplay(1));
            RefreshData(resetCanvasToLive: true);
        };
        menu.AddItem(solo1Item);

        var solo2Item = new FluentMenuItem("Hanya Layar 2", 30)
        {
            Width = 176,
            Radio = true,
            RadioChecked = isSolo2
        };
        solo2Item.Click += (_, _) =>
        {
            SetStatusMessage(_host.TriggerSoloDisplay(2));
            RefreshData(resetCanvasToLive: true);
        };
        menu.AddItem(solo2Item);

        var cloneItem = new FluentMenuItem("Duplikat (Clone)", 30)
        {
            Width = 176,
            Radio = true,
            RadioChecked = isClone
        };
        cloneItem.Click += (_, _) =>
        {
            SetStatusMessage(_host.TriggerProjectionMode("clone"));
            RefreshData(resetCanvasToLive: true);
        };
        menu.AddItem(cloneItem);

        menu.Closed += (_, _) => BeginInvoke(new Action(menu.Dispose));
        menu.Show(_multiScreenBtn, new Point(0, _multiScreenBtn.Height + 2));
    }

    private void ResetCanvasToLive()
    {
        _canvasMonitors = _liveMonitors.Select(m => m with { }).ToList();
        var pri = _canvasMonitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                  ?? _canvasMonitors.FirstOrDefault();
        _canvasPrimaryKey = pri?.StableKey;
        if (_selectedMonitorKey == null || !_canvasMonitors.Any(m => m.StableKey == _selectedMonitorKey))
            _selectedMonitorKey = _canvasPrimaryKey;
        _loadedProfileName = null;
        _hasUnappliedCanvasChanges = false;
        _resetCanvasBtn.Enabled = false;
        UpdateInspectorUi();
        _monitorCanvas.Invalidate();
    }

    private async Task ApplyCanvasArrangementAsync()
    {
        if (_canvasMonitors.Count == 0)
            return;
        _applyCanvasBtn.Enabled = false;
        var (ok, msg) = await _host.TriggerApplyArrangementAsync(_canvasMonitors, _canvasPrimaryKey);
        _applyCanvasBtn.Enabled = true;
        SetStatusMessage(msg);
        if (ok)
            RefreshData(resetCanvasToLive: true);
    }

    private async void OnAlignClickedAsync(object? sender, EventArgs e)
    {
        if (_canvasMonitors.Count < 2)
            return;
        _canvasMonitors = DisplayTopology.AlignMonitorsVertically(
            _canvasMonitors, VerticalAlignment.Top, _canvasPrimaryKey);
        _hasUnappliedCanvasChanges = true;
        _resetCanvasBtn.Enabled = true;
        _monitorCanvas.Invalidate();
        UpdateInspectorUi();
        if (_autoApplyCheck.Checked)
            await ApplyCanvasArrangementAsync();
        else
            SetStatusMessage("Layar disejajarkan rata atas (Y=0). Klik Terapkan Susunan untuk menyimpan.");
    }

    private void OnAutoApplyCanvasCheckedChanged(object? sender, EventArgs e)
    {
        if (_updatingUi)
            return;
        _host.SetAutoApplyDrag(_autoApplyCheck.Checked);
        _updatingUi = true;
        _autoApplyGeneralCheck.Checked = _autoApplyCheck.Checked;
        _updatingUi = false;
    }

    private FluentDialogButton CreateQuickPosButton(string text, int x, int y, int w, string direction)
    {
        var btn = new FluentDialogButton
        {
            Text = text,
            IsPrimary = false,
            Location = new Point(x, y),
            Size = new Size(w, 24)
        };
        btn.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(_selectedMonitorKey) || _canvasMonitors.Count < 2)
                return;
            var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
            var others = _canvasMonitors.Where(m => m.StableKey != _selectedMonitorKey).ToList();
            if (sel == null || others.Count == 0)
                return;

            var primaryOrFirst = others.FirstOrDefault(m => m.StableKey == _canvasPrimaryKey) ?? others[0];
            int newX = sel.Position.X;
            int newY = sel.Position.Y;
            switch (direction)
            {
                case "left":
                    var leftMost = others.OrderBy(m => m.Position.X).First();
                    newX = leftMost.Position.X - sel.Position.Width;
                    newY = leftMost.Position.Y;
                    break;
                case "right":
                    var rightMost = others.OrderByDescending(m => m.Position.Right).First();
                    newX = rightMost.Position.Right;
                    newY = rightMost.Position.Y;
                    break;
                case "top":
                    newX = primaryOrFirst.Position.X;
                    newY = others.Min(m => m.Position.Y) - sel.Position.Height;
                    break;
                case "bottom":
                    newX = primaryOrFirst.Position.X;
                    newY = others.Max(m => m.Position.Bottom);
                    break;
            }
            var snapped = DisplayTopology.SnapMonitorPosition(_canvasMonitors, sel.StableKey, newX, newY, alignSnapThreshold: 64);
            _canvasMonitors = _canvasMonitors
                .Select(m => m.StableKey == sel.StableKey ? m with { Position = snapped } : m)
                .ToList();
            _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _canvasPrimaryKey);
            _hasUnappliedCanvasChanges = true;
            _resetCanvasBtn.Enabled = true;
            _monitorCanvas.Invalidate();
            UpdateInspectorUi();
            if (_autoApplyCheck.Checked)
                await ApplyCanvasArrangementAsync();
            else
                SetStatusMessage($"Layar dipindahkan ({text}). Klik Terapkan Susunan atau Simpan Profil.");
        };
        return btn;
    }

    private void UpdateInspectorUi()
    {
        _updatingUi = true;
        try
        {
            var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey)
                      ?? _canvasMonitors.FirstOrDefault();
            if (sel == null)
            {
                _selectedMonTitle.Text = "Tidak ada monitor dipilih";
                _selectedMonSub.Text = "Pilih salah satu layar pada kanvas di atas.";
                _makePrimaryCheck.Enabled = false;
                _soloDisplayBtn.Enabled = false;
                _resolutionCombo.Enabled = false;
                _orientationCombo.Enabled = false;
                _moveLeftBtn.Enabled = false;
                _moveRightBtn.Enabled = false;
                _moveTopBtn.Enabled = false;
                _moveBottomBtn.Enabled = false;
                return;
            }

            _selectedMonitorKey = sel.StableKey;
            int idx = _canvasMonitors.IndexOf(sel) + 1;
            string num = IdentifyOverlayForm.ExtractDisplayNumber(sel.StableKey, sel.FriendlyName, idx);
            string shortName = ShortDisplayName(sel.FriendlyName);
            bool isPrimary = sel.StableKey == _canvasPrimaryKey || (sel.Position.X == 0 && sel.Position.Y == 0);

            _selectedMonTitle.Text = $"Layar {num} \u2014 {shortName}";
            _selectedMonSub.Text = $"{sel.Position.Width} \u00D7 {sel.Position.Height} \u2022 Posisi ({sel.Position.X}, {sel.Position.Y})"
                + (isPrimary ? " \u2022 Layar Utama" : "");
            _makePrimaryCheck.Checked = isPrimary;
            _makePrimaryCheck.Enabled = _canvasMonitors.Count >= 1;

            if (_canvasMonitors.Count <= 1)
            {
                _soloDisplayBtn.Enabled = false;
                _soloDisplayBtn.Text = "Hanya Layar Aktif";
            }
            else
            {
                _soloDisplayBtn.Enabled = true;
                _soloDisplayBtn.Text = $"Hanya Layar {num} Saja";
            }

            int clientW = _root != null ? _root.ClientSize.Width : ClientSize.Width;
            bool isCompact = GetBreakpoint(clientW) == WindowBreakpoint.Compact || _monitorCanvas.Width < 740;
            if (_canvasMonitors.Count <= 1)
            {
                _multiScreenBtn.Enabled = false;
                _multiScreenBtn.Text = isCompact ? "1 Layar" : "1 Layar Terdeteksi";
            }
            else
            {
                _multiScreenBtn.Enabled = true;
                bool isCloned = _canvasMonitors.Count >= 2 &&
                    _canvasMonitors[0].Position.X == _canvasMonitors[1].Position.X &&
                    _canvasMonitors[0].Position.Y == _canvasMonitors[1].Position.Y;
                if (isCloned)
                {
                    _multiScreenBtn.Text = isCompact ? "Duplikat \u25BE" : "Multi-layar: Duplikat \u25BE";
                }
                else
                {
                    _multiScreenBtn.Text = isCompact ? "Multi-layar \u25BE" : "Multi-layar: Perluas \u25BE";
                }
            }

            bool multi = _canvasMonitors.Count > 1;
            _moveLeftBtn.Enabled = multi;
            _moveRightBtn.Enabled = multi;
            _moveTopBtn.Enabled = multi;
            _moveBottomBtn.Enabled = multi;

            var resolutions = _host.GetSupportedResolutions(sel);
            var resLabels = new List<string>(resolutions.Count);
            int selResIdx = 0;
            for (int i = 0; i < resolutions.Count; i++)
            {
                var (rw, rh) = resolutions[i];
                string label = i == 0 ? $"{rw} \u00D7 {rh} (Disarankan)" : $"{rw} \u00D7 {rh}";
                resLabels.Add(label);
                if (rw == sel.Position.Width && rh == sel.Position.Height)
                    selResIdx = i;
            }
            _resolutionCombo.SetItems(resLabels, selResIdx);
            _resolutionCombo.Enabled = true;

            _orientationCombo.SelectedIndex = sel.Orientation switch
            {
                DisplayFlip.Core.Orientation.Portrait => 1,
                DisplayFlip.Core.Orientation.Rotated => 2,
                DisplayFlip.Core.Orientation.Unhandled => 3,
                _ => 0
            };
            _orientationCombo.Enabled = true;
        }
        finally
        {
            _updatingUi = false;
        }
    }

    private async void OnResolutionComboChanged(object? sender, EventArgs e)
    {
        if (_updatingUi || string.IsNullOrEmpty(_selectedMonitorKey) || _resolutionCombo.SelectedItem is not string text)
            return;
        string clean = text.Replace("\u00D7", "x").Replace("X", "x");
        var parts = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int w = 0, h = 0;
        if (parts.Length >= 3 && int.TryParse(parts[0], out w) && int.TryParse(parts[2], out h))
        {
        }
        else if (parts.Length >= 1)
        {
            var subParts = parts[0].Split('x');
            if (subParts.Length == 2 && int.TryParse(subParts[0], out w) && int.TryParse(subParts[1], out h))
            {
            }
        }

        if (w > 0 && h > 0)
        {
            var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
            if (sel == null || (sel.Position.Width == w && sel.Position.Height == h))
                return;

            var oldPos = sel.Position;
            int dw = w - oldPos.Width;
            int dh = h - oldPos.Height;

            _canvasMonitors = _canvasMonitors.Select(m =>
            {
                if (m.StableKey == _selectedMonitorKey)
                {
                    return m with
                    {
                        Position = m.Position with { Width = w, Height = h },
                        ResolutionWidth = w,
                        ResolutionHeight = h
                    };
                }
                int nx = m.Position.X >= oldPos.Right ? m.Position.X + dw : m.Position.X;
                int ny = m.Position.Y >= oldPos.Bottom ? m.Position.Y + dh : m.Position.Y;
                return m with { Position = m.Position with { X = nx, Y = ny } };
            }).ToList();

            _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _canvasPrimaryKey);
            _hasUnappliedCanvasChanges = true;
            _resetCanvasBtn.Enabled = true;
            _monitorCanvas.Invalidate();
            UpdateInspectorUi();
            if (_autoApplyCheck.Checked)
                await ApplyCanvasArrangementAsync();
            else
                SetStatusMessage($"Resolusi diubah ke {w}x{h} di kanvas. Klik Terapkan Susunan untuk menerapkan.");
        }
    }

    private async void OnOrientationComboChanged(object? sender, EventArgs e)
    {
        if (_updatingUi || string.IsNullOrEmpty(_selectedMonitorKey))
            return;
        var newOrient = _orientationCombo.SelectedIndex switch
        {
            1 => DisplayFlip.Core.Orientation.Portrait,
            2 => DisplayFlip.Core.Orientation.Rotated,
            3 => DisplayFlip.Core.Orientation.Unhandled,
            _ => DisplayFlip.Core.Orientation.Landscape
        };
        var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
        if (sel == null || sel.Orientation == newOrient)
            return;

        var oldPos = sel.Position;
        bool wasPortrait = oldPos.Height > oldPos.Width;
        bool wantPortrait = newOrient is DisplayFlip.Core.Orientation.Portrait or DisplayFlip.Core.Orientation.Unhandled;
        int newW = wantPortrait != wasPortrait ? oldPos.Height : oldPos.Width;
        int newH = wantPortrait != wasPortrait ? oldPos.Width : oldPos.Height;
        int dw = newW - oldPos.Width;
        int dh = newH - oldPos.Height;

        _canvasMonitors = _canvasMonitors.Select(m =>
        {
            if (m.StableKey == _selectedMonitorKey)
            {
                return m with
                {
                    Orientation = newOrient,
                    Position = m.Position with { Width = newW, Height = newH },
                    ResolutionWidth = newW,
                    ResolutionHeight = newH
                };
            }
            int nx = m.Position.X >= oldPos.Right ? m.Position.X + dw : m.Position.X;
            int ny = m.Position.Y >= oldPos.Bottom ? m.Position.Y + dh : m.Position.Y;
            return m with { Position = m.Position with { X = nx, Y = ny } };
        }).ToList();

        _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _canvasPrimaryKey);
        _hasUnappliedCanvasChanges = true;
        _resetCanvasBtn.Enabled = true;
        _monitorCanvas.Invalidate();
        UpdateInspectorUi();
        if (_autoApplyCheck.Checked)
            await ApplyCanvasArrangementAsync();
        else
            SetStatusMessage("Orientasi diubah di kanvas. Klik Terapkan Susunan untuk menerapkan.");
    }

    private void SetStatusMessage(string message)
    {
        _statusMessage = message;
        _statusBar.Invalidate();
    }

    private void AddNavItem(string key, string label, char glyph, int y)
    {
        var btn = new NavButton(label, glyph)
        {
            Location = new Point(6, y),
            Size = new Size(NavWidth - 12, 40)
        };
        btn.Click += (_, _) => SwitchTab(key);
        _navButtons[key] = btn;
        _navPanel.Controls.Add(btn);
    }

    private void SwitchTab(string key)
    {
        _activeTab = key;
        foreach (var kv in _navButtons)
            kv.Value.SetActive(kv.Key == key);

        _profilePage.Visible = key == "Profil";
        _shortcutPage.Visible = key == "Pintasan";
        _generalPage.Visible = key == "Umum";
        _aboutPage.Visible = key == "Tentang";
        RefreshData(resetCanvasToLive: false);
    }

    public void ExportResponsive(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var sizes = new (string Name, Size Size)[]
        {
            ("compact", new Size(720, 640)),
            ("normal", new Size(FormWidth, FormHeight)),
            ("wide", new Size(1320, 760)),
            ("min", new Size(MinWidth, MinHeight)),
            ("default", new Size(FormWidth, FormHeight))
        };
        Show();
        SwitchTab("Profil");
        foreach (var (name, size) in sizes)
        {
            Size = size;
            for (int i = 0; i < 3; i++)
                Application.DoEvents();
            UpdateShellLayout();
            LayoutProfilePage();
            Refresh();
            using var bmp = new Bitmap(Width, Height);
            DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
            bmp.Save(Path.Combine(outputDir, $"settings-v2-{name}.png"), System.Drawing.Imaging.ImageFormat.Png);
            bmp.Save(Path.Combine(outputDir, $"settings-profil-{name}.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        Hide();
    }

    public void ExportScreenshots(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        string[] tabs = { "Profil", "Pintasan", "Umum", "Tentang" };
        Show();
        Size = new Size(FormWidth, FormHeight);
        UpdateShellLayout();
        foreach (var t in tabs)
        {
            SwitchTab(t);
            Application.DoEvents();
            Refresh();
            using var bmp = new Bitmap(Width, Height);
            DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
            bmp.Save(Path.Combine(outputDir, $"settings-v2-{t.ToLowerInvariant()}.png"), System.Drawing.Imaging.ImageFormat.Png);
            bmp.Save(Path.Combine(outputDir, $"settings-{t.ToLowerInvariant()}.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        Hide();
    }

    private void RebuildProfileRows()
    {
        _profileListPanel.SuspendLayout();
        while (_profileListPanel.Controls.Count > 0)
        {
            var c = _profileListPanel.Controls[0];
            _profileListPanel.Controls.RemoveAt(0);
            c.Dispose();
        }

        if (_profiles.Count == 0)
        {
            _emptyProfileLabel.Width = Math.Max(100, _profileListPanel.ClientSize.Width);
            _profileListPanel.Controls.Add(_emptyProfileLabel);
            _profileListPanel.ResumeLayout();
            return;
        }

        var pal = FluentTheme.Current;
        int rowHeight = 56;
        int y = 0;
        int rowWidth = ProfileRowWidth();
        _lastProfileRowWidth = rowWidth;
        bool compactRow = rowWidth < 520;

        for (int i = 0; i < _profiles.Count; i++)
        {
            var prof = _profiles[i];
            var row = CreateSurfaceCard(0, y, rowWidth, rowHeight);

            string capturedName = prof.Name;
            var primaryMon = prof.Monitors.FirstOrDefault(m => m.StableKey == prof.PrimaryStableKey);
            string primaryLabel = primaryMon != null
                ? ShortDisplayName(primaryMon.FriendlyName)
                : ShortDisplayName(prof.PrimaryStableKey);
            string updatedLabel = FormatDateShort(prof.UpdatedAt);
            string meta = $"{prof.Monitors.Count} monitor \u2022 Primary: {primaryLabel}";
            if (!compactRow)
                meta += $" \u2022 diperbarui {updatedLabel}";
            else if (!string.IsNullOrWhiteSpace(prof.Shortcut))
                meta += $" \u2022 {prof.Shortcut}";

            var moreBtn = new MoreButton
            {
                Location = new Point(row.Width - 36, 13),
                Size = new Size(28, 28)
            };
            moreBtn.Click += (_, _) => ShowProfileContextMenu(moreBtn, prof);

            var applyBtn = new FluentDialogButton
            {
                Text = "Apply",
                IsPrimary = false,
                Location = new Point(moreBtn.Left - 6 - 64, 13),
                Size = new Size(64, 28)
            };
            applyBtn.Click += async (_, _) =>
            {
                var (ok, msg) = await _host.TriggerApplyProfileAsync(capturedName);
                SetStatusMessage(ok ? $"Profil diterapkan: {capturedName}" : msg);
                RefreshData(resetCanvasToLive: true);
            };

            var loadCanvasBtn = new FluentDialogButton
            {
                Text = "Kanvas",
                Glyph = FluentIcons.Edit,
                IsPrimary = false,
                Location = new Point(applyBtn.Left - 6 - (compactRow ? 68 : 76), 13),
                Size = new Size(compactRow ? 68 : 76, 28)
            };
            loadCanvasBtn.Click += (_, _) => LoadProfileIntoCanvas(prof);

            FluentDialogButton? scBtn = null;
            int rightBoundary = loadCanvasBtn.Left;
            if (!compactRow)
            {
                string scText = string.IsNullOrWhiteSpace(prof.Shortcut) ? "+ Shortcut" : prof.Shortcut;
                scBtn = new FluentDialogButton
                {
                    Text = scText,
                    Glyph = FluentIcons.Keyboard,
                    IsPrimary = false,
                    Location = new Point(loadCanvasBtn.Left - 6 - 112, 13),
                    Size = new Size(112, 28)
                };
                scBtn.Click += (_, _) =>
                {
                    var updated = ShortcutRecorderDialog.Prompt($"Profil \"{capturedName}\"", prof.Shortcut);
                    if (updated != null)
                    {
                        _host.SetProfileShortcut(capturedName, updated);
                        SetStatusMessage(string.IsNullOrEmpty(updated)
                            ? $"Pintasan dihapus untuk {capturedName}."
                            : $"Pintasan {updated} diatur untuk {capturedName}.");
                        RefreshData(resetCanvasToLive: false);
                    }
                };
                rightBoundary = scBtn.Left;
            }

            int textMaxW = Math.Max(80, rightBoundary - 14 - 8);

            var nameLbl = new Label
            {
                Text = prof.Name,
                Font = FluentTheme.BodySemiboldFont,
                ForeColor = pal.Text,
                BackColor = pal.Surface,
                Location = new Point(14, 8),
                Size = new Size(textMaxW, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var metaLbl = new Label
            {
                Text = meta,
                Font = FluentTheme.CaptionFont,
                ForeColor = pal.TextSecondary,
                BackColor = pal.Surface,
                Location = new Point(14, 30),
                Size = new Size(textMaxW, 16),
                TextAlign = ContentAlignment.MiddleLeft
            };

            row.Controls.Add(nameLbl);
            if (scBtn != null) row.Controls.Add(scBtn);
            row.Controls.Add(metaLbl);
            row.Controls.Add(loadCanvasBtn);
            row.Controls.Add(applyBtn);
            row.Controls.Add(moreBtn);
            _profileListPanel.Controls.Add(row);
            y += rowHeight + 6;
        }

        _profileListPanel.ResumeLayout();
    }

    private void LoadProfileIntoCanvas(LayoutProfile prof)
    {
        _canvasMonitors = prof.Monitors.Select(ms => new DisplayMonitor(
            StableKey: ms.StableKey,
            FriendlyName: ms.FriendlyName,
            Position: ms.Position,
            ResolutionWidth: ms.ResolutionWidth > 0 ? ms.ResolutionWidth : ms.Position.Width,
            ResolutionHeight: ms.ResolutionHeight > 0 ? ms.ResolutionHeight : ms.Position.Height,
            Orientation: ms.Orientation,
            IsEnabled: ms.IsEnabled)).ToList();
        _canvasPrimaryKey = prof.PrimaryStableKey;
        _selectedMonitorKey = prof.PrimaryStableKey;
        _loadedProfileName = prof.Name;
        _hasUnappliedCanvasChanges = true;
        _resetCanvasBtn.Enabled = true;
        _monitorCanvas.Invalidate();
        UpdateInspectorUi();
        SetStatusMessage($"Profil \"{prof.Name}\" dimuat ke kanvas. Geser layar atau klik Terapkan Susunan.");
    }

    private void ShowProfileContextMenu(Control anchor, LayoutProfile profile)
    {
        string profileName = profile.Name;
        var menu = new FluentMenu(250);

        var applyItem = new FluentMenuItem("Terapkan profil") { Glyph = FluentIcons.Play, Width = 238 };
        applyItem.Click += async (_, _) =>
        {
            var (ok, msg) = await _host.TriggerApplyProfileAsync(profileName);
            SetStatusMessage(ok ? $"Profil diterapkan: {profileName}" : msg);
            RefreshData(resetCanvasToLive: true);
        };

        var editSetupItem = new FluentMenuItem("Edit & setup profil...") { Glyph = FluentIcons.Edit, Width = 238 };
        editSetupItem.Click += (_, _) =>
        {
            var profMonitors = profile.Monitors.Select(ms => new DisplayMonitor(
                ms.StableKey, ms.FriendlyName, ms.Position,
                ms.ResolutionWidth, ms.ResolutionHeight, ms.Orientation, ms.IsEnabled)).ToList();
            if (_host.TriggerSaveNewProfile(out var saved, profMonitors, profile.PrimaryStableKey, profileName))
                SetStatusMessage($"Profil diperbarui: {saved}");
            RefreshData(resetCanvasToLive: false);
        };

        var shortcutItem = new FluentMenuItem("Atur pintasan keyboard...") { Glyph = FluentIcons.Keyboard, Width = 238 };
        shortcutItem.Click += (_, _) =>
        {
            var updated = ShortcutRecorderDialog.Prompt($"Profil \"{profileName}\"", profile.Shortcut);
            if (updated != null)
            {
                _host.SetProfileShortcut(profileName, updated);
                SetStatusMessage($"Pintasan profil {profileName} disimpan.");
                RefreshData(resetCanvasToLive: false);
            }
        };

        var updateItem = new FluentMenuItem("Perbarui dari susunan kanvas") { Glyph = FluentIcons.Save, Width = 238 };
        updateItem.Click += (_, _) =>
        {
            if (_host.TriggerOverwriteProfile(profileName, out var msg, _canvasMonitors, _canvasPrimaryKey))
                SetStatusMessage(msg);
            RefreshData(resetCanvasToLive: false);
        };

        var deleteItem = new FluentMenuItem("Hapus profil") { Glyph = FluentIcons.Delete, Width = 238 };
        deleteItem.Click += (_, _) =>
        {
            if (_host.TriggerDeleteProfile(profileName))
                SetStatusMessage($"Profil dihapus: {profileName}");
            RefreshData(resetCanvasToLive: false);
        };

        menu.AddItem(applyItem);
        menu.AddItem(editSetupItem);
        menu.AddItem(shortcutItem);
        menu.AddItem(updateItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.AddItem(deleteItem);
        menu.Closed += (_, _) => BeginInvoke(new Action(menu.Dispose));
        menu.Show(anchor, new Point(anchor.Width - 250, anchor.Height + 2));
    }

    private void RebuildShortcutRows()
    {
        _shortcutListPanel.SuspendLayout();
        while (_shortcutListPanel.Controls.Count > 0)
        {
            var c = _shortcutListPanel.Controls[0];
            _shortcutListPanel.Controls.RemoveAt(0);
            c.Dispose();
        }

        var pal = FluentTheme.Current;
        var cfg = _host.GetSettings();
        int y = 0;

        var globalHeader = new Label
        {
            Text = "Aksi Cepat Global",
            Font = FluentTheme.SubtitleFont,
            ForeColor = pal.Text,
            Location = new Point(0, y),
            Size = new Size(ContentInnerWidth, 24)
        };
        _shortcutListPanel.Controls.Add(globalHeader);
        y += 28;

        AddShortcutRow(
            "Tukar susunan kiri-kanan (Mirror Flip)",
            "Menukar posisi monitor kiri-kanan dengan menjaga monitor primary tetap.",
            cfg.MirrorShortcut,
            y,
            val => _host.SetGlobalShortcut("mirror", val));
        y += 60;

        AddShortcutRow(
            "Siklus ganti layar utama (Cycle Primary)",
            "Memindahkan status Primary ke monitor aktif berikutnya secara instan.",
            cfg.CyclePrimaryShortcut,
            y,
            val => _host.SetGlobalShortcut("primary", val));
        y += 60;

        AddShortcutRow(
            "Identifikasi nomor layar (Identify Displays)",
            "Menampilkan overlay angka besar (1, 2, dst.) pada setiap layar fisik.",
            cfg.IdentifyShortcut,
            y,
            val => _host.SetGlobalShortcut("identify", val));
        y += 60;

        AddShortcutRow(
            "Hanya Layar 1 (Solo Display 1)",
            "Hanya menampilkan layar 1 (Utama / Laptop).",
            cfg.Solo1Shortcut,
            y,
            val => _host.SetGlobalShortcut("solo1", val));
        y += 60;

        AddShortcutRow(
            "Hanya Layar 2 (Solo Display 2)",
            "Hanya menampilkan layar 2 (Eksternal).",
            cfg.Solo2Shortcut,
            y,
            val => _host.SetGlobalShortcut("solo2", val));
        y += 60;

        AddShortcutRow(
            "Perluas Multi-Layar (Extend Displays)",
            "Mengaktifkan kembali seluruh monitor dalam mode perluas (Extend).",
            cfg.ExtendShortcut,
            y,
            val => _host.SetGlobalShortcut("extend", val));
        y += 72;

        var profHeader = new Label
        {
            Text = "Pintasan Profil Tersimpan",
            Font = FluentTheme.SubtitleFont,
            ForeColor = pal.Text,
            Location = new Point(0, y),
            Size = new Size(ContentInnerWidth, 24)
        };
        _shortcutListPanel.Controls.Add(profHeader);
        y += 28;

        if (_profiles.Count == 0)
        {
            var empty = new Label
            {
                Text = "Belum ada profil tersimpan. Buat profil di tab Layar & Profil untuk mengatur pintasan profil.",
                Font = FluentTheme.BodyFont,
                ForeColor = pal.TextSecondary,
                BackColor = pal.Surface,
                Location = new Point(0, y),
                Size = new Size(ContentInnerWidth, 54),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _shortcutListPanel.Controls.Add(empty);
        }
        else
        {
            foreach (var p in _profiles)
            {
                string captured = p.Name;
                AddShortcutRow(
                    $"Terapkan Profil: {p.Name}",
                    $"{p.Monitors.Count} monitor \u2022 Primary: {ShortDisplayName(p.PrimaryStableKey)}",
                    p.Shortcut,
                    y,
                    val => _host.SetProfileShortcut(captured, val));
                y += 60;
            }
        }

        _shortcutListPanel.ResumeLayout();
    }

    private void AddShortcutRow(string title, string sub, string currentShortcut, int y, Action<string> onSave)
    {
        var pal = FluentTheme.Current;
        var card = CreateSurfaceCard(0, y, ContentInnerWidth - 18, 54);

        var tLbl = new Label
        {
            Text = title,
            Font = FluentTheme.BodySemiboldFont,
            ForeColor = pal.Text,
            BackColor = pal.Surface,
            Location = new Point(14, 8),
            Size = new Size(card.Width - 190, 18)
        };
        var sLbl = new Label
        {
            Text = sub,
            Font = FluentTheme.CaptionFont,
            ForeColor = pal.TextSecondary,
            BackColor = pal.Surface,
            Location = new Point(14, 28),
            Size = new Size(card.Width - 190, 16)
        };

        string badgeText = string.IsNullOrWhiteSpace(currentShortcut) ? "(Belum diatur)" : currentShortcut;
        var editBtn = new FluentDialogButton
        {
            Text = badgeText,
            Glyph = FluentIcons.Keyboard,
            IsPrimary = !string.IsNullOrWhiteSpace(currentShortcut),
            Location = new Point(card.Width - 156, 11),
            Size = new Size(142, 32)
        };
        editBtn.Click += (_, _) =>
        {
            var next = ShortcutRecorderDialog.Prompt(title, currentShortcut);
            if (next != null)
            {
                onSave(next);
                SetStatusMessage(string.IsNullOrEmpty(next)
                    ? $"Pintasan dihapus untuk {title}."
                    : $"Pintasan {next} disimpan untuk {title}.");
                RefreshData(resetCanvasToLive: false);
            }
        };

        card.Controls.Add(tLbl);
        card.Controls.Add(sLbl);
        card.Controls.Add(editBtn);
        _shortcutListPanel.Controls.Add(card);
    }

    private Panel CreateIconStyleCard(string styleKey, string title, string desc, int x, int y, int w)
    {
        var card = new BufferedPanel
        {
            Location = new Point(x, y),
            Size = new Size(w, 134),
            BackColor = FluentTheme.Current.Surface,
            Cursor = Cursors.Hand
        };
        card.Paint += (_, pe) =>
        {
            var pal = FluentTheme.Current;
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool isSelected = string.Equals(_host.GetSettings().IconStyle, styleKey, StringComparison.OrdinalIgnoreCase);
            var r = new Rectangle(1, 1, card.Width - 3, card.Height - 3);
            using var path = FluentMenu.RoundedRect(r, FluentTheme.RadiusItem);
            using (var bg = new SolidBrush(isSelected ? Color.FromArgb(28, pal.Accent) : pal.Surface))
                g.FillPath(bg, path);
            using (var pen = new Pen(isSelected ? pal.Accent : pal.Divider, isSelected ? 2f : 1f))
                g.DrawPath(pen, path);

            bool compact = card.Height < 100;
            if (compact)
            {
                using var bmp36 = FluentIcons.RenderModernAppBitmap(styleKey, 36, pal.Accent);
                g.DrawImage(bmp36, 14, (card.Height - 36) / 2, 36, 36);

                int textX = 58;
                int textW = Math.Max(50, card.Width - textX - (isSelected ? 56 : 14));
                TextRenderer.DrawText(g, title, FluentTheme.BodySemiboldFont,
                    new Rectangle(textX, 12, textW, 20), pal.Text,
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, desc, FluentTheme.CaptionFont,
                    new Rectangle(textX, 34, textW, 30), pal.TextSecondary,
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (isSelected)
                {
                    TextRenderer.DrawText(g, "Aktif", FluentTheme.CaptionFont,
                        new Rectangle(card.Width - 52, (card.Height - 20) / 2, 42, 20), pal.Accent,
                        TextFormatFlags.Right | TextFormatFlags.SingleLine);
                }
            }
            else
            {
                using var bmp44 = FluentIcons.RenderModernAppBitmap(styleKey, 44, pal.Accent);
                g.DrawImage(bmp44, 14, 14, 44, 44);

                using var bmp20 = FluentIcons.RenderModernAppBitmap(styleKey, 20, pal.Accent);
                g.DrawImage(bmp20, 66, 36, 20, 20);

                if (isSelected)
                {
                    TextRenderer.DrawText(g, "Aktif", FluentTheme.CaptionFont,
                        new Rectangle(card.Width - 52, 12, 42, 18), pal.Accent,
                        TextFormatFlags.Right | TextFormatFlags.SingleLine);
                }

                TextRenderer.DrawText(g, title, FluentTheme.BodySemiboldFont,
                    new Rectangle(12, 64, card.Width - 24, 20), pal.Text,
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(g, desc, FluentTheme.CaptionFont,
                    new Rectangle(12, 84, card.Width - 24, 42), pal.TextSecondary,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
        };
        card.Click += (_, _) =>
        {
            _host.SetIconStyle(styleKey);
            SetStatusMessage($"Gaya ikon diubah ke: {title}");
            foreach (var kv in _iconCards)
                kv.Value.Invalidate();
        };
        _iconCards[styleKey] = card;
        return card;
    }

    // =========================================================================
    // KANVAS INTERAKTIF MONITOR (Windows 11 System > Display + Drag & Snap 2D)
    // =========================================================================
    private void PaintMonitorCanvas(object? sender, PaintEventArgs e)
    {
        var pal = FluentTheme.Current;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(pal.WellBackground);

        var cardRect = new Rectangle(0, 0, _monitorCanvas.Width - 1, _monitorCanvas.Height - 1);
        using (var cardPath = FluentMenu.RoundedRect(cardRect, FluentTheme.RadiusMenu))
        using (var cardBrush = new SolidBrush(pal.WellBackground))
        using (var cardPen = new Pen(pal.Divider, 1))
        {
            g.FillPath(cardBrush, cardPath);
            g.DrawPath(cardPen, cardPath);
        }

        // Garis pemisah toolbar bawah kanvas
        int bottomBarY = _monitorCanvas.Height - 42;
        using (var divPen = new Pen(pal.Divider, 1))
            g.DrawLine(divPen, 1, bottomBarY, _monitorCanvas.Width - 2, bottomBarY);

        _monitorHitBoxes.Clear();
        if (_canvasMonitors.Count == 0)
        {
            TextRenderer.DrawText(g, "Tidak ada monitor aktif terdeteksi.", FluentTheme.BodyFont,
                new Rectangle(0, 0, _monitorCanvas.Width, bottomBarY), pal.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        int minX = _canvasMonitors.Min(m => m.Position.X);
        int minY = _canvasMonitors.Min(m => m.Position.Y);
        int maxR = _canvasMonitors.Max(m => m.Position.Right);
        int maxB = _canvasMonitors.Max(m => m.Position.Bottom);
        double totalW = Math.Max(1, maxR - minX);
        double totalH = Math.Max(1, maxB - minY);

        int padX = 24;
        int padY = 12;
        int availW = _monitorCanvas.Width - padX * 2;
        int availH = bottomBarY - padY * 2;
        double avgW = _canvasMonitors.Average(m => (double)m.Position.Width);
        double avgH = _canvasMonitors.Average(m => (double)m.Position.Height);
        double viewW = totalW + avgW * 0.5;
        double viewH = totalH + avgH * 0.5;
        double scale = Math.Max(0.005, Math.Min(availW / viewW, availH / viewH));

        int drawnW = (int)Math.Round(totalW * scale);
        int drawnH = (int)Math.Round(totalH * scale);
        int offsetX = padX + (availW - drawnW) / 2;
        int offsetY = padY + (availH - drawnH) / 2;

        _lastCanvasScale = scale;
        _lastCanvasMinX = minX;
        _lastCanvasMinY = minY;
        _lastCanvasOffsetX = offsetX;
        _lastCanvasOffsetY = offsetY;

        for (int i = 0; i < _canvasMonitors.Count; i++)
        {
            var m = _canvasMonitors[i];
            int dispNum = int.TryParse(IdentifyOverlayForm.ExtractDisplayNumber(m.FriendlyName, i + 1), out int dn) ? dn : (i + 1);

            int x1 = offsetX + (int)Math.Round((m.Position.X - minX) * scale) + 2;
            int y1 = offsetY + (int)Math.Round((m.Position.Y - minY) * scale) + 2;
            int x2 = offsetX + (int)Math.Round((m.Position.Right - minX) * scale) - 2;
            int y2 = offsetY + (int)Math.Round((m.Position.Bottom - minY) * scale) - 2;
            int w = Math.Max(44, x2 - x1);
            int h = Math.Max(30, y2 - y1);

            var rect = new Rectangle(x1, y1, w, h);
            _monitorHitBoxes.Add((rect, m, dispNum));

            if (_isDraggingMonitor && m.StableKey == _draggingMonitorKey)
                continue; // Digambar mengambang di atas semua monitor

            DrawMonitorTile(g, rect, m, dispNum, isFloating: false);
        }

        // Bila sedang drag, gambar garis panduan snap
        if (_isDraggingMonitor && _activeGuideLines.Count > 0)
        {
            using var guidePen = new Pen(Color.FromArgb(200, pal.Accent), 1.5f) { DashStyle = DashStyle.Dash };
            foreach (var (p1, p2) in _activeGuideLines)
            {
                g.DrawLine(guidePen, p1, p2);
            }
        }

        if (_isDraggingMonitor && _draggingMonitorKey != null)
        {
            var draggedMon = _canvasMonitors.FirstOrDefault(m => m.StableKey == _draggingMonitorKey);
            if (draggedMon != null)
            {
                int idx = _canvasMonitors.IndexOf(draggedMon) + 1;
                int dispNum = int.TryParse(IdentifyOverlayForm.ExtractDisplayNumber(draggedMon.FriendlyName, idx), out int dn) ? dn : idx;
                DrawMonitorTile(g, _dragFloatingRect, draggedMon, dispNum, isFloating: true);
            }
        }
    }

    private void DrawMonitorTile(Graphics g, Rectangle rect, DisplayMonitor m, int dispNum, bool isFloating)
    {
        var pal = FluentTheme.Current;
        bool isSelected = m.StableKey == _selectedMonitorKey;
        bool isPrimary = m.StableKey == _canvasPrimaryKey || (m.Position.X == 0 && m.Position.Y == 0);
        bool isHovered = _hoveredMonitorKey == m.StableKey;

        using var tilePath = FluentMenu.RoundedRect(rect, 6);

        Color tileFill;
        Color fgPrimary;
        Color fgSecondary;
        if (isSelected)
        {
            tileFill = isFloating ? Color.FromArgb(90, pal.Accent) : Color.FromArgb(48, pal.Accent);
            fgPrimary = pal.Text;
            fgSecondary = pal.TextSecondary;
        }
        else
        {
            bool dark = FluentTheme.IsDarkMode();
            tileFill = isHovered
                ? (dark ? Color.FromArgb(0x35, 0x35, 0x3A) : Color.FromArgb(0xDC, 0xE0, 0xE6))
                : (dark ? Color.FromArgb(0x28, 0x28, 0x2C) : Color.FromArgb(0xE6, 0xE8, 0xEC));
            fgPrimary = pal.Text;
            fgSecondary = pal.TextSecondary;
        }

        using (var fillBrush = new SolidBrush(tileFill))
            g.FillPath(fillBrush, tilePath);

        using (var borderPen = new Pen(isSelected ? pal.Accent : (isHovered ? pal.TextSecondary : pal.Divider), isSelected ? 2f : 1f))
            g.DrawPath(borderPen, tilePath);

        // Adaptive label layout based on available tile dimensions
        bool isNarrow = rect.Width < 80;
        bool isShort = rect.Height < 58;
        bool isCompact = isNarrow || isShort;

        var numFont = isCompact ? FluentTheme.TitleFont : FluentTheme.DisplayNumberFont;
        var numBounds = isCompact
            ? new Rectangle(rect.X, rect.Y, rect.Width, rect.Height)
            : new Rectangle(rect.X, rect.Y + 2, rect.Width, rect.Height - 12);

        // Large display number in center
        TextRenderer.DrawText(g, dispNum.ToString(), numFont,
            numBounds,
            fgPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        // Primary star badge in top-right
        if (isPrimary)
        {
            g.DrawImage(FluentIcons.Render(FluentIcons.StarSolid, 11, isSelected ? pal.Accent : pal.BadgeAccent),
                rect.Right - 16, rect.Y + 5);
        }

        // Only render header title and footer resolution if tile is wide and tall enough to avoid truncation
        if (!isCompact)
        {
            string title = ShortDisplayName(m.FriendlyName);
            int maxTitleW = rect.Width - (isPrimary ? 28 : 14);
            var titleSize = TextRenderer.MeasureText(g, title, FluentTheme.CaptionFont);

            if (maxTitleW >= 40 && titleSize.Width <= maxTitleW)
            {
                TextRenderer.DrawText(g, title, FluentTheme.CaptionFont,
                    new Rectangle(rect.X + 7, rect.Y + 4, maxTitleW, 14),
                    fgSecondary, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            var pos = isFloating ? _dragSnappedDesktopPos : m.Position;
            string footer = isFloating
                ? $"Snap ({pos.X},{pos.Y})"
                : $"{pos.Width} \u00D7 {pos.Height}";

            var footerSize = TextRenderer.MeasureText(g, footer, FluentTheme.CaptionFont);
            if (footerSize.Width <= rect.Width - 12)
            {
                TextRenderer.DrawText(g, footer, FluentTheme.CaptionFont,
                    new Rectangle(rect.X + 6, rect.Bottom - 16, rect.Width - 12, 14),
                    fgSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
        }
    }

    private void OnMonitorCanvasMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        for (int i = _monitorHitBoxes.Count - 1; i >= 0; i--)
        {
            var (bounds, mon, _) = _monitorHitBoxes[i];
            if (bounds.Contains(e.Location))
            {
                _selectedMonitorKey = mon.StableKey;
                _draggingMonitorKey = mon.StableKey;
                _dragMouseStart = e.Location;
                _dragMonitorOriginalPos = mon.Position;
                _dragFloatingRect = bounds;
                _dragSnappedDesktopPos = mon.Position;
                _isDraggingMonitor = false;
                _monitorCanvas.Capture = true;
                UpdateInspectorUi();
                _monitorCanvas.Invalidate();
                break;
            }
        }
    }

    private void OnMonitorCanvasMouseMove(object? sender, MouseEventArgs e)
    {
        if (_draggingMonitorKey != null && e.Button == MouseButtons.Left && _canvasMonitors.Count >= 2)
        {
            int dxPx = e.X - _dragMouseStart.X;
            int dyPx = e.Y - _dragMouseStart.Y;
            if (!_isDraggingMonitor && (Math.Abs(dxPx) >= 4 || Math.Abs(dyPx) >= 4))
            {
                _isDraggingMonitor = true;
                _monitorCanvas.Cursor = Cursors.SizeAll;
            }

            if (_isDraggingMonitor)
            {
                double scale = Math.Max(0.01, _lastCanvasScale);
                int snapThresholdDesktop = (int)Math.Clamp(Math.Round(10.0 / scale), 40, 240);
                int rawDesktopX = _dragMonitorOriginalPos.X + (int)Math.Round(dxPx / scale);
                int rawDesktopY = _dragMonitorOriginalPos.Y + (int)Math.Round(dyPx / scale);

                _dragSnappedDesktopPos = DisplayTopology.SnapMonitorPosition(
                    _canvasMonitors, _draggingMonitorKey, rawDesktopX, rawDesktopY, snapThresholdDesktop);

                int origCanvasX = _lastCanvasOffsetX + (int)Math.Round((_dragMonitorOriginalPos.X - _lastCanvasMinX) * scale) + 2;
                int origCanvasY = _lastCanvasOffsetY + (int)Math.Round((_dragMonitorOriginalPos.Y - _lastCanvasMinY) * scale) + 2;

                int rawCanvasX = origCanvasX + dxPx;
                int rawCanvasY = origCanvasY + dyPx;

                int snappedCanvasX = _lastCanvasOffsetX + (int)Math.Round((_dragSnappedDesktopPos.X - _lastCanvasMinX) * scale) + 2;
                int snappedCanvasY = _lastCanvasOffsetY + (int)Math.Round((_dragSnappedDesktopPos.Y - _lastCanvasMinY) * scale) + 2;

                // Magnetic snap lock: tile actively locks to snapped position
                _dragFloatingRect = new Rectangle(
                    snappedCanvasX,
                    snappedCanvasY,
                    _dragFloatingRect.Width,
                    _dragFloatingRect.Height);

                ComputeAlignmentGuides(scale);
                _monitorCanvas.Invalidate();
                return;
            }
        }

        string? hit = null;
        foreach (var (bounds, mon, _) in _monitorHitBoxes)
        {
            if (bounds.Contains(e.Location))
            {
                hit = mon.StableKey;
                break;
            }
        }
        if (hit != _hoveredMonitorKey)
        {
            _hoveredMonitorKey = hit;
            _monitorCanvas.Cursor = hit != null ? Cursors.Hand : Cursors.Default;
            _monitorCanvas.Invalidate();
        }
    }

    private async void OnMonitorCanvasMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _draggingMonitorKey == null)
            return;

        _monitorCanvas.Capture = false;
        bool wasDragging = _isDraggingMonitor;
        string draggedKey = _draggingMonitorKey;
        var snappedPos = _dragSnappedDesktopPos;

        _draggingMonitorKey = null;
        _isDraggingMonitor = false;
        _activeGuideLines.Clear();
        _monitorCanvas.Cursor = Cursors.Default;

        if (wasDragging)
        {
            double scale = Math.Max(0.01, _lastCanvasScale);
            int snapThresholdDesktop = (int)Math.Clamp(Math.Round(10.0 / scale), 40, 240);

            _canvasMonitors = _canvasMonitors
                .Select(m => m.StableKey == draggedKey ? m with { Position = snappedPos } : m)
                .ToList();
            _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _canvasPrimaryKey, snapThresholdDesktop);
            _hasUnappliedCanvasChanges = true;
            _resetCanvasBtn.Enabled = true;
            _monitorCanvas.Invalidate();
            UpdateInspectorUi();

            if (_autoApplyCheck.Checked)
            {
                await ApplyCanvasArrangementAsync();
            }
            else
            {
                SetStatusMessage("Susunan monitor diperbarui di kanvas. Klik Terapkan Susunan atau Simpan Profil.");
            }
        }
        else
        {
            _monitorCanvas.Invalidate();
        }
    }

    private void ComputeAlignmentGuides(double scale)
    {
        _activeGuideLines.Clear();
        if (_draggingMonitorKey == null) return;

        var others = _canvasMonitors.Where(m => m.StableKey != _draggingMonitorKey).ToList();
        var sp = _dragSnappedDesktopPos;

        foreach (var anchor in others)
        {
            var ap = anchor.Position;
            if (sp.Y == ap.Y)
            {
                int y = _lastCanvasOffsetY + (int)Math.Round((sp.Y - _lastCanvasMinY) * scale) + 2;
                int x1 = _lastCanvasOffsetX + (int)Math.Round((Math.Min(sp.X, ap.X) - _lastCanvasMinX) * scale);
                int x2 = _lastCanvasOffsetX + (int)Math.Round((Math.Max(sp.Right, ap.Right) - _lastCanvasMinX) * scale);
                _activeGuideLines.Add((new Point(x1 - 8, y), new Point(x2 + 8, y)));
            }
            if (sp.Bottom == ap.Bottom)
            {
                int y = _lastCanvasOffsetY + (int)Math.Round((sp.Bottom - _lastCanvasMinY) * scale) - 2;
                int x1 = _lastCanvasOffsetX + (int)Math.Round((Math.Min(sp.X, ap.X) - _lastCanvasMinX) * scale);
                int x2 = _lastCanvasOffsetX + (int)Math.Round((Math.Max(sp.Right, ap.Right) - _lastCanvasMinX) * scale);
                _activeGuideLines.Add((new Point(x1 - 8, y), new Point(x2 + 8, y)));
            }
            if (sp.Y + sp.Height / 2 == ap.Y + ap.Height / 2)
            {
                int y = _lastCanvasOffsetY + (int)Math.Round((sp.Y + sp.Height / 2.0 - _lastCanvasMinY) * scale);
                int x1 = _lastCanvasOffsetX + (int)Math.Round((Math.Min(sp.X, ap.X) - _lastCanvasMinX) * scale);
                int x2 = _lastCanvasOffsetX + (int)Math.Round((Math.Max(sp.Right, ap.Right) - _lastCanvasMinX) * scale);
                _activeGuideLines.Add((new Point(x1 - 8, y), new Point(x2 + 8, y)));
            }
            if (sp.X == ap.X)
            {
                int x = _lastCanvasOffsetX + (int)Math.Round((sp.X - _lastCanvasMinX) * scale) + 2;
                int y1 = _lastCanvasOffsetY + (int)Math.Round((Math.Min(sp.Y, ap.Y) - _lastCanvasMinY) * scale);
                int y2 = _lastCanvasOffsetY + (int)Math.Round((Math.Max(sp.Bottom, ap.Bottom) - _lastCanvasMinY) * scale);
                _activeGuideLines.Add((new Point(x, y1 - 8), new Point(x, y2 + 8)));
            }
            if (sp.Right == ap.Right)
            {
                int x = _lastCanvasOffsetX + (int)Math.Round((sp.Right - _lastCanvasMinX) * scale) - 2;
                int y1 = _lastCanvasOffsetY + (int)Math.Round((Math.Min(sp.Y, ap.Y) - _lastCanvasMinY) * scale);
                int y2 = _lastCanvasOffsetY + (int)Math.Round((Math.Max(sp.Bottom, ap.Bottom) - _lastCanvasMinY) * scale);
                _activeGuideLines.Add((new Point(x, y1 - 8), new Point(x, y2 + 8)));
            }
            if (sp.X + sp.Width / 2 == ap.X + ap.Width / 2)
            {
                int x = _lastCanvasOffsetX + (int)Math.Round((sp.X + sp.Width / 2.0 - _lastCanvasMinX) * scale);
                int y1 = _lastCanvasOffsetY + (int)Math.Round((Math.Min(sp.Y, ap.Y) - _lastCanvasMinY) * scale);
                int y2 = _lastCanvasOffsetY + (int)Math.Round((Math.Max(sp.Bottom, ap.Bottom) - _lastCanvasMinY) * scale);
                _activeGuideLines.Add((new Point(x, y1 - 8), new Point(x, y2 + 8)));
            }
        }
    }

    private void CancelMonitorDrag()
    {
        _isDraggingMonitor = false;
        _draggingMonitorKey = null;
        _activeGuideLines.Clear();
        _monitorCanvas.Capture = false;
        _monitorCanvas.Cursor = Cursors.Default;
        _monitorCanvas.Invalidate();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && (_isDraggingMonitor || _draggingMonitorKey != null))
        {
            CancelMonitorDrag();
            return true;
        }

        if (_activeTab == "Profil" && !_isDraggingMonitor && _selectedMonitorKey != null && ActiveControl is not TextBoxBase)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down)
            {
                NudgeSelectedMonitor(keyData);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (_isDraggingMonitor || _draggingMonitorKey != null)
        {
            CancelMonitorDrag();
        }
    }

    private void NudgeSelectedMonitor(Keys key)
    {
        if (_selectedMonitorKey == null || _canvasMonitors.Count < 2) return;
        var target = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
        if (target == null) return;

        double scale = Math.Max(0.01, _lastCanvasScale);
        int step = (int)Math.Clamp(Math.Round(16.0 / scale), 64, 320);

        int rawX = target.Position.X;
        int rawY = target.Position.Y;
        switch (key)
        {
            case Keys.Left: rawX -= step; break;
            case Keys.Right: rawX += step; break;
            case Keys.Up: rawY -= step; break;
            case Keys.Down: rawY += step; break;
        }

        int snapThreshold = (int)Math.Clamp(Math.Round(10.0 / scale), 40, 240);
        var snapped = DisplayTopology.SnapMonitorPosition(
            _canvasMonitors, target.StableKey, rawX, rawY, snapThreshold);

        _canvasMonitors = _canvasMonitors
            .Select(m => m.StableKey == target.StableKey ? m with { Position = snapped } : m)
            .ToList();
        _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _canvasPrimaryKey, snapThreshold);
        _hasUnappliedCanvasChanges = true;
        _resetCanvasBtn.Enabled = true;
        _monitorCanvas.Invalidate();
        UpdateInspectorUi();
    }

    private void PaintStatusBar(object? sender, PaintEventArgs e)
    {
        var pal = FluentTheme.Current;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var tint = new SolidBrush(Color.FromArgb(12, pal.Text)))
            g.FillRectangle(tint, _statusBar.ClientRectangle);

        var primary = _liveMonitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0);
        string primaryName = primary != null ? ShortDisplayName(primary.FriendlyName) : "-";
        string unsavedBadge = _hasUnappliedCanvasChanges ? " \u2022 [Kanvas belum diterapkan]" : string.Empty;
        string leftText = $"{_liveMonitors.Count} monitor terdeteksi \u2022 Primary: {primaryName}{unsavedBadge}";

        Color dotCol = _hasUnappliedCanvasChanges
            ? Color.FromArgb(0xF5, 0x9E, 0x0B)
            : Color.FromArgb(0x10, 0x7C, 0x10);
        using (var dotBrush = new SolidBrush(dotCol))
            g.FillEllipse(dotBrush, 16, (StatusBarHeight - 8) / 2, 8, 8);

        TextRenderer.DrawText(g, leftText, FluentTheme.CaptionFont,
            new Rectangle(32, 0, 420, StatusBarHeight), pal.TextSecondary,
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        if (!string.IsNullOrWhiteSpace(_statusMessage))
        {
            TextRenderer.DrawText(g, _statusMessage, FluentTheme.CaptionFont,
                new Rectangle(456, 0, Math.Max(0, _statusBar.Width - 472), StatusBarHeight), pal.TextSecondary,
                TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    private static string ShortDisplayName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Display";
        string cleaned = raw.Split('|')[0];
        return cleaned.StartsWith("\\\\.\\", StringComparison.Ordinal) ? cleaned[4..] : cleaned;
    }

    private static string FormatDateShort(string isoUtc)
    {
        if (DateTime.TryParse(isoUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.ToLocalTime().ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        return "-";
    }

    private static FluentSelectButton CreateFluentSelect(int x, int y, int w)
    {
        return new FluentSelectButton
        {
            Location = new Point(x, y),
            Size = new Size(w, 24)
        };
    }

    private static Panel CreateSurfaceCard(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = FluentTheme.Current.Surface
        };
        p.Paint += (_, pe) =>
        {
            var pal = FluentTheme.Current;
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            using var path = FluentMenu.RoundedRect(r, FluentTheme.RadiusItem);
            using var pen = new Pen(pal.Divider, 1);
            pe.Graphics.DrawPath(pen, path);
        };
        return p;
    }

    private static Button CreateCaptionButton(string glyph, bool isClose, int x)
    {
        var btn = new CaptionButton(glyph, isClose)
        {
            Location = new Point(x, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(46, TitleBarHeight)
        };
        return btn;
    }

    private sealed class BufferedPanel : Panel
    {
        public BufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }

    private sealed class CaptionButton : Button
    {
        private string _glyph;
        private readonly bool _isClose;
        public string Glyph { get => _glyph; set { _glyph = value; Invalidate(); } }
        private bool _hovered;

        public bool Hovered
        {
            get => _hovered;
            set
            {
                if (_hovered != value)
                {
                    _hovered = value;
                    Invalidate();
                }
            }
        }

        public CaptionButton(string glyph, bool isClose)
        {
            _glyph = glyph;
            _isClose = isClose;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var pal = FluentTheme.Current;
            var g = pevent.Graphics;
            Color bg = _hovered
                ? (_isClose ? Color.FromArgb(0xC4, 0x2B, 0x1C) : pal.Hover)
                : pal.Background;
            using (var b = new SolidBrush(bg))
                g.FillRectangle(b, ClientRectangle);

            Color fg = _hovered && _isClose ? Color.White : pal.Text;
            var bmp = FluentIcons.Render(_glyph[0], 12, fg);
            g.DrawImage(bmp, (Width - 12) / 2, (Height - 12) / 2);
        }
    }

    private sealed class NavButton : Button
    {
        private readonly string _label;
        private readonly char _glyph;
        private bool _active;
        private bool _hovered;
        private bool _compactMode;

        public string Label => _label;

        public bool CompactMode
        {
            get => _compactMode;
            set
            {
                if (_compactMode != value)
                {
                    _compactMode = value;
                    Invalidate();
                }
            }
        }

        public NavButton(string label, char glyph)
        {
            _label = label;
            _glyph = glyph;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetActive(bool active)
        {
            _active = active;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var pal = FluentTheme.Current;
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(pal.ShellBackground);

            if (_active || _hovered)
            {
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = FluentMenu.RoundedRect(r, FluentTheme.RadiusItem);
                using var brush = new SolidBrush(pal.Hover);
                g.FillPath(brush, path);
            }

            if (_active)
            {
                using var pillPath = FluentMenu.RoundedRect(new Rectangle(0, 11, 3, 18), 1);
                using var accentBrush = new SolidBrush(pal.Accent);
                g.FillPath(accentBrush, pillPath);
            }

            if (_compactMode)
            {
                g.DrawImage(FluentIcons.Render(_glyph, 16, _active ? pal.Accent : pal.Text),
                    (Width - 16) / 2, (Height - 16) / 2);
            }
            else
            {
                g.DrawImage(FluentIcons.Render(_glyph, 16, _active ? pal.Accent : pal.Text), 12, (Height - 16) / 2);
                TextRenderer.DrawText(g, _label, _active ? FluentTheme.BodySemiboldFont : FluentTheme.BodyFont,
                    new Rectangle(38, 0, Width - 44, Height), pal.Text,
                    TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }

    private sealed class MoreButton : Button
    {
        private bool _hovered;

        public MoreButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var pal = FluentTheme.Current;
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(pal.Surface);
            if (_hovered)
            {
                using var path = FluentMenu.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), FluentTheme.RadiusItem);
                using var brush = new SolidBrush(pal.Hover);
                g.FillPath(brush, path);
            }
            g.DrawImage(FluentIcons.Render(FluentIcons.More, 16, pal.Text), (Width - 16) / 2, (Height - 16) / 2);
        }
    }
}
