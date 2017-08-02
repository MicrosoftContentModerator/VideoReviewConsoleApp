using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;


namespace Microsoft.ContentModerator.AMSComponentClient
{
    class Program
    {
        static string videoPath = string.Empty;
        static string confidence = string.Empty;
        static bool generateVtt = false;


        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            GetUserInputs();

            AmsComponent amsComponent = new AmsComponent();
            AmsConfigurations amsConfigurations = new AmsConfigurations();

            VideoModerator videoModerator = new VideoModerator(amsConfigurations);
            UploadAssetResult uploadResult = new UploadAssetResult();
            FrameGenerator frameGenerator = new FrameGenerator(amsConfigurations, confidence);
            VideoReviewApi videoReviewApi = new VideoReviewApi(amsConfigurations);

            var compressedVideoPath = amsComponent.CompressVideo(videoPath);
            UploadVideoStreamRequest uploadVideoStreamRequest = CreateVideoStreamingRequest(compressedVideoPath);
            string reviewId = string.Empty;

            Console.WriteLine("\nVideo review process started...");

            if (videoModerator.UploadAndModerate(uploadVideoStreamRequest, ref uploadResult, generateVtt))
            {
                List<FrameEventDetails> frameEntityList = frameGenerator.GenerateAndSubmitFrames(uploadResult, ref reviewId);
                videoReviewApi.ProcessReviewAPI(uploadResult, frameEntityList, reviewId);

            }

            Console.WriteLine("\nVideo review successfully completed...");

            sw.Stop();
            Console.WriteLine("\nTotal Elapsed Time: {0}", sw.Elapsed);
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine("Total Elapsed Time: {0}", sw.Elapsed);
            }
            Console.ReadLine();
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

        private static  void  GetUserInputs()
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
            bool v2 = response == ConsoleKey.Y;
            if (v2)
            {
                string filepath = videoPath.Replace(".mp4", ".json");
                UploadAssetResult.V2JSONPath = filepath;
                confidence = "0";
            }
            else
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

     
    }
}
