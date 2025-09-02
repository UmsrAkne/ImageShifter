using CommunityToolkit.Mvvm.Input;
using ImageShifter.Core;
using ImageShifter.Utils;
using Prism.Mvvm;

namespace ImageShifter.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private AppVersionInfo appVersionInfo = new();
    private string targetDirectoryPath = string.Empty;

    public string Title => appVersionInfo.GetAppNameWithVersion();

    public string TargetDirectoryPath
    {
        get => targetDirectoryPath;
        set => SetProperty(ref targetDirectoryPath, value);
    }

    public AsyncRelayCommand ConvertImagesAsyncCommand => new (async () =>
    {
        var result = await ImageConverterUtil.ConvertBmpToPngAsync(TargetDirectoryPath);
        System.Diagnostics.Debug.WriteLine($"Total: {result.Total}(MainWindowViewModel : 18)");
        System.Diagnostics.Debug.WriteLine($"Success: {result.SuccessCount}(MainWindowViewModel : 18)");
    });
}