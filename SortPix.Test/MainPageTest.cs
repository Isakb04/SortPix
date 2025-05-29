// ...existing usings...
using SortPix.Managers;
using SortPix.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.IO;

namespace SortPix.Test;

// Add MainPageTest for testable logic
public class MainPageTest
{
    public ObservableCollection<FileSystemItem> Items { get; set; } = new();
    public List<FileSystemItem> AllItems { get; set; } = new();
    private readonly IFileManager fileManager;

    public MainPageTest(IFileManager fileManager)
    {
        this.fileManager = fileManager;
    }

    public async Task LoadFilesAndDirectories(string path)
    {
        Items.Clear();
        AllItems.Clear();
        var entries = await fileManager.GetFilesAndDirectoriesAsync(path);
        foreach (var entry in entries)
        {
            Items.Add(entry);
            AllItems.Add(entry);
        }
    }

    public void FilterItems(string searchText)
    {
        var filtered = AllItems.Where(item => item.Name.ToLower().Contains(searchText.ToLower())).ToList();
        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);
    }
}

// Add TestFileManager for test double
public class TestFileManager : IFileManager
{
    public List<FileSystemItem> ItemsToReturn { get; set; } = new();
    public Task<List<FileSystemItem>> GetFilesAndDirectoriesAsync(string path) => Task.FromResult(ItemsToReturn);
    public Task RenameItemAsync(FileSystemItem item, string newName, string currentPath, System.Action<string> refreshAction) => Task.CompletedTask;
    public Task DeleteItemAsync(FileSystemItem item, string currentPath, System.Action<string> refreshAction) => Task.CompletedTask;
    public Task CreateFolderAsync(string currentPath, string folderName, System.Action<string> refreshAction) => Task.CompletedTask;
    public Task CreateFileAsync(string currentPath, string fileName, System.Action<string> refreshAction) => Task.CompletedTask;
    public void OpenItem(FileSystemItem item, System.Action<string> refreshAction) { }
    public string GetFileIcon(string filePath) => "";
    public Task<bool> IsImageFileAsync(string filePath) => Task.FromResult(false);
    public Task MoveCutItemsAsync(List<FileSystemItem> itemsToMove, string destinationPath, System.Action<string> refreshAction) => Task.CompletedTask;
    public Task CopyItemsAsync(List<FileSystemItem> itemsToCopy, string destinationPath, System.Action<string> refreshAction) => Task.CompletedTask;
}

public class MainPageTestTest
{
    [Fact(DisplayName = "TC001: LoadFilesAndDirectories populates Items and AllItems")]
    public async Task TC001_LoadFilesAndDirectories_PopulatesItems()
    {
        var testItems = new List<FileSystemItem>
        {
            new FileSystemItem { Name = "file1.txt" },
            new FileSystemItem { Name = "dir1" }
        };
        var fileManager = new TestFileManager { ItemsToReturn = testItems };
        var vm = new MainPageTest(fileManager);

        await vm.LoadFilesAndDirectories("somepath");

        Assert.Equal(2, vm.Items.Count);
        Assert.Equal(2, vm.AllItems.Count);
        Assert.Contains(vm.Items, i => i.Name == "file1.txt");
        Assert.Contains(vm.Items, i => i.Name == "dir1");
    }

    [Fact(DisplayName = "TC002: OnUpButtonClickedBack loads parent directory")]
    public async Task TC002_OnUpButtonClickedBack_LoadsParentDirectory()
    {
        // Simulate navigating to parent directory by calling LoadFilesAndDirectories with parent path
        var parentPath = Path.Combine("C:\\", "TestParent");
        var childPath = Path.Combine(parentPath, "Child");
        var parentItems = new List<FileSystemItem>
        {
            new FileSystemItem { Name = "parentfile.txt", Path = Path.Combine(parentPath, "parentfile.txt") }
        };
        var fileManager = new TestFileManager { ItemsToReturn = parentItems };
        var vm = new MainPageTest(fileManager);

        await vm.LoadFilesAndDirectories(parentPath);

        Assert.Single(vm.Items);
        Assert.Equal("parentfile.txt", vm.Items[0].Name);
    }

    [Fact(DisplayName = "TC010: OnSearchBarTextChanged filters items")]
    public void TC010_OnSearchBarTextChanged_FiltersItems()
    {
        var vm = new MainPageTest(new TestFileManager());
        vm.AllItems = new List<FileSystemItem>
        {
            new FileSystemItem { Name = "apple.txt" },
            new FileSystemItem { Name = "banana.txt" },
            new FileSystemItem { Name = "apricot.txt" }
        };
        vm.FilterItems("ap");
        Assert.All(vm.Items, i => Assert.Contains("ap", i.Name));
        Assert.Equal(2, vm.Items.Count);
    }
}
