using Xunit;
using SortPix.Managers;
using System;
using System.IO;

namespace SortPix.Test;

public class SideBarTest
{
    [Fact(DisplayName = "TC001: SideBarManager constructor populates SidebarItems")]
    public void TC001_SideBarManager_Constructor_PopulatesSidebarItems()
    {
        var manager = new SideBarManager();
        Assert.NotNull(manager.SidebarItems);
        // Check for some default items
        Assert.Contains(manager.SidebarItems, item => item.Name == "Desktop");
        Assert.Contains(manager.SidebarItems, item => item.Name == "Downloads");
        Assert.Contains(manager.SidebarItems, item => item.Name == "Documents");
        Assert.Contains(manager.SidebarItems, item => item.Name == "Pictures");
        Assert.Contains(manager.SidebarItems, item => item.Name == "SortPix");
    }

    [Fact(DisplayName = "TC002: GetSortPixProcessedImagesPath returns valid directory")]
    public void TC002_GetSortPixProcessedImagesPath_ReturnsValidPath()
    {
        // Use reflection to call the private static method
        var method = typeof(SideBarManager).GetMethod("GetSortPixProcessedImagesPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var path = (string)method.Invoke(null, null);
        Assert.False(string.IsNullOrEmpty(path));
        // Directory may not exist, but path should end with "SortPixPyFiles\\Processed_Images"
        Assert.EndsWith(Path.Combine("SortPixPyFiles", "Processed_Images"), path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "TC003: GetSpecialFolderPath returns correct Desktop path")]
    public void TC003_GetSpecialFolderPath_ReturnsDesktopPath()
    {
        var method = typeof(SideBarManager).GetMethod("GetSpecialFolderPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var desktopPath = (string)method.Invoke(null, new object[] { Environment.SpecialFolder.Desktop });
        Assert.False(string.IsNullOrEmpty(desktopPath));
        Assert.True(Directory.Exists(desktopPath));
    }
}
