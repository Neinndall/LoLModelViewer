using Microsoft.Win32;
using System.Windows;

namespace LoLModelViewer.Services
{
    public class DialogService : IDialogService
    {
        public string? ShowOpenFileDialog(string filter)
        {
            var openFileDialog = new OpenFileDialog { Filter = filter };
            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        public void ShowMessageBox(string message, string caption)
        {
            MessageBox.Show(message, caption);
        }
    }
}
