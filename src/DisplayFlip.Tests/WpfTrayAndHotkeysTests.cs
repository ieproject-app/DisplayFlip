using System;
using System.IO;
using System.Threading;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views.Pages;
using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

/// <summary>
/// Unit test untuk Tahap 6: HotkeyInterop, SingleInstanceService, CliRunner, IconHelper, dan Tray integration.
/// </summary>
public sealed class WpfTrayAndHotkeysTests
{
    // =========================================================================
    // 1. HOTKEY INTEROP TESTS
    // =========================================================================

    [Theory]
    [InlineData("Ctrl+Alt+F", HotkeyInterop.MOD_CONTROL | HotkeyInterop.MOD_ALT, (uint)'F')]
    [InlineData("Ctrl+Shift+1", HotkeyInterop.MOD_CONTROL | HotkeyInterop.MOD_SHIFT, (uint)'1')]
    [InlineData("Win+Left", HotkeyInterop.MOD_WIN, HotkeyInterop.VK_LEFT)]
    [InlineData("Ctrl+Alt+Right", HotkeyInterop.MOD_CONTROL | HotkeyInterop.MOD_ALT, HotkeyInterop.VK_RIGHT)]
    [InlineData("Ctrl+Alt+Num5", HotkeyInterop.MOD_CONTROL | HotkeyInterop.MOD_ALT, HotkeyInterop.VK_NUMPAD0 + 5)]
    [InlineData("Ctrl+Alt+NumPad3", HotkeyInterop.MOD_CONTROL | HotkeyInterop.MOD_ALT, HotkeyInterop.VK_NUMPAD0 + 3)]
    [InlineData("Alt+Shift+Delete", HotkeyInterop.MOD_ALT | HotkeyInterop.MOD_SHIFT, HotkeyInterop.VK_DELETE)]
    [InlineData("Ctrl+Space", HotkeyInterop.MOD_CONTROL, HotkeyInterop.VK_SPACE)]
    public void HotkeyInterop_ParsesValidShortcuts_Correctly(string shortcut, uint expectedModifiers, uint expectedVk)
    {
        bool success = HotkeyInterop.TryParse(shortcut, out uint modifiers, out uint vk);

        success.Should().BeTrue();
        modifiers.Should().Be(expectedModifiers);
        vk.Should().Be(expectedVk);
    }

    [Fact]
    public void HotkeyInterop_ParsesFunctionKeyWithoutModifier()
    {
        bool success = HotkeyInterop.TryParse("F9", out uint modifiers, out uint vk);

        success.Should().BeTrue();
        modifiers.Should().Be(0u);
        vk.Should().Be(HotkeyInterop.VK_F1 + 8);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("A")]                   // Non-function key tanpa modifier
    [InlineData("1")]                   // Angka tanpa modifier
    [InlineData("Ctrl+Alt+A+B")]         // Multi-key tidak diizinkan
    [InlineData("Ctrl+UnknownButton")]   // Nama tombol asing
    [InlineData("Alt+99999")]            // Key number invalid
    public void HotkeyInterop_RejectsInvalidShortcuts(string? invalidShortcut)
    {
        bool success = HotkeyInterop.TryParse(invalidShortcut, out _, out _);

        success.Should().BeFalse();
    }

    // =========================================================================
    // 2. CLI RUNNER TESTS
    // =========================================================================

