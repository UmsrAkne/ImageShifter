using System.Windows.Controls;

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
    }
}