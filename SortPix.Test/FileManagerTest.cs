using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SortPix.Managers;
using SortPix.Models;
using Xunit;

namespace SortPix.Test;

public class FileManagerTest
{
    private readonly FileManager _fileManager = new FileManager();

    [Fact]
    public async Task TC001_GetFilesAndDirectoriesAsync_ReturnsFilesAndDirectoriesForValidPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "testfile.txt");
        var dirPath = Path.Combine(tempDir, "subdir");
        File.WriteAllText(filePath, "test");
        Directory.CreateDirectory(dirPath);

        var result = await _fileManager.GetFilesAndDirectoriesAsync(tempDir);

        Assert.Contains(result, x => x.Name == "testfile.txt" && !x.IsDirectory);
        Assert.Contains(result, x => x.Name == "subdir" && x.IsDirectory);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task TC002_GetFilesAndDirectoriesAsync_HandlesEmptyPath()
    {
        var result = await _fileManager.GetFilesAndDirectoriesAsync(string.Empty);

        Assert.Single(result);
        Assert.Equal("Recycle Bin (Not Supported)", result[0]?.Name);
    }

    [Fact]
    public async Task TC003_RenameItemAsync_RenamesFileCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "oldname.txt");
        File.WriteAllText(filePath, "test");
        var item = new FileSystemItem { Name = "oldname.txt", Path = filePath, IsDirectory = false };

        bool refreshCalled = false;
        await _fileManager.RenameItemAsync(item, "newname.txt", tempDir, _ => refreshCalled = true);

        var newPath = Path.Combine(tempDir, "newname.txt");
        Assert.True(File.Exists(newPath));
        Assert.True(refreshCalled);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task TC004_RenameItemAsync_RenamesDirectoryCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var dirPath = Path.Combine(tempDir, "olddir");
        Directory.CreateDirectory(dirPath);
        var item = new FileSystemItem { Name = "olddir", Path = dirPath, IsDirectory = true };

        bool refreshCalled = false;
        await _fileManager.RenameItemAsync(item, "newdir", tempDir, _ => refreshCalled = true);

        var newPath = Path.Combine(tempDir, "newdir");
        Assert.True(Directory.Exists(newPath));
        Assert.True(refreshCalled);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task TC005_DeleteItemAsync_DeletesFileCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "todelete.txt");
        File.WriteAllText(filePath, "test");
        var item = new FileSystemItem { Name = "todelete.txt", Path = filePath, IsDirectory = false };

        bool refreshCalled = false;
        await _fileManager.DeleteItemAsync(item, tempDir, _ => refreshCalled = true);

        Assert.False(File.Exists(filePath));
        Assert.True(refreshCalled);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task TC006_DeleteItemAsync_DeletesDirectoryCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var dirPath = Path.Combine(tempDir, "todeletedir");
        Directory.CreateDirectory(dirPath);
        var item = new FileSystemItem { Name = "todeletedir", Path = dirPath, IsDirectory = true };

        bool refreshCalled = false;
        await _fileManager.DeleteItemAsync(item, tempDir, _ => refreshCalled = true);

        Assert.False(Directory.Exists(dirPath));
        Assert.True(refreshCalled);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task TC007_CreateFolderAsync_CreatesFolderCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        bool refreshCalled = false;
        await _fileManager.CreateFolderAsync(tempDir, "newfolder", _ => refreshCalled = true);

        var newFolderPath = Path.Combine(tempDir, "newfolder");
        Assert.True(Directory.Exists(newFolderPath));
        Assert.True(refreshCalled);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task TC008_CreateFileAsync_CreatesFileCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        bool refreshCalled = false;
        await _fileManager.CreateFileAsync(tempDir, "newfile.txt", _ => refreshCalled = true);

        var newFilePath = Path.Combine(tempDir, "newfile.txt");
        Assert.True(File.Exists(newFilePath));
        Assert.True(refreshCalled);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void TC009_OpenItem_OpensFileWithDefaultApplication()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "openme.txt");
        File.WriteAllText(filePath, "test");
        var item = new FileSystemItem { Name = "openme.txt", Path = filePath, IsDirectory = false };

        var ex = Record.Exception(() => _fileManager.OpenItem(item, _ => { }));
        Assert.Null(ex);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void TC010_OpenItem_OpensDirectoryCallsRefresh()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var item = new FileSystemItem { Name = "subdir", Path = tempDir, IsDirectory = true };

        string? refreshedPath = null;
        _fileManager.OpenItem(item, p => refreshedPath = p);

        Assert.Equal(tempDir, refreshedPath);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void TC011_GetFileIcon_ReturnsCorrectIconForKnownType()
    {
        var icon = _fileManager.GetFileIcon("file.txt");
        Assert.Equal("text_icon.png", icon);
    }

    [Fact]
    public void TC012_GetFileIcon_ReturnsDefaultIconForUnknownType()
    {
        var icon = _fileManager.GetFileIcon("file.unknown");
        Assert.Equal("unknown_icon.png", icon);
    }

    [Fact]
    public async Task TC013_IsImageFileAsync_IdentifiesImageFile()
    {
        var result = await _fileManager.IsImageFileAsync("image.jpg");
        Assert.True(result);
    }

    [Fact]
    public async Task TC014_IsImageFileAsync_IdentifiesNonImageFile()
    {
        var result = await _fileManager.IsImageFileAsync("file.txt");
        Assert.False(result);
    }

    [Fact]
    public async Task TC015_MoveCutItemsAsync_MovesFilesAndDirectoriesCorrectly()
    {
        var tempSrc = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempDst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempSrc);
        Directory.CreateDirectory(tempDst);

        var filePath = Path.Combine(tempSrc, "movefile.txt");
        File.WriteAllText(filePath, "test");
        var dirPath = Path.Combine(tempSrc, "movedir");
        Directory.CreateDirectory(dirPath);

        var items = new List<FileSystemItem>
        {
            new FileSystemItem { Name = "movefile.txt", Path = filePath, IsDirectory = false },
            new FileSystemItem { Name = "movedir", Path = dirPath, IsDirectory = true }
        };

        bool refreshCalled = false;
        await _fileManager.MoveCutItemsAsync(items, tempDst, _ => refreshCalled = true);

        Assert.True(File.Exists(Path.Combine(tempDst, "movefile.txt")));
        Assert.True(Directory.Exists(Path.Combine(tempDst, "movedir")));
        Assert.True(refreshCalled);

        Directory.Delete(tempSrc, true);
        Directory.Delete(tempDst, true);
    }

    [Fact]
    public async Task TC016_CopyItemsAsync_CopiesFilesAndDirectoriesCorrectly()
    {
        var tempSrc = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempDst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempSrc);
        Directory.CreateDirectory(tempDst);

        var filePath = Path.Combine(tempSrc, "copyfile.txt");
        File.WriteAllText(filePath, "test");
        var dirPath = Path.Combine(tempSrc, "copydir");
        Directory.CreateDirectory(dirPath);

        var items = new List<FileSystemItem>
        {
            new FileSystemItem { Name = "copyfile.txt", Path = filePath, IsDirectory = false },
            new FileSystemItem { Name = "copydir", Path = dirPath, IsDirectory = true }
        };

        bool refreshCalled = false;
        await _fileManager.CopyItemsAsync(items, tempDst, _ => refreshCalled = true);

        Assert.True(File.Exists(Path.Combine(tempDst, "copyfile.txt")));
        Assert.True(Directory.Exists(Path.Combine(tempDst, "copydir")));
        Assert.True(refreshCalled);

        Directory.Delete(tempSrc, true);
        Directory.Delete(tempDst, true);
    }
}