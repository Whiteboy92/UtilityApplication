using System.Diagnostics;
using System.IO;
using System.Collections.Concurrent;
using ImageMagick;

namespace UtilityApplication.PhotoProcessing
{
    public class PhotoProcessingHandler
    {
        public async Task<(List<string> failedFiles, TimeSpan elapsed)> ConvertFilesAsync(
            string rootFolder,
            IProgress<int>? progress = null,
            int? maxDegreeOfParallelism = null)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                throw new ArgumentNullException(nameof(rootFolder));

            if (!Directory.Exists(rootFolder))
                throw new DirectoryNotFoundException($"The directory '{rootFolder}' does not exist.");

            var heicFiles = Directory.GetFiles(rootFolder, "*.heic", SearchOption.TopDirectoryOnly).ToList();

            if (heicFiles.Count == 0)
                return (new List<string>(), TimeSpan.Zero);

            // Step 1: Copy all HEIC files to "copies" folder inside root folder
            string copiesFolder = Path.Combine(rootFolder, "copies");
            Directory.CreateDirectory(copiesFolder);

            var failedFiles = new ConcurrentBag<string>();
            int processedCount = 0;
            int maxParallel = maxDegreeOfParallelism ?? Environment.ProcessorCount;

            foreach (var heicFile in heicFiles)
            {
                try
                {
                    string destPath = Path.Combine(copiesFolder, Path.GetFileName(heicFile));
                    File.Copy(heicFile, destPath, overwrite: true);
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{heicFile} - Failed to copy to 'copies': {ex.Message}");
                }
            }

            var stopwatch = Stopwatch.StartNew();

            using var semaphore = new SemaphoreSlim(maxParallel);

            // Step 2: Convert original HEIC files in root folder to PNG
            var tasks = heicFiles.Select(async heicFile =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    await Task.Run(() =>
                    {
                        if (!File.Exists(heicFile))
                        {
                            failedFiles.Add($"{heicFile} - Original file not found.");
                            return;
                        }

                        string pngFile = Path.ChangeExtension(heicFile, ".png");

                        try
                        {
                            using var image = new MagickImage(heicFile);
                            image.Format = MagickFormat.Png;
                            image.Write(pngFile);
                        }
                        catch (Exception imgEx)
                        {
                            failedFiles.Add($"{heicFile} - Image processing failed: {imgEx.Message}");
                            return;
                        }

                        if (!File.Exists(pngFile))
                        {
                            failedFiles.Add($"{heicFile} - Failed to save PNG.");
                            return;
                        }

                        // Delete original HEIC after successful PNG conversion
                        try
                        {
                            File.Delete(heicFile);
                        }
                        catch (Exception delEx)
                        {
                            failedFiles.Add($"{heicFile} - Failed to delete original HEIC file: {delEx.Message}");
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{heicFile} - Exception: {ex.Message}");
                }
                finally
                {
                    progress?.Report(Interlocked.Increment(ref processedCount));
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            stopwatch.Stop();

            return (failedFiles.ToList(), stopwatch.Elapsed);
        }
    }
}
