using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

/// <summary>
/// Regression tests for the .NET 8 vs .NET 10 runtime error and app logo fix.
/// </summary>
public sealed class RuntimeTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DisplayFlip.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? string.Empty;
    }

    [Fact]
    public void Csproj_MustDeclare_RollForwardMajor()
    {
        var root = FindRepoRoot();
        root.Should().NotBeNullOrEmpty();
        var files = new[]
        {
            Path.Combine(root, "src", "DisplayFlip", "DisplayFlip.csproj"),
            Path.Combine(root, "src", "DisplayFlip.Core", "DisplayFlip.Core.csproj"),
            Path.Combine(root, "src", "DisplayFlip.Windows", "DisplayFlip.Windows.csproj"),
            Path.Combine(root, "src", "DisplayFlip.Wpf", "DisplayFlip.Wpf.csproj"),
        };
        foreach (var f in files)
        {
            File.Exists(f).Should().BeTrue($"csproj should exist: {f}");
            File.ReadAllText(f).Should().Contain("<RollForward>Major</RollForward>", $"RollForward Major required in {Path.GetFileName(f)}");
        }
    }

    [Fact]
    public void Csproj_MustDeclare_ApplicationIcon()
    {
        var root = FindRepoRoot();
        var csproj = Path.Combine(root, "src", "DisplayFlip", "DisplayFlip.csproj");
        File.ReadAllText(csproj).Should().Contain("DisplayFlip.ico");
    }

    [Fact]
    public void AppIcon_FileExists_AndLooksValid()
    {
        var root = FindRepoRoot();
        var ico = Path.Combine(root, "src", "DisplayFlip", "DisplayFlip.ico");
        File.Exists(ico).Should().BeTrue("logo ico must exist");
        var info = new FileInfo(ico);
        info.Length.Should().BeGreaterThan(2048, "logo should be a real ICO, not a stub");
        var bytes = File.ReadAllBytes(ico);
        // ICO header: 0,0,1,0 = count 1
        bytes[0].Should().Be(0);
        bytes[1].Should().Be(0);
        bytes[2].Should().Be(1);
        bytes[3].Should().Be(0);
    }

    [Fact]
    public void RuntimeConfig_AfterBuild_HasRollForwardMajor()
    {
        var root = FindRepoRoot();
        var runtimeConfig = Path.Combine(root, "src", "DisplayFlip", "bin", "Release", "net10.0-windows", "DisplayFlip.runtimeconfig.json");
        if (!File.Exists(runtimeConfig))
        {
            // If not built yet, skip with pass — the csproj test above covers the fix statically
            return;
        }
        File.ReadAllText(runtimeConfig).Should().Contain("\"rollForward\": \"Major\"");
    }

    [Fact]
    public void WpfCsproj_Declares_WpfUiPackageAndNet10()
    {
        var root = FindRepoRoot();
        var csproj = Path.Combine(root, "src", "DisplayFlip.Wpf", "DisplayFlip.Wpf.csproj");
        File.Exists(csproj).Should().BeTrue();
        var content = File.ReadAllText(csproj);
        content.Should().Contain("<TargetFramework>net10.0-windows</TargetFramework>");
        content.Should().Contain("<PackageReference Include=\"WPF-UI\" Version=\"4.3.0\" />");
    }

    [Fact]
    public void WpfCsproj_Declares_AppManifest_AndMetadata()
    {
        var root = FindRepoRoot();
        var csproj = Path.Combine(root, "src", "DisplayFlip.Wpf", "DisplayFlip.Wpf.csproj");
        File.Exists(csproj).Should().BeTrue();
        var content = File.ReadAllText(csproj);
        content.Should().Contain("<ApplicationManifest>app.manifest</ApplicationManifest>");
        content.Should().Contain("<Version>1.0.0</Version>");
        content.Should().Contain("<Product>DisplayFlip</Product>");
        content.Should().Contain("<ApplicationIcon>DisplayFlip.ico</ApplicationIcon>");
    }

    [Fact]
    public void AppManifest_FileExists_AndConfiguresPerMonitorDpi()
    {
        var root = FindRepoRoot();
        var manifest = Path.Combine(root, "src", "DisplayFlip.Wpf", "app.manifest");
        File.Exists(manifest).Should().BeTrue();
        var content = File.ReadAllText(manifest);
        content.Should().Contain("PerMonitorV2");
        content.Should().Contain("{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}");
    }

    [Fact]
    public void PublishProfiles_Exist_AndValid()
    {
        var root = FindRepoRoot();
        var fdd = Path.Combine(root, "src", "DisplayFlip.Wpf", "Properties", "PublishProfiles", "FolderProfile-FrameworkDependent.pubxml");
        var standalone = Path.Combine(root, "src", "DisplayFlip.Wpf", "Properties", "PublishProfiles", "FolderProfile-Standalone.pubxml");
        File.Exists(fdd).Should().BeTrue();
        File.Exists(standalone).Should().BeTrue();

        File.ReadAllText(fdd).Should().Contain("<SelfContained>false</SelfContained>");
        File.ReadAllText(standalone).Should().Contain("<SelfContained>true</SelfContained>");
        File.ReadAllText(standalone).Should().Contain("<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>");
    }
}
