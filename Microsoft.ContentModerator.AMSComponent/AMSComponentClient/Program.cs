using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Web;
using System.Threading.Tasks;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    class Program
    {
        static bool generateVtt = false;
        static AmsComponent amsComponent;
        static AmsConfigurations amsConfigurations;
        static VideoModerator videoModerator;
        static VideoReviewApi videoReviewApi;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                string videoPath = string.Empty;
                GetUserInputs(out videoPath);
                Initialize();
                try
                {
                    ProcessVideo(videoPath).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(args[0]);
                if (args.Length == 2)
                    bool.TryParse(args[1], out generateVtt);
                Initialize();

                var files = directoryInfo.GetFiles("*.mp4", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        ProcessVideo(file.FullName).Wait();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static async Task ProcessVideo(string videoPath)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nVideo compression process started...");

            var compressedVideoPath = amsComponent.CompressVideo(videoPath);
            if (string.IsNullOrWhiteSpace(compressedVideoPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Video Compression failed.");
            }

            Console.WriteLine("\nVideo compression process completed...");

            UploadVideoStreamRequest uploadVideoStreamRequest = CreateVideoStreamingRequest(compressedVideoPath);
            UploadAssetResult uploadResult = new UploadAssetResult();
            
            if (generateVtt)
            {
                uploadResult.GenerateVTT = generateVtt;
            }
            Console.WriteLine("\nVideo moderation process started...");

            if (!videoModerator.CreateAzureMediaServicesJobToModerateVideo(uploadVideoStreamRequest, uploadResult))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Video Review process failed.");
            }

            Console.WriteLine("\nVideo moderation process completed...");

            Console.WriteLine("\nVideo review process started...");

            string reviewId = await videoReviewApi.CreateVideoReviewInContentModerator(uploadResult);

            Console.WriteLine("\nVideo review successfully completed...");

            sw.Stop();
            Console.WriteLine("\nTotal Elapsed Time: {0}", sw.Elapsed);
            using (var stw = new StreamWriter(AmsConfigurations.logFilePath, true))
            {
                stw.WriteLine("Video File Name: " + Path.GetFileName(videoPath));
                stw.WriteLine($"ReviewId: {reviewId}");
                stw.WriteLine("Total Elapsed Time: {0}", sw.Elapsed);
            }
        }

        private static UploadVideoStreamRequest CreateVideoStreamingRequest(string compressedVideoFilePath)
        {
            return
                new UploadVideoStreamRequest
                {
                    VideoStream = File.ReadAllBytes(compressedVideoFilePath),
                    VideoName = Path.GetFileName(compressedVideoFilePath),
                    EncodingRequest = new EncodingRequest()
                    {
                        EncodingBitrate = AmsEncoding.AdaptiveStreaming
                    },
                    VideoFilePath = compressedVideoFilePath
                };
        }

        private static void GetUserInputs(out string videoPath)
        {
            Console.WriteLine("\nEnter the fully qualified local path for Uploading the video : \n ");
            ConsoleKey response;
            videoPath = Console.ReadLine().Replace("\"", "");
            while (!File.Exists(videoPath))
            {
                Console.WriteLine("\nPlease Enter Valid File path : ");
                videoPath = Console.ReadLine();
            }
            do
            {
                Console.Write("Generate Video Transcript? [y/n] : ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);
            generateVtt = response == ConsoleKey.Y;
        }

        private static void Initialize()
        {
            amsComponent = new AmsComponent();
            amsConfigurations = new AmsConfigurations();
            videoModerator = new VideoModerator(amsConfigurations);
            videoReviewApi = new VideoReviewApi(amsConfigurations);
        }
    }
}
