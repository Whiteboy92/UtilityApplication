using UtilityApplication.Services;

namespace UtilityApplication.DownloadHandler
{
    public class DownloadHandler
    {
        private readonly YtDlpService ytDlpService = new();

        public async Task<bool> DownloadPlaylistAsync(string playlistUrl, int maxVideos, IProgress<int>? progress = null)
        {
            Console.WriteLine($"Starting download of up to {maxVideos} videos from playlist: {playlistUrl}");

            bool success = await ytDlpService.DownloadAudioAsync(playlistUrl, maxVideos, new Progress<int>(count =>
            {
                Console.WriteLine($"Progress reported: {count} files downloaded");
                progress?.Report(count);
            }));

            Console.WriteLine(success 
                ? "DownloadPlaylistAsync completed successfully." 
                : "DownloadPlaylistAsync failed or no files downloaded.");

            return success;
        }
    }
}