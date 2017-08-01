using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ContentModerator.BusinessEntities;
using Microsoft.ContentModerator.AMSComponent;
using System.IO;
using Microsoft.ContentModerator.BusinessEntities.CustomExceptions;
using Microsoft.ContentModerator.BusinessEntities.Entities;
using Microsoft.ContentModerator.BusinessEntities.Enum;
using System.Diagnostics;

namespace AMSComponentClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("\nEnter the fully qualified local path for Uploading the video \n ");

            ConsoleKey response;
            string confidence = string.Empty;
            string videoPath = Console.ReadLine().Replace("\"", "");
            while (!File.Exists(videoPath))
            {
                Console.WriteLine("\nPlease Enter Valid File path");
                videoPath = Console.ReadLine();
            }

            //string confidence = "0";
            //string filepath = videoPath.Replace(".mp4", ".json");
            
            do
            {
                Console.WriteLine("\nUse V2 JSON? [y/n]");
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
                while (!File.Exists(filepath))
                {
                    Console.WriteLine("\n V2 Json needs to be in the same folder as the video, with same file name and .json extension.");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
            else
            {
                Console.WriteLine("\nEnter Confidence Value between 0 to 1");
                confidence = Console.ReadLine();
                double outval;
                while (!double.TryParse(confidence, out outval) || outval > 1 || outval < 0)
                {
                    Console.WriteLine("\nPlease Enter value between 0 to 1");
                    confidence = Console.ReadLine();
                }
            }
            do
            {
                Console.WriteLine("Generate Video Transcript? [y/n]");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }

            } while (response != ConsoleKey.Y && response != ConsoleKey.N);
            bool generateVtt = response == ConsoleKey.Y;

            try
            {
                AmsComponent amsComponent = new AmsComponent();
                string reviewId = string.Empty;
                if (amsComponent.ProcessVideoModeration(videoPath, confidence, ref reviewId, generateVtt))
                {
                    if (!string.IsNullOrEmpty(reviewId))
                        Console.WriteLine(" Review Id:" + reviewId);
                    else
                        Console.WriteLine(" Failed to generate Review Id");
                }
                else
                {
                    Console.WriteLine("Configurations check failed .. Please cross check the configurations");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to process the video .." + e.ToString());
                //TODO :Logging
            }
            sw.Stop();
            Console.WriteLine("\nTotal Elapsed Time: {0}", sw.Elapsed);
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine("Total Elapsed Time: {0}", sw.Elapsed);
            }
            Console.ReadLine();
        }
    }
}
