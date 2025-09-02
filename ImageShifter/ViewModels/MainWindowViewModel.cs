using ImageShifter.Utils;
using Prism.Mvvm;

namespace ImageShifter.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private AppVersionInfo appVersionInfo = new();

    public string Title => appVersionInfo.GetAppNameWithVersion();
}