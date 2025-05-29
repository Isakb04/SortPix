using System;
using CommunityToolkit.Maui.Views;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SortPix.Popups
{
    public partial class ManualSortPopup : Popup, INotifyPropertyChanged
    {
        private List<string> allTags = new();
        public ObservableCollection<string> FilteredTags { get; set; } = new();
        public ObservableCollection<string> SelectedTags { get; set; } = new();

        private string manualTagJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SortPixPyFiles", "ManualTag.json");
        private string selectedImagePath;

        private string selectedImageSource;
        public string SelectedImageSource
        {
            get => selectedImageSource;
            set
            {
                if (selectedImageSource != value)
                {
                    selectedImageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public ManualSortPopup(string imagePath = null)
        {
            InitializeComponent();
            BindingContext = this;
            LoadTags();
            UpdateSelectedTagsLabel();

            // Only set selectedImagePath if imagePath is not null or empty
            if (!string.IsNullOrEmpty(imagePath))
            {
                selectedImagePath = imagePath;
                if (File.Exists(selectedImagePath))
                {
                    SelectedImageSource = selectedImagePath;
                }
                else
                {
                    SelectedImageSource = null;
                }
            }
            // Do not overwrite selectedImagePath if imagePath is null
        }

        private void LoadTags()
        {
            // Load tags from both files
            string cocoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SortPixPyFiles", "coco.names");
            string imagenetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SortPixPyFiles", "imagenet_classes.txt");

            var tags = new List<string>();
            if (File.Exists(imagenetPath))
                tags.AddRange(File.ReadAllLines(imagenetPath).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()));
            if (File.Exists(cocoPath))
                tags.AddRange(File.ReadAllLines(cocoPath).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()));

            allTags = tags.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();

            FilteredTags.Clear();
            foreach (var tag in allTags.Take(25))
                FilteredTags.Add(tag);
        }

        private void FilterTags(string search)
        {
            FilteredTags.Clear();

            // Search through all tags
            var matchingTags = allTags.Where(t => t.Contains(search, StringComparison.OrdinalIgnoreCase));

            foreach (var tag in matchingTags)
                FilteredTags.Add(tag);
        }

        private void UpdateSelectedTagsLabel()
        {
            SelectedTagsLabel.Text = string.Join(", ", SelectedTags);
        }

        private void OnCloseButtonClicked(object sender, EventArgs e)
        {
            Close();
        }

        private async void OnSaveButtonClicked(object? sender, EventArgs e)
        {
            // Check both selectedImagePath and SelectedImageSource
            if (string.IsNullOrEmpty(selectedImagePath) && string.IsNullOrEmpty(SelectedImageSource))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select an image.", "OK");
                return;
            }
            if (SelectedTags == null || SelectedTags.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select at least one tag.", "OK");
                return;
            }

            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Manual Tag",
                $"Are you sure you want to save these tags for '{Path.GetFileName(selectedImagePath ?? SelectedImageSource)}'?",
                "Yes", "No");

            if (!confirm)
                return;
            
            // Get the directory the app is running from
            string binPath = AppContext.BaseDirectory;
            Console.WriteLine("binPath: " + binPath);

            // Go up 3 levels to get to project root
            string projectRoot = Directory.GetParent(binPath)?.Parent?.Parent?.Parent?.Parent?.FullName;
            Console.WriteLine("projectRoot: " + projectRoot);

            if (projectRoot == null)
            {
                throw new Exception("Failed to find project root.");
            }

            var manualTagJsonPath = Path.Combine(projectRoot, "SortPixPyFiles", "ManualTag.json");
            var manualTagData = new ManualTagJson { ManualTagImages = new List<ManualTagImage>() };

            if (File.Exists(manualTagJsonPath))
            {
                var json = File.ReadAllText(manualTagJsonPath);
                manualTagData = JsonSerializer.Deserialize<ManualTagJson>(json) ?? new ManualTagJson();
            }

            // Remove existing entry for this image if present
            string imageName = Path.GetFileName(selectedImagePath ?? SelectedImageSource);
            manualTagData.ManualTagImages.RemoveAll(x => x.ImageName == imageName);

            // Add the new entry
            manualTagData.ManualTagImages.Add(new ManualTagImage
            {
                ImageName = imageName,
                Tags = SelectedTags.ToList()
            });

            // Save the updated JSON back to the file
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(manualTagData, options);
            File.WriteAllText(manualTagJsonPath, jsonString);
            await Application.Current.MainPage.DisplayAlert("Success", "Tags saved successfully.", "OK");
            Close();
        }

        private void OnSearchBarTextChangedTags(object? sender, TextChangedEventArgs e)
        {
            FilterTags(e.NewTextValue ?? string.Empty);
        }

        private void TagsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedTags.Clear();
            foreach (var tag in e.CurrentSelection.OfType<string>())
            {
                SelectedTags.Add(tag);
            }
            UpdateSelectedTagsLabel();
        }

        // Helper classes for JSON serialization
        public class ManualTagJson
        {
            public List<ManualTagImage> ManualTagImages { get; set; } = new();
        }

        public class ManualTagImage
        {
            public string ImageName { get; set; }
            public List<string> Tags { get; set; }
        }

        // Add INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
