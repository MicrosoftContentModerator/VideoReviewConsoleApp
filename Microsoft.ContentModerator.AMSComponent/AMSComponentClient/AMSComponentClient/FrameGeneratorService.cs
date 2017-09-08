using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    /// Represents a FrameGeneratorService.
    /// </summary>
    public class FrameGenerator
    {
        private AmsConfigurations _amsConfig;
        private double _confidence;
        public CloudStorageAccount StorageAccount { get; set; } = null;

        /// <summary>
        /// Instaiates an instance of Frame generator.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="confidenceVal"></param>
        public FrameGenerator(AmsConfigurations config, string confidenceVal)
        {
            _amsConfig = config;
            StorageAccount = CloudStorageAccount.Parse(_amsConfig.BlobConnectionString);
            _confidence = Convert.ToDouble(confidenceVal);
        }

        /// <summary>
        /// Generates And Submit Frames
        /// </summary>
        /// <param name="assetInfo">assetInfo</param>
        /// <returns>Retruns Review Id</returns>
        public List<FrameEventDetails> CreateVideoFrames(UploadAssetResult uploadAssetResult)
        {
            List<FrameEventDetails> frameEventsList = new List<FrameEventDetails>();
            PopulateFrameEvents(uploadAssetResult.ModeratedJson, frameEventsList, uploadAssetResult);
            return frameEventsList;
        }

        /// <summary>
        ///  GetGeneratedFrameList method used for Generating Frames using Moderated Json 
        /// </summary>
        /// <param name="eventsList">resultDownloaddetailsList</param>
        /// <param name="assetInfo"></param>
        public List<FrameEventDetails> GenerateAndUploadFrameImages(List<FrameEventDetails> eventsList, UploadAssetResult assetInfo, string reviewId)
        {
            string frameStorageLocalPath = this._amsConfig.FfmpegFramesOutputPath + reviewId;
            Directory.CreateDirectory(frameStorageLocalPath);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nVideo Frames Creation inprogress...");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string ffmpegBlobUrl = string.Empty;
            if (File.Exists(_amsConfig.FfmpegExecutablePath))
            {
                ffmpegBlobUrl = _amsConfig.FfmpegExecutablePath;
            }

            List<string> args = new List<string>();
            StringBuilder sb = new StringBuilder();
            int frameCounter = 0;
            foreach (var frame in eventsList)
            {
                frame.FrameName = reviewId + frame.FrameName;
                TimeSpan ts = TimeSpan.FromSeconds(Convert.ToDouble(frame.TimeStamp / frame.TimeScale));
                var line = "-ss " + ts + " -i \"" + assetInfo.VideoFilePath + "\" -map " + frameCounter + ":v -frames:v 1 \"" + frameStorageLocalPath + "\\" + frame.FrameName + "\" ";
                frameCounter++;
                sb.Append(line);
                if (sb.Length > 30000)
                {
                    args.Add(sb.ToString());
                    sb.Clear();
                    frameCounter = 0;
                }
            }
            if (sb.Length != 0)
            {
                args.Add(sb.ToString());
            }

            Parallel.ForEach(args, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                arg => CreateTaskProcess(arg, ffmpegBlobUrl));

            sw.Stop();
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine("Frame Creation Elapsed time: {0}", sw.Elapsed);
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Frames(" + eventsList.Count() + ") created successfully.");


            //Parallel.ForEach(eventsList,
            //    new ParallelOptions { MaxDegreeOfParallelism = 4 },
            //    evnt => AddFrameToBlobGenerationProcess(evnt, frameStorageLocalPath + "\\" + evnt.FrameName));
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Frames(" + eventsList.Count() + ") uploaded successfully ");
            Directory.CreateDirectory(frameStorageLocalPath + @"_zip");
            ZipFile.CreateFromDirectory(frameStorageLocalPath, frameStorageLocalPath + @"_zip\frameZip.zip");
            return eventsList;
        }
        /// <summary>
        /// Generates frames based on moderated json source.
        /// </summary>
        /// <param name="moderatedJsonstring">moderatedJsonstring</param>
        /// <param name="resultEventDetailsList">resultEventDetailsList</param>

        private void PopulateFrameEvents(string moderatedJsonstring, List<FrameEventDetails> resultEventDetailsList, UploadAssetResult uploadResult)
        {
            if (uploadResult.V2JSONPath != null)
            {
                try
                {
                    using (var streamReader = new StreamReader(uploadResult.V2JSONPath))
                    {
                        string jsonv2 = streamReader.ReadToEnd();
                        moderatedJsonstring = jsonv2;
                    }

                }
                catch (Exception)
                {
                    Console.WriteLine("Json file associated with video is not present. V2 Json needs to be in the same folder as video with same name with .json extension.");
                    throw;
                }
                var moderatedJsonV2 = JsonConvert.DeserializeObject<VideoModerationResult>(moderatedJsonstring);

                if (moderatedJsonV2.Shots != null)
                {
                    int timescale = Convert.ToInt32(moderatedJsonV2.TimeScale);
                    int frameCount = 0;
                    foreach (var shot in moderatedJsonV2.Shots)
                    {
                        if (shot.Clips != null)
                        {
                            foreach (var clip in shot.Clips)
                            {
                                if (clip.Frames != null)
                                {

                                    foreach (var frameObj in clip.Frames)
                                    {
                                        if (Convert.ToDouble(frameObj.AdultConfidence) > _confidence)
                                        {
                                            var eventDetailsObj = new FrameEventDetails
                                            {
                                                TimeStamp = frameObj.TimeStamp,
                                                IsAdultContent = frameObj.IsAdultContent,
                                                AdultConfidence = frameObj.AdultConfidence,
                                                Index = frameObj.Index,
                                                TimeScale = timescale,
                                                IsRacyContent = frameObj.IsRacyContent,
                                                RacyConfidence = frameObj.RacyConfidence,

                                            };
                                            frameCount++;
                                            eventDetailsObj.FrameName = "_" + frameCount + ".png";
                                            resultEventDetailsList.Add(eventDetailsObj);

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var jsonModerateObject = JsonConvert.DeserializeObject<VideoModerationResult>(moderatedJsonstring);

                if (jsonModerateObject != null)
                {
                    var timeScale = Convert.ToString(jsonModerateObject.TimeScale);
                    var timescale = Convert.ToInt32(timeScale);

                    int frameCount = 0;
                    foreach (var item in jsonModerateObject.Fragments)
                    {
                        if (item.Events != null)
                        {
                            foreach (var events in item.Events)
                            {
                                foreach (FrameEventDetails eventObj in events)
                                {
                                    if (Convert.ToDouble(eventObj.AdultConfidence) > _confidence)
                                    {
                                        var eventDetailsObj = new FrameEventDetails
                                        {
                                            TimeStamp = eventObj.TimeStamp,
                                            IsAdultContent = eventObj.IsAdultContent,
                                            AdultConfidence = eventObj.AdultConfidence,
                                            Index = eventObj.Index,
                                            TimeScale = timescale
                                        };
                                        frameCount++;
                                        eventDetailsObj.FrameName = "_" + frameCount + ".png";
                                        resultEventDetailsList.Add(eventDetailsObj);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Frame generation using ffmpeg
        /// </summary>
        /// <param name="eventTimeStamp"></param>
        /// <param name="keyframefolderpath"></param>
        /// <param name="timescale"></param>
        /// <param name="framename"></param>
        /// <param name="ffmpegBlobUrl"></param>
        private void CreateTaskProcess(string arg, string ffmpegBlobUrl)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.FileName = ffmpegBlobUrl;
            processStartInfo.Arguments = arg;
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
        }
    }
}
