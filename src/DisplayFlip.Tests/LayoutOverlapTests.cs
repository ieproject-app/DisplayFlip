using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DisplayFlip;
using DisplayFlip.Windows;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DisplayFlip.Tests;

public class LayoutOverlapTests
{
    private readonly ITestOutputHelper _output;

    public LayoutOverlapTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(680, 560, false)]
    [InlineData(720, 640, false)]
    [InlineData(850, 720, false)]
    [InlineData(980, 720, false)]
    [InlineData(1160, 760, false)]
    [InlineData(1320, 760, false)]
    [InlineData(680, 560, true)]
    [InlineData(720, 640, true)]
    [InlineData(850, 720, true)]
    [InlineData(980, 720, true)]
    [InlineData(1160, 760, true)]
    [InlineData(1320, 760, true)]
    public void CheckCanvasBottomToolbar_ControlsDoNotOverlap(int width, int height, bool hasUnapplied)
    {
        using var host = new TrayHost(showSettingsOnLaunch: false);
        using var form = new FluentSettingsForm(host, null);
        form.Show();
        form.Size = new Size(width, height);

        typeof(FluentSettingsForm).GetMethod("SwitchTab", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(form, new object[] { "Profil" });

        if (hasUnapplied)
        {
            typeof(FluentSettingsForm).GetField("_hasUnappliedCanvasChanges", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(form, true);
        }

        typeof(FluentSettingsForm).GetMethod("UpdateShellLayout", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(form, null);

        var canvasField = typeof(FluentSettingsForm).GetField("_monitorCanvas", BindingFlags.NonPublic | BindingFlags.Instance);
        var canvas = (Panel)canvasField!.GetValue(form)!;

        var controls = canvas.Controls.Cast<Control>().Where(c => c.Visible).ToList();
        _output.WriteLine($"Size: {width}x{height}, HasUnapplied: {hasUnapplied}, Canvas: {canvas.Bounds}");
        foreach (var c in controls)
        {
            _output.WriteLine($"  Control '{c.Text}' bounds: {c.Bounds}");
            c.Right.Should().BeLessThanOrEqualTo(canvas.Width, $"Control '{c.Text}' exceeds canvas width");
            c.Left.Should().BeGreaterThanOrEqualTo(0, $"Control '{c.Text}' has negative X");
        }

        foreach (var c1 in controls)
        {
            foreach (var c2 in controls)
            {
                if (c1 == c2) continue;
                var r1 = c1.Bounds;
                var r2 = c2.Bounds;
                var inter = Rectangle.Intersect(r1, r2);
                if (!inter.IsEmpty)
                {
                    Assert.Fail($"Overlap at {width}x{height} (unapplied={hasUnapplied}) between {c1.GetType().Name} '{c1.Text}' ({r1}) and {c2.GetType().Name} '{c2.Text}' ({r2}): intersection={inter}");
                }
            }
        }
    }

    [Theory]
    [InlineData(680, 560)]
    [InlineData(980, 720)]
    [InlineData(1160, 760)]
    [InlineData(1320, 760)]
    public void CheckSettingsCards_ControlsDoNotOverlap(int width, int height)
    {
        using var host = new TrayHost(showSettingsOnLaunch: false);
        using var form = new FluentSettingsForm(host, null);
        form.Show();
        form.Size = new Size(width, height);

        typeof(FluentSettingsForm).GetMethod("SwitchTab", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(form, new object[] { "Profil" });

        typeof(FluentSettingsForm).GetMethod("UpdateShellLayout", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(form, null);

        string[] cardNames = { "_cardIdentity", "_cardQuickMove", "_cardResOrient" };
        foreach (var cardName in cardNames)
        {
            var cardField = typeof(FluentSettingsForm).GetField(cardName, BindingFlags.NonPublic | BindingFlags.Instance);
            var card = (Panel)cardField!.GetValue(form)!;
            var visibleControls = card.Controls.Cast<Control>().Where(c => c.Visible).ToList();

            foreach (var c in visibleControls)
            {
                c.Right.Should().BeLessThanOrEqualTo(card.Width + 2, $"Control '{c.Text}' on {cardName} exceeds card width");
            }

            // Check non-label interactive controls don't overlap each other
            var interactive = visibleControls.Where(c => !(c is Label)).ToList();
            for (int i = 0; i < interactive.Count; i++)
            {
                for (int j = i + 1; j < interactive.Count; j++)
                {
                    var inter = Rectangle.Intersect(interactive[i].Bounds, interactive[j].Bounds);
                    inter.IsEmpty.Should().BeTrue($"Interactive controls on {cardName} overlap: {interactive[i].Text} vs {interactive[j].Text}");
                }
            }
        }
    }

    [Fact]
    public void ShellControls_DoNotExceedRootBounds()
    {
        using var host = new TrayHost(showSettingsOnLaunch: false);
        using var form = new FluentSettingsForm(host, null);
        form.Show();
        form.Size = new Size(850, 720);

        var rootField = typeof(FluentSettingsForm).GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance);
        var root = (Panel)rootField!.GetValue(form)!;

        var closeBtnField = typeof(FluentSettingsForm).GetField("_closeBtn", BindingFlags.NonPublic | BindingFlags.Instance);
        var closeBtn = (Control)closeBtnField!.GetValue(form)!;

        closeBtn.Right.Should().BeLessThanOrEqualTo(root.ClientSize.Width);
    }
}
