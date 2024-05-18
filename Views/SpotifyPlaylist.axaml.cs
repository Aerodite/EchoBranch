using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;

namespace EchoBranch.Views;

public partial class SpotifyPlaylistAxaml : UserControl
{
    public SpotifyPlaylistAxaml()
    {
        InitializeComponent();
        this.DataContext = this;
        LoadPlaylistLayout("playlistId");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public class Playlist
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tracks")]
        public Tracks? Tracks { get; set; }
    }

    public class Tracks
    {
        [JsonPropertyName("items")]
        public List<TrackItem>? Items { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }
    }

    public class TrackItem
    {
        [JsonPropertyName("track")]
        public Track? Track { get; set; }
    }

    public class Track
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist>? Artists { get; set; }

        [JsonPropertyName("album")]
        public Album? Album { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class Artist
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class Album
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public static SpotifyView.SpotifyTokenResponse LoadToken()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var filePath = Path.Combine(appDataPath, "EchoBranch", "data", "spotifyToken.json");
        var jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SpotifyView.SpotifyTokenResponse>(jsonString);
    }

    private async Task<Playlist?> FetchPlaylist(string playlistId)
    {
        var token = LoadToken();
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.access_token);
        var response = await httpClient.GetAsync($"https://api.spotify.com/v1/playlists/{Uri.EscapeDataString(playlistId)}");
        var responseString = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"JSON response: {responseString}");

        try
        {
            var playlist = JsonSerializer.Deserialize<Playlist>(responseString);
            return playlist;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public class Error
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        LoadPlaylist("playlistId");
    }

    public string? ArtistName { get; set; }
    public string? SongName { get; set; }
    public string? AlbumName { get; set; }

    public async void LoadPlaylist(string playlistId)
    {
        var playlist = await FetchPlaylist(playlistId);

        //grab the names, albums, artists, and total number of songs in the playlist and print them
        if (playlist != null)
        {
            Console.WriteLine($"Playlist name: {playlist.Name}");
            if (playlist.Tracks != null)
            {
                Console.WriteLine($"Number of songs: {playlist.Tracks.Total}");
                foreach (var song in playlist.Tracks.Items)
                {
                    Console.WriteLine(
                        $"Song: {song.Track.Name}, Artist: {song.Track.Artists[0].Name}, Album: {song.Track.Album.Name}");
                }
            }
        }
    }

    public async void LoadPlaylistLayout(string playlistId)
    {
        Playlist? playlist = null;
        try
        {
            for (int i = 0; i < 5; i++)
            {
                playlist = await FetchPlaylist(playlistId);
                foreach (var song in playlist.Tracks.Items)
                {
                    Console.WriteLine("Playlist link: " + song.Track.Id);
                }
                if (playlist != null && playlist.Tracks.Total != null)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Tracks is null. Retrying...");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }

            if (playlist == null || playlist.Tracks.Total == null)
            {
                throw new Exception("Failed to fetch playlist or tracks after 5 attempts.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return;
        }

        var stackPanel = new StackPanel
            { Orientation = Orientation.Horizontal, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        if (playlist?.Tracks?.Items != null)
        {
            foreach (var song in playlist.Tracks.Items)
            {
                if (song == null)
                {
                    Console.WriteLine("Song is null.");
                    continue;
                }

                //testing this out again not final
                const int songBoxWidth = 70;
                const float coverArtAspectRatio = 1.13636364f;
                const float songBoxHeight = (songBoxWidth / coverArtAspectRatio);

                var textBlock = new TextBlock
                {
                    Text = song.Track.Name, Width = songBoxWidth, Height = songBoxHeight,
                    TextWrapping = TextWrapping.WrapWithOverflow, Foreground = new SolidColorBrush(Colors.White)
                };
                FontSize = 16;
                FontWeight = FontWeight.Bold;
                var border = new Border
                {
                    Child = textBlock,
                    BorderBrush = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(songBoxWidth, songBoxHeight),
                    RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    Margin = new Thickness(20, 0),
                    CornerRadius = new CornerRadius(10),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                border.Classes.Add("hoverable-border");

                stackPanel.Children.Add(border);
            }
        }

            this.Content = stackPanel;
    }


    public void DisplaySongs(Playlist playlist)
    {
        var playlistStackPanel = new StackPanel { Orientation = Orientation.Vertical };

        if (playlist != null && playlist.Tracks != null)
        {
            foreach (var song in playlist.Tracks.Items)
            {
                var songStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var textBlock1 = new TextBlock { Text = song.Track.Name, Foreground = new SolidColorBrush(Colors.White) };
                var textBlock2 = new TextBlock { Text = song.Track.Artists[0].Name, Foreground = new SolidColorBrush(Colors.White) };
                var textBlock3 = new TextBlock { Text = song.Track.Album.Name, Foreground = new SolidColorBrush(Colors.White) };

                songStackPanel.Children.Add(textBlock1);
                songStackPanel.Children.Add(textBlock2);
                songStackPanel.Children.Add(textBlock3);

                playlistStackPanel.Children.Add(songStackPanel);
            }
        }

        this.Content = playlistStackPanel;
    }
}