using System;
using System.IO;
using System.Diagnostics;
using Microsoft.ContentModerator.BusinessEntities.Entities;
using Microsoft.ContentModerator.AMSComponent;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.Write("\nEnter the fully qualified local path for Uploading the video : ");

            ConsoleKey response;
            string confidence = string.Empty;
            string videoPath = Console.ReadLine().Replace("\"", ",");

            while (!File.Exists(videoPath))
            {
                Console.Write("\nPlease Enter Valid File path : ");
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
                double outval;
                while (!double.TryParse(confidence, out outval) || outval > 1 || outval < 0)
                {
                    Console.Write("\nPlease Enter value between 0 to 1 : ");
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
            bool generateVtt = response == ConsoleKey.Y;

            VideoReviewProcess(videoPath, confidence, generateVtt);
            sw.Stop();
            Console.WriteLine("\nTotal Elapsed Time: {0}", sw.Elapsed);
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine("Total Elapsed Time: {0}", sw.Elapsed);
            }
            Console.ReadLine();
        }

        private static void VideoReviewProcess(string videoPath, string confidence, bool generateVtt)
        {
            try
            {
                AmsComponent amsComponent = new AmsComponent();
                string reviewId = string.Empty;
                if (amsComponent.ProcessVideoModeration(videoPath, confidence, ref reviewId, generateVtt))
                {
                    if (!string.IsNullOrEmpty(reviewId))
                        Console.WriteLine("Review Id: " + reviewId);
                    else
                        Console.WriteLine("Failed to generate Review Id");
                }
                else
                {
                    Console.WriteLine("Configurations check failed .. Please cross check the configurations");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Video Review process failed" + e.Message.ToString());
                //TODO :Logging
            }
        }
    }
}
