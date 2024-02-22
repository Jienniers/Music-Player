﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using ShapesPath = System.Windows.Shapes.Path;
using System.Drawing.Imaging;
using System.Windows.Documents;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using MusicPlayer.Dialogs;
using Microsoft.Web.WebView2.Wpf;
using System.Diagnostics;



namespace MusicPlayer
{
    public partial class MainWindow : Window
    {
        private bool isDraggingSlider = false;
        private DispatcherTimer timer;
        public List<String> playlist_songs = new List<String>();
        private int playlistIndex = 0;
        private bool isPlaying = false;
        private string songPlayingPath;
        public Dictionary<string, string> FavJsonData = new Dictionary<string, string>();
        private bool ytMusicOpened = false;
        private bool maximized = false;


        public MainWindow()
        {
            InitializeComponent();
            LoadAppStartup();
        }

        private void LoadAppStartup()
        {
            mediaElement.LoadedBehavior = MediaState.Manual;
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            this.mediaElement.MediaEnded += MediaElement_MediaEnded;
            this.timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Input, Timer_Tick, this.Dispatcher);
        }

        class CallFunctions (WebView2 webView)
        {

            public async void InitializeWebView()
            {
                await webView.EnsureCoreWebView2Async(null);
            }

            public void LoadWebPage(string url)
            {
                webView.Source = new Uri(url);
            }

            public void AddDataToJsonFile(string filePath, Dictionary<string, string> newData)
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                Dictionary<string, string> existingData = new Dictionary<string, string>();
                if (File.Exists(filePath))
                {
                    string existingJson = File.ReadAllText(filePath);
                    existingData = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson);
                }
                foreach (var entry in newData)
                {
                    if (!existingData.ContainsKey(entry.Key))
                    {
                        existingData.Add(entry.Key, entry.Value);
                    }
                }
                string jsonString = JsonSerializer.Serialize(existingData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonString);
            }
        }

        private void PlayNextSong()
        {
            if (playlistIndex < playlist_songs.Count)
            {
                mediaElement.Source = new Uri(playlist_songs[playlistIndex], UriKind.RelativeOrAbsolute);
                mediaElement.Play();
                playlistIndex++;
            }
            else
            {
                mediaElement.Stop();
            }
        }

        public void FavPlaySong(string filepath)
        {
            mediaElement.Source = new Uri(filepath, UriKind.RelativeOrAbsolute);
            mediaElement.Play();
            this.slider.Value = 0;
            this.progressBar.Value = 0;
            isPlaying = true;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CrossButtonClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void PlayButton(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Music",
                DefaultExt = ".mp3",
                Filter = "Audio Files (.mp3)|*.mp3"
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                songPlayingPath = dialog.FileName;
                mediaElement.Source = new Uri(songPlayingPath, UriKind.RelativeOrAbsolute);
                mediaElement.Play();
            }
            this.slider.Value = 0;
            this.progressBar.Value = 0;
            isPlaying = true;
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (this.mediaElement.NaturalDuration.HasTimeSpan)
            {
                this.slider.Maximum = this.mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                this.slider.Value = 0;
                this.timer.Start();
            }
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.timer.Stop();
            this.slider.Value = 0;
            this.progressBar.Value = 0;
            if (isPlaying) isPlaying = false;
            PlayNextSong();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!this.isDraggingSlider)
            {
                double currentPosition = this.mediaElement.Position.TotalSeconds;
                this.slider.Value = currentPosition;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.isDraggingSlider)
            {
                this.mediaElement.Position = TimeSpan.FromSeconds(slider.Value);
            }
            if (!timer.IsEnabled)
            {
                timer.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.timer.Stop();
            this.timer.Tick -= Timer_Tick;
        }

        private void PauseButtonClick(object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
        }

        private void ResumeSongButtonClick(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaElement.Play();
            }
            else
            {
                if (playlistIndex < playlist_songs.Count)
                {
                    mediaElement.Source = new Uri(playlist_songs[playlistIndex], UriKind.RelativeOrAbsolute);
                    mediaElement.Play();
                    playlistIndex++;
                    if (playlistIndex >= playlist_songs.Count)
                    {
                        playlistIndex = 0;
                    }
                }
            }
        }

        private void StopSongButtonClick(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
            this.slider.Value = 0;
            this.progressBar.Value = 0;
        }

        private void PlaylistButtonClick(object sender, RoutedEventArgs e)
        {
            Playlist pla = new Playlist(this);
            pla.ShowDialog();
        }

        private void FavouriteSongButtonClick(object sender, RoutedEventArgs e)
        {
            if (songPlayingPath is null)
            {
                MessageBox.Show("No Song Playing", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string filePath = "Data\\Favourites.json";
            FavJsonData = new Dictionary<string, string>
        {
            { Path.GetFileName(songPlayingPath), songPlayingPath },
        };

            // Call the function to add data to the JSON file
            CallFunctions callFunctions = new CallFunctions(webView);
            callFunctions.AddDataToJsonFile(filePath, FavJsonData);

            MessageBox.Show($"{Path.GetFileName(songPlayingPath)} has been added to favourites.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void FavouriteButtonClick(object sender, RoutedEventArgs e)
        {
            FavDialog favDialog = new FavDialog();
            favDialog.ShowDialog();
        }

        private void YoutubeMusicButtonClick(object sender, RoutedEventArgs e)
        {
            CallFunctions callFunctions = new CallFunctions(webView);
            try
            {
                if (!ytMusicOpened)
                {
                    callFunctions.InitializeWebView();
                    callFunctions.LoadWebPage("https://music.youtube.com/");
                    ytMusicGrid.Visibility = Visibility.Visible;
                    ytMusicOpened = true;
                }
                else
                {
                    callFunctions.InitializeWebView();
                    webView.Source = new Uri("about:blank");
                    ytMusicGrid.Visibility = Visibility.Hidden;
                    ytMusicOpened = false;
                }
            }catch (Exception ew)
            {
                throw ew;
            }
        }

        private void SettingButtonClick(object sender, RoutedEventArgs e)
        {
            Settings setting = new Settings();
            setting.ShowDialog();
        }

        private void MaximizeButtonClick(object sender, RoutedEventArgs e)
        {
            if (!maximized)
            {
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                WindowState = WindowState.Maximized;
                maximized = true;
            }
            else
            {
                WindowState = WindowState.Normal;
                maximized = false;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == Window.WindowStateProperty)
            {
                if (!maximized)
                {
                    MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                    MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                    WindowState = WindowState.Maximized;
                    maximized = true;
                }
                else
                {
                    WindowState = WindowState.Normal;
                    maximized = false;
                }
            }
        }
    }
}
