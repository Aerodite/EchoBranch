using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Platform.Storage;

namespace EchoBranch
{
    public static class DropEvent
    {
        public static void HandleDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;

            Debug.WriteLine(e.Data.Contains(DataFormats.FileNames)
                ? "FileNames detected in drag data."
                : "No FileNames detected in drag data.");
        }

        public static async void HandleDrop(object? sender, DragEventArgs e)
        {
            Console.WriteLine("Drop method called");

            if (!e.Data.Contains(DataFormats.FileNames)) return;
            var files = (e.Data.GetFileNames()).ToArray();

            foreach (var file in files)
            {
                // Check if the file is a valid audio file
                if (!IsValidAudioFile(file)) continue;
                // Get the name of the file
                var fileName = Path.GetFileName((string?)file);

                // Create the destination path
                if (fileName == null) continue;
                var destinationPath = Path.Combine(AppData.GetAppDataPath(), fileName);

                // Copy the file to the AppData directory
                File.Copy(file, destinationPath, true);
            }
        }

        private static bool IsValidAudioFile(string filename)
        {
            var audioFileExtensions = new List<string> { ".mp3", ".wav", ".flac" };
            var extension = Path.GetExtension(filename);
            return audioFileExtensions.Contains(extension);
        }
    }
}