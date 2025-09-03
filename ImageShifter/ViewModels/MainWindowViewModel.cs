using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using ImageShifter.Core;
using ImageShifter.Utils;
using Prism.Mvvm;

namespace ImageShifter.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MainWindowViewModel : BindableBase
    {
        private readonly StringBuilder stringBuilder = new();
        private readonly AppVersionInfo appVersionInfo = new();
        private string targetDirectoryPath = string.Empty;
        private string logText = string.Empty;
        private bool isDeleteOriginalFilesEnabled = true;

        public string Title => appVersionInfo.GetAppNameWithVersion();

        public string TargetDirectoryPath
        {
            get => targetDirectoryPath;
            set => SetProperty(ref targetDirectoryPath, value);
        }

        public string LogText { get => logText; set => SetProperty(ref logText, value); }

        public bool IsDeleteOriginalFilesEnabled
        {
            get => isDeleteOriginalFilesEnabled;
            set => SetProperty(ref isDeleteOriginalFilesEnabled, value);
        }

        public AsyncRelayCommand ConvertImagesAsyncCommand => new (async () =>
        {
            await ImageConverterUtil.ConvertBmpToPngAsync(
                TargetDirectoryPath, IsDeleteOriginalFilesEnabled, async log =>
            {
                // UIスレッドで更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    stringBuilder.AppendLine(log);
                    LogText = stringBuilder.ToString();
                });

                await using var writer = new StreamWriter("log.txt", true, Encoding.UTF8);
                await writer.WriteLineAsync(log);
            });
        });
    }
}