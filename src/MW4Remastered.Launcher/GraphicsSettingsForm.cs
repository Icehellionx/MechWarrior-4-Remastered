using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Launcher;

internal sealed class GraphicsSettingsForm : Form
{
    private readonly GameDisplayGeometry geometry;
    private readonly ComboBox resampling = new();
    private readonly ComboBox resolution = new();
    private readonly ComboBox filtering = new();
    private readonly ComboBox antialiasing = new();
    private readonly Label loading = new();
    private readonly Label gameplayLine = new();
    private readonly Label barsLine = new();
    private readonly Button apply = new();

    public GraphicsSettingsForm(GameDisplayGeometry geometry)
    {
        this.geometry = geometry;
        Text = "Display settings";
        ClientSize = new Size(540, 544);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(16, 20, 21);
        ForeColor = Color.FromArgb(230, 233, 229);
        Font = new Font("Segoe UI", 10F);

        Controls.Add(Label("DISPLAY SETTINGS", 22, 18, 490, 30, 17F, true));
        Controls.Add(Label($"Monitor: {geometry.Monitor.Width} × {geometry.Monitor.Height}", 24, 62, 490, 25));
        gameplayLine.Location = new Point(24, 91);
        gameplayLine.Size = new Size(490, 25);
        barsLine.Location = new Point(24, 120);
        barsLine.Size = new Size(490, 25);
        Controls.Add(gameplayLine);
        Controls.Add(barsLine);
        Controls.Add(Label("Gameplay resolution (4:3)", 24, 157, 490, 25));
        resolution.DropDownStyle = ComboBoxStyle.DropDownList;
        resolution.Location = new Point(24, 181);
        resolution.Width = 490;
        resolution.Items.Add(new ResolutionOption("Automatic (largest listed game mode fitting this monitor)", null));
        foreach (var preset in GameResolutionPreset.Choices)
            resolution.Items.Add(new ResolutionOption(preset.Label, preset.Resolution));
        resolution.SelectedIndex = 0;
        resolution.SelectedIndexChanged += (_, _) => UpdateGeometry(geometry);
        Controls.Add(resolution);
        UpdateGeometry(geometry);
        Controls.Add(Label("Display scaling filter", 24, 222, 490, 25));

        resampling.DropDownStyle = ComboBoxStyle.DropDownList;
        resampling.Location = new Point(24, 247);
        resampling.Width = 490;
        resampling.Items.AddRange(
        [
            new Option("High quality (default)", DisplayResampling.Lanczos3),
            new Option("Soft", DisplayResampling.Bilinear),
            new Option("Sharp pixels", DisplayResampling.PointSampled),
        ]);
        resampling.SelectedIndex = 0;
        Controls.Add(resampling);

        Controls.Add(Label("Anisotropic texture filtering", 24, 288, 490, 25));
        Configure(filtering, 313, ["Off", "2×", "4×", "8×", "16× (default)"]);
        Controls.Add(Label("Antialiasing", 24, 354, 490, 25));
        Configure(antialiasing, 379, ["Off", "2×", "4× (default)", "8×"]);

        loading.Text = "Reading installed settings…";
        loading.Location = new Point(24, 422);
        loading.Size = new Size(490, 20);
        Controls.Add(loading);

        Controls.Add(Label(
            "The 4:3 display area may exceed the game render size. The original HUD supports render heights of 600, 768 and 1200 here; other 4:3 modes need a game fix.",
            24, 449, 490, 37, 9F));

        apply.Text = "APPLY";
        apply.Location = new Point(370, 495);
        apply.Size = new Size(144, 32);
        apply.Enabled = false;
        apply.DialogResult = DialogResult.OK;
        var cancel = new Button
        {
            Text = "CANCEL",
            Location = new Point(242, 495),
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

    private void UpdateGeometry(GameDisplayGeometry monitorGeometry)
    {
        var selected = SelectedResolution ?? GameResolutionPreset.BestFitting(monitorGeometry.Gameplay);
        gameplayLine.Text = $"Render: {selected.Width} × {selected.Height}; displayed: {monitorGeometry.Gameplay.Width} × {monitorGeometry.Gameplay.Height} (4:3)";
        barsLine.Text = $"Bars: {monitorGeometry.LeftPillarboxWidth}px sides, {monitorGeometry.TopLetterboxHeight}px top/bottom";
        loading.Text = selected.Width > monitorGeometry.Gameplay.Width || selected.Height > monitorGeometry.Gameplay.Height
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
        UpdateGeometry(geometry);
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
}
