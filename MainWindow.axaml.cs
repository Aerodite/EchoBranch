using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NAudio.Wave;
using Avalonia.Input;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using NLog;

namespace EchoBranch {
    public partial class MainWindow : Window {
        private IWavePlayer? _waveOutDevice;
        private AudioFileReader? _audioFileReader;
        public MainWindow(IWavePlayer? waveOutDevice, AudioFileReader? audioFileReader) {
            _waveOutDevice = waveOutDevice;
            _audioFileReader = audioFileReader;
            InitializeComponent();
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

        private void DisposeWave() {
            if (_waveOutDevice is { PlaybackState: PlaybackState.Playing }) {
                _waveOutDevice.Stop();
            }

            _waveOutDevice?.Dispose();
            _waveOutDevice = null;

            _audioFileReader?.Dispose();
            _audioFileReader = null;
        }
    }
}