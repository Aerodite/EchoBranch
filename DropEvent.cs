using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace EchoBranch
{
    public static class DropEvent
    {
        public static void HandleDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;

            Console.WriteLine(e.Data.Contains(DataFormats.FileNames)
                ? "FileNames detected in drag data."
                : "No FileNames detected in drag data.");
        }

        public static async void HandleDrop(object? sender, DragEventArgs e)
        {
            Console.WriteLine("Drop method called");

            var files = e.Data.GetFileNames().ToArray();

            Console.WriteLine("Files dropped:");
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }

            // Create the Playlists subfolder within the AppData directory
            var playlistsPath = Path.Combine(AppData.GetAppDataPath(), "playlists");
            if (!Directory.Exists(playlistsPath))
            {
                Directory.CreateDirectory(playlistsPath);
                LogFileHandler.WriteLog("Playlist folder created");
            }

            foreach (var file in files)
            {
                // Check if the file is a valid audio file
                if (!IsValidAudioFile(file)) continue;

                // Get the name of the file
                var fileName = Path.GetFileName((string?)file);
                if (fileName == null) continue;

                // Create the destination path within the Playlists subfolder
                var destinationPath = Path.Combine(playlistsPath, fileName);

                // Check if the file already exists at the destination
                if (File.Exists(destinationPath)) continue;

                // Copy the file to the Playlists subfolder within the AppData directory
                File.Copy(file, destinationPath, true);
            }
        }

        private static bool IsValidAudioFile(string filename)
        {
            Console.WriteLine($"Checking if file is valid audio file: {filename}");
            //TODO: make this a more comprehensive list
            var audioFileExtensions = new List<string> {".mp3", ".wav", ".flac", ".ogg", ".aiff", ".wma", ".aac", ".mp4"};
            var extension = Path.GetExtension(filename);
            LogFileHandler.CreateLogFile();
            if (audioFileExtensions.Contains(extension))
            {
                Console.WriteLine($"File is a valid audio file: {filename}");
                LogFileHandler.WriteLog($"{filename} was found to be an audio file.");
            }
            else
            {
                Console.WriteLine($"File is not a valid audio file: {filename}");
                LogFileHandler.WriteLog($"{filename} was found to not be an audio file.");

            }
            return audioFileExtensions.Contains(extension);
        }
    }
}