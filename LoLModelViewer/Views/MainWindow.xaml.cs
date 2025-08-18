using System.Windows;
using LoLModelViewer.Services;
using LoLModelViewer.ViewModels;
using Material.Icons.WPF;
using Material.Icons;

namespace LoLModelViewer.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var dialogService = new DialogService();
            var textureLoadingService = new TextureLoadingService();
            var modelLoadingService = new ModelLoadingService(textureLoadingService);

            _viewModel = new MainViewModel(dialogService, modelLoadingService, textureLoadingService);
            DataContext = _viewModel;
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // The environment is now loaded by the ViewModel
        }

        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (ControlPanel.Visibility == Visibility.Collapsed)
            {
                ControlPanel.Visibility = Visibility.Visible;
                MainGridSplitter.IsEnabled = true;
                ControlPanelColumn.Width = new GridLength(250);
                TogglePanelIcon.Kind = MaterialIconKind.ChevronLeft;
            }
            else
            {
                ControlPanel.Visibility = Visibility.Collapsed;
                MainGridSplitter.IsEnabled = false;
                ControlPanelColumn.Width = new GridLength(0);
                TogglePanelIcon.Kind = MaterialIconKind.ChevronRight;
            }
        }
    }
}
