using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UTubePrj
{
    public class Additions
    {
        public Additions(string UrlFilePath, string downloadedDirectoryPath)
        {
            bool somthingChanged = false;
            if (!File.Exists(UrlFilePath))
            {
                File.WriteAllText(UrlFilePath, "При указании ссылки на плейлист - в конце url добавьте символ | . Например: www.site/index/playlist |");

            }
            if (!Directory.Exists(downloadedDirectoryPath))
            {
                Directory.CreateDirectory(downloadedDirectoryPath);
            }
            if (somthingChanged)
                Environment.Exit(0);
        }
        public void StartOfProgram()
        {
            Console.WriteLine("1 - Начать загрузку \n2 - Соединить выбранный файл со видео со звуком");
            switch (Console.ReadLine())
            {
                case "1":
                    {
                        return;
                    }
                case "2":
                    {
                        Console.WriteLine("Введите название файла видео(с расширением)");
                        string video = Console.ReadLine();
                        Console.WriteLine("Введите название файла звука(с расширением)");
                        string music = Console.ReadLine();
                        Program.Merger(video, music, Directory.GetCurrentDirectory() + @"\" + Program.DownloadedDirectoryPath + @"\" + video.Replace(".webm", ".mp4"));
                          Environment.Exit(0);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Введите цифру");
                        Environment.Exit(0);
                        break;
                    }
            }
        }
        public string[] ReadUrls(string UrlsFilePath)
        {
            string[] allStrings = File.ReadAllLines(UrlsFilePath);
            string[] stringWithoutComment = new string[allStrings.Length - 1];
            Array.Copy(allStrings, 1, stringWithoutComment, 0, stringWithoutComment.Length);
            return stringWithoutComment;
        }
        public bool CheckIfPlaylist(string url)
        {
            if (url.Contains("|"))
                return true;
            else
                return false;
        }
        public void RemoveTempFiles(string videoPath, string audioPath)
        {
            if (File.Exists(videoPath))
                File.Delete(videoPath);
            if (File.Exists(audioPath))
                File.Delete(audioPath);
        }
        public static bool UrlFileExist(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                return false;
            }
        }
    }
}
