using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NAudio.Wave;

namespace EchoBranch {
    public partial class MainWindow : Window {
        private IWavePlayer? _waveOutDevice;
        private AudioFileReader? _audioFileReader;

        public MainWindow(IWavePlayer? waveOutDevice, AudioFileReader? audioFileReader) {
            _waveOutDevice = waveOutDevice;
            _audioFileReader = audioFileReader;
            InitializeComponent();
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