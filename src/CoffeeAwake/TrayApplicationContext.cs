// =============================================================================
// CoffeeAwake — TrayApplicationContext.cs
// The heart of the application. Manages the system tray icon, context menu,
// and wires everything together.
//
// Inherits from ApplicationContext so no main window is ever created.
// WinForms message loop runs entirely through the tray notification icon.
// =============================================================================

using CoffeeAwake.Services;
using CoffeeAwake.UI;

namespace CoffeeAwake;

/// <summary>
/// Provides the application lifetime context. No main window is shown.
/// The tray icon IS the application's entire UI surface.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------
    private readonly AwakeService     _awakeService;
    private readonly NotifyIcon       _trayIcon;
    private readonly ContextMenuStrip _contextMenu;

    // Used to marshal callbacks to the UI thread without needing a visible form.
    private readonly System.Windows.Forms.Timer _uiTimer;
    private volatile bool _pendingIsAwake;
    private volatile bool _hasPendingUpdate;

    // Menu items we need to update dynamically
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _startupItem;

    // Owned icons — disposed with this context
    private Icon _iconActive;
    private Icon _iconInactive;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------
    public TrayApplicationContext()
    {
        _awakeService = new AwakeService();

        // Pre-render both icons
        _iconActive   = IconFactory.Create(isActive: true);
        _iconInactive = IconFactory.Create(isActive: false);

        // Build context menu
        _contextMenu = BuildContextMenu(out _toggleItem, out _startupItem);

        // Build tray icon
        _trayIcon = new NotifyIcon
        {
            Icon             = _iconInactive,
            Text             = TooltipText(false),
            ContextMenuStrip = _contextMenu,
            Visible          = true,
        };

        // Double-click toggles state
        _trayIcon.DoubleClick += (_, _) => _awakeService.Toggle();

        // Low-frequency timer used ONLY to marshal AwakeService state changes
        // back to the UI thread (since System.Threading.Timer fires on ThreadPool).
        // Interval is 200 ms — negligible CPU, instant enough to feel responsive.
        _uiTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _uiTimer.Tick += OnUiTimerTick;
        _uiTimer.Start();

        // Subscribe AFTER timer is ready
        _awakeService.StateChanged += OnAwakeStateChanged;
    }

    // -------------------------------------------------------------------------
    // Context menu builder
    // -------------------------------------------------------------------------

    private ContextMenuStrip BuildContextMenu(
        out ToolStripMenuItem toggleItem,
        out ToolStripMenuItem startupItem)
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new ToolStripProfessionalRenderer(new ModernColorTable()),
            Font     = new Font("Segoe UI", 9f),
        };

        // — Toggle (bold = primary action)
        toggleItem = new ToolStripMenuItem("☕  Ativar")
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        toggleItem.Click += (_, _) => _awakeService.Toggle();

        // — Timed sessions
        var timedMenu = new ToolStripMenuItem("⏱  Manter acordado por...");
        timedMenu.DropDownItems.AddRange(BuildTimedItems());

        // — Start with Windows
        startupItem = new ToolStripMenuItem("🚀  Iniciar com o Windows")
        {
            CheckOnClick = true,
            Checked      = StartupService.IsEnabled,
        };
        startupItem.CheckedChanged += OnStartupCheckedChanged;

        // — Separator + Exit
        var exitItem = new ToolStripMenuItem("✖  Sair");
        exitItem.Click += (_, _) => ExitApplication();

        menu.Items.AddRange([
            toggleItem,
            new ToolStripSeparator(),
            timedMenu,
            new ToolStripSeparator(),
            startupItem,
            new ToolStripSeparator(),
            exitItem,
        ]);

        return menu;
    }

    private ToolStripItem[] BuildTimedItems()
    {
        (string label, TimeSpan duration)[] options =
        [
            ("1 hora",  TimeSpan.FromHours(1)),
            ("2 horas", TimeSpan.FromHours(2)),
            ("4 horas", TimeSpan.FromHours(4)),
        ];

        return options
            .Select(opt =>
            {
                var item = new ToolStripMenuItem(opt.label);
                item.Click += (_, _) => _awakeService.ActivateFor(opt.duration);
                return (ToolStripItem)item;
            })
            .ToArray();
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by <see cref="AwakeService"/> whenever the state changes.
    /// May be called from a background thread (timer expiry), so we set a flag
    /// and let the UI timer pick it up on the message-loop thread.
    /// </summary>
    private void OnAwakeStateChanged(object? sender, AwakeStateChangedEventArgs e)
    {
        // Volatile write — safe across threads
        _pendingIsAwake   = e.IsAwake;
        _hasPendingUpdate = true;
    }

    /// <summary>
    /// Runs on the WinForms message-loop thread. Applies any pending state updates.
    /// </summary>
    private void OnUiTimerTick(object? sender, EventArgs e)
    {
        if (!_hasPendingUpdate) return;
        _hasPendingUpdate = false;
        ApplyState(_pendingIsAwake);
    }

    private void ApplyState(bool isAwake)
    {
        _trayIcon.Icon = isAwake ? _iconActive : _iconInactive;
        _trayIcon.Text = TooltipText(isAwake);

        _toggleItem.Text = isAwake
            ? "☕  Desativar"
            : "☕  Ativar";

        // Brief balloon notification for timed sessions
        _trayIcon.BalloonTipTitle = "CoffeeAwake";
        _trayIcon.BalloonTipText  = isAwake
            ? "Modo ativo — o PC não vai dormir."
            : "Modo inativo — o PC pode dormir normalmente.";
        _trayIcon.ShowBalloonTip(2000);
    }

    private void OnStartupCheckedChanged(object? sender, EventArgs e)
    {
        var success = StartupService.SetEnabled(_startupItem.Checked);
        if (!success)
        {
            // Revert the checkbox if the registry write failed
            _startupItem.CheckedChanged -= OnStartupCheckedChanged;
            _startupItem.Checked = !_startupItem.Checked;
            _startupItem.CheckedChanged += OnStartupCheckedChanged;

            MessageBox.Show(
                "Não foi possível modificar as configurações de inicialização.",
                "CoffeeAwake",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // -------------------------------------------------------------------------
    // Exit
    // -------------------------------------------------------------------------

    private void ExitApplication()
    {
        // Hide icon immediately so the user sees instant feedback
        _trayIcon.Visible = false;

        // Deactivate awake state before quitting
        _awakeService.Deactivate();

        Application.Exit();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string TooltipText(bool isAwake) =>
        isAwake ? "CoffeeAwake: Ativo ☕" : "CoffeeAwake: Inativo";

    // -------------------------------------------------------------------------
    // Dispose
    // -------------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _uiTimer.Stop();
            _uiTimer.Dispose();

            _awakeService.StateChanged -= OnAwakeStateChanged;
            _awakeService.Dispose();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _contextMenu.Dispose();

            _iconActive.Dispose();
            _iconInactive.Dispose();
        }

        base.Dispose(disposing);
    }
}

// =============================================================================
// ModernColorTable — subtle flat styling for the context menu
// =============================================================================
internal sealed class ModernColorTable : ProfessionalColorTable
{
    private static readonly Color Background  = Color.FromArgb(30,  30,  30);
    private static readonly Color Highlight   = Color.FromArgb(60,  60,  60);
    private static readonly Color Border      = Color.FromArgb(70,  70,  70);
    private static readonly Color CheckBg     = Color.FromArgb(80,  80,  80);

    public override Color MenuItemSelected         => Highlight;
    public override Color MenuItemBorder           => Color.Transparent;
    public override Color MenuItemSelectedGradientBegin => Highlight;
    public override Color MenuItemSelectedGradientEnd   => Highlight;
    public override Color ToolStripDropDownBackground   => Background;
    public override Color ImageMarginGradientBegin => Background;
    public override Color ImageMarginGradientMiddle => Background;
    public override Color ImageMarginGradientEnd   => Background;
    public override Color MenuBorder               => Border;
    public override Color CheckBackground          => CheckBg;
    public override Color CheckSelectedBackground  => Highlight;
    public override Color SeparatorDark            => Border;
    public override Color SeparatorLight           => Border;
}


