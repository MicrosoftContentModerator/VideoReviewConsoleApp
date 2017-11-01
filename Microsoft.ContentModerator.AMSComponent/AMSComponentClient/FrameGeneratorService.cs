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

        /// <summary>
        /// Instaiates an instance of Frame generator.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="confidenceVal"></param>
        public FrameGenerator(AmsConfigurations config)
        {
            _amsConfig = config;
        }

        /// <summary>
        /// Generates And Submit Frames
        /// </summary>
        /// <param name="assetInfo">assetInfo</param>
        /// <returns>Retruns Review Id</returns>
        public List<ProcessedFrameDetails> CreateVideoFrames(UploadAssetResult uploadAssetResult)
        {
            List<ProcessedFrameDetails> frameEventsList = new List<ProcessedFrameDetails>();
            PopulateFrameEvents(uploadAssetResult.ModeratedJson, frameEventsList, uploadAssetResult);
            return frameEventsList;
        }

        /// <summary>
        ///  GetGeneratedFrameList method used for Generating Frames using Moderated Json 
        /// </summary>
        /// <param name="eventsList">resultDownloaddetailsList</param>
        /// <param name="assetInfo"></param>
        public List<ProcessedFrameDetails> GenerateFrameImages(List<ProcessedFrameDetails> eventsList, UploadAssetResult assetInfo, string reviewId)
        {
            string frameStorageLocalPath = this._amsConfig.FfmpegFramesOutputPath + reviewId;
            Directory.CreateDirectory(frameStorageLocalPath);
            int batchSize = _amsConfig.FrameBatchSize;
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
            int frameProcessedCount = 0;
            int segmentCount = 0;
            string dirPath = string.Empty;
            foreach (var frame in eventsList)
            {
                if (frameProcessedCount % batchSize == 0)
                {
                    segmentCount = frameProcessedCount / batchSize;
                    dirPath = $"{frameStorageLocalPath}\\{segmentCount}";
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                }
                frameProcessedCount++;
                frame.FrameName = reviewId + frame.FrameName;
                TimeSpan ts = TimeSpan.FromSeconds(Convert.ToDouble(frame.TimeStamp / frame.TimeScale));
                var line = "-ss " + ts + " -i \"" + assetInfo.VideoFilePath + "\" -map " + frameCounter + ":v -frames:v 1 \"" + dirPath + "\\" + frame.FrameName + "\" ";
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
            using (var stw = new StreamWriter(AmsConfigurations.logFilePath, true))
            {
                stw.WriteLine("Frame Creation Elapsed time: {0}", sw.Elapsed);
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Frames(" + eventsList.Count() + ") created successfully.");
            DirectoryInfo di = new DirectoryInfo(frameStorageLocalPath);
            DirectoryInfo[] diArr = di.GetDirectories();
            Directory.CreateDirectory(frameStorageLocalPath + @"_zip");
            foreach (var dir in diArr)
            {
                ZipFile.CreateFromDirectory(dir.FullName, frameStorageLocalPath + $"_zip\\{dir.Name}.zip");
            }
            return eventsList;
        }
        /// <summary>
        /// Generates frames based on moderated json source.
        /// </summary>
        /// <param name="moderatedJsonstring">moderatedJsonstring</param>
        /// <param name="resultEventDetailsList">resultEventDetailsList</param>

        private void PopulateFrameEvents(string moderatedJsonstring, List<ProcessedFrameDetails> resultEventDetailsList, UploadAssetResult uploadResult)
        {
            var jsonModerateObject = JsonConvert.DeserializeObject<VideoModerationResult>(moderatedJsonstring);
            if (jsonModerateObject != null)
            {
                var timeScale = Convert.ToInt32(jsonModerateObject.TimeScale);
                int frameCount = 0;
                foreach (var item in jsonModerateObject.Fragments)
                {
                    if (item.Events != null)
                    {
                        foreach (var frameEventDetailList in item.Events)
                        {
                            foreach (FrameEventDetails frameEventDetails in frameEventDetailList)
                            {
                                var eventDetailsObj = new ProcessedFrameDetails
                                {
                                    ReviewRecommended = frameEventDetails.ReviewRecommended,
                                    TimeStamp = (frameEventDetails.TimeStamp * 1000 / timeScale) ,
                                    IsAdultContent = double.Parse(frameEventDetails.AdultScore) > _amsConfig.AdultFrameThreshold ? true : false,
                                    AdultScore = frameEventDetails.AdultScore,
                                    IsRacyContent = double.Parse(frameEventDetails.RacyScore) > _amsConfig.RacyFrameThreshold ? true : false,
                                    RacyScore = frameEventDetails.RacyScore,
                                    TimeScale = timeScale,
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
