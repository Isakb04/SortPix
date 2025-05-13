using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

        private List<FileSystemItem> itemsToCut = new List<FileSystemItem>();
        private List<FileSystemItem> itemsToCopy = new List<FileSystemItem>();

        public ICommand OnItemSingleTappedCommand { get; }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this; // Set BindingContext to the MainPage itself
            Items = new ObservableCollection<FileSystemItem>();
            allItems = new List<FileSystemItem>();
            fileManager = new FileManager();
            sideBarManager = new SideBarManager();

            OnItemSingleTappedCommand = new Command<FileSystemItem>(OnItemSingleTapped);

            FileListView.ItemsSource = Items;
            SidebarListView.ItemsSource = sideBarManager.SidebarItems;
            FileListView.ItemAppearing += OnFileItemAppearing;
            LoadFilesAndDirectories(sideBarManager.SidebarItems.FirstOrDefault()?.Path);
        }

        private void OnSidebarItemSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedSidebarItem = e.CurrentSelection.FirstOrDefault() as SidebarItem;
            LoadFilesAndDirectories(selectedSidebarItem?.Path);

            if (selectedSidebarItem?.Name == "SortPix" || selectedSidebarItem?.Name == "OneDrive")
            {
                LocationLabel.Text = selectedSidebarItem?.Name;
            }
            else
            {
                LocationLabel.Text = selectedSidebarItem?.Path;
            }
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
                    var confirm = await DisplayAlert("Confirm Directory Navigation",
                        "You are about to leave the selected default directory choices. Do you want to proceed?", "Yes",
                        "No");
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
            MainGrid.IsEnabled = false;

            SidebarListView.IsEnabled = false;
            FileListView.IsEnabled = false;

            try
            {
                var sortPixManager = new SortPixManager();
                string result = await Task.Run(() =>
                    sortPixManager.RunSortPixTagger("SortPixPyFiles/Images1", "SortPixPyFiles/Processed_Images"));
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

        private void OnbuttonClickedDontSort(object sender, EventArgs e)
        {
            // Handle the "Don't Sort" button click
            // You can implement any logic you want here
            // For example, you might want to close the sort dialog or reset some state
        }

        private async void OnbuttonClickedManualSort(object sender, EventArgs e)
        {
            // var manualSortPopup = new ManualSortPopup();
            // await PopupNavigation.Instance.PushAsync(manualSortPopup);
        }

        private void OnItemSingleTapped(FileSystemItem tappedItem)
        {
            if (tappedItem != null)
            {
                foreach (var item in Items)
                {
                    item.IsSelected = false; // Deselect all other items
                }

                tappedItem.IsSelected = true; // Select the tapped item
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
            var selectedItems = Items.Where(item => item.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                await DisplayAlert("Error", "No items selected to rename.", "OK");
                return;
            }

            foreach (var selectedItem in selectedItems)
            {
                var newName = await DisplayPromptAsync($"Rename {selectedItem.Name}", "Enter new name:");
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    await fileManager.RenameItemAsync(selectedItem, newName, currentPath, LoadFilesAndDirectories);
                    FileListView.SelectedItem = null; // Deselect the item
                }
            }
        }

        private async void OnbuttonClickedDeleteSelected(object sender, EventArgs e)
        {
            var selectedItems = Items.Where(item => item.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                await DisplayAlert("Error", "No items selected to delete.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Confirm Delete",
                $"Are you sure you want to delete {selectedItems.Count} items?", "Yes", "No");
            if (confirm)
            {
                foreach (var selectedItem in selectedItems)
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
                appearingItem.IconPath = appearingItem.IsImage
                    ? appearingItem.Path
                    : fileManager.GetFileIcon(appearingItem.Path);
            }
        }

        private void UpdateSortButtonVisibility()
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                var selectedSidebarItem = SidebarListView.SelectedItem as SidebarItem;
                if (selectedSidebarItem?.Name == "SortPix" || selectedSidebarItem?.Name == "Pictures")
                {
                    SortButton.IsVisible = true;
                }
                else
                {
                    SortButton.IsVisible = false;
                }
            }
            else
            {
                SortButton.IsVisible = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnbuttonClickedCutSelected(object sender, EventArgs e)
        {
            itemsToCut = Items.Where(item => item.IsSelected).ToList();
            if (!itemsToCut.Any())
            {
                DisplayAlert("Error", "No items selected to cut.", "OK");
            }
            else
            {
                foreach (var item in itemsToCut)
                {
                    item.IsSelected = false; // Deselect items after marking them for cut
                }

                FileListView.SelectedItem = null;
                DisplayAlert("Cut", $"{itemsToCut.Count} item(s) marked for moving.", "OK");
            }
        }

        private async void OnbuttonClickedCopySelected(object? sender, EventArgs e)
        {
            itemsToCopy = Items.Where(item => item.IsSelected).ToList();
            if (!itemsToCopy.Any())
            {
                await DisplayAlert("Error", "No items selected to copy.", "OK");
                return;
            }

            foreach (var item in itemsToCopy)
            {
                item.IsSelected = false; // Deselect items after marking them for copy
            }

            FileListView.SelectedItem = null;
            await DisplayAlert("Copy", $"{itemsToCopy.Count} item(s) marked for copying.", "OK");
        }

        private async void OnbuttonClickedPasteSelected(object sender, EventArgs e)
        {
            if (itemsToCut.Any())
            {
                try
                {
                    await fileManager.MoveCutItemsAsync(itemsToCut, currentPath, LoadFilesAndDirectories);
                    itemsToCut.Clear(); // Clear the cut list after moving
                    await DisplayAlert("Paste", "Items moved successfully.", "OK");
                }
                catch
                {
                    await DisplayAlert("Error", "Failed to move items.", "OK");
                }
            }
            else if (itemsToCopy.Any())
            {
                try
                {
                    await fileManager.CopyItemsAsync(itemsToCopy, currentPath, LoadFilesAndDirectories);
                    itemsToCopy.Clear(); // Clear the copy list after copying
                    await DisplayAlert("Paste", "Items copied successfully.", "OK");
                }
                catch
                {
                    await DisplayAlert("Error", "Failed to copy items.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "No items to paste. Use 'Cut' or 'Copy' first.", "OK");
            }
        }

        // select all items in the list
        private void OnbuttonClickedSelectAll(object? sender, EventArgs e)
        {
            foreach (var item in Items)
            {
                item.IsSelected = true; // Select both files and directories
            }

            // Refresh the ListView to ensure checkboxes reflect the changes
            FileListView.ItemsSource = null;
            FileListView.ItemsSource = Items;
        }

        private void OnbuttonClickedDeselectAll(object? sender, EventArgs e)
        {
            foreach (var item in Items)
            {
                item.IsSelected = false; // Deselect both files and directories
            }

            // Refresh the ListView to ensure checkboxes reflect the changes
            FileListView.ItemsSource = null;
            FileListView.ItemsSource = Items;
        }
    }
}
