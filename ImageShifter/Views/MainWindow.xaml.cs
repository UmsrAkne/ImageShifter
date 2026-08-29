using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ImageShifter.ViewModels;

namespace ImageShifter.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            LogScrollViewer.ScrollToEnd();
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
            {
                return;
            }

            // 最初のディレクトリを取得
            var directories = files.Where(Directory.Exists).ToList();
            var paths = string.Join(Environment.NewLine, directories);

            if (!string.IsNullOrWhiteSpace(paths) && DataContext is MainWindowViewModel viewModel)
            {
                viewModel.TargetDirectoryPaths = paths;
            }
        }
    }
}