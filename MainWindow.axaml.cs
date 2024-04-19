using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NAudio.Wave;
using Avalonia.Input;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace EchoBranch {
    public partial class MainWindow : Window {
        private IWavePlayer? _waveOutDevice;
        private AudioFileReader? _audioFileReader;
        public MainWindow(IWavePlayer? waveOutDevice, AudioFileReader? audioFileReader) {
            _waveOutDevice = waveOutDevice;
            _audioFileReader = audioFileReader;
            InitializeComponent();
            this.AttachedToVisualTree += OnAttachedToVisualTree;

            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = -1;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            this.ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = 32;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            this.Background = Brushes.Black;
            this.PointerPressed += (sender, e) => BeginMoveDrag(e);
            this.BorderBrush = Brushes.Black;
            DragDrop.SetAllowDrop(this, true);
        }
        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) {
            DragDrop.SetAllowDrop(MainGrid, true);
            MainGrid.AddHandler(DragDrop.DragOverEvent, DragOver, RoutingStrategies.Tunnel);
            MainGrid.AddHandler(DragDrop.DropEvent, Drop, RoutingStrategies.Tunnel);
        }

        /*
         *
          TODO: Make it actually do shit with the dragged in audio files
         *
         */

        private static void DragOver(object? sender, DragEventArgs e) {
            if (e.Data.Contains(DataFormats.FileNames)) {
                e.DragEffects = DragDropEffects.Copy;
                Console.WriteLine("FileNames detected in drag data.");
            } else {
                e.DragEffects = DragDropEffects.None;
                Console.WriteLine("No FileNames detected in drag data.");
            }
            e.Handled = true;
        }
        private static void Drop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains(DataFormats.FileNames)) return;
            var filenames = e.Data.GetFileNames()?.ToArray();
            if (filenames == null) return;
            foreach (var filename in filenames) {
                // Handle the dropped files here
                Console.WriteLine(filename);
            }
        }

        public class Playlist(List<string> audioFiles, string name)
        {
            public string Name { get; private set; } = name;
            private List<string> AudioFiles { get; set; } = audioFiles;

            public Playlist(string name) : this([], name)
            {
            }

            public void AddAudioFile(string filePath)
            {
                AudioFiles.Add(filePath);
            }

            public void Rename(string newName)
            {
                Name = newName;
                // Here we can also rename the corresponding folder in the AppData path
            }
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