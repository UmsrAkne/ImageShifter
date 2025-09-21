using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ImageShifter.Utils;

namespace ImageShifter.Core
{
    public static class ImageConverterUtil
    {
        public static async Task<ConversionResult> ConvertBmpToPngAsync(
            string folderPath, bool deleteOriginalFiles, Func<string, Task> onLog = null, AppVersionInfo versionInfo = null)
        {
            async Task Log(string message)
            {
                if (onLog != null)
                {
                    await onLog.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                }
            }

            var result = new ConversionResult();

            await Log("処理を開始します…------------------------------");
            await Log($"App version : {versionInfo?.GetAppNameWithVersion()}");

            if (!Directory.Exists(folderPath))
            {
                await Log("指定されたディレクトリが存在しません。");
                result.Errors.Add("ディレクトリが存在しません。");
                return result;
            }

            var bmpFiles = Directory.GetFiles(folderPath, "*.bmp");
            result.Total = bmpFiles.Length;

            if (bmpFiles.Length == 0)
            {
                await Log("対象の .bmp ファイルが見つかりませんでした。");
                result.Errors.Add("対象の .bmp ファイルが見つかりませんでした。");
                return result;
            }

            await Log($"対象ディレクトリ: {folderPath}");
            await Log($"対象ファイル数: {result.Total} 件");
            await Log("変換開始");

            var successList = new List<string>();

            foreach (var bmpFile in bmpFiles)
            {
                var fileName = Path.GetFileName(bmpFile);
                await Log($"変換中: {fileName}");

                try
                {
                    var pngPath = Path.ChangeExtension(bmpFile, ".png");

                    await Task.Run(() =>
                    {
                        using var bmpStream = File.OpenRead(bmpFile);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = bmpStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // WPFで使うには必須

                        using var pngStream = File.Create(pngPath);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        encoder.Save(pngStream);
                    });

                    var fi = new FileInfo(pngPath);
                    if (fi.Length == 0)
                    {
                        throw new IOException("出力された PNG のサイズが 0 バイトです。");
                    }

                    successList.Add(bmpFile);
                    await Log($"成功　: {fileName}");
                }
                catch (Exception ex)
                {
                    await Log($"失敗　: {fileName} → {ex.Message}");
                    result.Errors.Add($"{fileName}: {ex.Message}");
                }
            }

            result.SuccessCount = successList.Count;

            var isSuccess = result.SuccessCount == result.Total;
            if (!deleteOriginalFiles)
            {
                await Log(isSuccess ? "全件変換に成功しました" : $"失敗数: {result.FailCount} 件");
                await Log("変換完了");
                return result;
            }

            if (isSuccess)
            {
                await Log("全件成功、元の .bmp を削除します…");

                foreach (var bmp in successList)
                {
                    try
                    {
                        File.Delete(bmp);
                        await Log($"削除: {Path.GetFileName(bmp)}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"{Path.GetFileName(bmp)} の削除に失敗: {ex.Message}");
                        await Log($"削除失敗: {Path.GetFileName(bmp)} → {ex.Message}");
                    }
                }
            }
            else
            {
                await Log($"失敗数: {result.FailCount} 件。元の .bmp は削除しません。");
            }

            await Log("変換完了。");

            return result;
        }
    }
}