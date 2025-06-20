using System.IO;
using File = TagLib.File;

namespace UtilityApplication.FileDataHandler;

public class FileMetadataHandler
{
    public async Task AddTagsToMp3FilesAsync(string folderPath, IProgress<int>? progress = null)
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("Target folder does not exist.");
            return;
        }

        string[] mp3Files = Directory.GetFiles(folderPath, "*.mp3");

        if (mp3Files.Length == 0)
        {
            Console.WriteLine("No MP3 files found in the folder.");
            return;
        }

        int count = 0;
        object sync = new();

        await Task.WhenAll(mp3Files.Select((filePath, index) => Task.Run(() =>
        {
            try
            {
                var file = File.Create(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string[] parts = fileName.Split('-', 2);

                if (parts.Length != 2)
                {
                    Console.WriteLine($"Invalid file name format: {Path.GetFileName(filePath)}");
                    return;
                }

                string title = parts[0].Trim();
                string artist = parts[1].Trim();

                file.Tag.Title = title;
                file.Tag.Performers = [artist];
                file.Tag.Album = "My Music";
                file.Tag.Genres = ["Rock/Metal/House/Bass"];
                file.Tag.Track = (uint)(index + 1);
                file.Save();

                Console.WriteLine($"Tagged: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to tag: {Path.GetFileName(filePath)} - {ex.Message}");
            }
            finally
            {
                lock (sync)
                {
                    count++;
                    progress?.Report(count);
                }
            }
        })));

        Console.WriteLine("All MP3 tags processed.");
    }
}