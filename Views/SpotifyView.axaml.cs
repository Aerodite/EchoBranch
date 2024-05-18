﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Runtime.InteropServices;

namespace EchoBranch.Views
{
    public partial class SpotifyView : UserControl
    {
        private const string ClientId = "3d31767e98fc4291b6c80eab03a82d53";
        //no clue if the client secret works on other computers, testing is needed :(
        private static readonly string ClientSecret = Environment.GetEnvironmentVariable("CLIENTSECRET") ?? throw new InvalidOperationException("Client Secret not found in environment variables");
        private const string RedirectUri = "http://localhost:9090";
        public event Action? LayoutChanged;

        public SpotifyView()
        {
            InitializeComponent();
            this.FindControl<Button>("SpotifyApiLink")!.Click += (sender, e) => AuthorizeSpotify();

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var filePath = Path.Combine(appDataPath, "EchoBranch", "data", "spotifyToken.json");

            // check if the spotifyToken.json file exists
            if (File.Exists(filePath))
            {
                LoadNewLayout();
            }
            RefreshTokenOnStartup();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void RefreshTokenOnStartup()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                var filePath = Path.Combine(appDataPath, "EchoBranch", "data", "spotifyToken.json");

                if (File.Exists(filePath))
                {
                    var token = LoadToken();

                    if (token.expiration_time <= DateTime.Now)
                    {
                        await RefreshAccessToken(token.refresh_token);
                    }

                    LoadNewLayout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                LogFileHandler.CreateLogFile();
                LogFileHandler.WriteLog($"Error refreshing Spotify token: {ex.Message}");
            }
        }

private async void LoadNewLayout()
{
    var playlists = await FetchPlaylists();

    if (playlists == null || !playlists.Any())
    {
        Console.WriteLine("No playlists found.");
        return;
    }

    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };

    using var client = new HttpClient();

    foreach (var playlist in playlists)
    {
        if (playlist == null)
        {
            Console.WriteLine("Playlist is null.");
            continue;
        }

        var coverArtUrl = playlist.coverArtUrl;
        if (coverArtUrl == null)
        {
            Console.WriteLine("No cover art found for this playlist item.");
            continue;
        }

        await using var imageStream = await client.GetStreamAsync(coverArtUrl);
        var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        var bitmap = new Avalonia.Media.Imaging.Bitmap(memoryStream);

        var lightness = CalculateImageLightness(bitmap);
        Debug.WriteLine($"Calculated lightness: {lightness}");

        var textColor = lightness > 0.4 ? Colors.Black : Colors.White;
        Debug.WriteLine($"Chosen text color: {(textColor == Colors.Black ? "Black" : "White")}");
        const int playlistBoxWidth = 70; // feels good for now but is here for now for future user customization
        const float coverArtAspectRatio = 1.13636364f;
        const float playlistBoxHeight = (playlistBoxWidth / coverArtAspectRatio);
        // ^^ these two wont be customizable though because of the specific width and height spotify uses with this exact aspect ratio.

        var textBlock = new TextBlock { Text = playlist.name, Width = playlistBoxWidth, Height = playlistBoxHeight, TextWrapping = TextWrapping.WrapWithOverflow, Foreground = new SolidColorBrush(textColor) };
        FontSize = 16;
        FontWeight = FontWeight.Bold;
        var border = new Border {
            Child = textBlock,
            BorderBrush = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(playlistBoxWidth, playlistBoxHeight),
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            Margin = new Thickness(20, 0),
            CornerRadius = new CornerRadius(10),
            Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill },
            VerticalAlignment = VerticalAlignment.Bottom // Align the text to the bottom
        };
        border.Classes.Add("hoverable-border");

        border.PointerPressed += (sender, e) => OpenPlaylist(playlist.id);

        stackPanel.Children.Add(border);
    }

    var scrollViewer = new ScrollViewer
    {
        Content = stackPanel,
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
    };

    this.Content = scrollViewer;

    LayoutChanged?.Invoke();
}

private double CalculateImageLightness(Avalonia.Media.Imaging.Bitmap bitmap) {

    var pixelSize = new Avalonia.PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
    var renderTargetBitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize);

    using (var ctx = renderTargetBitmap.CreateDrawingContext()) {
        ctx.DrawImage(bitmap, new Rect(bitmap.Size), new Rect(bitmap.Size));
    }

    var pixelData = new byte[bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4];
    var buffer = Marshal.AllocHGlobal(pixelData.Length);
    try {
        const int bytesPerPixel = 4;
        const int alignment = 4;
        var stride = (bitmap.PixelSize.Width * bytesPerPixel + (alignment - 1)) / alignment * alignment;

        renderTargetBitmap.CopyPixels(new Avalonia.PixelRect(pixelSize), buffer, pixelData.Length, stride);

        Marshal.Copy(buffer, pixelData, 0, pixelData.Length);

        var lightness = 0.0;
        var pixelCount = 0;

        for (var i = 0; i < pixelData.Length; i += 4)
        {
            var b = pixelData[i];
            var g = pixelData[i + 1];
            var r = pixelData[i + 2];

            lightness += (r + g + b) / 3.0 / 255.0;
            pixelCount++;
        }

        return lightness / pixelCount;
    }
    finally
    {
        Marshal.FreeHGlobal(buffer);
    }
}


