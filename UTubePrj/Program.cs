using System;
using System.IO;
using System;
using System.IO;

using FFmpeg.Net;


using VideoLibrary;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Net;



namespace UTubePrj
{
    class Program
    {
        static string link = "https://www.youtube.com/watch?v=SjF0znFTfKg";
        static string UrlsFilePath = "Urls.txt"; 
        static async Task Main()
        {
            Stopwatch sw = Stopwatch.StartNew();
            string[] urls = ReadUrls();

            foreach (var url in urls)
            {
                await Task.WhenAll(DownloadFiles(url));
            }
            Console.ReadLine();
        }
        static async Task DownloadFiles(string url)
        {
            var youTube = YouTube.Default;
            var videoInfos = youTube.GetAllVideos(url);
            Task<string> videoFileNameTask = VideoDownload(videoInfos);
            Task<string> audioFileNameTask = AudioDownload(videoInfos);
            string videoFileName = await videoFileNameTask;
            string audioFileName = await audioFileNameTask;
            Merger(videoFileName, audioFileName, videoFileName);
        }
        static string[] ReadUrls()
        {
            return File.ReadAllLines(UrlsFilePath);
        }
        static async Task<string> AudioDownload(IEnumerable<YouTubeVideo> youTubeVideos)
        {
            var audioFormat = youTubeVideos.Where(i => i.AudioFormat == AudioFormat.Aac);
            var bitrate = audioFormat.First(i => i.AudioBitrate == audioFormat.Max(j => j.AudioBitrate));
            //File.WriteAllBytes(bitrate.FullName + "_audio", bitrate.GetBytes());
            await Task.Run(() => DownloadFile(bitrate.Uri, bitrate.FullName + "_audio", 5));
            return bitrate.FullName + "_audio";
        }
        static async Task<string> VideoDownload(IEnumerable<YouTubeVideo> youTubeVideos)
        {
            var maxResolution = youTubeVideos.First(i => i.Resolution == youTubeVideos.Max(j => j.Resolution));
            //File.WriteAllBytesAsync(maxResolution.FullName, maxResolution.GetBytes());
            Console.WriteLine("gg");
            await Task.Run(() => DownloadFile(maxResolution.Uri, maxResolution.FullName + "_video", 0));
            return maxResolution.FullName + "_video";
        }
        public static void Merger(string videoPath, string audioPath, string outputPath)
        {
            // Команда FFmpeg для добавления звука
            string ffmpegCommand = $"-i \"{videoPath}\" -i \"{audioPath}\" -map 0:v -map 1:a -c copy \"{outputPath}\"";

            // Запуск FFmpeg с помощью Process
            ProcessStartInfo processInfo = new ProcessStartInfo("ffmpeg.exe", ffmpegCommand);
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            Process process = new Process();
            process.StartInfo = processInfo;
            process.Start();

            // Вывод сообщения об успешном выполнении или ошибке
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());

            process.WaitForExit();

            Console.WriteLine("Завершение работы.");
        }
        static void DownloadFile(string fileUrl, string filePath,int urlNum)
        {
            long fileSize = 0;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileUrl);
            request.Method = "HEAD";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                fileSize = response.ContentLength;
                // Создаем объект WebClient
                using (WebClient client = new WebClient())
                {
                    if (filePath.Contains("_audio"))
                    {
                        Console.SetCursorPosition(0, urlNum);
                        Console.Write("Загрузка файла звука");
                    }
                    if (filePath.Contains("_video"))
                    {
                        Console.SetCursorPosition(0, urlNum);
                        Console.Write("Загрузка файла видео");
                    }
                    Console.SetCursorPosition(0, urlNum + 1);
                    // Устанавливаем прогресс-бар
                    Console.Write("Прогресс: 0%");

                    // Определяем счетчик загруженных байт
                    long downloadedBytes = 0;

                    // Загружаем файл
                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        Console.SetCursorPosition(0, urlNum + 1);
                        Console.Write("Прогресс: 100%");
                    };
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        // Рассчитываем процент загрузки
                        int progress = (int)(((double)e.BytesReceived / fileSize) * 100);

                        // Отображаем процент загрузки
                        Console.SetCursorPosition(0, urlNum + 1);
                        Console.Write("Прогресс: " + progress + "%");
                    };
                    client.DownloadFileAsync(new Uri(fileUrl), filePath);
                    while(downloadedBytes < fileSize)
                    {
                        System.Threading.Thread.Sleep(100);

                        fileSize = response.ContentLength;
                    }
                }
            }
        }
    } 
}