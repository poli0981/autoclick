using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using AutoClick.Core.Settings;
using AutoClick.Services.Engine;
using AutoClick.UI;
using AutoClick.Win32;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace AutoClick.Architecture.Tests;

/// <summary>
/// Core ← Win32 / Services ← UI. Core and Services stay free of WPF so they can be tested
/// (and reasoned about) without a UI thread.
/// </summary>
public sealed class LayeringTests
{
    private static readonly Assembly Core = typeof(AppSettings).Assembly;
    private static readonly Assembly Services = typeof(ClickEngine).Assembly;
    private static readonly Assembly Win32 = typeof(ElevationService).Assembly;
    private static readonly Assembly Ui = typeof(AppLinks).Assembly;

    private static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader().LoadAssemblies(Core, Services, Win32, Ui).Build();

    private static readonly string[] WpfAssemblies = ["PresentationFramework", "PresentationCore", "WindowsBase", "System.Xaml"];

    [Fact]
    public void Core_ReferencesNoOtherLayerAndNoFrameworkForUi()
    {
        var references = ReferenceNames(Core);
        Assert.DoesNotContain(references, r => r.StartsWith("AutoClick.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => WpfAssemblies.Contains(r) || r.StartsWith("Serilog", StringComparison.Ordinal) || r.StartsWith("Velopack", StringComparison.Ordinal));
    }

    [Fact]
    public void Services_DependOnlyOnCore()
    {
        var references = ReferenceNames(Services);
        Assert.Contains("AutoClick.Core", references);
        Assert.DoesNotContain(references, r => r is "AutoClick.Win32" or "AutoClick.UI");
        Assert.DoesNotContain(references, r => WpfAssemblies.Contains(r));
    }

    [Fact]
    public void Win32_DependsOnlyOnCore()
    {
        var references = ReferenceNames(Win32);
        Assert.DoesNotContain(references, r => r is "AutoClick.Services" or "AutoClick.UI");
        Assert.DoesNotContain(references, r => WpfAssemblies.Contains(r));
    }

    [Fact]
    public void ViewModels_DoNotDependOnViewsOrOverlays() =>
        Types().That().ResideInNamespace("AutoClick.UI.ViewModels")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^AutoClick\.UI\.(Views|Overlays)(\..*)?$")
            .Check(Architecture);

    private static string[] ReferenceNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
}
