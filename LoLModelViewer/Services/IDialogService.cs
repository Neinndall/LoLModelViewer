namespace LoLModelViewer.Services
{
    public interface IDialogService
    {
        string? ShowOpenFileDialog(string filter);
        void ShowMessageBox(string message, string caption);
    }
}