private void OpenPlaylist(string? playlistId)
{
    if (playlistId == null)
    {
        Console.WriteLine("Playlist id is null.");
        return;
    }

    var spotifyPlaylistView = new SpotifyPlaylistAxaml();
    spotifyPlaylistView.LoadPlaylist(playlistId);

    MainWindow.Instance?.SwitchToPlaylistView(spotifyPlaylistView);
}




        public void UpdateLayout()
        {
            LoadNewLayout();
        }

        public class Image
        {
            public string? url { get; set; }
        }

        private async Task<List<PlaylistItem>> FetchPlaylists()
        {
            var token = LoadToken();
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.access_token);
            var response = await httpClient.GetAsync("https://api.spotify.com/v1/me/playlists");
            var responseString = await response.Content.ReadAsStringAsync();

            var playlistsResponse = JsonSerializer.Deserialize<PlaylistsResponse>(responseString);

            if (playlistsResponse.items != null)
            {
                foreach (var item in playlistsResponse.items)
                {
                    if (item.images != null && item.images.Count > 0)
                    {
                        Console.WriteLine(item.images[0].url);
                    }
                    else
                    {
                        Console.WriteLine("No images found for this playlist item.");
                    }
                }
            }

            return playlistsResponse.items?.Select(i => new PlaylistItem { name = i.name, coverArtUrl = i.images?[0].url, id = i.id }).ToList() ?? new List<PlaylistItem>();
        }

        public static SpotifyTokenResponse LoadToken()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var filePath = Path.Combine(appDataPath, "EchoBranch", "data", "spotifyToken.json");
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SpotifyTokenResponse>(jsonString);
        }

        public class PlaylistsResponse
        {
            public List<PlaylistItem> items { get; set; }
        }

        public class PlaylistItem
        {
            public string? name { get; set; }
            public string? coverArtUrl { get; set; }
            public string? id { get; set; }

            public List<Image>? images { get; set; } // images can be null
        }

        private static async void AuthorizeSpotify()
        {
            const string scopes = "user-read-playback-state user-modify-playback-state user-read-currently-playing playlist-read-private playlist-read-collaborative user-library-read";
            var authUrl = $"https://accounts.spotify.com/authorize?client_id={ClientId}&response_type=code&redirect_uri={WebUtility.UrlEncode(RedirectUri)}&scope={WebUtility.UrlEncode(scopes)}";

            using var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri + "/");
            listener.Start();

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            var context = await listener.GetContextAsync();
            var response = context.Response;
            const string responseString = "<html><body>You can close the window now, or whatever really, up to you.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer);
            responseOutput.Close();

            var code = context.Request.QueryString.Get("code");
            if (code != null) await HandleSpotifyResponse(code);

            listener.Stop();

            LogFileHandler.CreateLogFile();
            LogFileHandler.WriteLog($"Spotify authorization attempted");
        }

        private static async Task HandleSpotifyResponse(string code)
        {
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", RedirectUri }
            });

            var response = await httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseString);

            SaveToken(tokenResponse);

            LogFileHandler.CreateLogFile();
            LogFileHandler.WriteLog($"Spotify authorization complete and successful!");

            if (tokenResponse.expiration_time <= DateTime.Now)
            {
                await RefreshAccessToken(tokenResponse.refresh_token);
            }
        }

        private static void SaveToken(SpotifyTokenResponse tokenResponse)
        {
            tokenResponse.expiration_time = DateTime.Now.AddSeconds(tokenResponse.expires_in);

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var directoryPath = Path.Combine(appDataPath, "EchoBranch", "data");
            Directory.CreateDirectory(directoryPath);

            var jsonString = JsonSerializer.Serialize(tokenResponse);

            var filePath = Path.Combine(directoryPath, "spotifyToken.json");
            File.WriteAllText(filePath, jsonString);

            LogFileHandler.CreateLogFile();
            LogFileHandler.WriteLog($"Spotify token saved to {filePath}");
        }

        private static async Task RefreshAccessToken(string refreshToken)
        {
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });

            var response = await httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseString);

            SaveToken(tokenResponse);
        }
        public class SpotifyTokenResponse
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string scope { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public DateTime expiration_time { get; set; }
        }
    }
}