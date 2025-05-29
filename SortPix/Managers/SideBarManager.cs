using System.Collections.ObjectModel;
using SortPix.Models;

namespace SortPix.Managers;

using System.Linq;

public class SideBarManager
{
    public ObservableCollection<SidebarItem> SidebarItems { get; private set; }
    private SidebarItem SortManagerItem { get; }

    public SideBarManager()
    {
        SortManagerItem = new SidebarItem
        {
            Name = "Sort Manager",
            IconPath = "sort_manager__dropdown_icon.png",
        };

        SidebarItems = new ObservableCollection<SidebarItem>
        {
            new SidebarItem
            {
                Name = "Desktop", Path = GetSpecialFolderPath(Environment.SpecialFolder.Desktop),
                IconPath = "desktop_icon.png"
            },
            new SidebarItem
            {
                Name = "Downloads",
                Path = Path.Combine(GetSpecialFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                IconPath = "downloads_icon.png"
            },
            new SidebarItem
            {
                Name = "Documents", Path = GetSpecialFolderPath(Environment.SpecialFolder.MyDocuments),
                IconPath = "document_icon.png"
            },
            new SidebarItem
            {
                Name = "Pictures", Path = GetSpecialFolderPath(Environment.SpecialFolder.MyPictures),
                IconPath = "photos_icon.png"
            },
            new SidebarItem
            {
                Name = "SortPix",
                Path = GetSortPixProcessedImagesPath(),
                IconPath = "tagged_photos_icon.png"
            },
            new SidebarItem
            {
                Name = "Music", Path = GetSpecialFolderPath(Environment.SpecialFolder.MyMusic),
                IconPath = "music_icon.png"
            },
            new SidebarItem
            {
                Name = "Videos", Path = GetSpecialFolderPath(Environment.SpecialFolder.MyVideos),
                IconPath = "movie_icon.png"
            },
            new SidebarItem
            {
                Name = "Recycle Bin", Path = null, IconPath = "bin_icon.png"
            },
            new SidebarItem
            {
                Name = "OneDrive",
                Path = GetSpecialFolderPath(Environment.SpecialFolder.UserProfile) +
                       @"\OneDrive - The University of Winchester",
                IconPath = "onedrive_icon.png"
            }
        };
    }

    private static string GetSortPixProcessedImagesPath()
    {
        string binPath = AppContext.BaseDirectory;
        string projectRoot = Directory.GetParent(binPath)?.Parent?.Parent?.Parent?.Parent?.FullName;

        if (projectRoot == null)
        {
            throw new Exception("Failed to find project root.");
        }

        return Path.Combine(projectRoot, "SortPixPyFiles", "Processed_Images");
    }

    private static string GetSpecialFolderPath(Environment.SpecialFolder folder)
    {
        return Environment.GetFolderPath(folder);
    }
}