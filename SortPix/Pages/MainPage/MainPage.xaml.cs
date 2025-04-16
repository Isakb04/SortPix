using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SortPix.Managers;
using SortPix.Models;

namespace SortPix.Pages.MainPage
{
    public partial class MainPage : INotifyPropertyChanged
    {
        private string currentPath;
        public string CurrentPath
        {
            get => currentPath;
            set
            {
                if (currentPath != value)
                {
                    currentPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<FileSystemItem> Items { get; set; }
        private List<FileSystemItem> allItems; // Store all items for filtering
        private readonly FileManager fileManager;
        private readonly SideBarManager sideBarManager;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this; // Set BindingContext to the MainPage itself
            Items = new ObservableCollection<FileSystemItem>();
            allItems = new List<FileSystemItem>();
            fileManager = new FileManager();
            sideBarManager = new SideBarManager();

            FileListView.ItemsSource = Items;
            SidebarListView.ItemsSource = sideBarManager.SidebarItems;
            FileListView.ItemAppearing += OnFileItemAppearing;
            LoadFilesAndDirectories(sideBarManager.SidebarItems.FirstOrDefault()?.Path);
        }

        private void OnSidebarItemSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedSidebarItem = e.CurrentSelection.FirstOrDefault() as SidebarItem;
            LoadFilesAndDirectories(selectedSidebarItem?.Path);
        }

        private async void LoadFilesAndDirectories(string path)
        {
            Items.Clear();
            allItems.Clear();
            CurrentPath = path; // Update CurrentPath for binding
            UpdateSortButtonVisibility(); // Update SortButton visibility

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                var entries = await fileManager.GetFilesAndDirectoriesAsync(path);
                foreach (var entry in entries)
                {
                    Items.Add(entry);
                    allItems.Add(entry);
                }
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }
        
        private async void OnUpButtonClickedBack(object sender, EventArgs e)
        {
            var parentDir = Directory.GetParent(currentPath)?.FullName;
            if (!string.IsNullOrEmpty(parentDir))
            {
                var selectedSidebarItem = SidebarListView.SelectedItem as SidebarItem;
                var selectedSidebarDir = selectedSidebarItem?.Path;

                if (currentPath == selectedSidebarDir)
                {
                    var confirm = await DisplayAlert("Confirm Directory Navigation", "You are about to leave the selected default directory choices. Do you want to proceed?", "Yes", "No");
                    if (!confirm)
                    {
                        return;
                    }
                }
                LoadFilesAndDirectories(parentDir);
                UpdateSortButtonVisibility(); // Update SortButton visibility
            }
        }

        private async void OnbuttonClickedSort(object sender, EventArgs e)
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            // disable all buttons and search
            MainGrid .IsEnabled = false;
            
            SidebarListView.IsEnabled = false;
            FileListView.IsEnabled = false;

            try
            {
                var sortPixManager = new SortPixManager();
                string result = await Task.Run(() => sortPixManager.RunSortPixTagger("SortPixPyFiles/Images1", "SortPixPyFiles/Processed_Images"));
                System.Diagnostics.Debug.WriteLine(result);
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                LoadFilesAndDirectories(currentPath);
                SidebarListView.IsEnabled = true;
                FileListView.IsEnabled = true;
                MainGrid.IsEnabled = true;
                await DisplayAlert("SortPix Tagger", "Sorting completed successfully!", "OK");
            }
        }
        
        private void OnbuttonClickedDontSort (object sender, EventArgs e)
        {
            
        }
        
        private void OnbuttonClickedManualSort (object sender, EventArgs e)
        {

        }

        private void OnItemSingleTapped(object sender, EventArgs e)
        {
            var tappedItem = (sender as View)?.BindingContext as FileSystemItem;
            if (tappedItem != null)
            {
                FileListView.SelectedItem = tappedItem;
            }
        }

        private void OnItemDoubleTapped(object sender, EventArgs e)
        {
            var tappedItem = (sender as View)?.BindingContext as FileSystemItem;
            if (tappedItem != null)
            {
                fileManager.OpenItem(tappedItem, LoadFilesAndDirectories);
            }
        }

        private async void OnUpButtonClickedRenameSelected(object sender, EventArgs e)
        {
            var selectedItem = FileListView.SelectedItem as FileSystemItem;
            if (selectedItem == null)
            {
                await DisplayAlert("Error", "No item selected to rename.", "OK");
                return;
            }

            var newName = await DisplayPromptAsync("Rename File", "Enter your input:");
            if (!string.IsNullOrWhiteSpace(newName))
            {
                await fileManager.RenameItemAsync(selectedItem, newName, currentPath, LoadFilesAndDirectories);
            }
        }

        private async void OnbuttonClickedDeleteSelected(object sender, EventArgs e)
        {
            var selectedItem = FileListView.SelectedItem as FileSystemItem;
            if (selectedItem != null)
            {
                var confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{selectedItem.Name}'?", "Yes", "No");
                if (confirm)
                {
                    await fileManager.DeleteItemAsync(selectedItem, currentPath, LoadFilesAndDirectories);
                }
            }
        }

        private async void OnbuttonClickedCreateFolder(object sender, EventArgs e)
        {
            var newFolderName = await DisplayPromptAsync("Create Folder", "Enter folder name:");
            if (!string.IsNullOrWhiteSpace(newFolderName))
            {
                await fileManager.CreateFolderAsync(currentPath, newFolderName, LoadFilesAndDirectories);
            }
        }

        private async void OnbuttonClickedCreateFile(object sender, EventArgs e)
        {
            var newFileName = await DisplayPromptAsync("Create File", "Enter file name:");
            if (!string.IsNullOrWhiteSpace(newFileName))
            {
                await fileManager.CreateFileAsync(currentPath, newFileName, LoadFilesAndDirectories);
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;
            var filteredItems = allItems.Where(item => item.Name.ToLower().Contains(searchText)).ToList();

            Items.Clear();
            foreach (var item in filteredItems)
            {
                Items.Add(item);
            }
        }

        private async void OnFileItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            var appearingItem = e.Item as FileSystemItem;
            if (appearingItem != null && !appearingItem.IsImage && !string.IsNullOrEmpty(appearingItem.Path))
            {
                appearingItem.IsImage = await fileManager.IsImageFileAsync(appearingItem.Path);
                appearingItem.IconPath = appearingItem.IsImage ? appearingItem.Path : fileManager.GetFileIcon(appearingItem.Path);
            }
        }

        private void UpdateSortButtonVisibility()
        {
            SortButton.IsVisible = !string.IsNullOrEmpty(CurrentPath) && CurrentPath.Contains("Processed_Images");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
