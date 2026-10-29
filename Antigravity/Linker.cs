using System;
using System.Collections.Generic;
using System.IO;

namespace osuLazer2TaikoNauts
{
    public class Linker
    {
        private readonly string _filesPath;
        private readonly string _osuTaikoPath;

        public Linker(string filesPath, string outputPath)
        {
            _filesPath = filesPath;
            _osuTaikoPath = Path.Combine(outputPath, "osuTaiko");
        }

        private string GetHashedFilePath(string fileHash)
        {
            if (string.IsNullOrEmpty(fileHash) || fileHash.Length < 2)
            {
                return "";
            }
            // osu!lazerの実体ファイルは files/a/ab/abcdef... のような階層構造で保存されている
            return Path.Combine(_filesPath, fileHash.Substring(0, 1), fileHash.Substring(0, 2), fileHash);
        }

        public void ProcessDeletions(List<string> deleteList)
        {
            foreach (var folderName in deleteList)
            {
                string folderPath = Path.Combine(_osuTaikoPath, folderName);
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        Directory.Delete(folderPath, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"フォルダの削除に失敗しました ({folderPath}): {ex.Message}");
                    }
                }
            }
        }

        public void ProcessCreationsAndUpdates(Dictionary<string, BeatmapSetData> itemList)
        {
            foreach (var kvp in itemList)
            {
                var bInfo = kvp.Value;
                string folderPath = Path.Combine(_osuTaikoPath, bInfo.FolderName);
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                foreach (var fileKvp in bInfo.Files)
                {
                    string fileName = fileKvp.Key;
                    string fileHash = fileKvp.Value;
                    
                    string linkPath = Path.Combine(folderPath, fileName);
                    string targetPath = GetHashedFilePath(fileHash);

                    // サブフォルダがある場合は親ディレクトリを確保
                    string? subDir = Path.GetDirectoryName(linkPath);
                    if (!string.IsNullOrEmpty(subDir) && !Directory.Exists(subDir))
                    {
                        Directory.CreateDirectory(subDir);
                    }

                    if (File.Exists(linkPath) || Directory.Exists(linkPath))
                    {
                        try
                        {
                            File.Delete(linkPath);
                        }
                        catch { }
                    }

                    if (File.Exists(targetPath))
                    {
                        try
                        {
                            File.CreateSymbolicLink(linkPath, targetPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"リンク作成エラー ({linkPath} -> {targetPath}): {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"警告: ターゲットファイルが存在しません: {targetPath}");
                    }
                }
            }
        }
    }
}
