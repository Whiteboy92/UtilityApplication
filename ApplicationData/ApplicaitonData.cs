using System.IO;
using System.Text.Json;

namespace UtilityApplication.ApplicationData
{
    public static class ApplicationData
    {
        private static readonly string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UtilityApplication");

        private static readonly string FilePath = Path.Combine(DirectoryPath, "data.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
        };

        public class AppDataModel
        {
            public string? FirstDownloadedVideoName { get; set; }
            // Add more properties here as needed
        }

        public static void SaveData(AppDataModel data)
        {
            try
            {
                if (!Directory.Exists(DirectoryPath))
                {
                    Directory.CreateDirectory(DirectoryPath);
                }

                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to save application data: {ex}");
            }
        }

        public static AppDataModel LoadData()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new AppDataModel();
                }

                string json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<AppDataModel>(json, JsonOptions);
                return data ?? new AppDataModel();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load application data: {ex}");
                return new AppDataModel();
            }
        }
    }
}