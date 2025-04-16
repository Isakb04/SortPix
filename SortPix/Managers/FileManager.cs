using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SortPix.Models;

namespace SortPix.Pages.MainPage;

public class FileManager
{
    public async Task<List<FileSystemItem>> GetFilesAndDirectoriesAsync(string path)
    {
        var items = new List<FileSystemItem>();

        if (string.IsNullOrEmpty(path))
        {
            items.Add(new FileSystemItem { Name = "Recycle Bin (Not Supported)", Path = string.Empty, IsDirectory = false, IconPath = "recycle_bin_icon.png" });
            return items;
        }

        try
        {
            var directoryInfo = new DirectoryInfo(path);
            var entries = directoryInfo.GetDirectories().Select(dir => new FileSystemItem
                {
                    Name = dir.Name,
                    Path = dir.FullName,
                    IsDirectory = true,
                    IconPath = "folder_icon.png"
                })
                .Concat(directoryInfo.GetFiles()
                    .Where(file => !file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    .Select(file => new FileSystemItem
                    {
                        Name = file.Name,
                        Path = file.FullName,
                        IsDirectory = false,
                        IsImage = false, // Initially set to false
                        IconPath = GetFileIcon(file.FullName)
                    }));

            foreach (var entry in entries)
            {
                if (entry.IsImage)
                {
                    await Task.Delay(20); // Simulate delay for image loading
                }
                items.Add(entry);
            }
        }
        catch
        {
            items.Add(new FileSystemItem { Name = "Error loading directory", Path = string.Empty, IsDirectory = false, IconPath = "error_icon.png" });
        }

        return items;
    }

    public async Task RenameItemAsync(FileSystemItem item, string newName, string currentPath, Action<string> refreshAction)
    {
        var directoryName = Path.GetDirectoryName(item.Path);
        if (!string.IsNullOrEmpty(directoryName))
        {
            var newPath = Path.Combine(directoryName, newName);
            try
            {
                if (item.IsDirectory)
                {
                    Directory.Move(item.Path, newPath);
                }
                else
                {
                    File.Move(item.Path, newPath);
                }
                refreshAction(currentPath);
            }
            catch
            {
                // Handle rename error
            }
        }
    }

    public async Task DeleteItemAsync(FileSystemItem item, string currentPath, Action<string> refreshAction)
    {
        try
        {
            if (item.IsDirectory)
            {
                Directory.Delete(item.Path, true);
            }
            else
            {
                using (File.Open(item.Path, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                File.Delete(item.Path);
            }
            refreshAction(currentPath);
        }
        catch
        {
            // Handle delete error
        }
    }

    public async Task CreateFolderAsync(string currentPath, string folderName, Action<string> refreshAction)
    {
        var newPath = Path.Combine(currentPath, folderName);
        try
        {
            Directory.CreateDirectory(newPath);
            refreshAction(currentPath);
        }
        catch
        {
            // Handle folder creation error
        }
    }

    public async Task CreateFileAsync(string currentPath, string fileName, Action<string> refreshAction)
    {
        var newPath = Path.Combine(currentPath, fileName);
        try
        {
            File.Create(newPath).Dispose();
            refreshAction(currentPath);
        }
        catch
        {
            // Handle file creation error
        }
    }

    public void OpenItem(FileSystemItem item, Action<string> refreshAction)
    {
        if (item.IsDirectory)
        {
            refreshAction(item.Path);
        }
        else
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.Path,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Handle file open error
            }
        }
    }

    public string GetFileIcon(string filePath)
    {
        if (IsImageFile(filePath))
        {
            return filePath;
        }

        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".txt" => "text_icon.png",
            ".pdf" => "pdf_icon.png",
            ".mp3" => "audio_icon.png",
            ".mp4" => "video_icon.png",
            ".mkv" => "video_icon.png",
            ".zip" => "zip_icon.png",
            ".rar" => "rar_icon.png",
            ".exe" => "exe_icon.png",
            ".docx" => "word_icon.png", // Example for Word files
            ".doc" => "word_icon.png", // Example for Word files
            ".xlsx" => "excel_icon.png", // Example for Excel files
            ".pptx" => "powerpoint_icon.png", // Example for PowerPoint files
                
            // programing lang
            ".py" => "python_icon.png",
            ".js" => "javascript_icon.png",
            ".java" => "java_icon.png",
            ".cpp" => "cplus_icon.png",
            ".c" => "c_icon.png",
            ".cs" => "csharp_icon.png",
            ".html" => "html_icon.png",
            ".css" => "css_icon.png",
            ".php" => "php_icon.png",
            ".go" => "go_icon.png",
            ".rb" => "ruby_icon.png",
            ".swift" => "swift_icon.png",
            ".sql" => "sql_icon.png",
            ".mysql" => "mysql_icon.png",
            ".xml" => "xml_icon.png",
            ".json" => "json_icon.png",
            ".csv" => "csv_icon.png",
            ".yaml" => "yaml_icon.png",
            ".yml" => "yaml_icon.png",
            ".fxml" => "fxml_icon.png",
            ".lua" => "lua_icon.png",
            ".kt" => "kotlin_icon.png",
            ".hs" => "haskell_icon.png",
            ".r" => "r_icon.png",
            ".dart" => "dart_icon.png",
            ".md" => "markdown_icon.png",
            ".7z" => "sevenzip_icon.png",
            ".pkt" => "packet_icon.png",
            _ => "unknown_icon.png" // Default icon for unknown file types
        };
    }

    private bool IsImageFile(string filePath)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        return extensions.Contains(Path.GetExtension(filePath).ToLower());
    }

    public async Task<bool> IsImageFileAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            return extensions.Contains(Path.GetExtension(filePath).ToLower());
        });
    }
}
