using System.IO;
using System.Windows;
using System.Windows.Input;
using UtilityApplication.Commands;
using UtilityApplication.FileDataHandler;
using UtilityApplication.Interfaces;
using UtilityApplication.PhotoProcessing;
using UtilityApplication.VideoProcessing;

namespace UtilityApplication.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IUserDialogService dialogService;

        private int totalFilesToConvert = 1;
        private int processedFilesCount;

        private int totalVideosToDownload = 1;
        private int downloadedVideos;

        private int totalFilesToFix = 1;
        private int processedFiles;

        private bool isProcessing;
        private bool isDownloadingPlaylist;
        private bool isFixingAndTagging;

        private bool isCompressingVideo;
        private string videoCompressionProgressDisplay = string.Empty;
        private double videoCompressionProgress;

        private const int MaxVideos = 5;

        public ICommand DownloadPlaylistCommand { get; }
        public ICommand ConvertHeicToPngCommand { get; }
        public ICommand FixAndTagMp3Command { get; }
        public ICommand CompressVideoCommand { get; }

        public MainWindowViewModel(IUserDialogService dialogService)
        {
            this.dialogService = dialogService;

            ConvertHeicToPngCommand = new RelayCommand(async () => await ConvertHeicToPngAsync(), CanExecuteConvert);
            DownloadPlaylistCommand = new RelayCommand(async () => await DownloadPlaylistAsync(), () => !IsBusy);
            FixAndTagMp3Command = new RelayCommand(async () => await FixAndTagMp3Async(), CanExecuteConvert);
            CompressVideoCommand = new RelayCommand(async () => await CompressVideoAsync(), () => !IsBusy);
        }

        private bool IsBusy => IsProcessing || IsDownloadingPlaylist || IsFixingAndTagging || IsCompressingVideo;

        private int TotalFilesToConvert
        {
            get => totalFilesToConvert;
            set => SetProperty(ref totalFilesToConvert, value, nameof(ConversionProgressDisplay));
        }

        private int ProcessedFilesCount
        {
            get => processedFilesCount;
            set => SetProperty(ref processedFilesCount, value, nameof(ConversionProgressDisplay));
        }

        public string ConversionProgressDisplay => $"{ProcessedFilesCount} / {TotalFilesToConvert}";

        private int TotalVideosToDownload
        {
            get => totalVideosToDownload;
            set => SetProperty(ref totalVideosToDownload, value, nameof(DownloadProgressDisplay));
        }

        private int DownloadedVideosCount
        {
            get => downloadedVideos;
            set => SetProperty(ref downloadedVideos, value, nameof(DownloadProgressDisplay));
        }

        public string DownloadProgressDisplay => $"{DownloadedVideosCount} / {TotalVideosToDownload}";

        private int TotalFilesToFix
        {
            get => totalFilesToFix;
            set => SetProperty(ref totalFilesToFix, value, nameof(FixFileNameProgressDisplay));
        }

        private int ProcessedFiles
        {
            get => processedFiles;
            set => SetProperty(ref processedFiles, value, nameof(FixFileNameProgressDisplay));
        }

        public string FixFileNameProgressDisplay => $"{ProcessedFiles} / {TotalFilesToFix}";

        private bool IsProcessing
        {
            get => isProcessing;
            set
            {
                if (SetProperty(ref isProcessing, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        private bool IsDownloadingPlaylist
        {
            get => isDownloadingPlaylist;
            set
            {
                if (SetProperty(ref isDownloadingPlaylist, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        private bool IsFixingAndTagging
        {
            get => isFixingAndTagging;
            set
            {
                if (SetProperty(ref isFixingAndTagging, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        private bool IsCompressingVideo
        {
            get => isCompressingVideo;
            set
            {
                if (SetProperty(ref isCompressingVideo, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        public string VideoCompressionProgressDisplay
        {
            get => videoCompressionProgressDisplay;
            set => SetProperty(ref videoCompressionProgressDisplay, value);
        }

        public double VideoCompressionProgress
        {
            get => videoCompressionProgress;
            set => SetProperty(ref videoCompressionProgress, value);
        }

        private bool CanExecuteConvert() => !IsBusy;

        private void RaiseCanExecuteChangedForCommands()
        {
            (ConvertHeicToPngCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DownloadPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (FixAndTagMp3Command as RelayCommand)?.RaiseCanExecuteChanged();
            (CompressVideoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task ConvertHeicToPngAsync()
        {
            var folder = dialogService.SelectFolder("Select folder containing HEIC files");
            if (string.IsNullOrWhiteSpace(folder))
                return;

            var files = Directory.EnumerateFiles(folder, "*.heic", SearchOption.AllDirectories).ToList();
            if (files.Count == 0)
            {
                dialogService.ShowMessage("No HEIC files found.", "No Files", MessageBoxImage.Information);
                return;
            }

            TotalFilesToConvert = files.Count;
            ProcessedFilesCount = 0;
            IsProcessing = true;

            var photoProcessor = new PhotoProcessingHandler();
            var progress = new Progress<int>(count => ProcessedFilesCount = count);
            var (failedFiles, elapsed) = await photoProcessor.ConvertFilesAsync(files, progress);

            IsProcessing = false;
            TotalFilesToConvert = 1;
            ProcessedFilesCount = 0;

            if (failedFiles.Any())
            {
                dialogService.ShowMessage("Failed to convert files:\n" + string.Join("\n", failedFiles), "Errors", MessageBoxImage.Error);
            }
            else
            {
                dialogService.ShowMessage($"All files converted successfully!\nElapsed time: {elapsed}", "Success", MessageBoxImage.Information);
            }
        }

        private async Task FixAndTagMp3Async()
        {
            string sourceFolder = @"C:\Users\marci\Desktop\Playlist";
            string targetFolder = @"C:\Users\marci\Desktop\Admin\Music Mp3";

            var mp3Files = Directory.GetFiles(sourceFolder, "*.mp3");
            if (mp3Files.Length == 0)
            {
                dialogService.ShowMessage("No MP3 files found in source folder.", "Info", MessageBoxImage.Information);
                return;
            }

            TotalFilesToFix = mp3Files.Length;
            ProcessedFiles = 0;
            IsFixingAndTagging = true;

            try
            {
                var fileOps = new FileOperationsHandler();
                var progress = new Progress<int>(count => ProcessedFiles = count);

                await fileOps.FixFileNamesAsync(sourceFolder, progress);
                await fileOps.MoveMp3FilesAsync(sourceFolder, targetFolder);
                await fileOps.AddTagsAsync(targetFolder, progress);
                fileOps.DeleteFolder(sourceFolder);

                dialogService.ShowMessage("Files moved, tagged, and source folder deleted successfully!", "Success", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageBoxImage.Error);
            }
            finally
            {
                IsFixingAndTagging = false;
                TotalFilesToFix = 1;
                ProcessedFiles = 0;
            }
        }

        private async Task DownloadPlaylistAsync()
        {
            string? input = dialogService.ShowInputDialog(
                "Enter the number of videos to download:",
                "Download Videos",
                MaxVideos.ToString());

            if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int count) || count <= 0)
            {
                dialogService.ShowMessage("Invalid number entered.", "Error", MessageBoxImage.Error);
                return;
            }

            TotalVideosToDownload = count;
            DownloadedVideosCount = 0;
            IsDownloadingPlaylist = true;

            var handler = new DownloadHandler.DownloadHandler();
            var progress = new Progress<int>(p => DownloadedVideosCount = p);

            bool success = await handler.DownloadPlaylistAsync(
                "https://www.youtube.com/watch?v=pSyUBkOEJVs&list=PL2ApIZPouz9z7l2PO0ju8X7sZBO6bUMPc",
                count,
                progress);

            IsDownloadingPlaylist = false;

            dialogService.ShowMessage(
                success ? "Playlist downloaded successfully!" : "Failed to download playlist.",
                success ? "Success" : "Error",
                success ? MessageBoxImage.Information : MessageBoxImage.Error);

            TotalVideosToDownload = 1;
            DownloadedVideosCount = 0;
        }

        private async Task CompressVideoAsync()
        {
            var inputFile = dialogService.SelectFile("Select an MP4 file to compress", "MP4 files (*.mp4)|*.mp4");
            if (string.IsNullOrWhiteSpace(inputFile))
                return;

            var outputFile = Path.Combine(Path.GetDirectoryName(inputFile)!,
                Path.GetFileNameWithoutExtension(inputFile) + "_compressed.mp4");

            IsCompressingVideo = true;
            VideoCompressionProgressDisplay = "Compressing...";
            VideoCompressionProgress = 0;

            var progress = new Progress<double>(p =>
            {
                VideoCompressionProgress = p; // 0.0-1.0 for ProgressBar.Value binding (0-100)
                VideoCompressionProgressDisplay = $"Compressing... {(p * 100):F1}%";
            });

            try
            {
                var handler = new VideoProcessingHandler();
                await handler.CompressMp4WithProgressAsync(inputFile, outputFile, progress);
                VideoCompressionProgressDisplay = "Compression complete!";
                VideoCompressionProgress = 1;
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage($"Error during compression:\n{ex.Message}", "Error", MessageBoxImage.Error);
                VideoCompressionProgressDisplay = "Compression failed.";
                VideoCompressionProgress = 0;
            }

            await Task.Delay(1500);
            IsCompressingVideo = false;
            VideoCompressionProgressDisplay = string.Empty;
            VideoCompressionProgress = 0;
        }
    }
}
