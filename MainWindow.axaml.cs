using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NAudio.Wave;
using Avalonia.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using EchoBranch.Views;
using NLog;

namespace EchoBranch {
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        private Dictionary<string, UserControl> views = new Dictionary<string, UserControl>();
        private IWavePlayer? _waveOutDevice;
        private AudioFileReader? _audioFileReader;
        public MainWindow(IWavePlayer? waveOutDevice, AudioFileReader? audioFileReader) {
            Instance = this;
            _waveOutDevice = waveOutDevice;
            _audioFileReader = audioFileReader;
            InitializeComponent();
            /*
               I have no clue why the FUCK this has to be async to work
               when I tried using just SetupViews(); it would completely crash
               but oh well it works now.
            */
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(SetupViews);
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = -1;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = 32;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            Background = Brushes.Black;
            PointerPressed += (sender, e) =>
            {
                var point = e.GetPosition(this);
                const int titleBarHeight = 34; // Height of the title bar + an extra 2px of wiggle room
                if (point.Y <= titleBarHeight)
                {
                    BeginMoveDrag(e);
                }
            };
            BorderBrush = Brushes.Black;
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DropEvent, DropEvent.HandleDrop);
            AddHandler(DragDrop.DragOverEvent, DropEvent.HandleDragOver);
            Console.WriteLine("Dragged");
            AppData.CreateAppDataFolder();
            Console.WriteLine($"AppData Path: {AppData.GetAppDataPath()}");
            // Create a log file in the AppData directory
            LogFileHandler.CreateLogFile();
            LogFileHandler.WriteLog($"Log file initialized");
            var spotifyView = this.FindControl<SpotifyView>("SpotifyView");
            this.AddHandler(DragDrop.DropEvent, (sender, e) => DropEvent.HandleDrop(this, e));
        }
        private void SetupViews() {
            // This was the easiest way I could think of setting up the multiple views
            // If anyone has a better implementation please let me know.
            views["Home"] = new HomeView();
            views["Spotify"] = new Views.SpotifyView();
            views["Playlist"] = new PlaylistView();
            MainContent.Content = views["Home"];
        }
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private void PlayButton_Click() {
            // Make sure nothing is playing at runtime
            DisposeWave();

            /*
             * Temporary for now just to make sure the NAudio Library works
             * For now just using this as reference to see how to implement a dynamic player later.
             */
            _audioFileReader = new AudioFileReader(@"C:\Users\Anthony\Documents\GitHub\EchoBranch-stable\cmake-build-debug\data\song.mp3");
            _waveOutDevice = new WaveOutEvent();
            _waveOutDevice.Init(_audioFileReader);
            _waveOutDevice.Play();
        }

        public void SwitchToPlaylistView(UserControl spotifyPlaylistView)
        {
            views["Playlist"] = spotifyPlaylistView;
            SwitchView("Playlist");
        }

        private void DisposeWave() {
            if (_waveOutDevice is { PlaybackState: PlaybackState.Playing }) {
                _waveOutDevice.Stop();
            }

            _waveOutDevice?.Dispose();
            _waveOutDevice = null;

            _audioFileReader?.Dispose();
            _audioFileReader = null;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            MainContent = this.FindControl<ContentControl>("MainContent");
            SetupViews();
        }
        private void SwitchView(string viewName)
        {
            if (views.TryGetValue(viewName, out var value))
            {
                MainContent.Content = value;
            }
            else
            {
                Console.WriteLine($"View {viewName} not found");
                LogFileHandler.CreateLogFile();
                LogFileHandler.WriteLog($"View {viewName} not found");

            }
        }

        private void HomeButton_Click(object? sender, RoutedEventArgs e)
        {
            SwitchView("Home");
        }

        private void SpotifyButton_Click(object? sender, RoutedEventArgs e)
        {

            SwitchView("Spotify");
        }
    }
}