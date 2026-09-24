using System.Security.Cryptography;
using System.Text;

namespace MW4Remastered.Core.Install;

// Produces a shareable, read-only report from fixed product and file names.
// User paths, manifest-provided paths, logs, and media contents are never rendered.
public sealed class InstallationDiagnosticsService
{
    private readonly InstallManifestVerifier verifier;

    public InstallationDiagnosticsService() : this(new InstallManifestVerifier()) { }

    public InstallationDiagnosticsService(InstallManifestVerifier verifier) =>
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));

    public string Build(IReadOnlyList<ProductStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var byId = statuses.ToDictionary(item => item.Product.Id, StringComparer.OrdinalIgnoreCase);
        var report = new StringBuilder();
        report.AppendLine("MechWarrior 4 Remastered installation diagnostics");
        report.AppendLine("No personal paths, logs, or media contents are included.");
        report.AppendLine();
        foreach (var product in ProductCatalog.All)
        {
            if (!byId.TryGetValue(product.Id, out var status))
                throw new InvalidDataException("A product status is missing from diagnostics.");
            report.Append(product.DisplayName).Append(": ").AppendLine(status.State.ToString());
            if (product.Kind != ProductKind.Game || status.State == ProductInstallState.Missing) continue;
            if (status.State == ProductInstallState.NeedsRepair)
            {
                report.AppendLine("  Owned installation needs repair; run Setup again with the original media.");
                continue;
            }
            if (status.InstallPath is null || status.LaunchPath is null)
                throw new InvalidDataException("A ready game has no verified installation or launch path.");
            var check = verifier.Verify(status.InstallPath, InstallVerificationScope.OwnedFiles);
            var physicalProduct = product.Id == "black-knight" ? "vengeance" : product.Id;
            if (!check.IsValid || check.Manifest is null ||
                !check.Manifest.ProductId.Equals(physicalProduct, StringComparison.OrdinalIgnoreCase) ||
                !check.Manifest.HasComponent(product.Id))
            {
                report.AppendLine("  Owned files changed during diagnostics; run Setup again with the original media.");
                continue;
            }
            var owned = check.Manifest.Files.Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var prefix = product.Id == "black-knight" ? "MW4X/" : "";
            var executable = product.ExecutableCandidates.Single();
            foreach (var relative in new[] { executable, prefix + "dgVoodoo.conf", prefix + "DDraw.dll",
                         prefix + "D3DImm.dll", prefix + "SampleAddon.dll" })
            {
                if (!owned.Contains(relative))
                {
                    report.Append("  ").Append(relative).AppendLine(": not owned");
                    continue;
                }
                var path = StagedInstallTransaction.ResolveContainedPath(status.InstallPath, relative);
                StagedInstallTransaction.RejectContainedFilePath(status.InstallPath, path);
                using var stream = File.OpenRead(path);
                report.Append("  ").Append(relative).Append(" SHA-256: ")
                    .AppendLine(Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant());
            }
        }
        return report.ToString();
    }
}
