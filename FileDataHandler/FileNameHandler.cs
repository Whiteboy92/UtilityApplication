using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace UtilityApplication.FileDataHandler;

public static partial class FileNameFormatter
{
    private static readonly string[] UnwantedPatterns =
    [
        @"\(\s*Official\s+Music\s+Video\s*\)",
        @"\(\s*Official\s+Lyric\s+Video\s*\)",
        @"\[COPYRIGHT\s+FREE\s+Music\]",
        @"\(\s*Lyric\s+Video\s*\)",
        @"\(\s*Official\s+Video\s*\)",
        @"\(\s*Official\s+Audio\s*\)",
        @"\[\s*Official\s+Visualizer\s*\]",
        @"\(\s*Official\s+Visualizer\s*\)",
        @"\(\s*Visualizer\s*\)",
        @"\(\s*Audio\s*\)",
        @"\(\s*HD\s*\)",
    ];

    /// <summary>
    /// Main entry point for fixing MP3 file names in the specified directory.
    /// Applies cleaning capitalization and formatting logic.
    /// </summary>
    /// <param name="directory">The directory containing MP3 files.</param>
    /// <param name="progress">Display of progress</param>
    public static async Task FixDownloadedFileNamesAsync(string directory, IProgress<int>? progress = null)
    {
        var files = Directory.GetFiles(directory, "*.mp3");

        int count = 0;
        object sync = new();

        await Parallel.ForEachAsync(files, async (filePath, _) =>
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var directoryPath = Path.GetDirectoryName(filePath) ?? "";

                var rawName = GetFileNameWithoutExtension(fileName);
                var cleanedAndCapitalizedName = CorrectFileNameFormat(rawName);
                var rearrangedFileName = RearrangeFileName($"{cleanedAndCapitalizedName}.mp3");

                var newFilePath = Path.Combine(directoryPath, rearrangedFileName);
                await Task.Run(() => RenameFile(filePath, newFilePath, fileName), _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{filePath}': {ex.Message}");
            }
            finally
            {
                lock (sync)
                {
                    count++;
                    progress?.Report(count);
                }
            }
        });
    }

    /// <summary>
    /// Removes known noise patterns from file names (e.g. "(Official Video)").
    /// </summary>
    /// <param name="fileName">The raw file name string.</param>
    /// <returns>The cleaned file name.</returns>
    private static string CleanFileName(string fileName)
    {
        foreach (var pattern in UnwantedPatterns)
        {
            fileName = Regex.Replace(fileName, pattern, "", RegexOptions.IgnoreCase).Trim();
        }

        fileName = Regex.Replace(fileName, @"\s{2,}", " ");
        return fileName.Trim(' ', '-');
    }

    /// <summary>
    /// Capitalizes the first letter of each word using the current culture.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>A title-cased version of the string.</returns>
    private static string CapitalizeWords(string input)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(input.ToLower());
    }

    /// <summary>
    /// Cleans and capitalizes the file name.
    /// </summary>
    /// <param name="fileName">Raw file name without extension.</param>
    /// <returns>Formatted file name.</returns>
    private static string CorrectFileNameFormat(string fileName)
    {
        var cleaned = CleanFileName(fileName);
        return CapitalizeWords(cleaned);
    }

    /// <summary>
    /// Rearranges the file name format from "Artist - Song" to "Song - Artist".
    /// </summary>
    /// <param name="fileName">Formatted file name with extension.</param>
    /// <returns>Rearranged file name if possible, original otherwise.</returns>
    private static string RearrangeFileName(string fileName)
    {
        var fileExtension = Path.GetExtension(fileName);
        var nameOnly = Path.GetFileNameWithoutExtension(fileName);
        nameOnly = CleanFileName(nameOnly);

        var parts = nameOnly.Split('-');
    
        string title, artist;

        if (parts.Length == 2)
        {
            artist = parts[0].Trim();
            title = parts[1].Trim();
        }
        else
        {
            title = nameOnly.Trim();
            artist = "Unknown Artist";
        }

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        title = textInfo.ToTitleCase(title.ToLower());
        artist = textInfo.ToTitleCase(artist.ToLower());

        return $"{title} - {artist}{fileExtension}";
    }

    /// <summary>
    /// Gets the file name without its extension and removes any leading track number.
    /// </summary>
    /// <param name="fileName">The full file name.</param>
    /// <returns>Cleaned base file name.</returns>
    private static string GetFileNameWithoutExtension(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        return RemoveLeadingTrackNumber(name);
    }

    /// <summary>
    /// Removes leading digits and separators from a file name (e.g., "01 - Song.mp3" -> "Song").
    /// </summary>
    /// <param name="fileName">The base file name.</param>
    /// <returns>File name without a leading track number.</returns>
    private static string RemoveLeadingTrackNumber(string fileName)
    {
        return MyRegex().Replace(fileName, "");
    }

    /// <summary>
    /// Attempts to rename a file if the new name is available. Logs the result.
    /// </summary>
    /// <param name="oldPath">Current file path.</param>
    /// <param name="newPath">Proposed a new file path.</param>
    /// <param name="originalFileName">Original file name (for logging).</param>
    private static void RenameFile(string oldPath, string newPath, string originalFileName)
    {
        bool renamed = false;

        if (IsFileNameAvailable(newPath))
        {
            File.Move(oldPath, newPath);
            renamed = true;
        }

        LogRenameResult(originalFileName, newPath, renamed);
    }

    /// <summary>
    /// Checks if the proposed file path is available (not already used).
    /// </summary>
    /// <param name="newFilePath">The full file path to check.</param>
    /// <returns>True if a file does not exist.</returns>
    private static bool IsFileNameAvailable(string newFilePath)
    {
        return !File.Exists(newFilePath);
    }

    /// <summary>
    /// Logs the result of a rename attempt to the console.
    /// </summary>
    /// <param name="originalFileName">Original file name.</param>
    /// <param name="newFilePath">Target path of the renamed file.</param>
    /// <param name="renamed">Whether the rename was successful.</param>
    private static void LogRenameResult(string originalFileName, string newFilePath, bool renamed)
    {
        Console.WriteLine(renamed
            ? $"Renamed '{originalFileName}' -> '{Path.GetFileName(newFilePath)}'"
            : $"Skipping rename for '{originalFileName}' because target filename already exists.");
    }

    /// <summary>
    /// Regex that matches a leading number followed by space, dash, underscore or dot.
    /// </summary>
    [GeneratedRegex(@"^\d+[\s_\-\.]+")]
    private static partial Regex MyRegex();
}
