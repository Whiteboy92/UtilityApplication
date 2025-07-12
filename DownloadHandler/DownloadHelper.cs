using System.Windows;
using UtilityApplication.Interfaces;

namespace UtilityApplication.DownloadHandler
{
    public class DownloadHelper
    {
        private readonly IUserDialogService dialogService;

        public DownloadHelper(IUserDialogService dialogService)
        {
            this.dialogService = dialogService;
        }

        public async Task<bool> DownloadPlaylistAsync(int count, IProgress<int> progress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var handler = new DownloadHandler();
                    return handler.DownloadPlaylistAsync(
                        "https://www.youtube.com/watch?v=pSyUBkOEJVs&list=PL2ApIZPouz9z7l2PO0ju8X7sZBO6bUMPc",
                        count,
                        progress
                    ).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    dialogService.ShowMessage("Download was cancelled.", "Cancelled", MessageBoxImage.Warning);
                    return false;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Download failed: {ex}");
                    dialogService.ShowMessage($"Failed to download playlist:\n{ex.Message}", "Error", MessageBoxImage.Error);
                    return false;
                }
            });
        }
    }
}