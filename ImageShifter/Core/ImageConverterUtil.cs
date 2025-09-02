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
        /// <returns>変換処理のログを返します。</returns>
        /// <exception cref="IOException">
        /// ファイルの変換に失敗した場合などにスローされます。
        /// 変換に失敗したケースが含まれる場合、bmp ファイルの削除は実行されません。
        /// </exception>
        public static async Task<ConversionResult> ConvertBmpToPngAsync(string folderPath)
        {
            var result = new ConversionResult();

            if (!Directory.Exists(folderPath))
            {
                result.Errors.Add("ディレクトリが存在しません。");
                return result;
            }

            var bmpFiles = Directory.GetFiles(folderPath, "*.bmp");
            result.Total = bmpFiles.Length;

            if (bmpFiles.Length == 0)
            {
                result.Errors.Add("対象の .bmp ファイルが見つかりませんでした。");
                return result;
            }

            var successList = new List<string>();

            foreach (var bmpFile in bmpFiles)
            {
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

                        // png に保存
                        using var pngStream = File.Create(pngPath);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        encoder.Save(pngStream);
                    });

                    // PNGのサイズ確認
                    var fi = new FileInfo(pngPath);
                    if (fi.Length == 0)
                    {
                        throw new IOException("出力された PNG のサイズが 0 バイトです。");
                    }

                    successList.Add(bmpFile);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{Path.GetFileName(bmpFile)}: {ex.Message}");
                }
            }

            result.SuccessCount = successList.Count;

            if (result.SuccessCount == result.Total)
            {
                // 全件成功 → 元 .bmp を削除
                foreach (var bmp in successList)
                {
                    try
                    {
                        File.Delete(bmp);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"{Path.GetFileName(bmp)} の削除に失敗: {ex.Message}");
                    }
                }
            }

            return result;
        }
    }
}