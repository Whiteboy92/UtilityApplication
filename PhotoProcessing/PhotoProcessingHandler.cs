using System.Diagnostics;
using System.IO;

namespace UtilityApplication.PhotoProcessing
{
    public class PhotoProcessingHandler
    {
        private readonly string ffmpegPath;

        // Provide path to ffmpeg.exe on your machine
        public PhotoProcessingHandler(string ffmpegExecutablePath = @"C:\ffmpeg-2024-12-23-git-6c9218d748-full_build\bin\ffmpeg.exe")
        {
            this.ffmpegPath = ffmpegExecutablePath;
        }

        /// <summary>
        /// Convert all HEIC files asynchronously with progress reporting and error collection using FFmpeg.
        /// </summary>
        public async Task<(List<string> failedFiles, TimeSpan elapsed)> ConvertFilesAsync(
            IEnumerable<string> filesToConvert,
            IProgress<int>? progress = null,
            int maxDegreeOfParallelism = 4)
        {
            var stopwatch = Stopwatch.StartNew();

            var failedFiles = new List<string>();
            int processedCount = 0;
            object sync = new();
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            var tasks = filesToConvert.Select(async heicFile =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string pngFile = Path.ChangeExtension(heicFile, ".png");
                    var args = $"-hwaccel cuda -i \"{heicFile}\" -vf scale=-2:-2 \"{pngFile}\" -y";
                    var exitCode = await RunProcessAsync(ffmpegPath, args);

                    if (exitCode != 0 || !File.Exists(pngFile))
                    {
                        lock(sync) failedFiles.Add($"{heicFile} - FFmpeg failed with exit code {exitCode}");
                    }
                    else
                    {
                        File.Delete(heicFile);
                    }
                }
                catch (Exception ex)
                {
                    lock(sync) failedFiles.Add($"{heicFile} - Exception: {ex.Message}");
                }
                finally
                {
                    lock (sync)
                    {
                        processedCount++;
                        progress?.Report(processedCount);
                    }
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            return (failedFiles, stopwatch.Elapsed);
        }

        private static Task<int> RunProcessAsync(string fileName, string arguments)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            process.Exited += (_, _) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            
            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            return tcs.Task;
        }
    }
}
