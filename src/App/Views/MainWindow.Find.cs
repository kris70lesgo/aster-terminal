namespace Aster.App.Views;

public partial class MainWindow
{
    private void ShowSearch() => SearchBar.Open(Active);
    private void HideSearch() => SearchBar.Close();
    private void SetFindPane(TerminalPane? pane) => SearchBar.SetPane(pane);
}
