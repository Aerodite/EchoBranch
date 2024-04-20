using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using Avalonia.Input;

namespace EchoBranch
{
    public partial class SpotifyView : UserControl
    {

        public SpotifyView()
        {
            InitializeComponent();
            SpotifyApiLink.Tapped += Hyperlink_Click;
        }

        private void Hyperlink_Click(object? sender, TappedEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://developer.spotify.com/documentation/web-api/",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}