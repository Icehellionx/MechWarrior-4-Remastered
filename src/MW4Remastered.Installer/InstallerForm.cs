using System.ComponentModel;
using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal sealed class InstallerForm : Form
{
    private static readonly Color Armor = Color.FromArgb(39, 48, 51);
    private static readonly Color Panel = Color.FromArgb(22, 29, 31);
    private static readonly Color PanelEdge = Color.FromArgb(70, 83, 84);
    private static readonly Color Amber = Color.FromArgb(220, 157, 54);
    private static readonly Color TextColor = Color.FromArgb(224, 230, 224);
    private static readonly Color Muted = Color.FromArgb(139, 151, 149);
    private static readonly Color Ready = Color.FromArgb(126, 190, 124);
    private static readonly Color Warning = Color.FromArgb(220, 116, 70);

    private readonly MediaSourceInspector inspector;
    private readonly MediaSelectionSet selection;
    private readonly MediaSelectionSessionFactory selectionSessions;
    private readonly InstallDestinationPlanner destinationPlanner;
    private readonly Dictionary<string, CapabilityCard> cards = new(StringComparer.OrdinalIgnoreCase);
    private readonly ListBox evidenceList = new();
    private readonly Label exclusionStatus = new();
    private readonly Label operationStatus = new();
    private readonly ProgressBar progress = new();
    private readonly Button addFilesButton = new();
    private readonly Button addFolderButton = new();
    private readonly Button cancelButton = new();
    private readonly Button revalidateButton = new();
    private readonly Button installButton = new();
    private readonly TextBox destinationText = new();
    private readonly Label destinationStatus = new();
    private readonly Button destinationButton = new();
    private CancellationTokenSource? operationCancellation;

    public InstallerForm(
        MediaSourceInspector inspector,
        MediaSelectionSet selection,
        MediaSelectionSessionFactory selectionSessions,
        InstallDestinationPlanner destinationPlanner)
    {
        this.inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        this.selection = selection ?? throw new ArgumentNullException(nameof(selection));
        this.selectionSessions = selectionSessions ?? throw new ArgumentNullException(nameof(selectionSessions));
        this.destinationPlanner = destinationPlanner ?? throw new ArgumentNullException(nameof(destinationPlanner));

        Text = "MechWarrior 4 Remastered Setup";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Armor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(980, 680);
        Size = new Size(1120, 760);
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(CreateRootLayout());
        RefreshSnapshot(selection.Current);
    }

    private Control CreateRootLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(34, 26, 34, 28),
            ColumnCount = 1,
            RowCount = 6,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateCapabilityGrid(), 0, 1);
        root.Controls.Add(CreateSourceToolbar(), 0, 2);
        root.Controls.Add(CreateDestinationPanel(), 0, 3);
        root.Controls.Add(CreateEvidencePanel(), 0, 4);
        root.Controls.Add(CreateFooter(), 0, 5);
        return root;
    }

    private Control CreateDestinationPanel()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 4,
            Margin = new Padding(0, 0, 0, 12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Muted,
            Text = "INSTALL TO",
            Margin = new Padding(0, 8, 12, 0),
        }, 0, 0);

        destinationText.Dock = DockStyle.Fill;
        destinationText.BackColor = Panel;
        destinationText.ForeColor = TextColor;
        destinationText.BorderStyle = BorderStyle.FixedSingle;
        destinationText.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MechWarrior 4 Remastered");
        destinationText.TextChanged += (_, _) => RefreshDestinationPlan();
        panel.Controls.Add(destinationText, 1, 0);

        ConfigureSourceButton(destinationButton, "BROWSE");
        destinationButton.Click += (_, _) => SelectDestination();
        panel.Controls.Add(destinationButton, 2, 0);

        destinationStatus.AutoSize = true;
        destinationStatus.ForeColor = Muted;
        destinationStatus.Margin = new Padding(0, 5, 0, 0);
        panel.SetColumnSpan(destinationStatus, 3);
        panel.Controls.Add(destinationStatus, 0, 1);
        return panel;
    }

    private void SelectDestination()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose the MechWarrior 4 Remastered install folder",
            SelectedPath = destinationText.Text,
            ShowNewFolderButton = true,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) destinationText.Text = dialog.SelectedPath;
    }

    private static Control CreateHeader()
    {
        var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, Margin = new Padding(0, 0, 0, 16) };
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 25F),
            ForeColor = Amber,
            Text = "MECHWARRIOR 4  /  REMASTERED",
        });
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Muted,
            Text = "MEDIA INTAKE  •  SELECT ORIGINAL DISCS OR ISO-ONLY ARCHIVES",
            Margin = new Padding(3, 2, 0, 0),
        });
        return panel;
    }

    private Control CreateCapabilityGrid()
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 5,
            Margin = new Padding(0, 0, 0, 18),
        };
        for (var index = 0; index < 5; index++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

        foreach (var product in ProductCatalog.All)
        {
            var card = new CapabilityCard(product);
            cards.Add(product.Id, card);
            grid.Controls.Add(card.Panel);
        }
        return grid;
    }

    private Control CreateSourceToolbar()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            Margin = new Padding(0, 0, 0, 12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        ConfigureSourceButton(addFilesButton, "ADD ISO / ZIP");
        ConfigureSourceButton(addFolderButton, "ADD MOUNTED FOLDER");
        addFilesButton.Click += async (_, _) => await SelectFilesAsync();
        addFolderButton.Click += async (_, _) => await SelectFolderAsync();
        ConfigureSourceButton(cancelButton, "CANCEL");
        cancelButton.Enabled = false;
        cancelButton.Click += (_, _) => CancelOperation();

        operationStatus.AutoSize = true;
        operationStatus.Anchor = AnchorStyles.Left;
        operationStatus.ForeColor = Muted;
        operationStatus.Margin = new Padding(16, 0, 0, 0);
        operationStatus.Text = "Waiting for original media.";
        panel.Controls.Add(addFilesButton, 0, 0);
        panel.Controls.Add(addFolderButton, 1, 0);
        panel.Controls.Add(cancelButton, 2, 0);
        panel.Controls.Add(operationStatus, 3, 0);
        return panel;
    }

    private Control CreateEvidencePanel()
    {
        var panel = new TableLayoutPanel
        {
            BackColor = Panel,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12F),
            ForeColor = TextColor,
            Text = "VALIDATED MEDIA",
            Margin = new Padding(0, 0, 0, 10),
        }, 0, 0);

        evidenceList.Dock = DockStyle.Fill;
        evidenceList.BackColor = Panel;
        evidenceList.ForeColor = TextColor;
        evidenceList.BorderStyle = BorderStyle.None;
        evidenceList.IntegralHeight = false;
        panel.Controls.Add(evidenceList, 0, 1);

        exclusionStatus.AutoSize = true;
        exclusionStatus.ForeColor = Muted;
        exclusionStatus.Margin = new Padding(0, 10, 0, 5);
        panel.Controls.Add(exclusionStatus, 0, 2);

        progress.Dock = DockStyle.Fill;
        progress.Height = 5;
        progress.Style = ProgressBarStyle.Marquee;
        progress.MarqueeAnimationSpeed = 24;
        progress.Visible = false;
        panel.Controls.Add(progress, 0, 3);
        return panel;
    }

    private Control CreateFooter()
    {
        var footer = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            ColumnCount = 3,
            Margin = new Padding(0, 18, 0, 0),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Muted,
            Text = "Selection is read-only. Installation stays locked until the patch and source-lifetime pipeline is qualified.",
        }, 0, 0);

        ConfigureSourceButton(revalidateButton, "REVALIDATE MEDIA");
        revalidateButton.Enabled = false;
        revalidateButton.Click += async (_, _) => await RevalidateSelectionAsync();
        footer.Controls.Add(revalidateButton, 1, 0);

        installButton.AutoSize = true;
        installButton.Enabled = false;
        installButton.FlatStyle = FlatStyle.Flat;
        installButton.BackColor = Amber;
        installButton.ForeColor = Color.Black;
        installButton.FlatAppearance.BorderSize = 0;
        installButton.Padding = new Padding(16, 7, 16, 7);
        installButton.Text = "INSTALLATION LOCKED";
        footer.Controls.Add(installButton, 2, 0);
        return footer;
    }

    private async Task SelectFilesAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Supported media (*.iso;*.zip)|*.iso;*.zip|Disc images (*.iso)|*.iso|ISO archives (*.zip)|*.zip",
            Multiselect = true,
            Title = "Select MechWarrior 4 media",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) await InspectSourcesAsync(dialog.FileNames);
    }

    private async Task SelectFolderAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a mounted or extracted original MechWarrior 4 disc",
            ShowNewFolderButton = false,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) await InspectSourcesAsync(new[] { dialog.SelectedPath });
    }

    private async Task InspectSourcesAsync(IReadOnlyList<string> paths)
    {
        SetBusy(true);
        var cancellationToken = operationCancellation!.Token;
        var errors = new List<string>();
        var cancelled = false;
        try
        {
            foreach (var path in paths)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    break;
                }
                operationStatus.Text = $"Inspecting {Path.GetFileName(path)}…";
                try
                {
                    var inspection = await Task.Run(() => inspector.Inspect(path, cancellationToken), cancellationToken);
                    RefreshSnapshot(selection.Add(path, inspection));
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or TimeoutException)
                {
                    errors.Add($"{Path.GetFileName(path)}: {error.Message}");
                }
            }
        }
        finally
        {
            SetBusy(false);
        }

        operationStatus.Text = cancelled ? "Media inspection cancelled; owned temporary resources were released."
            : errors.Count == 0 ? "Media inspection complete." : $"Completed with {errors.Count} rejected source(s).";
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine + Environment.NewLine, errors), "Media not accepted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task RevalidateSelectionAsync()
    {
        SetBusy(true);
        var cancellationToken = operationCancellation!.Token;
        operationStatus.Text = "Reopening and revalidating selected media…";
        try
        {
            var count = await Task.Run(() =>
            {
                using var openSelection = selectionSessions.Open(selection.Current, cancellationToken);
                return openSelection.Layouts.Count;
            }, cancellationToken);
            operationStatus.Text = $"Revalidated {count} selected media layout(s); all owned resources were released.";
        }
        catch (OperationCanceledException)
        {
            operationStatus.Text = "Media revalidation cancelled; owned temporary resources were released.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or TimeoutException)
        {
            operationStatus.Text = "Media revalidation failed.";
            MessageBox.Show(this, error.Message, "Media changed or unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RefreshSnapshot(MediaSelectionSnapshot snapshot)
    {
        foreach (var capability in snapshot.Capabilities) cards[capability.ProductId].Update(capability);

        evidenceList.BeginUpdate();
        evidenceList.Items.Clear();
        foreach (var media in snapshot.Layouts)
        {
            var container = media.ArchiveRelativePath is null ? string.Empty : $"  [{media.ArchiveRelativePath}]";
            evidenceList.Items.Add($"✓  {media.Layout.DisplayName}{container}  —  {media.SourcePath}");
        }
        if (snapshot.Layouts.Count == 0) evidenceList.Items.Add("No supported media selected yet.");
        evidenceList.EndUpdate();

        exclusionStatus.Text = snapshot.ExcludedContentCount == 0
            ? "No prohibited or unrelated archive content encountered."
            : $"{snapshot.ExcludedContentCount} prohibited or unrelated item(s) identified and excluded; none were imported.";
        exclusionStatus.ForeColor = snapshot.ExcludedContentCount == 0 ? Muted : Warning;
        revalidateButton.Enabled = snapshot.Layouts.Count > 0 && !UseWaitCursor;
        RefreshDestinationPlan();
    }

    private void RefreshDestinationPlan()
    {
        try
        {
            var plan = destinationPlanner.Plan(selection.Current, destinationText.Text);
            if (!plan.HasSelectedGames)
            {
                destinationStatus.ForeColor = Muted;
                destinationStatus.Text = "Select complete game media to calculate destination space.";
                return;
            }

            destinationStatus.ForeColor = plan.HasEnoughSpace ? Ready : Warning;
            destinationStatus.Text = $"{plan.Products.Count} game(s) planned  •  {FormatBytes(plan.RequiredBytes)} required  •  {FormatBytes(plan.AvailableBytes)} available";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or DirectoryNotFoundException)
        {
            destinationStatus.ForeColor = Warning;
            destinationStatus.Text = error.Message;
        }
    }

    private void SetBusy(bool busy)
    {
        if (busy)
        {
            operationCancellation?.Dispose();
            operationCancellation = new CancellationTokenSource();
        }
        addFilesButton.Enabled = !busy;
        addFolderButton.Enabled = !busy;
        cancelButton.Enabled = busy;
        destinationText.Enabled = !busy;
        destinationButton.Enabled = !busy;
        revalidateButton.Enabled = !busy && selection.Current.Layouts.Count > 0;
        progress.Visible = busy;
        UseWaitCursor = busy;
        if (!busy)
        {
            operationCancellation?.Dispose();
            operationCancellation = null;
        }
    }

    private void CancelOperation()
    {
        cancelButton.Enabled = false;
        operationStatus.Text = "Cancelling after the current safe cleanup boundary…";
        operationCancellation?.Cancel();
    }

    private static string FormatBytes(long bytes)
    {
        const double gibibyte = 1024d * 1024d * 1024d;
        return bytes >= gibibyte
            ? $"{bytes / gibibyte:0.0} GiB"
            : $"{bytes / (1024d * 1024d):0} MiB";
    }

    private static void ConfigureSourceButton(Button button, string text)
    {
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = PanelEdge;
        button.BackColor = Panel;
        button.ForeColor = TextColor;
        button.Margin = new Padding(0, 0, 10, 0);
        button.Padding = new Padding(12, 6, 12, 6);
        button.Text = text;
    }

    private sealed class CapabilityCard
    {
        private readonly Label status;
        private readonly Label detail;

        public CapabilityCard(ProductDefinition product)
        {
            Panel = new Panel { BackColor = InstallerForm.Panel, Height = 112, Dock = DockStyle.Fill, Margin = new Padding(5) };
            var name = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                Padding = new Padding(10, 10, 10, 0),
                Font = new Font("Segoe UI Semibold", product.Kind == ProductKind.Game ? 11F : 9.5F),
                ForeColor = TextColor,
                Text = product.DisplayName.ToUpperInvariant(),
            };
            status = new Label { Dock = DockStyle.Top, Height = 28, Padding = new Padding(10, 3, 10, 0) };
            detail = new Label { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 6), ForeColor = Muted, Font = new Font("Segoe UI", 8.5F) };
            Panel.Controls.Add(detail);
            Panel.Controls.Add(status);
            Panel.Controls.Add(name);
        }

        public Panel Panel { get; }

        public void Update(MediaCapabilityStatus capability)
        {
            status.ForeColor = capability.IsComplete ? Ready : Muted;
            status.Text = capability.IsComplete ? "●  MEDIA READY" : "○  MEDIA NEEDED";
            detail.Text = capability.IsComplete
                ? capability.Kind == ProductKind.Game ? "All required discs validated" : "Optional content detected"
                : $"{capability.PresentLayoutIds.Count} / {capability.RequiredLayoutIds.Count} required source(s)";
        }
    }
}
