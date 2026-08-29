using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private string targetDirectoryPaths = string.Empty;
        private string logText = string.Empty;
        private bool isDeleteOriginalFilesEnabled = true;
        private bool isConvertButtonEnabled = true;
        private int progressValue;
        private bool processing;

        public string Title => Processing
            ? "[Processing]" + appVersionInfo.GetAppNameWithVersion()
            : appVersionInfo.GetAppNameWithVersion();

        public string TargetDirectoryPaths
        {
            get => targetDirectoryPaths;
            set => SetProperty(ref targetDirectoryPaths, value);
        }

        public string LogText { get => logText; set => SetProperty(ref logText, value); }

        public bool IsDeleteOriginalFilesEnabled
        {
            get => isDeleteOriginalFilesEnabled;
            set => SetProperty(ref isDeleteOriginalFilesEnabled, value);
        }

        public bool IsConvertButtonEnabled
        {
            get => isConvertButtonEnabled;
            set => SetProperty(ref isConvertButtonEnabled, value);
        }

        public int ProgressValue
        {
            get => progressValue;
            set => SetProperty(ref progressValue, value);
        }

        public AsyncRelayCommand ConvertImagesAsyncCommand => new (async () =>
        {
            var paths = TargetDirectoryPaths?
                .Split(new[] { "\r\n", "\r", "\n", }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct() // 必要に応じて重複を除外
                .ToArray() ?? Array.Empty<string>();

            if (paths.Length == 0)
            {
                return;
            }

            IsConvertButtonEnabled = false;
            Processing = true;

            try
            {
                var baseDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
                var appLogFilePath = Path.Combine(baseDirectoryPath, "log.txt");

                foreach (var targetPath in paths)
                {
                    // 存在しないディレクトリのガード
                    if (!Directory.Exists(targetPath))
                    {
                        var errorMsg = $"[スキップ] ディレクトリが存在しません: {targetPath}";
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            stringBuilder.AppendLine(errorMsg);
                            LogText = stringBuilder.ToString();
                        });
                        await SaveLogEntryAsync(errorMsg, appLogFilePath);
                        continue;
                    }

                    try
                    {
                        await ImageConverterUtil.ConvertBmpToPngAsync(
                            targetPath, // 分割した単一パスを渡す
                            IsDeleteOriginalFilesEnabled,
                            async log =>
                            {
                                // UIスレッドでログ更新
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    stringBuilder.AppendLine(log);
                                    LogText = stringBuilder.ToString();
                                });

                                // アプリ共通ログ & 対象フォルダ配下ログに出力
                                await SaveLogEntryAsync(log, appLogFilePath);
                                await SaveLogEntryAsync(log, Path.Combine(targetPath, "log.txt"));
                            },
                            (done, total) =>
                            {
                                // 進捗率の更新 (各フォルダごと 0〜100% の場合)
                                Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    ProgressValue = total == 0 ? 0 : done * 100 / total;
                                });
                            },
                            appVersionInfo);
                    }
                    catch (Exception ex)
                    {
                        // 1つのフォルダで予期せぬ例外が発生しても後続のフォルダ処理を継続する
                        var errorMsg = $"[エラー] 処理失敗 ({targetPath}): {ex.Message}";
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            stringBuilder.AppendLine(errorMsg);
                            LogText = stringBuilder.ToString();
                        });
                        await SaveLogEntryAsync(errorMsg, appLogFilePath);
                    }
                }
            }
            finally
            {
                IsConvertButtonEnabled = true;
                Processing = false;
            }
        });

        private bool Processing
        {
            get => processing;
            set
            {
                if (SetProperty(ref processing, value))
                {
                    RaisePropertyChanged(nameof(Title));
                }
            }
        }

        private async Task SaveLogEntryAsync(string log, string path)
        {
            await using var writer = new StreamWriter(path, true, Encoding.UTF8);
            await writer.WriteLineAsync(log);
        }
    }
}