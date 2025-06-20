using System.IO;

namespace UtilityApplication.FileDataHandler
{
    public class FileOperationsHandler
    {
        public async Task FixFileNamesAsync(string folderPath, IProgress<int> progress)
        {
            await FileNameFormatter.FixDownloadedFileNamesAsync(folderPath, progress);
        }

        public async Task MoveMp3FilesAsync(string sourceFolder, string targetFolder)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                var files = Directory.GetFiles(sourceFolder, "*.mp3");
                foreach (var filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(targetFolder, fileName);
                    File.Move(filePath, destPath, overwrite: true);
                }
            });
        }

        public async Task AddTagsAsync(string folderPath, IProgress<int> progress)
        {
            await new FileMetadataHandler().AddTagsToMp3FilesAsync(folderPath, progress);
        }

        public void DeleteFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, recursive: true);
        }
    }
}