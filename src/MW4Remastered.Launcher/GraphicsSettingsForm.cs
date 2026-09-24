using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Launcher;

internal sealed class GraphicsSettingsForm : Form
{
    private readonly ComboBox resampling = new();
    private readonly ComboBox displayPreview = new();
    private readonly ComboBox filtering = new();
    private readonly ComboBox antialiasing = new();
    private readonly Label loading = new();
    private readonly Label pictureLine = new();
    private readonly Button apply = new();

    public GraphicsSettingsForm(GameDisplayGeometry geometry)
    {
        Text = "Display settings";
        ClientSize = new Size(540, 525);
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
        pictureLine.Location = new Point(24, 150);
        pictureLine.Size = new Size(490, 25);
        Controls.Add(pictureLine);
        UpdateGeometry();
        Controls.Add(Label("Display scaling filter", 24, 194, 490, 25));

        resampling.DropDownStyle = ComboBoxStyle.DropDownList;
        resampling.Location = new Point(24, 219);
        resampling.Width = 490;
        resampling.Items.AddRange(
        [
            new Option("High quality (default)", DisplayResampling.Lanczos3),
            new Option("Soft", DisplayResampling.Bilinear),
            new Option("Sharp pixels", DisplayResampling.PointSampled),
        ]);
        resampling.SelectedIndex = 0;
        Controls.Add(resampling);

        Controls.Add(Label("Anisotropic texture filtering", 24, 260, 490, 25));
        Configure(filtering, 285, ["Off", "2×", "4×", "8×", "16× (default)"]);
        Controls.Add(Label("Antialiasing", 24, 326, 490, 25));
        Configure(antialiasing, 351, ["Off", "2×", "4× (default)", "8×"]);

        loading.Text = "Reading installed settings…";
        loading.Location = new Point(24, 395);
        loading.Size = new Size(490, 20);
        Controls.Add(loading);

        Controls.Add(Label(
            "Launch uses your current Windows monitor and automatically selects a safe 4:3 render size. The preview only shows how the picture is framed.",
            24, 422, 490, 42, 9F));

        apply.Text = "APPLY";
        apply.Location = new Point(370, 477);
        apply.Size = new Size(144, 32);
        apply.Enabled = false;
        apply.DialogResult = DialogResult.OK;
        var cancel = new Button
        {
            Text = "CANCEL",
            Location = new Point(242, 477),
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
    private void UpdateGeometry()
    {
        var preview = ((DisplayPreviewOption)displayPreview.SelectedItem!).Geometry;
        pictureLine.Text = $"Centered 4:3 picture: {preview.Gameplay.Width} × {preview.Gameplay.Height}";
    }

    public void SetCurrent(GraphicsChoices? current)
    {
        var choices = current ?? GraphicsChoices.Default;
        resampling.SelectedIndex = resampling.Items.Cast<Option>().ToList()
            .FindIndex(option => option.Value == choices.Resampling);
        filtering.SelectedIndex = (int)choices.Filtering;
        antialiasing.SelectedIndex = (int)choices.Antialiasing;
        if (current is null)
            loading.Text = "Installed games differ; Apply will unify their settings.";
        else
            loading.Text = string.Empty;
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

    private sealed record DisplayPreviewOption(string Name, GameDisplayGeometry Geometry)
    {
        public override string ToString() => Name;
    }
}
