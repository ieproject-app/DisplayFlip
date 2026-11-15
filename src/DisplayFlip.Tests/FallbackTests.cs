using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

/// <summary>Regression tests for CCD 87 fallback that broke Save with 2 monitors.</summary>
public sealed class FallbackTests
{
    [Fact]
    public void Adapter_ReadTopologies_MustFallbackWhenCcdReturns87()
    {
        var adapter = new DisplayFlip.Windows.CcdTopologyAdapter();
        var monitors = adapter.ReadTopologies(out var paths, out var modes);
        // On this Intel UHD driver CCD returns 87 and 0 paths; fallback must still see monitors.
        // On healthy CCD drivers paths/modes are non-empty and monitors come from CCD.
        // Either way, with the OS reporting 2 displays via Screen/WMI, we must not return 0.
        monitors.Count.Should().BeGreaterThan(0, "Save depends on ReadTopologies; CCD 87 must not zero out the count");
        foreach (var m in monitors)
        {
            m.StableKey.Should().NotBeNullOrEmpty();
            m.Position.Width.Should().BeGreaterThan(0);
            m.Position.Height.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Adapter_Validate_WithEmptyCcdLists_MustPassForFallback()
    {
        var adapter = new DisplayFlip.Windows.CcdTopologyAdapter();
        var (ok, msg) = adapter.Validate(new List<DisplayFlip.Windows.NativeMethods.DISPLAYCONFIG_PATH_INFO>(), new List<DisplayFlip.Windows.NativeMethods.DISPLAYCONFIG_MODE_INFO>());
        ok.Should().BeTrue($"CCD 87 vector must validate via fallback so TryApply reaches ApplyViaFallback, got: {msg}");
    }

    [Fact]
    public void CcdTopologyAdapter_HasFallbackApplyEntryPoint()
    {
        typeof(DisplayFlip.Windows.CcdTopologyAdapter).GetMethods().Select(m => m.Name)
            .Should().Contain("ApplyViaFallback");
        typeof(DisplayFlip.Windows.CcdTopologyAdapter).GetMethods().Select(m => m.Name)
            .Should().Contain("ReadTopologies");
    }

    [Fact]
    public void ScreenFallback_DevMode_MarshalsAt220Bytes()
    {
        var t = typeof(DisplayFlip.Windows.CcdTopologyAdapter).Assembly.GetType("DisplayFlip.Windows.ScreenFallback+DEVMODE", throwOnError: true)!;
        System.Runtime.InteropServices.Marshal.SizeOf(t).Should().Be(220);
    }
}
