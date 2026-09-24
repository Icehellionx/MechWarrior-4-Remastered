using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Launcher;

internal sealed class GraphicsSettingsForm : Form
{
    private readonly GameDisplayGeometry activeGeometry;
    private readonly ComboBox resampling = new();
    private readonly ComboBox displayPreview = new();
    private readonly ComboBox resolution = new();
    private readonly ComboBox filtering = new();
    private readonly ComboBox antialiasing = new();
    private readonly Label loading = new();
    private readonly Label gameplayLine = new();
    private readonly Label barsLine = new();
    private readonly Button apply = new();

    public GraphicsSettingsForm(GameDisplayGeometry geometry)
    {
        activeGeometry = geometry;
        Text = "Display settings";
        ClientSize = new Size(540, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(16, 20, 21);
        ForeColor = Color.FromArgb(230, 233, 229);
        Font = new Font("Segoe UI", 10F);

        Controls.Add(Label("DISPLAY SETTINGS", 22, 18, 490, 30, 17F, true));
        Controls.Add(Label($"Current Windows monitor: {geometry.Monitor.Width} × {geometry.Monitor.Height}", 24, 57, 490, 25));
        Controls.Add(Label("Preview 4:3 layout for", 24, 84, 490, 25));
        displayPreview.DropDownStyle = ComboBoxStyle.DropDownList;
        displayPreview.Location = new Point(24, 109);
        displayPreview.Width = 490;
        displayPreview.Items.Add(new DisplayPreviewOption(
            $"Current monitor ({geometry.Monitor.Width} × {geometry.Monitor.Height}) — used at launch", geometry));
        foreach (var preset in GameDisplayPreset.Choices)
            displayPreview.Items.Add(new DisplayPreviewOption(preset.Label,
                GameDisplayGeometry.ForMonitor(preset.Monitor.Width, preset.Monitor.Height)));
        displayPreview.SelectedIndex = 0;
        displayPreview.SelectedIndexChanged += (_, _) => UpdateGeometry();
        Controls.Add(displayPreview);
        gameplayLine.Location = new Point(24, 150);
        gameplayLine.Size = new Size(490, 25);
        barsLine.Location = new Point(24, 179);
        barsLine.Size = new Size(490, 25);
        Controls.Add(gameplayLine);
        Controls.Add(barsLine);
        Controls.Add(Label("Gameplay render resolution (safe 4:3 modes)", 24, 216, 490, 25));
        resolution.DropDownStyle = ComboBoxStyle.DropDownList;
        resolution.Location = new Point(24, 240);
        resolution.Width = 490;
        resolution.Items.Add(new ResolutionOption("Automatic (largest listed game mode fitting this monitor)", null));
        foreach (var preset in GameResolutionPreset.Choices)
            resolution.Items.Add(new ResolutionOption(preset.Label, preset.Resolution));
        resolution.SelectedIndex = 0;
        resolution.SelectedIndexChanged += (_, _) => UpdateGeometry();
        Controls.Add(resolution);
        UpdateGeometry();
        Controls.Add(Label("Display scaling filter", 24, 281, 490, 25));

        resampling.DropDownStyle = ComboBoxStyle.DropDownList;
        resampling.Location = new Point(24, 306);
        resampling.Width = 490;
        resampling.Items.AddRange(
        [
            new Option("High quality (default)", DisplayResampling.Lanczos3),
            new Option("Soft", DisplayResampling.Bilinear),
            new Option("Sharp pixels", DisplayResampling.PointSampled),
        ]);
        resampling.SelectedIndex = 0;
        Controls.Add(resampling);

        Controls.Add(Label("Anisotropic texture filtering", 24, 347, 490, 25));
        Configure(filtering, 372, ["Off", "2×", "4×", "8×", "16× (default)"]);
        Controls.Add(Label("Antialiasing", 24, 413, 490, 25));
        Configure(antialiasing, 438, ["Off", "2×", "4× (default)", "8×"]);

        loading.Text = "Reading installed settings…";
        loading.Location = new Point(24, 482);
        loading.Size = new Size(490, 20);
        Controls.Add(loading);

        Controls.Add(Label(
            "Preview 4:3 pillarboxes for standard and ultrawide screens. Launch uses your current Windows monitor. Only the listed render modes are safe for MW4's HUD.",
            24, 507, 490, 48, 9F));

        apply.Text = "APPLY";
        apply.Location = new Point(370, 571);
        apply.Size = new Size(144, 32);
        apply.Enabled = false;
        apply.DialogResult = DialogResult.OK;
        var cancel = new Button
        {
            Text = "CANCEL",
            Location = new Point(242, 571),
            Size = new Size(116, 32),
            DialogResult = DialogResult.Cancel,
        };
        Controls.Add(apply);
        Controls.Add(cancel);
        AcceptButton = apply;
        CancelButton = cancel;
    }

    public GraphicsChoices Selection => new(((Option)resampling.SelectedItem!).Value,
        (TextureFiltering)filtering.SelectedIndex, (DisplayAntialiasing)antialiasing.SelectedIndex);
    public GameResolution? SelectedResolution => ((ResolutionOption)resolution.SelectedItem!).Value;

    private void UpdateGeometry()
    {
        var preview = ((DisplayPreviewOption)displayPreview.SelectedItem!).Geometry;
        var selected = SelectedResolution ?? GameResolutionPreset.BestFitting(activeGeometry.Gameplay);
        gameplayLine.Text = $"Launch render: {selected.Width} × {selected.Height}; preview image: {preview.Gameplay.Width} × {preview.Gameplay.Height}";
        barsLine.Text = $"Pillarboxes: {preview.LeftPillarboxWidth}px each side; letterboxes: {preview.TopLetterboxHeight}px top/bottom";
        loading.Text = selected.Width > activeGeometry.Gameplay.Width || selected.Height > activeGeometry.Gameplay.Height
            ? "Above monitor size: downsampling is experimental; check gameplay."
            : string.Empty;
    }

    public void SetCurrent(GraphicsChoices? current, GameResolution? selectedResolution)
    {
        var choices = current ?? GraphicsChoices.Default;
        resampling.SelectedIndex = resampling.Items.Cast<Option>().ToList()
            .FindIndex(option => option.Value == choices.Resampling);
        filtering.SelectedIndex = (int)choices.Filtering;
        antialiasing.SelectedIndex = (int)choices.Antialiasing;
        var selectedIndex = resolution.Items.Cast<ResolutionOption>().ToList()
            .FindIndex(option => option.Value == selectedResolution);
        resolution.SelectedIndex = Math.Max(0, selectedIndex);
        UpdateGeometry();
        if (selectedResolution is not null && selectedIndex < 0)
            loading.Text = "Saved render mode is unsupported; Automatic will be used at launch.";
        else if (current is null)
            loading.Text = "Installed games differ; Apply will unify their settings.";
        apply.Enabled = true;
    }

    private void Configure(ComboBox box, int y, string[] values)
    {
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Location = new Point(24, y);
        box.Width = 490;
        box.Items.AddRange(values);
        box.SelectedIndex = 0;
        Controls.Add(box);
    }

    private static Label Label(string text, int x, int y, int width, int height, float size = 10F, bool bold = false) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = Color.FromArgb(230, 233, 229),
        };

    private sealed record Option(string Name, DisplayResampling Value)
    {
        public override string ToString() => Name;
    }

    private sealed record ResolutionOption(string Name, GameResolution? Value)
    {
        public override string ToString() => Name;
    }

    private sealed record DisplayPreviewOption(string Name, GameDisplayGeometry Geometry)
    {
        public override string ToString() => Name;
    }
}
