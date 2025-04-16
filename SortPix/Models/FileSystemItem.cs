namespace SortPix.Models;

public class FileSystemItem
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsDirectory { get; set; }
    public virtual bool IsImage { get; set; }
    public string IconPath { get; set; }
    public bool IsSelected { get; set; } // Property to track selection state
}

