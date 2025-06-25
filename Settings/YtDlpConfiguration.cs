using System.IO;

namespace UtilityApplication.Settings;

/// <summary>
/// Holds configuration for the yt-dlp download process.
/// </summary>
public class DownloadConfig
{
    public static string OutputDirectory => @"C:\Users\marci\Desktop\Playlist";
    public static string FfmpegLocation => @"C:\ffmpeg-2024-12-23-git-6c9218d748-full_build\bin";
    private static string CookiePath => @"C:\Users\marci\Desktop\Playlist\cookies.txt";

    /// <summary>
    /// Ensures the output directory exists.
    /// </summary>
    public static void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }
    }

    /// <summary>
    /// Builds the full yt-dlp command-line arguments.
    /// </summary>
    public static string BuildYtDlpArguments(string videoUrl, int? maxDownloads = null, int? index = null)
    {
        string outputTemplate = index.HasValue
            ? $@"{OutputDirectory}\{index:00}_%(title).200s.%(ext)s"
            : $@"{OutputDirectory}\%(title).200s.%(ext)s";

        var args = 
            $"-f bestaudio " +
            $"--extract-audio " +
            $"--audio-format mp3 " +
            $"--audio-quality 0 " +
            $"--cookies \"{CookiePath}\" " +
            $"--ffmpeg-location \"{FfmpegLocation}\" " +
            $"--output \"{outputTemplate}\" ";

        if (maxDownloads.HasValue)
        {
            args += $"--max-downloads {maxDownloads.Value} ";
        }

        args += videoUrl;

        return args;
    }
}