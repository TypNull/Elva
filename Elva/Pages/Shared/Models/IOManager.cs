using DownloadAssistant.Base;
using DownloadAssistant.Request;
using Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Shared.Models
{
    internal static class IOManager
    {
        public static string TempPath => Path.Combine(Path.GetTempPath(), "Elva\\");
        public static string LocalDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Elva\\");
        public static string DataPath => Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!, "data\\");
        public static string DownloadPath { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Elva\\");
        public static event EventHandler<string>? DownloadPathChanged;

        public static string GetLogoPath(this Website website)
        {
            string imagePath = DataPath + website.Name + website.Suffix + ".image";
            if (File.Exists(imagePath))
                return imagePath;
            else return string.Empty;
        }


        public static Website[] LoadWebsites()
        {
            Directory.CreateDirectory(DataPath);
            FileInfo[] files = new DirectoryInfo(DataPath).GetFiles("*.wsf");
            List<Website> websites = new(files.Length);

            foreach (FileInfo file in files)
            {
                try
                {
                    Website? website = Website.LoadWebsite(file.FullName);

                    if (website == null)
                        continue;
                    string imagePath = DataPath + website.Name + website.Suffix + ".image";
                    if (!File.Exists(imagePath))
                        DeserializeWebsiteImage(file.FullName);
                    websites.Add(website);
                }
                catch (Exception)
                {
                }
            }
            if (websites.Count == 0)
                return Array.Empty<Website>();

            return websites.DistinctBy(x => x.Name + x.Suffix).ToArray();
        }

        public static void DeserializeWebsiteImage(string path)
        {
            JsonElement element;
            FileStream? openStream = null;
            try
            {
                using (openStream = File.OpenRead(path))
                {
                    element = JsonSerializer.Deserialize<JsonElement>(openStream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, WriteIndented = true });
                    openStream.Flush();
                }
                if (element.TryGetProperty("Logo", out JsonElement value) && element.TryGetProperty("Name", out JsonElement name) && element.TryGetProperty("Suffix", out JsonElement suffix))
                {
                    string imagePath = Path.GetDirectoryName(path) + "\\" + name.GetString() + suffix.GetString() + ".image";
                    if (!File.Exists(imagePath))
                    {
                        using MemoryStream stream = new(value.GetBytesFromBase64());
                        using FileStream fileStream = new(imagePath, FileMode.Create);
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(stream));
                        encoder.Save(fileStream);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                openStream?.Close();
            }
        }

        public static char[] IvalidFileNameChars = new char[]
          {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '?', '\\', '/'
          };


        public static async Task DownloadWebsitesFromRepoAsync()
        {
            RequestContainer<IRequest> requests = new();
            requests.Add(new OwnRequest(async (token) =>
            {
                HttpGet get = new(new HttpRequestMessage(HttpMethod.Get, new Uri("https://api.github.com/repos/TypNull/WebsiteScraper/contents/Websites")));
                HttpResponseMessage resonse = await get.LoadResponseAsync();
                string jsonString = await resonse.Content.ReadAsStringAsync();
                JsonElement[]? json = JsonSerializer.Deserialize<JsonElement[]>(jsonString);
                if (json?.Length <= 0)
                    return false;
                foreach (JsonElement item in json!)
                {
                    if (Path.GetExtension(item.GetProperty("name").ToString()) == ".wsf")
                        requests.Add(new GetRequest(item.GetProperty("download_url").ToString(), new()
                        {
                            Filename = item.GetProperty("name").ToString(),
                            DirectoryPath = DataPath
                        }));
                }
                return true;
            }));
            await requests.Task;
            return;
        }

        /// <summary>
        /// Removes all invalid Characters for a filename out of a string
        /// </summary>
        /// <param name="name">input filename</param>
        /// <returns>Clreared filename</returns>
        public static string RemoveInvalidFileNameChars(string name)
        {
            StringBuilder fileBuilder = new(name);
            foreach (char c in IvalidFileNameChars)
                fileBuilder.Replace(c.ToString(), string.Empty);
            return fileBuilder.ToString();
        }

        internal static void ChangeDownloadPath(string path)
        {
            if (Path.Exists(path))
                if (path.EndsWith('\\'))
                    DownloadPath = path;
                else DownloadPath = path + "\\";
            DownloadPathChanged?.Invoke(null, DownloadPath);
        }

        internal static string CopyFileTo(string path, string dataPath)
        {
            string filename = Path.GetFileName(path);
            string newFilePath = Path.Combine(dataPath, filename);
            File.Copy(path, newFilePath, true);
            return newFilePath;
        }
    }
}
