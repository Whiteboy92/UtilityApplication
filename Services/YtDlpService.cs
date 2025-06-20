using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UtilityApplication.Settings;

namespace UtilityApplication.Services
{
    public class YtDlpService
    {
        /// <summary>
        /// Downloads the audio files using yt-dlp with configured options.
        /// Reports progress as the number of downloaded mp3 files in the output folder.
        /// Logs download speed and other info from yt-dlp stdout.
        /// </summary>
        public async Task<bool> DownloadAudioAsync(string playlistUrl, int maxVideos, IProgress<int>? progress = null)
        {
            DownloadConfig.EnsureOutputDirectoryExists();

            string arguments = DownloadConfig.BuildYtDlpArguments(playlistUrl, maxVideos);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            var sw = Stopwatch.StartNew();

            try
            {
                Console.WriteLine($"Starting yt-dlp process with arguments: {arguments}");
                process.Start();

                // Read stderr asynchronously (log errors)
                _ = Task.Run(async () =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        var errLine = await process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(errLine))
                            Console.Error.WriteLine($"yt-dlp stderr: {errLine}");
                    }
                });

                int lastCount = 0;

                // Regex to match speed info lines, e.g.:
                // [download]  15.2% of 3.00MiB at 123.45KiB/s ETA 00:21
                var speedRegex = new Regex(@"at ([\d\.]+[KMG]i?B/s)", RegexOptions.IgnoreCase);

                // Read stdout line by line
                _ = Task.Run(async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            // Log full output line
                            Console.WriteLine($"yt-dlp stdout: {line}");

                            // Try to extract speed info and log it separately
                            var speedMatch = speedRegex.Match(line);
                            if (speedMatch.Success)
                            {
                                Console.WriteLine($"Download speed: {speedMatch.Groups[1].Value}");
                            }
                        }
                    }
                });

                // Poll output folder for new .mp3 files until process ends
                while (!process.HasExited)
                {
                    var mp3Files = Directory.GetFiles(DownloadConfig.OutputDirectory, "*.mp3");
                    int count = mp3Files.Length;
                    if (count != lastCount)
                    {
                        lastCount = count;
                        Console.WriteLine($"Downloaded mp3 count: {count}");
                        progress?.Report(count);
                    }

                    await Task.Delay(500); // poll every 0.5 seconds
                }

                // One last count after exit
                var finalFiles = Directory.GetFiles(DownloadConfig.OutputDirectory, "*.mp3");
                progress?.Report(finalFiles.Length);

                sw.Stop();

                Console.WriteLine($"yt-dlp process exited with code {process.ExitCode} after {sw.Elapsed}");
                Console.WriteLine($"Total files downloaded: {finalFiles.Length}");

                return finalFiles.Length > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception during yt-dlp process: {ex}");
                return false;
            }
            finally
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch { /* ignore */ }
                }
                process.Dispose();
            }
        }
    }
}
