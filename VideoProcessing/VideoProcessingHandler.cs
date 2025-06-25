using System.Diagnostics;
using System.IO;
using System.Text;
using UtilityApplication.Settings;
using UtilityApplication.Utility;

namespace UtilityApplication.VideoProcessing
{
    public class VideoProcessingHandler
    {
        public async Task CompressMp4WithProgressAsync(string inputPath, string outputPath, IProgress<double> progress)
        {
            var ffmpegPath = Path.Combine(DownloadConfig.FfmpegLocation, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException("FFmpeg not found.", ffmpegPath);
            
            TimeSpan totalDuration = GetVideoData.GetVideoDuration(inputPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputPath}\" -vcodec libx264 -crf 28 -preset slow -acodec aac -b:a 96k \"{outputPath}\" -y",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;

            var errorBuilder = new StringBuilder();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                errorBuilder.AppendLine(e.Data);
                
                var timeString = ParseTime(e.Data);
                if (timeString != null && TimeSpan.TryParse(timeString, out var currentTime))
                {
                    double percent = currentTime.TotalSeconds / totalDuration.TotalSeconds;
                    if (percent > 1) percent = 1;
                    progress.Report(percent);
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg exited with code {process.ExitCode}\n{errorBuilder}");
            }
        }

        private static string? ParseTime(string ffmpegLine)
        {
            const string timeKey = "time=";
            int timeIndex = ffmpegLine.IndexOf(timeKey, StringComparison.Ordinal);
            if (timeIndex < 0) return null;

            int start = timeIndex + timeKey.Length;
            int end = ffmpegLine.IndexOf(' ', start);
            if (end < 0) end = ffmpegLine.Length;

            return ffmpegLine.Substring(start, end - start);
        }
    }
}
