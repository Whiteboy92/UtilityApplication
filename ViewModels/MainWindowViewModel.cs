using System.IO;
using System.Windows;
using System.Windows.Input;
using UtilityApplication.Commands;
using UtilityApplication.DownloadHandler;
using UtilityApplication.FileDataHandler;
using UtilityApplication.Interfaces;
using UtilityApplication.PhotoProcessing;
using UtilityApplication.Settings;
using UtilityApplication.Utility;
using UtilityApplication.VideoProcessing;

namespace UtilityApplication.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IUserDialogService dialogService;

        #region Fields

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

        private string? firstDownloadedVideoName;

        private const int MaxVideos = 5;

        #endregion

        #region Commands

        public ICommand ConvertHeicToPngCommand { get; }
        public ICommand DownloadPlaylistCommand { get; }
        public ICommand FixAndTagMp3Command { get; }
        public ICommand CompressVideoCommand { get; }

        #endregion

        #region Constructor

        public MainWindowViewModel(IUserDialogService dialogService)
        {
            this.dialogService = dialogService;
            
            var savedData = ApplicationData.ApplicationData.LoadData();
            FirstDownloadedVideoName = savedData.FirstDownloadedVideoName;

            ConvertHeicToPngCommand = new RelayCommand(async () => await ConvertHeicToPngAsync(), CanExecuteOperations);
            DownloadPlaylistCommand = new RelayCommand(async () => await DownloadPlaylistAsync(), () => !IsBusy);
            FixAndTagMp3Command = new RelayCommand(async () => await FixAndTagMp3Async(), CanExecuteOperations);
            CompressVideoCommand = new RelayCommand(async () => await CompressVideoAsync(), () => !IsBusy);
        }

        #endregion

        #region Properties

        private bool IsBusy => IsProcessing || IsDownloadingPlaylist || IsFixingAndTagging || IsCompressingVideo;

        // HEIC to PNG conversion progress
        public int TotalFilesToConvert
        {
            get => totalFilesToConvert;
            private set => SetProperty(ref totalFilesToConvert, value, nameof(ConversionProgressDisplay));
        }

        public int ProcessedFilesCount
        {
            get => processedFilesCount;
            private set => SetProperty(ref processedFilesCount, value, nameof(ConversionProgressDisplay));
        }

        public string ConversionProgressDisplay => $"{ProcessedFilesCount} / {TotalFilesToConvert}";

        public bool IsProcessing
        {
            get => isProcessing;
            private set
            {
                if (SetProperty(ref isProcessing, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        // YouTube playlist download progress
        public int TotalVideosToDownload
        {
            get => totalVideosToDownload;
            private set
            {
                if (SetProperty(ref totalVideosToDownload, value))
                    OnPropertyChanged(nameof(DownloadProgressDisplay));
            }
        }

        public int DownloadedVideosCount
        {
            get => downloadedVideos;
            private set
            {
                if (SetProperty(ref downloadedVideos, value))
                    OnPropertyChanged(nameof(DownloadProgressDisplay));
            }
        }
        
        public string DownloadProgressDisplay => $"{DownloadedVideosCount} / {TotalVideosToDownload}";

        public bool IsDownloadingPlaylist
        {
            get => isDownloadingPlaylist;
            private set
            {
                if (SetProperty(ref isDownloadingPlaylist, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }
        
        public string? FirstDownloadedVideoName
        {
            get => firstDownloadedVideoName;
            private set
            {
                if (SetProperty(ref firstDownloadedVideoName, value))
                {
                    // Save updated value to disk
                    var data = new ApplicationData.ApplicationData.AppDataModel
                    {
                        FirstDownloadedVideoName = firstDownloadedVideoName,
                    };
                    try
                    {
                        ApplicationData.ApplicationData.SaveData(data);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to save app data: {ex}");
                    }
                }
            }
        }

        // Fix and tag MP3 progress
        public int TotalFilesToFix
        {
            get => totalFilesToFix;
            private set => SetProperty(ref totalFilesToFix, value, nameof(FixFileNameProgressDisplay));
        }

        public int ProcessedFiles
        {
            get => processedFiles;
            private set => SetProperty(ref processedFiles, value, nameof(FixFileNameProgressDisplay));
        }

        public string FixFileNameProgressDisplay => $"{ProcessedFiles} / {TotalFilesToFix}";

        public bool IsFixingAndTagging
        {
            get => isFixingAndTagging;
            private set
            {
                if (SetProperty(ref isFixingAndTagging, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        // Video compression progress
        public string VideoCompressionProgressDisplay
        {
            get => videoCompressionProgressDisplay;
            private set => SetProperty(ref videoCompressionProgressDisplay, value);
        }

        public double VideoCompressionProgress
        {
            get => videoCompressionProgress;
            private set => SetProperty(ref videoCompressionProgress, value);
        }

        public bool IsCompressingVideo
        {
            get => isCompressingVideo;
            private set
            {
                if (SetProperty(ref isCompressingVideo, value))
                    RaiseCanExecuteChangedForCommands();
            }
        }

        #endregion

        #region Command CanExecute Helper

        private bool CanExecuteOperations() => !IsBusy;

        private void RaiseCanExecuteChangedForCommands()
        {
            (ConvertHeicToPngCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DownloadPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (FixAndTagMp3Command as RelayCommand)?.RaiseCanExecuteChanged();
            (CompressVideoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command Implementations

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
                dialogService.ShowMessage("Failed to convert files:\n" + string.Join("\n", failedFiles), "Errors", MessageBoxImage.Error);
            else
                dialogService.ShowMessage($"All files converted successfully!\nElapsed time: {elapsed}", "Success", MessageBoxImage.Information);
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
            try
            {
                if (!TryGetVideoCountFromUser(out int count))
                    return;

                InitializeDownloadState(count);

                var progress = new Progress<int>(HandleDownloadProgress);
                var helper = new DownloadHelper(dialogService);
                bool success = await helper.DownloadPlaylistAsync(count, progress);
                
                if (success)
                {
                    await UpdateFirstDownloadedVideoNameAsync();
                }

                dialogService.ShowMessage(
                    success ? "Playlist downloaded successfully!" : "Failed to download playlist.",
                    success ? "Success" : "Error",
                    success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Unexpected error: {ex}");
                dialogService.ShowMessage($"An unexpected error occurred:\n{ex.Message}", "Error", MessageBoxImage.Error);
            }
            finally
            {
                ResetDownloadState();
            }
        }

        private bool TryGetVideoCountFromUser(out int count)
        {
            count = 0;
            string? input = dialogService.ShowInputDialog(
                "Enter the number of videos to download:",
                "Download Videos",
                MaxVideos.ToString());

            if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out count) || count <= 0)
            {
                dialogService.ShowMessage("Invalid number entered.", "Error", MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void InitializeDownloadState(int count)
        {
            TotalVideosToDownload = count;
            DownloadedVideosCount = 0;
            FirstDownloadedVideoName = null;
            IsDownloadingPlaylist = true;
        }

        private void ResetDownloadState()
        {
            TotalVideosToDownload = 1;
            DownloadedVideosCount = 0;
            IsDownloadingPlaylist = false;
        }

        private async void HandleDownloadProgress(int progressValue)
        {
            try
            {
                DownloadedVideosCount = progressValue;

                if (progressValue == 1 && string.IsNullOrEmpty(FirstDownloadedVideoName))
                {
                    await UpdateFirstDownloadedVideoNameAsync();
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error in HandleDownloadProgress: {ex}");

                dialogService.ShowMessage(
                    $"An error occurred while processing the download progress:\n{ex.Message}",
                    "Progress Error",
                    MessageBoxImage.Warning);
            }
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
                VideoCompressionProgress = p;
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

        #endregion

        private async Task UpdateFirstDownloadedVideoNameAsync()
        {
            var fileName = await GetVideoData.GetFirstDownloadedMp3FileNameAsync();
            if (!string.IsNullOrEmpty(fileName))
            {
                FirstDownloadedVideoName = fileName;
            }
        }

    }
}
