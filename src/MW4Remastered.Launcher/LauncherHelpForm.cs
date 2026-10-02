namespace MW4Remastered.Launcher;

internal sealed class LauncherHelpForm : Form
{
    public LauncherHelpForm()
    {
        Text = "Help & quirks";
        ClientSize = new Size(680, 520);
        MinimumSize = new Size(540, 380);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(16, 20, 21);
        ForeColor = Color.FromArgb(230, 233, 229);
        Font = new Font("Segoe UI", 10F);

        var content = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            ForeColor = ForeColor,
            BorderStyle = BorderStyle.None,
            AccessibleName = "Known quirks and workarounds",
            Text = """
JOYSTICK MODE
Joystick input starts ON unless you previously saved OFF. The button shows the current choice. Change it before starting a game; it applies on the next launch. OFF keeps keyboard/mouse controls and bypasses native joystick input. Your saved choice is kept across launcher restarts.

CONTROLLER BUTTON LIMITS
MW4 can crash when a controller exposes 32 or more buttons, even if you bind fewer. The game-local adapter now exposes at most 31 joystick buttons to these games. Extra buttons are omitted safely; they are not automatically mapped to actions or keys.

Some buttons within that limit may still be ignored by the original game's binding screen. Map extra or unrecognized buttons to keyboard keys with your controller software or a user-configured tool such as Joystick Gremlin. The adapter does not expand the original game's binding range.

MULTIPLE DEVICES AND MAPPING
Select the intended joystick in the game's Controls screen. Combining stick, throttle, and pedals may require a user-configured virtual device. The launcher installs no virtual joystick driver and does not combine devices. Confirm the logical device's axes and buttons in Windows before mapping in-game.

Bind from the main menu initially and save before launching. The adapter has passed native 31/32/128-button virtual-device tests. Physical HOTAS, multiple-device combinations, and force feedback still need field testing. If native input crashes, try JOYSTICK OFF with buttons mapped to keyboard keys; native analog joystick input will be unavailable.

ADAPTER REPAIR
If launch reports a missing or unrecognized DirectInput adapter, rerun Setup to repair the affected game. Joystick mode stays at your chosen setting. A broken adapter in another game does not disable joystick input for this one.

DISPLAY AND SAVES
The centered 4:3 picture and side bars preserve the original presentation. SETTINGS adjusts filtering and antialiasing. DIAGNOSTICS provides a shareable installation report. Uninstall preserves user saves and configuration under the project's preservation policy.
""".Replace("\r\n", "\n").Replace("\n", "\r\n"),
        };
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
        };
        var close = new Button { Text = "CLOSE", Width = 105, Height = 32, DialogResult = DialogResult.Cancel };
        footer.Controls.Add(close);
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
        body.Controls.Add(content);
        Controls.Add(body);
        Controls.Add(footer);
        CancelButton = close;
        Shown += (_, _) =>
        {
            content.Select(0, 0);
            ActiveControl = close;
        };
    }
}
