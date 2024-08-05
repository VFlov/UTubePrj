using System;
using System.IO;
using System;
using System.IO;
using FFmpeg.Net;
using VideoLibrary;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.ComponentModel.Design;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using Microsoft.PowerShell.Commands;



namespace UTubePrj
{
    class Program
    { 
        static async Task Main()
        {
            additions.StartOfProgram();
            string[] urls = additions.ReadUrls(UrlsFilePath);
            using (SemaphoreSlim semaphore = new SemaphoreSlim(NumsOfParallelDownloads))
            {
                // Create a task for each URL
                List<Task> downloadTasks = new List<Task>();
                for (int i = 0; i < urls.Length; i++)
                {
                    // Wait for a slot to become available
                    await semaphore.WaitAsync();

                    // Create and start the download task
                    int taskId = i; // Capture the index for task identification
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await DownloadFiles(urls[taskId], taskId*3);
                        }
                        finally
                        {
                            semaphore.Release(); // Release the slot when the task finishes
                        }
                    }));
                }

                // Wait for all download tasks to complete
                await Task.WhenAll(downloadTasks);

                Console.ReadLine();
            }
        }
        static async Task DownloadFiles(string url,int step)
        {
            var youTube = YouTube.Default;
            var videoInfos = youTube.GetAllVideos(url);
            string videoFileName = VideoDownload(videoInfos, step);
            string audioFileName = AudioDownload(videoInfos, step);
            Merger(videoFileName, audioFileName, Directory.GetCurrentDirectory() + @"\" + DownloadedDirectoryPath + @"\" + videoFileName);
            //additions.RemoveTempFiles(videoFileName, audioFileName);
        }
        
        static string AudioDownload(IEnumerable<YouTubeVideo> youTubeVideos, int step)
        {
            var audioFormat = youTubeVideos.Where(i => i.AudioFormat == AudioFormat.Aac);
            var bitrate = audioFormat.First(i => i.AudioBitrate == audioFormat.Max(j => j.AudioBitrate));
            //File.WriteAllBytes(bitrate.FullName + "_audio", bitrate.GetBytes());
            //Task.Run(() => DownloadFile(bitrate.Uri, bitrate.FullName + "_audio", 3));
            DownloadFile(bitrate.Uri, bitrate.FullName, step+2);
            return bitrate.FullName;
        }
        static string VideoDownload(IEnumerable<YouTubeVideo> youTubeVideos, int step)
        {
            var maxResolution = youTubeVideos.First(i => i.Resolution == youTubeVideos.Max(j => j.Resolution));
            //File.WriteAllBytesAsync(maxResolution.FullName, maxResolution.GetBytes());
            //Task.Run(() => DownloadFile(maxResolution.Uri, maxResolution.FullName + "_video", 0));
            DownloadFile(maxResolution.Uri, maxResolution.FullName, step);
            return maxResolution.FullName;
        }
        public static void Merger(string videoPath, string audioPath, string outputPath)
        {
            // Команда FFmpeg для добавления звука
            //ffmpeg - i video.mp4 - i audio.wav - c:v copy -c:a aac output.mp4
            /*
            string ffmpegCommand = "";
            if (videoPath.Contains(".webm"))
                ffmpegCommand = $"-i \"{videoPath}\" -i \"{audioPath}\" -c copy output.mp4";
            else
                ffmpegCommand = $"-i \"{videoPath}\" -i \"{audioPath}\" -map 0:v -map 1:a -c copy \"{outputPath}\"";
            */
            
            string ffmpegCommand = $"-i \"{Directory.GetCurrentDirectory()}\\{videoPath.Normalize().Replace("-", "–")}\" -i \"{Directory.GetCurrentDirectory()}\\{audioPath.Normalize().Replace("-", "–")}\" -c copy \"{Directory.GetCurrentDirectory()}\\hh.mp4\"";
            string ffmpegPath = Directory.GetCurrentDirectory() + "\\ffmpeg.exe";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "ffmpeg.exe";
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.StandardInputEncoding = System.Text.Encoding.GetEncoding(65001);
            startInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding(65001);
            startInfo.Arguments = ffmpegCommand;
            process.StartInfo = startInfo;
            process.Start();
            /*
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                // Создайте PowerShell-команду
                PowerShell ps = PowerShell.Create();
                ps.Runspace = runspace;
                string ffmpegPath = Directory.GetCurrentDirectory() + "\\ffmpeg.exe";
                videoPath = Encoding.Unicode.GetString(Encoding.Unicode.GetBytes(videoPath));
                audioPath = Encoding.Unicode.GetString(Encoding.Unicode.GetBytes(audioPath));

                string[] arguments = {
                "-i",
                $"\"{Directory.GetCurrentDirectory()}\\" + videoPath + "\"",
                "-i",
                $"\"{Directory.GetCurrentDirectory()}\\" + audioPath + "\"",
                "-c", "copy",
                $"\"{Directory.GetCurrentDirectory()}\\hh.mp4\""
                };

                // Use Start-Process
                ps.AddCommand("Start-Process");
                ps.AddParameter("FilePath", ffmpegPath);
                ps.AddParameter("ArgumentList", arguments);
                ps.Invoke();

                // Проверьте на ошибки
                if (ps.HadErrors)
                {
                    Console.WriteLine("ERROR");
                }
            }
            */
        }
        static void DownloadFile(string fileUrl, string filePath, int urlNum)
        {
            long fileSize = 0;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(fileUrl);
            request.Method = "HEAD";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                fileSize = response.ContentLength;
                // Создаем объект WebClient
                using (WebClient client = new WebClient())
                {
                    if (filePath.Contains(".webm") || filePath.Contains(".mp4"))
                    {
                        Console.SetCursorPosition(0, urlNum);
                        Console.Write("Загрузка файла видео: " + filePath.Substring(0, 40));
                    }
                    else
                    {
                        Console.SetCursorPosition(0, urlNum);
                        Console.Write("Загрузка файла звука: " + filePath.Substring(0, 40));
                    }

                    // Определяем счетчик загруженных байт
                    long downloadedBytes = 0;
                    Stopwatch sw = Stopwatch.StartNew();

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
                        downloadedBytes = e.BytesReceived;
                        // Отображаем процент загрузки
                        DisplayProgress(downloadedBytes, fileSize, sw.ElapsedMilliseconds/1000, urlNum);

                        //Console.SetCursorPosition(0, urlNum + 2);
                        //Console.Write("Прогресс: " + progress + "%" + " Скорость: " + e.BytesReceived/1000);
                    };
                    client.DownloadFileTaskAsync(new Uri(fileUrl), filePath);
                    while(downloadedBytes < fileSize)
                    {
                        System.Threading.Thread.Sleep(10);
                        fileSize = response.ContentLength;
                    }
                }
            }
        }
        static void DisplayProgress(long receivedBytes, long totalBytes, long seconds, int urlNum)
        {
            // Процент загрузки
            double percentage = (double)receivedBytes / totalBytes * 100;

            // Скорость загрузки
            double speed = (double)receivedBytes / seconds / 1024 / 1024;


            // Вывод прогресса в консоль
            Console.SetCursorPosition(0,urlNum+1);
            Console.Write("\rЗагружено: {0:0.00}% (Скорость: {1:0.00} МБ/с)", percentage, speed);
        }
        //Settings
        static readonly string UrlsFilePath = "Urls.txt";
        public static readonly string DownloadedDirectoryPath = "Downloaded";
        static readonly int NumsOfParallelDownloads = 5;
        static int FilesEndOfDownload = 0;
        static Additions additions = new Additions(UrlsFilePath, DownloadedDirectoryPath);
    } 
}