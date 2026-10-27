using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

internal static class Program
{
    private const float SyntheticScale = 2.5F;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [STAThread]
    private static int Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        try
        {
            CheckFooterLayout();
            Console.WriteLine("Launcher footer layout smoke passed (native DPI and synthetic 2.5x constrained scaling).");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void CheckFooterLayout()
    {
        var type = Assembly.Load("MW4RemasteredLauncher").GetType("MW4Remastered.Launcher.MainForm", throwOnError: true)!;
        var fixtureRoot = Path.Combine(Path.GetTempPath(), "mw4-launcher-layout-" + Guid.NewGuid().ToString("N"));
        var processes = new RejectProcessActions();
        var registration = new RejectRegistrationActions();
        using var form = (Form)Activator.CreateInstance(type,
            new InstallStatusReader(fixtureRoot), new LaunchOrchestrator(processes, registration),
            new DocumentOpener(processes), new OwnedInstallUninstaller(),
            new ApplicationUninstallOrchestrator(fixtureRoot, processes), registration,
            new NoOpGameWindowLifecycleGuard(), new GraphicsSettingsService(fixtureRoot),
            new InstallationDiagnosticsService(), new JoystickLaunchPreference(Path.Combine(fixtureRoot, "preference.txt")))!;

        // Do not Show the form or invoke user events: construction/layout must
        // not start games, inspect media, write registration or save preferences.
        var names = new[] { "joystickButton", "settingsButton", "diagnosticsButton", "creditsButton", "uninstallButton" };
        var buttons = names.Select(name => (Name: name, Button: (Button)Field(type, name).GetValue(form)!)).ToArray();
        Check(buttons.Select(item => item.Button.Parent).Distinct().Count() == 1,
            "The five actions must belong to one footer.");
        var footer = buttons[0].Button.Parent as TableLayoutPanel
            ?? throw new InvalidOperationException("The actual launcher footer is missing.");
        Layout(form);
        var nativeScale = form.DeviceDpi / 96F;
        CheckStatesAndResize("native", nativeScale);

        // Hidden ancestors do not participate in TableLayout sizing. Temporarily
        // detach the real footer so the production controls exercise its engine
        // without displaying a window or triggering MainForm.Shown.
        var owner = footer.Parent as TableLayoutPanel
            ?? throw new InvalidOperationException("The footer's owning layout is missing.");
        var cell = owner.GetPositionFromControl(footer);
        var dock = footer.Dock;
        var normalSize = footer.Size;
        owner.Controls.Remove(footer);
        try
        {
            footer.Dock = DockStyle.None;
            footer.Size = new Size(normalSize.Width, footer.GetRowHeights()[0]);
            Layout(footer);
            Check(footer.GetRowHeights()[1] == 0, "The regression must constrain the action row before scaling.");

            // The old launcher shrinks each anchored action to 1px here. Scaling
            // those constrained bounds turns the stored height into 2px, which
            // ordinary resize/maximize cannot recover. Scale the actual controls,
            // not a duplicate of the footer's layout or a minimum-size property.
            footer.Scale(new SizeF(SyntheticScale, SyntheticScale));
            form.Scale(new SizeF(SyntheticScale, SyntheticScale));
            footer.Size = new Size((int)Math.Round(normalSize.Width * SyntheticScale),
                (int)Math.Round(normalSize.Height * SyntheticScale));
            Layout(footer);
        }
        finally
        {
            owner.Controls.Add(footer, cell.Column, cell.Row);
            footer.Dock = dock;
        }
        Layout(form);
        CheckStatesAndResize("synthetic-2.5x", nativeScale * SyntheticScale);
        Check(!Directory.Exists(fixtureRoot), "Layout must not create installation or preference files.");

        void CheckStatesAndResize(string stage, float scale)
        {
            foreach (var joystickOn in new[] { true, false })
            foreach (var enabled in new[] { true, false })
            {
                Field(type, "joystickEnabled").SetValue(form, joystickOn);
                (type.GetMethod("UpdateJoystickButton", PrivateInstance)
                    ?? throw new MissingMethodException(type.FullName, "UpdateJoystickButton")).Invoke(form, null);
                foreach (var item in buttons) item.Button.Enabled = enabled;
                var captions = new[] { joystickOn ? "JOYSTICK ON" : "JOYSTICK OFF", "SETTINGS", "DIAGNOSTICS", "CREDITS", "UNINSTALL" };
                Check(buttons.Select(item => item.Button.Text).SequenceEqual(captions), "Footer captions are incorrect.");
                var scenario = $"{stage}, joystick={joystickOn}, enabled={enabled}";
                Layout(form);
                CheckButtons(scenario, scale);
                var original = form.ClientSize;
                form.ClientSize = new Size(original.Width + 220, original.Height + 140);
                Layout(form);
                CheckButtons(scenario + ", expanded", scale);
                form.ClientSize = original;
                Layout(form);
                CheckButtons(scenario + ", restored", scale);
            }
        }

        void CheckButtons(string stage, float scale)
        {
            foreach (var item in buttons)
            {
                var button = item.Button;
                var textHeight = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty,
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
                // The disabled production painter reserves 6px above/below text.
                var requiredHeight = Math.Max((int)Math.Ceiling(32 * scale), textHeight + 12);
                Check(button.ClientSize.Height >= requiredHeight,
                    $"{stage}: {item.Name} has client height {button.ClientSize.Height}; readable height requires {requiredHeight}.");
                Check(footer.ClientRectangle.Contains(button.Bounds),
                    $"{stage}: {item.Name} is outside its footer.");
            }
        }
    }

    private static FieldInfo Field(Type type, string name) => type.GetField(name, PrivateInstance)
        ?? throw new MissingFieldException(type.FullName, name);

    private static void Layout(Control parent)
    {
        parent.PerformLayout();
        foreach (Control child in parent.Controls) Layout(child);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class RejectProcessActions : IProcessStarter
    {
        public int Start(ProcessStartInfo startInfo) => throw new InvalidOperationException("Layout invoked a process action.");
    }

    private sealed class RejectRegistrationActions : ILegacyGameRegistration
    {
        public void Ensure(ProductStatus status) => throw new InvalidOperationException("Layout invoked registration.");
        public void ValidateOwned(ProductStatus status) => throw new InvalidOperationException("Layout invoked registration.");
        public void RemoveOwned(ProductStatus status) => throw new InvalidOperationException("Layout invoked registration.");
    }
}
