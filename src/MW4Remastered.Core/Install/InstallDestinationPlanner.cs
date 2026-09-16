using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed record InstallDestinationProduct(
    string ProductId,
    string DisplayName,
    string DestinationPath,
    long BudgetBytes);

public sealed record InstallDestinationPlan(
    string RootPath,
    IReadOnlyList<InstallDestinationProduct> Products,
    long RequiredBytes,
    long AvailableBytes)
{
    public bool HasSelectedGames => Products.Count > 0;
    public bool HasEnoughSpace => AvailableBytes >= RequiredBytes;
}

public interface IStorageCapacityReader
{
    long GetAvailableBytes(string destinationPath);
}

public sealed class DriveStorageCapacityReader : IStorageCapacityReader
{
    public long GetAvailableBytes(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var existingPath = Path.GetFullPath(destinationPath);
        while (!Directory.Exists(existingPath))
        {
            var parent = Directory.GetParent(existingPath);
            if (parent is null) throw new DirectoryNotFoundException($"Cannot resolve a storage volume for: {destinationPath}");
            existingPath = parent.FullName;
        }

        var volumeRoot = Path.GetPathRoot(existingPath);
        if (string.IsNullOrWhiteSpace(volumeRoot)) throw new DirectoryNotFoundException($"Cannot resolve a storage volume for: {destinationPath}");
        return new DriveInfo(volumeRoot).AvailableFreeSpace;
    }
}

public sealed class InstallDestinationPlanner
{
    private const long SafetyReserveBytes = 512L * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, (string Folder, long BudgetBytes)> GameBudgets =
        new Dictionary<string, (string Folder, long BudgetBytes)>(StringComparer.OrdinalIgnoreCase)
        {
            ["vengeance"] = ("Vengeance", 1280L * 1024 * 1024),
            ["black-knight"] = ("Black Knight", 768L * 1024 * 1024),
            ["mercenaries"] = ("Mercenaries", 1536L * 1024 * 1024),
        };

    private readonly IStorageCapacityReader capacityReader;

    public InstallDestinationPlanner(IStorageCapacityReader? capacityReader = null)
    {
        this.capacityReader = capacityReader ?? new DriveStorageCapacityReader();
    }

    public InstallDestinationPlan Plan(MediaSelectionSnapshot selection, string destinationRoot)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        if (!Path.IsPathFullyQualified(destinationRoot))
            throw new InvalidDataException("Install destination must be an absolute path.");

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationRoot));
        var volumeRoot = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root) ?? string.Empty);
        if (root.Equals(volumeRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Install destination must not be a filesystem root.");
        if (File.Exists(root))
            throw new InvalidDataException("Install destination must be a directory, not an existing file.");

        RejectExistingReparsePath(root);

        var products = new List<InstallDestinationProduct>();
        foreach (var capability in selection.Capabilities.Where(item => item.Kind == ProductKind.Game && item.IsComplete))
        {
            if (!GameBudgets.TryGetValue(capability.ProductId, out var budget))
                throw new InvalidDataException($"No install-size budget exists for product: {capability.ProductId}");

            var product = ProductCatalog.All.Single(item => item.Id.Equals(capability.ProductId, StringComparison.OrdinalIgnoreCase));
            var destination = Path.GetFullPath(Path.Combine(root, budget.Folder));
            if (!destination.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Planned product destination escaped the install root: {destination}");
            products.Add(new InstallDestinationProduct(product.Id, product.DisplayName, destination, budget.BudgetBytes));
        }

        var requiredBytes = products.Count == 0 ? 0 : checked(products.Sum(item => item.BudgetBytes) + SafetyReserveBytes);
        var availableBytes = capacityReader.GetAvailableBytes(root);
        return new InstallDestinationPlan(root, products, requiredBytes, availableBytes);
    }

    private static void RejectExistingReparsePath(string destinationRoot)
    {
        var current = destinationRoot;
        while (!Directory.Exists(current))
        {
            var parent = Directory.GetParent(current);
            if (parent is null) return;
            current = parent.FullName;
        }

        var target = new DirectoryInfo(current);
        while (target is not null)
        {
            if (target.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidDataException($"Install destination must not traverse a reparse point: {target.FullName}");
            target = target.Parent;
        }
    }
}
