using System.ComponentModel;
using MW4Remastered.Core;
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

    public MainForm(InstallStatusReader statusReader, LaunchOrchestrator launchOrchestrator, DocumentOpener documentOpener)
    {
        this.launchOrchestrator = launchOrchestrator ?? throw new ArgumentNullException(nameof(launchOrchestrator));
        this.documentOpener = documentOpener ?? throw new ArgumentNullException(nameof(documentOpener));
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

        var gameGrid = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Dock = DockStyle.Top,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
        };
        gameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        gameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        gameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

        var statuses = statusReader.Read();
        foreach (var status in statuses.Where(value => value.Product.Kind == ProductKind.Game))
        {
            gameGrid.Controls.Add(CreateGameCard(status));
        }

        var packRow = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 18, 0, 18),
        };
        foreach (var status in statuses.Where(value => value.Product.Kind == ProductKind.OptionalPack))
        {
            packRow.Controls.Add(CreatePackStatus(status));
        }

        var footer = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
        };
        footer.Controls.Add(CreateFooterButton("UNINSTALL", enabled: false));
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
    }

    private Control CreateGameCard(ProductStatus status)
    {
        var card = new Panel { BackColor = Panel, Height = 316, Dock = DockStyle.Fill, Margin = new Padding(8) };
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
        var launch = new Button { Dock = DockStyle.Bottom, Height = 46, FlatStyle = FlatStyle.Flat, Enabled = status.IsInstalled, Text = status.State == ProductInstallState.NeedsRepair ? "REPAIR REQUIRED" : status.IsInstalled ? "LAUNCH" : "INSTALL REQUIRED", BackColor = Amber, ForeColor = Color.Black };
        launch.FlatAppearance.BorderSize = 0;
        if (status.IsInstalled) launch.Click += (_, _) => TryAction(() => launchOrchestrator.Launch(status));
        card.Controls.Add(launch);
        card.Controls.Add(manual);
        card.Controls.Add(state);
        card.Controls.Add(name);
        card.Controls.Add(monogram);
        return card;
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
