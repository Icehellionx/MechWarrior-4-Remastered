namespace MW4Remastered.Launcher;

internal sealed class DiagnosticsForm : Form
{
    public DiagnosticsForm(string report)
    {
        Text = "Installation diagnostics";
        ClientSize = new Size(680, 480);
        MinimumSize = new Size(560, 360);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(16, 20, 21);
        ForeColor = Color.FromArgb(230, 233, 229);
        Font = new Font("Segoe UI", 10F);

        var output = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9F),
            Text = report,
        };
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
        };
        var close = new Button { Text = "CLOSE", Width = 105, Height = 32, DialogResult = DialogResult.Cancel };
        var copy = new Button { Text = "COPY REPORT", Width = 125, Height = 32 };
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(report); }
            catch (System.Runtime.InteropServices.ExternalException error)
            {
                MessageBox.Show(this, error.Message, "Clipboard unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        footer.Controls.Add(close);
        footer.Controls.Add(copy);
        Controls.Add(output);
        Controls.Add(footer);
        CancelButton = close;
    }
}
