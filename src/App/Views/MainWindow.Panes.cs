using Avalonia.Controls;

namespace Aster.App.Views;

public partial class MainWindow
{
    private Task MoveActivePaneAsync(int step) => ChangeLayoutAsync(() =>
    {
        if (Current is { } workspace && Active is { } active && workspace.Panes.Count > 1)
        {
            Unzoom(workspace);
            var activeIndex = workspace.Panes.IndexOf(active);
            if (activeIndex >= 0 && active.Parent is Grid activeParent)
            {
                var targetIndex = (activeIndex + step + workspace.Panes.Count) % workspace.Panes.Count;
                var target = workspace.Panes[targetIndex];
                if (target.Parent is Grid targetParent)
                {
                    var activeRow = Grid.GetRow(active);
                    var activeColumn = Grid.GetColumn(active);
                    var targetRow = Grid.GetRow(target);
                    var targetColumn = Grid.GetColumn(target);

                    if (ReferenceEquals(activeParent, targetParent))
                    {
                        Grid.SetRow(active, targetRow);
                        Grid.SetColumn(active, targetColumn);
                        Grid.SetRow(target, activeRow);
                        Grid.SetColumn(target, activeColumn);
                    }
                    else
                    {
                        activeParent.Children.Remove(active);
                        targetParent.Children.Remove(target);
                        Grid.SetRow(target, activeRow);
                        Grid.SetColumn(target, activeColumn);
                        Grid.SetRow(active, targetRow);
                        Grid.SetColumn(active, targetColumn);
                        activeParent.Children.Add(target);
                        targetParent.Children.Add(active);
                    }

                    workspace.Panes[activeIndex] = target;
                    workspace.Panes[targetIndex] = active;
                    Activate(workspace, active);
                    active.Terminal.Focus();
                }
            }
        }

        return Task.CompletedTask;
    });
}
