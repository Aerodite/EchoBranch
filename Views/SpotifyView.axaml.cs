using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace EchoBranch.Views
{
    public partial class SpotifyView : UserControl
    {
        private const string ClientId = "3d31767e98fc4291b6c80eab03a82d53";
        //no clue if the client secret works on other computers, testing is needed :(
        //this needs to grab from github secrets rather than IDE env variables.
        private static readonly string ClientSecret = Environment.GetEnvironmentVariable("CLIENTSECRET") ?? throw new InvalidOperationException("Client Secret not found in environment variables");
        private const string RedirectUri = "http://localhost:9090";
        public event Action? LayoutChanged;

        protected internal SpotifyView()
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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LoadNewLayout()
        {
            // Fetch the user's playlists from Spotify's Web API
            var playlists = await FetchPlaylists();

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            foreach (var textBlock in playlists.Select(playlist => new TextBlock { Text = playlist }))
            {
                stackPanel.Children.Add(textBlock);
            }

            this.Content = stackPanel;

            LayoutChanged?.Invoke();
        }

        private async Task<List<string?>> FetchPlaylists()
        {
            var token = LoadToken();
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.access_token);
            var response = await httpClient.GetAsync("https://api.spotify.com/v1/me/playlists");
            var responseString = await response.Content.ReadAsStringAsync();

            var playlistsResponse = JsonSerializer.Deserialize<PlaylistsResponse>(responseString);

            // Keep this null-safe please. Possible wacky behavior if for whatever reason end-user has no playlists
            // Best way to do this in the future imo would be to have a settings menu to ask what they want to show from spotify
            return playlistsResponse?.Items?.Select(i => i.Name).ToList() ?? [];
        }

        private static SpotifyTokenResponse? LoadToken()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var filePath = Path.Combine(appDataPath, "EchoBranch", "data", "spotifyToken.json");
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SpotifyTokenResponse>(jsonString);
        }

        public class PlaylistsResponse(List<PlaylistItem>? items)
        {
            public List<PlaylistItem>? Items { get; init; } = items;
        }

        public abstract class PlaylistItem
        {
            protected PlaylistItem(string? name)
            {
                Name = name;
            }

            public string? Name { get; }
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

            SaveToken(tokenResponse ?? throw new InvalidOperationException("Unable to save the token."));

            LogFileHandler.CreateLogFile();
            LogFileHandler.WriteLog($"Spotify authorization complete and successful!");

            //handle token expiration
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
            // this doesnt work yet for whatever reason, investigation needed.
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

            SaveToken(tokenResponse ?? throw new InvalidOperationException("Unable to refresh the Spotify token."));
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