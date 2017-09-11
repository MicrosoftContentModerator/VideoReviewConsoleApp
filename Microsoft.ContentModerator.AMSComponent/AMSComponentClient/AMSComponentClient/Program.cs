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
        static string confidence = "0";
        static bool generateVtt = false;
        static bool v2Json = false;
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
                if (args.Length == 2) bool.TryParse(args[1], out generateVtt);
                v2Json = true;
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
            if (v2Json)
            {
                string filepath = videoPath.Replace(".mp4", ".json");
                if (!File.Exists(filepath))
                {
                    Console.WriteLine("V2 Json does not exist for video.");
                    throw new Exception();
                }
                uploadResult.V2JSONPath = filepath;
            }
            Console.WriteLine("\nVideo moderation process started...");

            if (!videoModerator.CreateAzureMediaServicesJobToModerateVideo(uploadVideoStreamRequest, uploadResult, generateVtt))
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
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine(Path.GetFileName(videoPath));
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
                Console.Write("\nUse V2 JSON? [y/n] : ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);
            v2Json = response == ConsoleKey.Y;
            if (!v2Json)
            {
                Console.Write("\nEnter Confidence Value between 0 to 1 : ");
                confidence = Console.ReadLine();
                Console.WriteLine();
                double outval;
                while (!double.TryParse(confidence, out outval) || outval > 1 || outval < 0)
                {
                    Console.WriteLine("\nPlease Enter value between 0 to 1 : ");
                    confidence = Console.ReadLine();
                }
            }
            else
            {
                confidence = "0";
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
            videoReviewApi = new VideoReviewApi(amsConfigurations, confidence);
        }
    }
}
