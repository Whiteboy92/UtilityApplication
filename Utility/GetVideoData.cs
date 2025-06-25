using System.Diagnostics;
using System.IO;
using UtilityApplication.Settings;

namespace UtilityApplication.Utility;

public class GetVideoData
{
    public static TimeSpan GetVideoDuration(string videoPath)
    {
        var ffmpegPath = Path.Combine(DownloadConfig.FfmpegLocation, "ffprobe.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (double.TryParse(output.Trim(), out double seconds))
            {
                return TimeSpan.FromSeconds(seconds);
            }
        }

        throw new Exception("Could not retrieve video duration.");
    }

}