using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UtilityApplication.Settings;

namespace UtilityApplication.Services;

public class YtDlpService
{
    private static readonly Regex SpeedRegex = new(@"at ([\d\.]+[KMG]i?B/s)", RegexOptions.IgnoreCase);
    private static readonly Regex OutputFileRegex = new(@"\[ExtractAudio\] Destination: (.+\.mp3)", RegexOptions.IgnoreCase);

    public async Task<bool> DownloadAudioAsync(string playlistUrl, int maxVideos, IProgress<int>? progress = null)
    {
        DownloadConfig.EnsureOutputDirectoryExists();

        string arguments = DownloadConfig.BuildYtDlpArguments(playlistUrl, maxVideos);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = DownloadConfig.YtDlpPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        Console.WriteLine($"Starting yt-dlp with arguments: {arguments}");

        var downloadedFiles = new HashSet<string>();
        var sw = Stopwatch.StartNew();

        try
        {
            process.Start();
            
            Task stdOutTask = ReadOutputAsync(process.StandardOutput, downloadedFiles, progress);
            Task stdErrTask = ReadErrorAsync(process.StandardError);

            await Task.WhenAll(stdOutTask, stdErrTask, process.WaitForExitAsync());

            sw.Stop();
            Console.WriteLine($"yt-dlp exited with code {process.ExitCode} after {sw.Elapsed}");

            return downloadedFiles.Count > 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"yt-dlp failed: {ex}");
            return false;
        }
    }

    private async Task ReadOutputAsync(StreamReader output, HashSet<string> fileSet, IProgress<int>? progress)
    {
        while (!output.EndOfStream)
        {
            var line = await output.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            Console.WriteLine($"yt-dlp stdout: {line}");

            var match = OutputFileRegex.Match(line);
            if (match.Success)
            {
                var filePath = match.Groups[1].Value.Trim('"');
                if (fileSet.Add(filePath))
                {
                    Console.WriteLine($"New MP3 file detected: {Path.GetFileName(filePath)}");
                    progress?.Report(fileSet.Count);
                }
            }

            var speedMatch = SpeedRegex.Match(line);
            if (speedMatch.Success)
            {
                Console.WriteLine($"Speed: {speedMatch.Groups[1].Value}");
            }
        }
    }

    private async Task ReadErrorAsync(StreamReader error)
    {
        while (!error.EndOfStream)
        {
            var line = await error.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                await Console.Error.WriteLineAsync($"yt-dlp stderr: {line}");
            }
        }
    }
}
