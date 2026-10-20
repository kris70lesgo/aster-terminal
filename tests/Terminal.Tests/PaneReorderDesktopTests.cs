using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Aster.App.Views;
using Aster.Sessions;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task PaneReorderSwapsLiveSessionsAcrossNestedAndSharedSplits()
    {
        var window = new MainWindow(new TerminalProfile(
            "Pane reorder",
            OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh",
            Path.GetTempPath()));
        var panes = new List<TerminalPane>();
        var command = OperatingSystem.IsMacOS()
            ? RawInputModifiers.Meta
            : RawInputModifiers.Control | RawInputModifiers.Shift;
        var splitBelow = OperatingSystem.IsMacOS() ? command | RawInputModifiers.Shift : command | RawInputModifiers.Alt;
        var moveForward = OperatingSystem.IsMacOS()
            ? RawInputModifiers.Meta | RawInputModifiers.Alt
            : RawInputModifiers.Control | RawInputModifiers.Shift | RawInputModifiers.Alt;
        var moveBackward = moveForward;
        window.Show();
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var first = Panes(window)[0]; panes.Add(first);
            FindCommand(first, OperatingSystem.IsWindows() ? "echo PANE_FIRST_READY" : "printf '\\nPANE_FIRST_READY\\n'");
            await Until(() => Text(first).Contains("PANE_FIRST_READY"));

            SendKey(window, Key.D, command);
            await Until(() => Panes(window).Length == 2 && Panes(window).All(p => p.Terminal.IsRunning));
            var second = Panes(window).Single(p => p != first); panes.Add(second);
            FindCommand(second, OperatingSystem.IsWindows() ? "echo PANE_SECOND_READY" : "printf '\\nPANE_SECOND_READY\\n'");
            await Until(() => Text(second).Contains("PANE_SECOND_READY"));

            SendKey(window, Key.D, splitBelow);
            await Until(() => Panes(window).Length == 3 && Panes(window).All(p => p.Terminal.IsRunning));
            var third = Panes(window).Single(p => p != first && p != second); panes.Add(third);
            FindCommand(third, OperatingSystem.IsWindows() ? "echo PANE_THIRD_READY" : "printf '\\nPANE_THIRD_READY\\n'");
            await Until(() => Text(third).Contains("PANE_THIRD_READY"));
            var firstSession = first.Terminal.ProcessMetadata!.SessionId;
            var secondSession = second.Terminal.ProcessMetadata!.SessionId;
            var thirdSession = third.Terminal.ProcessMetadata!.SessionId;

            var firstGrid = Assert.IsType<Grid>(first.Parent);
            var secondGrid = Assert.IsType<Grid>(second.Parent);
            Assert.NotSame(firstGrid, secondGrid);
            var firstSlot = (Grid.GetRow(first), Grid.GetColumn(first));
            var thirdSlot = (Grid.GetRow(third), Grid.GetColumn(third));

            // The active third pane wraps forward to the first and crosses a nested split boundary.
            SendKey(window, Key.PageDown, moveForward);
            await Until(() => ReferenceEquals(third.Parent, firstGrid) && ReferenceEquals(first.Parent, secondGrid));
            Assert.Equal(firstSlot, (Grid.GetRow(third), Grid.GetColumn(third)));
            Assert.Equal(thirdSlot, (Grid.GetRow(first), Grid.GetColumn(first)));
            Assert.All(panes, pane => Assert.True(pane.Terminal.IsRunning));
            Assert.Contains("PANE_FIRST_READY", Text(first));
            Assert.Contains("PANE_SECOND_READY", Text(second));
            Assert.Contains("PANE_THIRD_READY", Text(third));

            // The reverse command restores the same sessions to their previous slots.
            SendKey(window, Key.PageUp, moveBackward);
            await Until(() => ReferenceEquals(third.Parent, secondGrid) && ReferenceEquals(first.Parent, firstGrid));
            Assert.Equal(firstSlot, (Grid.GetRow(first), Grid.GetColumn(first)));
            Assert.Equal(thirdSlot, (Grid.GetRow(third), Grid.GetColumn(third)));
            Assert.Equal(firstSession, first.Terminal.ProcessMetadata!.SessionId);
            Assert.Equal(secondSession, second.Terminal.ProcessMetadata!.SessionId);
            Assert.Equal(thirdSession, third.Terminal.ProcessMetadata!.SessionId);

            // Closing a reordered pane collapses the split while keeping the other live sessions.
            SendKey(window, Key.W, command);
            await Until(() => !third.Terminal.IsRunning && Panes(window).Length == 2);
            Assert.Same(first.Parent, second.Parent);
            var sharedGrid = Assert.IsType<Grid>(first.Parent);
            var firstSharedSlot = (Grid.GetRow(first), Grid.GetColumn(first));
            var secondSharedSlot = (Grid.GetRow(second), Grid.GetColumn(second));

            // The remaining panes share a grid; the active pane wraps to its neighbor in place.
            SendKey(window, Key.PageDown, moveForward);
            await Until(() => Grid.GetRow(first) == secondSharedSlot.Item1 && Grid.GetColumn(first) == secondSharedSlot.Item2);
            Assert.Same(sharedGrid, first.Parent);
            Assert.Same(sharedGrid, second.Parent);
            Assert.Equal(firstSharedSlot, (Grid.GetRow(second), Grid.GetColumn(second)));
            Assert.True(first.Terminal.IsRunning);
            Assert.True(second.Terminal.IsRunning);
        }
        finally
        {
            window.Close();
            await Until(() => !window.IsVisible && panes.All(pane => !pane.Terminal.IsRunning));
        }
    }
}
