using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using ImageShifter.Core;
using ImageShifter.Utils;
using Prism.Mvvm;

namespace ImageShifter.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly StringBuilder stringBuilder = new();
    private AppVersionInfo appVersionInfo = new();
    private string targetDirectoryPath = string.Empty;
    private string logText = string.Empty;

    public string Title => appVersionInfo.GetAppNameWithVersion();

    public string TargetDirectoryPath
    {
        get => targetDirectoryPath;
        set => SetProperty(ref targetDirectoryPath, value);
    }

    public string LogText { get => logText; set => SetProperty(ref logText, value); }

    public AsyncRelayCommand ConvertImagesAsyncCommand => new (async () =>
    {
        var result = await ImageConverterUtil.ConvertBmpToPngAsync(TargetDirectoryPath, log =>
        {
            // UIスレッドで更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                stringBuilder.AppendLine(log);
                LogText = stringBuilder.ToString();
            });
        });
    });
}