    [Theory]
    [InlineData(new[] { "--swap" }, true)]
    [InlineData(new[] { "--mirror" }, true)]
    [InlineData(new[] { "--flip" }, true)]
    [InlineData(new[] { "--identify" }, true)]
    [InlineData(new[] { "-i" }, true)]
    [InlineData(new[] { "--solo", "1" }, true)]
    [InlineData(new[] { "--internal" }, true)]
    [InlineData(new[] { "--external" }, true)]
    [InlineData(new[] { "--extend" }, true)]
    [InlineData(new[] { "--perluas" }, true)]
    [InlineData(new[] { "--apply", "Kerja Siang" }, true)]
    [InlineData(new[] { "--profile", "Malam" }, true)]
    [InlineData(new[] { "-p", "Malam" }, true)]
    [InlineData(new[] { "--list" }, true)]
    [InlineData(new[] { "--help" }, true)]
    [InlineData(new[] { "--tray" }, false)]       // Background tray launch bukan aksi CLI instan
    [InlineData(new[] { "--minimized" }, false)]
    [InlineData(new string[0], false)]
    public void CliRunner_DetectsCliActionAccurately(string[] args, bool expectedIsAction)
    {
        bool isAction = CliRunner.IsCliAction(args);

        isAction.Should().Be(expectedIsAction);
    }

    // =========================================================================
    // 3. ICON HELPER TESTS
    // =========================================================================

    [Fact]
    public void IconHelper_ReturnsHighResolutionFrame_256px()
    {
        var frame = IconHelper.GetHighResIconFrame();

        frame.Should().NotBeNull();
        frame.PixelWidth.Should().Be(256);
        frame.PixelHeight.Should().Be(256);
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(24, 24)]
    [InlineData(32, 32)]
    [InlineData(48, 48)]
    [InlineData(64, 64)]
    [InlineData(68, 128)] // 68px memilih frame 128px untuk downscale tajam
    [InlineData(128, 128)]
    [InlineData(256, 256)]
    public void IconHelper_GetBestIconFrame_SelectsEqualOrGreaterFrame(int requestedSize, int expectedPixelWidth)
    {
        var frame = IconHelper.GetBestIconFrame(requestedSize);

        frame.Should().NotBeNull();
        frame.PixelWidth.Should().Be(expectedPixelWidth);
    }

    [Fact]
    public void IconHelper_CreateNativeIconHandle_ReturnsValidHandle()
    {
        IntPtr hIcon = IconHelper.CreateNativeIconHandle(16);

        hIcon.Should().NotBe(IntPtr.Zero);
    }

    // =========================================================================
    // 4. SINGLE INSTANCE SERVICE TESTS
    // =========================================================================

    [Fact]
    public void SingleInstanceService_SignalsWakeupEvent()
    {
        string uniqueMutex = $"DisplayFlip_TestMutex_{Guid.NewGuid():N}";
        string uniqueEvent = $"DisplayFlip_TestEvent_{Guid.NewGuid():N}";

        using var singleInstance = new SingleInstanceService(uniqueMutex, uniqueEvent);
        singleInstance.IsFirstInstance.Should().BeTrue();

        // Second instance dengan mutex yang sama harus mendeteksi IsFirstInstance == false
        using var secondInstance = new SingleInstanceService(uniqueMutex, uniqueEvent);
        secondInstance.IsFirstInstance.Should().BeFalse();

        using var wakeReceived = new ManualResetEventSlim(false);
        singleInstance.OnWakeupRequested = () =>
        {
            wakeReceived.Set();
        };

        // Simulasikan instans kedua memanggil SignalRunningInstance
        bool signaled = SingleInstanceService.SignalRunningInstance(uniqueEvent);
        signaled.Should().BeTrue();

        // Tunggu event diterima oleh wait handler
        bool received = wakeReceived.Wait(TimeSpan.FromSeconds(3));
        received.Should().BeTrue();
    }

    // =========================================================================
    // 5. PAGE SERVICE GENERIC GETPAGE TEST
    // =========================================================================

    [Fact]
    public void PageService_GetPageGeneric_ReturnsSameCachedInstance()
    {
        RunInStaThread(() =>
        {
            var service = new PageService();

            var p1 = service.GetPage<GeneralPage>();
            var p2 = service.GetPage<GeneralPage>();

            p1.Should().NotBeNull();
            p2.Should().BeSameAs(p1);
        });
    }

    private static void RunInStaThread(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught != null)
        {
            throw new AggregateException("STA thread execution failed", caught);
        }
    }
}
