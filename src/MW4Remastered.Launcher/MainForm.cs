using System.ComponentModel;
using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Launcher;

internal sealed class MainForm : Form
{
    private static readonly Color Armor = Color.FromArgb(39, 48, 51);
    private static readonly Color Panel = Color.FromArgb(22, 29, 31);
    private static readonly Color Amber = Color.FromArgb(220, 157, 54);
    private static readonly Color TextColor = Color.FromArgb(224, 230, 224);
    private static readonly Color Ready = Color.FromArgb(126, 190, 124);
    private static readonly Color Warning = Color.FromArgb(220, 116, 70);
    private readonly LaunchOrchestrator launchOrchestrator;
    private readonly DocumentOpener documentOpener;
    private readonly InstallStatusReader statusReader;
    private readonly OwnedInstallUninstaller uninstaller;
    private readonly TableLayoutPanel gameGrid = new();
    private readonly FlowLayoutPanel packRow = new();

    public MainForm(
        InstallStatusReader statusReader,
        LaunchOrchestrator launchOrchestrator,
        DocumentOpener documentOpener,
        OwnedInstallUninstaller uninstaller)
    {
        this.statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
        this.launchOrchestrator = launchOrchestrator ?? throw new ArgumentNullException(nameof(launchOrchestrator));
        this.documentOpener = documentOpener ?? throw new ArgumentNullException(nameof(documentOpener));
        this.uninstaller = uninstaller ?? throw new ArgumentNullException(nameof(uninstaller));
        Text = "MechWarrior 4 Remastered";
        BackColor = Armor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(900, 600);
        Size = new Size(1080, 680);
        StartPosition = FormStartPosition.CenterScreen;

        var heading = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 24F),
            ForeColor = Amber,
            Text = "MECHWARRIOR 4  /  REMASTERED",
            Margin = new Padding(8, 8, 8, 20),
        };

        gameGrid.AutoSize = true;
        gameGrid.ColumnCount = 3;
        gameGrid.Dock = DockStyle.Top;
        gameGrid.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        gameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        gameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        gameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

        packRow.AutoSize = true;
        packRow.Dock = DockStyle.Top;
        packRow.FlowDirection = FlowDirection.LeftToRight;
        packRow.Padding = new Padding(0, 18, 0, 18);

        var footer = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
        };
        footer.Controls.Add(CreateFooterButton("APP UNINSTALL PENDING", enabled: false));
        footer.Controls.Add(CreateFooterButton("DIAGNOSTICS", enabled: false));
        footer.Controls.Add(CreateFooterButton("SETTINGS", enabled: false));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32),
            RowCount = 4,
            ColumnCount = 1,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(gameGrid, 0, 1);
        layout.Controls.Add(packRow, 0, 2);
        layout.Controls.Add(footer, 0, 3);
        Controls.Add(layout);
        RefreshStatuses();
    }

    private Control CreateGameCard(ProductStatus status)
    {
        var card = new Panel { BackColor = Panel, Height = 352, Dock = DockStyle.Fill, Margin = new Padding(8) };
        var monogram = new Label
        {
            Dock = DockStyle.Top,
            Height = 138,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 40F),
            ForeColor = Amber,
            Text = status.Product.DisplayName switch { "Vengeance" => "V", "Black Knight" => "BK", _ => "M" },
        };
        var name = new Label { Dock = DockStyle.Top, Height = 42, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 15F), Text = status.Product.DisplayName };
        var state = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = status.State switch { ProductInstallState.Ready => Ready, ProductInstallState.NeedsRepair => Warning, _ => Color.Gray },
            Text = status.State switch { ProductInstallState.Ready => "VERIFIED / READY", ProductInstallState.NeedsRepair => "REPAIR REQUIRED", _ => "NOT INSTALLED" },
        };
        var manual = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Enabled = status.ManualPath is not null,
            Text = status.ManualPath is null ? "MANUAL UNAVAILABLE" : "OPEN MANUAL",
            BackColor = Armor,
            ForeColor = TextColor,
        };
        manual.FlatAppearance.BorderColor = Color.FromArgb(70, 83, 84);
        if (status.ManualPath is not null) manual.Click += (_, _) => TryAction(() => documentOpener.Open(status.ManualPath));
        var remove = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            Enabled = status.InstallPath is not null,
            Text = status.InstallPath is null ? "NOTHING TO REMOVE" : "REMOVE GAME FILES",
            BackColor = Armor,
            ForeColor = status.InstallPath is null ? Color.Gray : Warning,
        };
        remove.FlatAppearance.BorderColor = Color.FromArgb(70, 83, 84);
        if (status.InstallPath is not null) remove.Click += async (_, _) => await RemoveGameAsync(status, remove);
        var launch = new Button { Dock = DockStyle.Bottom, Height = 46, FlatStyle = FlatStyle.Flat, Enabled = status.IsInstalled, Text = status.State == ProductInstallState.NeedsRepair ? "REPAIR REQUIRED" : status.IsInstalled ? "LAUNCH" : "INSTALL REQUIRED", BackColor = Amber, ForeColor = Color.Black };
        launch.FlatAppearance.BorderSize = 0;
        if (status.IsInstalled) launch.Click += (_, _) => TryAction(() => launchOrchestrator.Launch(status));
        card.Controls.Add(launch);
        card.Controls.Add(remove);
        card.Controls.Add(manual);
        card.Controls.Add(state);
        card.Controls.Add(name);
        card.Controls.Add(monogram);
        return card;
    }

    private void RefreshStatuses()
    {
        var statuses = statusReader.Read();
        gameGrid.SuspendLayout();
        packRow.SuspendLayout();
        gameGrid.Controls.Clear();
        packRow.Controls.Clear();
        foreach (var status in statuses.Where(value => value.Product.Kind == ProductKind.Game))
            gameGrid.Controls.Add(CreateGameCard(status));
        foreach (var status in statuses.Where(value => value.Product.Kind == ProductKind.OptionalPack))
            packRow.Controls.Add(CreatePackStatus(status));
        packRow.ResumeLayout(performLayout: true);
        gameGrid.ResumeLayout(performLayout: true);
    }

    private async Task RemoveGameAsync(ProductStatus status, Button sourceButton)
    {
        if (status.InstallPath is null) return;
        var confirmation = MessageBox.Show(this,
            $"Remove manifest-owned {status.Product.DisplayName} files?{Environment.NewLine}{Environment.NewLine}Unowned saves and configuration will be preserved.",
            "Remove game files", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes) return;

        sourceButton.Enabled = false;
        sourceButton.Text = "VERIFYING OWNERSHIP…";
        try
        {
            var result = await Task.Run(() => uninstaller.Remove(status.InstallPath));
            if (result.Status == InstallRemovalStatus.Blocked)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, result.Issues),
                    "Removal blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var preserved = result.PreservedPaths.Count == 0
                ? "No unowned files remained."
                : $"Preserved {result.PreservedPaths.Count} unowned save/configuration file(s).";
            MessageBox.Show(this, $"Removed {result.RemovedFiles.Count} manifest-owned file(s).{Environment.NewLine}{preserved}",
                "Game files removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, error.Message, "Removal failed safely", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            RefreshStatuses();
        }
    }

    private static Control CreatePackStatus(ProductStatus status)
    {
        return new Label
        {
            AutoSize = true,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(8),
            BackColor = Panel,
            ForeColor = status.IsInstalled ? Ready : Color.Gray,
            Text = $"{(status.IsInstalled ? "✓" : "○")}  {status.Product.DisplayName}",
        };
    }

    private static Button CreateFooterButton(string text, bool enabled)
    {
        return new Button { AutoSize = true, Enabled = enabled, FlatStyle = FlatStyle.Flat, Margin = new Padding(8), Text = text, ForeColor = TextColor, BackColor = Panel };
    }

    private void TryAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException or Win32Exception)
        {
            MessageBox.Show(this, error.Message, "MechWarrior 4 Remastered", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
