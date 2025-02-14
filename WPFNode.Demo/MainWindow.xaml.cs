using System.Windows;
using WPFNode.Demo.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace WPFNode.Demo
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SaveCanvas();
            }
        }
    }
} 
