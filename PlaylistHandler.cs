using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EchoBranch
{
    public static class PlaylistHandler
    {
        private static string PlaylistsDirectory => Path.Combine(AppData.GetAppDataPath(), "Playlists");

        public static void CreatePlaylist(string name)
        {
            var playlistDirectory = Path.Combine(PlaylistsDirectory, name);
            if (!Directory.Exists(playlistDirectory))
            {
                Directory.CreateDirectory(playlistDirectory);
            }
        }

        public static void DeletePlaylist(string name)
        {
            var playlistDirectory = Path.Combine(PlaylistsDirectory, name);
            if (Directory.Exists(playlistDirectory))
            {
                Directory.Delete(playlistDirectory, true);
            }
        }

        public static void RenamePlaylist(string oldName, string newName)
        {
            var oldPlaylistDirectory = Path.Combine(PlaylistsDirectory, oldName);
            var newPlaylistDirectory = Path.Combine(PlaylistsDirectory, newName);
            if (Directory.Exists(oldPlaylistDirectory))
            {
                Directory.Move(oldPlaylistDirectory, newPlaylistDirectory);
            }
        }

        public static List<string> GetPlaylists()
        {
            if (!Directory.Exists(PlaylistsDirectory))
            {
                Directory.CreateDirectory(PlaylistsDirectory);
            }

            var directories = Directory.GetDirectories(PlaylistsDirectory);
            return directories.Select(directory => new DirectoryInfo(directory)).Select(directoryInfo => directoryInfo.Name).ToList();
        }
    }
}