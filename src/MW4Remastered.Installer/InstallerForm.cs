using System.ComponentModel;
using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
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
    private readonly GameInstallationCoordinator installationCoordinator;
    private readonly InstalledLauncherOrchestrator installedLauncher;
    private readonly IReadOnlyList<string> initialMediaPaths;
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
    private InstallDestinationPlan? currentDestinationPlan;

    public InstallerForm(
        MediaSourceInspector inspector,
        MediaSelectionSet selection,
        MediaSelectionSessionFactory selectionSessions,
        InstallDestinationPlanner destinationPlanner,
        GameInstallationCoordinator installationCoordinator,
        InstalledLauncherOrchestrator installedLauncher,
        IReadOnlyList<string> initialMediaPaths)
    {
        this.inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        this.selection = selection ?? throw new ArgumentNullException(nameof(selection));
        this.selectionSessions = selectionSessions ?? throw new ArgumentNullException(nameof(selectionSessions));
        this.destinationPlanner = destinationPlanner ?? throw new ArgumentNullException(nameof(destinationPlanner));
        this.installationCoordinator = installationCoordinator ?? throw new ArgumentNullException(nameof(installationCoordinator));
        this.installedLauncher = installedLauncher ?? throw new ArgumentNullException(nameof(installedLauncher));
        this.initialMediaPaths = initialMediaPaths ?? throw new ArgumentNullException(nameof(initialMediaPaths));

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
        Shown += async (_, _) => await InspectInitialMediaAsync();
    }

    private async Task InspectInitialMediaAsync()
    {
        if (initialMediaPaths.Count > 0) await InspectSourcesAsync(initialMediaPaths);
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
            Text = "STEP 1 OF 2  •  ADD ORIGINAL ISOs OR ZIP FILES CONTAINING THEM",
            Margin = new Padding(3, 2, 0, 0),
        });
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Warning,
            Font = new Font("Segoe UI Semibold", 9F),
            Text = "MEDIA-FIRST SETUP  •  ALL THREE GAMES INSTALL DIRECTLY FROM VALIDATED ORIGINAL MEDIA",
            Margin = new Padding(3, 5, 0, 0),
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

        ConfigureSourceButton(addFilesButton, "1. CHOOSE ISO / ZIP FILES");
        ConfigureSourceButton(addFolderButton, "ADD MOUNTED FOLDER");
        addFilesButton.Click += async (_, _) => await SelectFilesAsync();
        addFolderButton.Click += async (_, _) => await SelectFolderAsync();
        ConfigureSourceButton(cancelButton, "CANCEL");
        cancelButton.Enabled = false;
        cancelButton.Visible = false;
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
            Text = "Validate media, choose a destination, then install every currently supported selected title.",
        }, 0, 0);

        ConfigureSourceButton(revalidateButton, "REVALIDATE MEDIA");
        revalidateButton.Enabled = false;
        revalidateButton.Visible = false;
        revalidateButton.Click += async (_, _) => await RevalidateSelectionAsync();
        footer.Controls.Add(revalidateButton, 1, 0);

        installButton.AutoSize = true;
        installButton.Enabled = false;
        installButton.FlatStyle = FlatStyle.Flat;
        installButton.BackColor = Amber;
        installButton.ForeColor = Color.Black;
        installButton.FlatAppearance.BorderSize = 0;
        installButton.Padding = new Padding(16, 7, 16, 7);
        installButton.Text = "ADD MEDIA TO BEGIN";
        installButton.Click += async (_, _) =>
        {
            if (HaveAllPlannedProductsReady()) OpenInstalledLauncher();
            else await InstallSelectedAsync();
        };
        footer.Controls.Add(installButton, 2, 0);
        return footer;
    }

    private void OpenInstalledLauncher()
    {
        if (!HaveAllPlannedProductsReady()) return;

        try
        {
            installedLauncher.Start();
            Close();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception)
        {
            operationStatus.Text = "The launcher could not be opened.";
            MessageBox.Show(this, error.Message, "Launcher unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task InstallSelectedAsync()
    {
        var plan = currentDestinationPlan;
        if (plan is null || !CanInstallPlan(plan) || !plan.HasEnoughSpace || HasConflictingPlannedDestination()) return;

        SetBusy(true);
        var cancellationToken = operationCancellation!.Token;
        var progressReporter = new Progress<GameInstallationProgress>(value =>
        {
            operationStatus.Text = $"{ProductDisplayName(value.ProductId)} — {value.Message}";
        });
        try
        {
            await Task.Run(() =>
            {
                using var media = selectionSessions.Open(selection.Current, cancellationToken);
                var alreadyReady = GetReadyProductIds(plan);
                foreach (var product in plan.Products.Where(item => !alreadyReady.Contains(item.ProductId)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    installationCoordinator.Install(
                        CreateInstallRequest(product.ProductId, media),
                        product.DestinationPath,
                        progressReporter,
                        cancellationToken);
                }
            }, cancellationToken);

            operationStatus.Text = "Installation completed and verified. Opening the launcher…";
            RefreshDestinationPlan();
            OpenInstalledLauncher();
        }
        catch (OperationCanceledException)
        {
            operationStatus.Text = "Installation cancelled at a safe boundary; committed titles remain ownership-managed.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or Win32Exception or TimeoutException)
        {
            operationStatus.Text = "Installation stopped safely before reporting success.";
            MessageBox.Show(this, error.Message, "Installation could not continue", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshDestinationPlan();
        }
    }

    private static GameInstallRequest CreateInstallRequest(string productId, IMediaSelectionSession media) => productId switch
    {
        "vengeance" => new VengeanceInstallRequest(
            media.GetRoot("vengeance-disc-1"),
            media.GetRoot("vengeance-disc-2")),
        "black-knight" => new BlackKnightInstallRequest(media.GetRoot("black-knight-disc-1")),
        "mercenaries" => new MercenariesInstallRequest(
            media.GetRoot("mercenaries-disc-1"),
            media.GetRoot("mercenaries-disc-2")),
        _ => throw new InvalidOperationException($"{ProductDisplayName(productId)} is not enabled in this installer build."),
    };

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
            : errors.Count == 0 ? GetNextStepText() : $"Completed with {errors.Count} rejected source(s). {GetNextStepText()}";
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
        var completeMediaProducts = snapshot.Capabilities
            .Where(item => item.IsComplete)
            .Select(item => item.ProductId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in snapshot.Capabilities)
            cards[capability.ProductId].Update(capability, completeMediaProducts);

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
        revalidateButton.Visible = snapshot.Layouts.Count > 0;
        RefreshDestinationPlan();
    }

    private void RefreshDestinationPlan()
    {
        currentDestinationPlan = null;
        try
        {
            var preliminaryPlan = destinationPlanner.Plan(selection.Current, destinationText.Text);
            var installedProductIds = new InstallStatusReader(preliminaryPlan.RootPath).Read()
                .Where(item => item.State == ProductInstallState.Ready)
                .Select(item => item.Product.Id);
            var plan = destinationPlanner.Plan(selection.Current, destinationText.Text, installedProductIds);
            currentDestinationPlan = plan;
            if (!plan.HasSelectedGames)
            {
                destinationStatus.ForeColor = plan.BlockedProducts.Count > 0 ? Warning : Muted;
                destinationStatus.Text = plan.BlockedProducts.Count > 0
                    ? FormatBlockedProducts(plan.BlockedProducts)
                    : "Select complete game media to calculate destination space.";
                UpdateInstallAvailability();
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
        UpdateInstallAvailability();
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
        cancelButton.Visible = busy;
        destinationText.Enabled = !busy;
        destinationButton.Enabled = !busy;
        revalidateButton.Enabled = !busy && selection.Current.Layouts.Count > 0;
        revalidateButton.Visible = selection.Current.Layouts.Count > 0;
        progress.Visible = busy;
        UseWaitCursor = busy;
        if (!busy)
        {
            operationCancellation?.Dispose();
            operationCancellation = null;
        }
        UpdateInstallAvailability();
    }

    private void UpdateInstallAvailability()
    {
        var snapshot = selection.Current;
        var alreadyInstalled = HaveAllPlannedProductsReady();
        var existingDestination = HasConflictingPlannedDestination();
        var launcherAvailable = alreadyInstalled && installedLauncher.IsAvailable && !UseWaitCursor;
        var canInstall = currentDestinationPlan is { HasEnoughSpace: true } plan &&
            CanInstallPlan(plan) && !HasConflictingPlannedDestination() && !alreadyInstalled && !UseWaitCursor;

        installButton.Enabled = launcherAvailable || canInstall;
        installButton.Text = launcherAvailable ? "DONE — OPEN LAUNCHER"
            : UseWaitCursor ? "WORKING…"
            : alreadyInstalled ? "INSTALLED — LAUNCHER UNAVAILABLE"
            : existingDestination ? "DESTINATION NEEDS REPAIR"
            : snapshot.Layouts.Count == 0 ? "ADD MEDIA TO VALIDATE"
            : canInstall ? "INSTALL SELECTED GAMES"
            : "SELECTED MEDIA NOT YET SUPPORTED";
        installButton.AccessibleDescription = canInstall
            ? "Install selected supported games directly from the validated original media."
            : "Installation requires a complete supported media set and a new destination.";
    }

    private bool HasConflictingPlannedDestination()
    {
        var plan = currentDestinationPlan;
        if (plan is null) return false;
        var ready = GetReadyProductIds(plan);
        return plan.Products.Any(item => !ready.Contains(item.ProductId) &&
            (Directory.Exists(item.DestinationPath) || File.Exists(item.DestinationPath)));
    }

    private bool HaveAllPlannedProductsReady()
    {
        var plan = currentDestinationPlan;
        if (plan is null || plan.Products.Count == 0) return false;
        var ready = GetReadyProductIds(plan);
        return plan.Products.All(item => ready.Contains(item.ProductId));
    }

    private static HashSet<string> GetReadyProductIds(InstallDestinationPlan plan) =>
        new InstallStatusReader(plan.RootPath).Read()
            .Where(item => item.State == ProductInstallState.Ready)
            .Select(item => item.Product.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private bool CanInstallPlan(InstallDestinationPlan plan)
    {
        if (!plan.HasSelectedGames || plan.BlockedProducts.Count > 0) return false;
        if (plan.Products.Any(item => item.ProductId is not ("vengeance" or "black-knight" or "mercenaries"))) return false;
        var unsupportedPackSelected = selection.Current.Capabilities.Any(item =>
            item.Kind == ProductKind.OptionalPack && item.IsComplete);
        return !unsupportedPackSelected;
    }

    private string GetNextStepText()
    {
        if (HaveAllPlannedProductsReady())
        {
            return installedLauncher.IsAvailable
                ? "An earlier verified game installation exists. Open the launcher to use or remove it."
                : "A game is already installed, but the launcher is missing. Repair the application shell before continuing.";
        }
        if (currentDestinationPlan is { } plan && CanInstallPlan(plan))
            return "Media validated. Click INSTALL SELECTED GAMES to continue.";
        if (selection.Current.Capabilities.Any(item => item.Kind == ProductKind.OptionalPack && item.IsComplete))
            return "Mech Pak media was validated, but pack entitlement installation is not enabled in this build.";
        if (selection.Current.Layouts.Count > 0) return "Add both discs for Vengeance or Mercenaries; Black Knight also requires Vengeance.";
        return "Choose one or more ISO or ZIP files to begin.";
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

    private static string FormatBlockedProducts(IReadOnlyList<BlockedInstallProduct> blocked)
    {
        return string.Join("  •  ", blocked.Select(item =>
            $"{item.DisplayName} requires {string.Join(" + ", item.MissingDependencyIds.Select(DependencyDisplayName))}"));
    }

    private static string DependencyDisplayName(string productId) =>
        ProductCatalog.All.Single(item => item.Id.Equals(productId, StringComparison.OrdinalIgnoreCase)).DisplayName;

    private static string ProductDisplayName(string productId) =>
        ProductCatalog.All.Single(item => item.Id.Equals(productId, StringComparison.OrdinalIgnoreCase)).DisplayName;

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

        public void Update(MediaCapabilityStatus capability, IReadOnlySet<string> availableProductIds)
        {
            status.ForeColor = capability.IsComplete ? Ready : Muted;
            if (!capability.IsComplete)
            {
                status.Text = "○  MEDIA NEEDED";
                detail.Text = $"{capability.PresentLayoutIds.Count} / {capability.RequiredLayoutIds.Count} required source(s)";
                return;
            }

            var missingDependencies = ProductDependencies.GetRequiredBaseProducts(capability.ProductId)
                .Where(required => !availableProductIds.Contains(required))
                .Select(DependencyDisplayName)
                .ToArray();
            if (missingDependencies.Length > 0)
            {
                status.ForeColor = Warning;
                status.Text = "△  BASE MEDIA REQUIRED";
                detail.Text = $"Add {string.Join(" + ", missingDependencies)} to install this selection";
                return;
            }

            if (capability.Kind == ProductKind.OptionalPack)
            {
                status.Text = "✓  PACK MEDIA ADDED";
                detail.Text = "Detected; pack installation support is still in progress";
                return;
            }

            status.Text = "✓  MEDIA VALIDATED";
            detail.Text = "Installation support is still in progress";
        }

        public void ShowInstalled()
        {
            status.ForeColor = Ready;
            status.Text = "✓  INSTALLED";
            detail.Text = "Setup is complete; open the launcher below";
        }
    }
}
