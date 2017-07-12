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


            string videoPath = Console.ReadLine().Replace("\"", "");
            while (!File.Exists(videoPath))
            {
                Console.WriteLine("\nPlease Enter Valid File path");
                videoPath = Console.ReadLine();

            }

            string confidence = "0";
            string filepath = videoPath.Replace(".mp4", ".json");
            UploadAssetResult.V2JSONPath = filepath;
            try
            {
                AmsComponent amsComponent = new AmsComponent();
                string reviewId = string.Empty;
                if (amsComponent.ProcessVideoModeration(videoPath, confidence, ref reviewId))
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
            Console.WriteLine("Total Elapsed Time: {0}",sw.Elapsed);
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine("\nTotal Elapsed Time: {0}", sw.Elapsed);
            }
            Console.ReadLine();
        }
    }
}
