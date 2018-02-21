using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
                AmsConfigurations.logFilePath = Path.Combine(Path.GetDirectoryName(videoPath), "log.txt");
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
                AmsConfigurations.logFilePath = Path.Combine(args[0], "log.txt");
                ConsoleKey response;
                do
                {
                    Console.Write("Create demo reviews? [y/n] : \n");
                    response = Console.ReadKey(false).Key;
                    if (response != ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                    }
                } while (response != ConsoleKey.Y && response != ConsoleKey.N);
                if (response == ConsoleKey.Y)
                {
                    CreateDemoVideoReviews();
                }
                else
                {
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
        }

        private static async Task ProcessVideo(string videoPath)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
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
                Console.WriteLine("\nVideo moderation process failed.");
            }

            Console.WriteLine("\nVideo moderation process completed...");
            Console.WriteLine("\nVideo review process started...");

            string reviewId = await videoReviewApi.CreateVideoReviewInContentModerator(uploadResult);

            watch.Stop();

            Console.WriteLine("\nVideo review successfully completed...");
            Console.WriteLine("\nTotal Elapsed Time: {0}", watch.Elapsed);
            Logger.Log("Video File Name: " + Path.GetFileName(videoPath));
            Logger.Log($"ReviewId: {reviewId}");
            Logger.Log($"Total Elapsed Time: {watch.Elapsed}");
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
        private static void CreateDemoVideoReviews()
        {
            List<string> demoVideoNames = new List<string>() { "hololens", "satya", "office365", "windows10", "surface" };
            string containerUrl = AmsConfigurations.DemoVideoContainerUrl;
            foreach (string video in demoVideoNames)
            {
                using (WebClient client = new WebClient())
                {
                    var urlPrefix = containerUrl + $"{video}/{video}";
                    try
                    {
                        string reviewRequestBody = client.DownloadString($"{urlPrefix}.json");
                        byte[] vttFile = client.DownloadData($"{urlPrefix}.vtt");

                        var reviewIds = videoReviewApi.ExecuteCreateReviewApi(reviewRequestBody).Result;
                        if (reviewIds != null)
                        {
                            string reviewId = reviewIds.FirstOrDefault();
                            var oRes = videoReviewApi.AddVideoTranscript(reviewId, vttFile).Result;
                            if (oRes.Response.IsSuccessStatusCode)
                            {
                                if (videoReviewApi.PublishReview(reviewId))
                                {
                                    Console.WriteLine($"Demo Video Review for {video}.mp4 has been created.");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
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
