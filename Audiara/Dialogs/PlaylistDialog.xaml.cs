﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Audiara.Shared;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Audiara
{
    /// <summary>
    /// Interaction logic for PlaylistDialog.xaml
    /// </summary>
    public partial class PlaylistDialog : Window
    {
        // Tracks the number of items added to the playlist (used for indexing).
        private int _playlistItemCount = 0;

        // Holds the playlist entries: filename (for UI) mapped to full path (for playback).
        private readonly Dictionary<string, string> _playlistFiles = new();

        // Reference to the main window to update the playing queue and index.
        private readonly MainWindow _mainWindow;

        // Constructor initializes UI and pre-loads existing playlist items.
        public PlaylistDialog(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            LoadInitialPlaylistItems();
        }

        // Triggered when the window is closed; ensures sync with main playlist state.
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SyncPlaylistToMainWindow();
        }

        // Adds all items from the shared playlist into the local playlist UI, avoiding duplicates.
        private void LoadInitialPlaylistItems()
        {
            foreach (string filePath in MainWindow.PlaylistSongs)
            {
                string fileName = Path.GetFileName(filePath);

                if (!_playlistFiles.ContainsValue(filePath))
                {
                    _playlistItemCount++;
                    _playlistFiles.Add(fileName, filePath);
                    ListBoxHelper.AddItem(SongsPlaylist, _playlistItemCount.ToString(), fileName);
                }
            }
        }

        // Pushes the local playlist state back to the main window.
        private void SyncPlaylistToMainWindow()
        {
            MainWindow.PlaylistSongs.Clear();
            foreach (var path in _playlistFiles.Values)
            {
                MainWindow.PlaylistSongs.Add(path);
            }
        }

        // Adds a single .mp3 file to the playlist via OpenFileDialog.
        private void OnAddFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                FileName = "Music",
                DefaultExt = ".mp3",
                Filter = "Audio Files (.mp3)|*.mp3"
            };

            if (dialog.ShowDialog() == true)
            {
                string fullPath = dialog.FileName;
                string fileName = Path.GetFileName(fullPath);

                if (_playlistFiles.ContainsValue(fullPath))
                {
                    MessageBox.Show("This mp3 file is already in the playlist.", "Error", MessageBoxButton.OK);
                    return;
                }

                _playlistItemCount++;
                _playlistFiles.Add(fileName, fullPath);
                ListBoxHelper.AddItem(SongsPlaylist, _playlistItemCount.ToString(), fileName);
            }
        }

        // Adds all .mp3 files from a selected folder, skipping existing entries.
        private void OnAddFolderClick(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = dialog.SelectedPath;
                try
                {
                    string[] mp3Files = Directory.GetFiles(folderPath, "*.mp3");
                    foreach (string file in mp3Files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (_playlistFiles.ContainsKey(fileName)) continue;

                        _playlistItemCount++;
                        _playlistFiles.Add(fileName, file);
                        ListBoxHelper.AddItem(SongsPlaylist, _playlistItemCount.ToString(), fileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        // Removes a selected file from the playlist and updates the UI.
        private void OnRemoveFileClick(object sender, RoutedEventArgs e)
        {
            string selectedFileName = GetSelectedDescription();
            if (!_playlistFiles.ContainsKey(selectedFileName)) return;

            string fullPath = _playlistFiles[selectedFileName];

            _playlistFiles.Remove(selectedFileName);
            MainWindow.PlaylistSongs.Remove(fullPath);

            RefreshPlaylistUI();
        }

        // Clears the entire playlist both visually and in memory.
        private void OnClearPlaylistClick(object sender, RoutedEventArgs e)
        {
            _playlistFiles.Clear();
            MainWindow.PlaylistSongs.Clear();
            _playlistItemCount = 0;
            SongsPlaylist.Items.Clear();
        }

        // Finalizes the playlist and starts playback from the selected or first item.
        private void OnPlayPlaylistClick(object sender, RoutedEventArgs e)
        {
            MainWindow.PlaylistSongs.Clear();
            foreach (string path in _playlistFiles.Values)
            {
                MainWindow.PlaylistSongs.Add(path);
            }

            string selectedFileName = GetSelectedDescription();
            if (!string.IsNullOrEmpty(selectedFileName) && _playlistFiles.ContainsKey(selectedFileName))
            {
                string selectedPath = _playlistFiles[selectedFileName];
                int index = MainWindow.PlaylistSongs.IndexOf(selectedPath);
                _mainWindow.CurrentPlaylistIndex = index != -1 ? index : 0;
            }
            else
            {
                _mainWindow.CurrentPlaylistIndex = 0;
            }

            _mainWindow.PlayNextSong();
            Close();
        }

        // Redraws the entire playlist list box to reflect current state.
        private void RefreshPlaylistUI()
        {
            SongsPlaylist.Items.Clear();
            _playlistItemCount = 0;
            foreach (var item in _playlistFiles)
            {
                _playlistItemCount++;
                ListBoxHelper.AddItem(SongsPlaylist, _playlistItemCount.ToString(), item.Key);
            }
        }

        // Extracts the display text (file name) of the currently selected playlist item.
        private string GetSelectedDescription()
        {
            if (SongsPlaylist.SelectedItem is ListBoxItem selectedItem &&
                selectedItem.Content is StackPanel stackPanel &&
                stackPanel.Children.Count > 1 &&
                stackPanel.Children[1] is TextBlock descriptionTextBlock)
            {
                return descriptionTextBlock.Text;
            }

            return string.Empty;
        }
    }
}