using System.ComponentModel;
using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Launcher;

internal sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(16, 20, 21);
    private static readonly Color Panel = Color.FromArgb(27, 34, 35);
    private static readonly Color PanelHover = Color.FromArgb(39, 49, 50);
    private static readonly Color Edge = Color.FromArgb(75, 88, 88);
    private static readonly Color Amber = Color.FromArgb(220, 157, 54);
    private static readonly Color TextColor = Color.FromArgb(230, 233, 229);
    private static readonly Color Muted = Color.FromArgb(145, 153, 151);
    private static readonly Color Ready = Color.FromArgb(126, 190, 124);
    private static readonly Color Warning = Color.FromArgb(220, 116, 70);

    private readonly LaunchOrchestrator launchOrchestrator;
    private readonly DocumentOpener documentOpener;
    private readonly InstallStatusReader statusReader;
    private readonly OwnedInstallUninstaller gameUninstaller;
    private readonly ApplicationUninstallOrchestrator applicationUninstaller;
    private readonly TableLayoutPanel operationGrid = new();
    private readonly FlowLayoutPanel packRow = new();
    private readonly Label statusLine = new();
    private readonly List<Button> actionButtons = new();
    private Button? uninstallButton;

    public MainForm(
        InstallStatusReader statusReader,
        LaunchOrchestrator launchOrchestrator,
        DocumentOpener documentOpener,
        OwnedInstallUninstaller gameUninstaller,
        ApplicationUninstallOrchestrator applicationUninstaller)
    {
        this.statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
        this.launchOrchestrator = launchOrchestrator ?? throw new ArgumentNullException(nameof(launchOrchestrator));
        this.documentOpener = documentOpener ?? throw new ArgumentNullException(nameof(documentOpener));
        this.gameUninstaller = gameUninstaller ?? throw new ArgumentNullException(nameof(gameUninstaller));
        this.applicationUninstaller = applicationUninstaller ?? throw new ArgumentNullException(nameof(applicationUninstaller));

        Text = "MechWarrior 4 Remastered";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(930, 590);
        MinimumSize = MaximumSize = new Size(946, 629);
        BackColor = Background;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(CreateLayout());
        ShowLoadingState();
        Shown += async (_, _) => await RefreshStatusesAsync();
    }

    private Control CreateLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 20, 26, 18),
            ColumnCount = 1,
            RowCount = 5,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 330));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(CreateHeader(), 0, 0);

        operationGrid.Dock = DockStyle.Fill;
        operationGrid.ColumnCount = 3;
        operationGrid.RowCount = 2;
        operationGrid.Padding = new Padding(0, 6, 0, 4);
        for (var column = 0; column < 3; column++) operationGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        operationGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 56F));
        operationGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 44F));
        root.Controls.Add(operationGrid, 0, 1);

        packRow.AutoSize = true;
        packRow.Dock = DockStyle.Fill;
        packRow.FlowDirection = FlowDirection.LeftToRight;
        packRow.WrapContents = false;
        packRow.Padding = new Padding(0, 11, 0, 0);
        root.Controls.Add(packRow, 0, 2);
        root.Controls.Add(new Label { Dock = DockStyle.Fill, BackColor = Edge }, 0, 3);
        root.Controls.Add(CreateFooter(), 0, 4);
        return root;
    }

    private static Control CreateHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var icon = new PictureBox
        {
            Location = new Point(0, 0),
            Size = new Size(54, 54),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Icon.ExtractAssociatedIcon(Application.ExecutablePath)?.ToBitmap(),
        };
        panel.Controls.Add(icon);
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(68, 0),
            Font = new Font("Segoe UI Semibold", 22F),
            ForeColor = TextColor,
            Text = "MECHWARRIOR 4",
        });
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(71, 40),
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Amber,
            Text = "R E M A S T E R E D",
        });
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(755, 37),
            Font = new Font("Consolas", 9F, FontStyle.Bold),
            ForeColor = Muted,
            Text = "SELECT OPERATION",
        });
        return panel;
    }

    private Control CreateFooter()
    {
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        statusLine.Dock = DockStyle.Fill;
        statusLine.TextAlign = ContentAlignment.MiddleLeft;
        statusLine.Font = new Font("Consolas", 9F, FontStyle.Bold);
        statusLine.ForeColor = Muted;
        footer.Controls.Add(statusLine, 0, 0);

        uninstallButton = new OperationButton
        {
            Text = "UNINSTALL",
            Size = new Size(118, 32),
            Anchor = AnchorStyles.Right,
            Enabled = applicationUninstaller.IsAvailable,
            BackColor = Panel,
            ForeColor = TextColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
        };
        uninstallButton.FlatAppearance.BorderColor = Warning;
        uninstallButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(54, 30, 26);
        uninstallButton.Click += async (_, _) => await UninstallAllAsync();
        actionButtons.Add(uninstallButton);
        footer.Controls.Add(uninstallButton, 1, 0);
        return footer;
    }

    private void ShowLoadingState()
    {
        operationGrid.Controls.Clear();
        var games = ProductCatalog.All.Where(product => product.Kind == ProductKind.Game).ToArray();
        for (var index = 0; index < games.Length; index++)
        {
            operationGrid.Controls.Add(CreateTile(
                $"{GameMark(games[index].Id)}    {games[index].DisplayName.ToUpperInvariant()}{Environment.NewLine}          CHECKING INSTALLATION…",
                enabled: false,
                large: true), index, 0);
            operationGrid.Controls.Add(CreateTile(
                $"▤    {games[index].DisplayName.ToUpperInvariant()} MANUAL{Environment.NewLine}       CHECKING…",
                enabled: false,
                large: false), index, 1);
        }
        statusLine.Text = "CHECKING INSTALLED GAMES…";
        if (uninstallButton is not null) uninstallButton.Enabled = false;
    }

    private async Task RefreshStatusesAsync()
    {
        statusLine.Text = "CHECKING INSTALLED GAMES…";
        IReadOnlyList<ProductStatus> statuses;
        try
        {
            statuses = await Task.Run(statusReader.Read);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            statusLine.Text = "INSTALLATION STATUS COULD NOT BE VERIFIED";
            statusLine.ForeColor = Warning;
            MessageBox.Show(this, error.Message, "Status check failed safely", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RenderStatuses(statuses);
        if (uninstallButton is not null) uninstallButton.Enabled = applicationUninstaller.IsAvailable;
    }

    private void RenderStatuses(IReadOnlyList<ProductStatus> statuses)
    {
        var games = statuses.Where(value => value.Product.Kind == ProductKind.Game).ToArray();
        operationGrid.SuspendLayout();
        var oldTiles = operationGrid.Controls.Cast<Control>().ToArray();
        operationGrid.Controls.Clear();
        foreach (var oldTile in oldTiles) oldTile.Dispose();
        actionButtons.RemoveAll(button => button.IsDisposed);
        for (var index = 0; index < games.Length; index++)
        {
            operationGrid.Controls.Add(CreateGameButton(games[index]), index, 0);
            operationGrid.Controls.Add(CreateManualButton(games[index]), index, 1);
        }
        operationGrid.ResumeLayout(performLayout: true);

        packRow.SuspendLayout();
        packRow.Controls.Clear();
        packRow.Controls.Add(new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0),
            ForeColor = Muted,
            Text = "MECH PAKS",
        });
        foreach (var pack in statuses.Where(value => value.Product.Kind == ProductKind.OptionalPack))
            packRow.Controls.Add(CreatePackIndicator(pack));
        packRow.ResumeLayout(performLayout: true);

        var readyGames = games.Count(game => game.State == ProductInstallState.Ready);
        var repairGames = games.Count(game => game.State == ProductInstallState.NeedsRepair);
        statusLine.Text = repairGames > 0
            ? $"{repairGames} GAME INSTALLATION(S) NEED REPAIR"
            : readyGames > 0 ? $"SYSTEM READY  •  {readyGames} GAME(S) INSTALLED" : "NO GAMES INSTALLED";
        statusLine.ForeColor = repairGames > 0 ? Warning : Muted;
    }

    private Button CreateGameButton(ProductStatus status)
    {
        var subtitle = status.State switch
        {
            ProductInstallState.Ready => "LAUNCH GAME",
            ProductInstallState.NeedsRepair => "REPAIR REQUIRED",
            _ => "NOT INSTALLED",
        };
        var button = CreateTile(
            $"{GameMark(status.Product.Id)}    {status.Product.DisplayName.ToUpperInvariant()}{Environment.NewLine}          {subtitle}",
            status.State == ProductInstallState.Ready,
            large: true);
        button.AccessibleName = $"{status.Product.DisplayName}: {subtitle}";
        if (status.State == ProductInstallState.Ready)
            button.Click += (_, _) => TryAction(() => launchOrchestrator.Launch(status));
        return button;
    }

    private Button CreateManualButton(ProductStatus status)
    {
        var available = status.ManualPath is not null;
        var button = CreateTile(
            $"▤    {status.Product.DisplayName.ToUpperInvariant()} MANUAL{Environment.NewLine}       {(available ? "OPEN PDF" : "NOT AVAILABLE")}",
            available,
            large: false);
        button.AccessibleName = $"{status.Product.DisplayName} manual: {(available ? "Open PDF" : "Not available")}";
        if (available) button.Click += (_, _) => TryAction(() => documentOpener.Open(status.ManualPath!));
        return button;
    }

    private Button CreateTile(string text, bool enabled, bool large)
    {
        var button = new OperationButton
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            Padding = new Padding(16, 0, 8, 0),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Enabled = enabled,
            BackColor = Panel,
            ForeColor = TextColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", large ? 12F : 9.5F),
        };
        button.FlatAppearance.BorderColor = Edge;
        button.FlatAppearance.MouseOverBackColor = PanelHover;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(58, 54, 35);
        actionButtons.Add(button);
        return button;
    }

    private static Control CreatePackIndicator(ProductStatus status)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(7, 2, 0, 0),
            Padding = new Padding(12, 6, 12, 6),
            BackColor = Panel,
            ForeColor = status.IsInstalled ? Ready : Muted,
            BorderStyle = BorderStyle.FixedSingle,
            Text = $"{(status.IsInstalled ? "✓" : "○")}  {status.Product.DisplayName.ToUpperInvariant()}",
        };
    }

    private async Task UninstallAllAsync()
    {
        var choice = MessageBox.Show(this,
            "Remove MechWarrior 4 Remastered and all verified game files? Unowned saves and configuration will be preserved for a future reinstall.",
            "Uninstall MechWarrior 4 Remastered",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (choice != DialogResult.Yes) return;

        SetBusy(true);
        statusLine.Text = "VERIFYING OWNERSHIP BEFORE UNINSTALL…";
        try
        {
            var installed = await Task.Run(() => statusReader.Read()
                .Where(item => item.Product.Kind == ProductKind.Game && item.InstallPath is not null)
                .ToArray());
            var repairRequired = installed.Where(item => item.State == ProductInstallState.NeedsRepair).ToArray();
            if (repairRequired.Length > 0)
            {
                MessageBox.Show(this,
                    "Removal cannot continue because ownership verification failed for: " +
                    string.Join(", ", repairRequired.Select(item => item.Product.DisplayName)) + ".",
                    "Uninstall blocked safely", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await RefreshStatusesAsync();
                return;
            }
            var results = await Task.Run(() => installed.Select(item => (item.Product.DisplayName, Result: gameUninstaller.Remove(item.InstallPath!))).ToArray());
            var blocked = results.Where(item => item.Result.Status == InstallRemovalStatus.Blocked).ToArray();
            if (blocked.Length > 0)
            {
                var details = blocked.SelectMany(item => item.Result.Issues.Select(issue => $"{item.DisplayName}: {issue}"));
                MessageBox.Show(this, string.Join(Environment.NewLine, details), "Uninstall blocked safely", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await RefreshStatusesAsync();
                return;
            }

            applicationUninstaller.Start();
            Close();
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException or InvalidDataException or Win32Exception)
        {
            MessageBox.Show(this, error.Message, "Uninstall failed safely", MessageBoxButtons.OK, MessageBoxIcon.Error);
            await RefreshStatusesAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        if (busy)
        {
            foreach (var button in actionButtons.Where(button => !button.IsDisposed)) button.Enabled = false;
            return;
        }

        if (uninstallButton is not null) uninstallButton.Enabled = applicationUninstaller.IsAvailable;
    }

    private bool TryAction(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException or Win32Exception)
        {
            MessageBox.Show(this, error.Message, "MechWarrior 4 Remastered", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static string GameMark(string productId) => productId switch
    {
        "vengeance" => "V",
        "black-knight" => "BK",
        _ => "M",
    };

    private sealed class OperationButton : Button
    {
        protected override void OnPaint(PaintEventArgs paintEvent)
        {
            if (Enabled)
            {
                base.OnPaint(paintEvent);
                return;
            }

            paintEvent.Graphics.Clear(BackColor);
            ControlPaint.DrawBorder(paintEvent.Graphics, ClientRectangle, Edge, ButtonBorderStyle.Solid);
            var textBounds = Rectangle.Inflate(ClientRectangle, -Padding.Left, -6);
            TextRenderer.DrawText(
                paintEvent.Graphics,
                Text,
                Font,
                textBounds,
                Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }
}
