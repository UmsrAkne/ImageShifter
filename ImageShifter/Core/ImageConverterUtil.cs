using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageShifter.Core
{
    public static class ImageConverterUtil
    {
        /// <summary>
        /// 指定ディレクトリ内の .bmp を同名の .png に非同期変換。
        /// 全て成功した場合のみ .bmp を削除します。
        /// </summary>
        /// <param name="folderPath">対象フォルダのフルパスを入力します。</param>
        /// <param name="deleteOriginalFiles">変換完了後、オリジナルファイルを削除するかどうかを設定します。</param>
        /// <param name="onLog">ログの出力先の Action を入力します。</param>
        /// <returns>変換処理のログを返します。</returns>
        /// <exception cref="IOException">
        /// ファイルの変換に失敗した場合などにスローされます。
        /// 変換に失敗したケースが含まれる場合、bmp ファイルの削除は実行されません。
        /// </exception>
        public static async Task<ConversionResult> ConvertBmpToPngAsync(
            string folderPath, bool deleteOriginalFiles, Func<string, Task> onLog = null)
        {
            void Log(string message)
            {
                onLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
            }

            var result = new ConversionResult();

            Log("処理を開始します…------------------------------");

            if (!Directory.Exists(folderPath))
            {
                Log("指定されたディレクトリが存在しません。");
                result.Errors.Add("ディレクトリが存在しません。");
                return result;
            }

            var bmpFiles = Directory.GetFiles(folderPath, "*.bmp");
            result.Total = bmpFiles.Length;

            if (bmpFiles.Length == 0)
            {
                Log("対象の .bmp ファイルが見つかりませんでした。");
                result.Errors.Add("対象の .bmp ファイルが見つかりませんでした。");
                return result;
            }

            Log($"対象ディレクトリ: {folderPath}");
            Log($"対象ファイル数: {result.Total} 件");
            Log("変換開始");

            var successList = new List<string>();

            foreach (var bmpFile in bmpFiles)
            {
                var fileName = Path.GetFileName(bmpFile);
                Log($"変換中: {fileName}");

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
                    Log($"成功: {fileName}");
                }
                catch (Exception ex)
                {
                    Log($"失敗: {fileName} → {ex.Message}");
                    result.Errors.Add($"{fileName}: {ex.Message}");
                }
            }

            result.SuccessCount = successList.Count;

            var isSuccess = result.SuccessCount == result.Total;
            if (!deleteOriginalFiles)
            {
                Log(isSuccess ? "全件変換に成功しました" : $"失敗数: {result.FailCount} 件");
                Log("変換完了");
                return result;
            }

            if (isSuccess)
            {
                Log("全件成功、元の .bmp を削除します…");

                foreach (var bmp in successList)
                {
                    try
                    {
                        File.Delete(bmp);
                        Log($"削除: {Path.GetFileName(bmp)}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"{Path.GetFileName(bmp)} の削除に失敗: {ex.Message}");
                        Log($"削除失敗: {Path.GetFileName(bmp)} → {ex.Message}");
                    }
                }
            }
            else
            {
                Log($"失敗数: {result.FailCount} 件。元の .bmp は削除しません。");
            }

            Log("変換完了。");

            return result;
        }
    }
}