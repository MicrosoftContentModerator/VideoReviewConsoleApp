using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.ContentModerator.BusinessEntities;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.ContentModerator.BusinessEntities.Entities;
using Microsoft.ContentModerator.BusinessEntities.CustomExceptions;
using Microsoft.ContentModerator.Services;
using Microsoft.ContentModerator.RESTUtilityServices;
using System.Text;

namespace Microsoft.ContentModerator.FFMPEG
{

    /// <summary>
    /// Represents a FrameGeneratorService.
    /// </summary>
    public class FrameGenerator
    {
        private AmsConfigurations _amsConfig;
        private string _videoPublishUri = string.Empty;
        private string _videoName = string.Empty;
        private string _videoContainerName = string.Empty;
        private double _confidence;
        CloudBlobClient _blobClient = null;
        string _blobContainerName = string.Empty;
        CloudBlobContainer _container = null;

        List<FrameEventDetails> _frameEventsSource = null;
        private VideoReviewApi _reviewApIobj = null;

        public CloudStorageAccount StorageAccount { get; set; } = null;

        /// <summary>
        /// Instaiates an instance of Frame generator.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="confidenceVal"></param>
        public FrameGenerator(AmsConfigurations config, string confidenceVal)
        {
            this._amsConfig = config;
            _reviewApIobj = new VideoReviewApi(config);
            _frameEventsSource = new List<FrameEventDetails>();
            StorageAccount = CloudStorageAccount.Parse(this._amsConfig.BlobConnectionString);
            _blobClient = StorageAccount.CreateCloudBlobClient();
            _confidence = Convert.ToDouble(confidenceVal);
        }

        #region Generate Frames
        /// <summary>
        /// Generates And Submit Frames
        /// </summary>
        /// <param name="assetInfo">assetInfo</param>
        /// <returns>Retruns Review Id</returns>
        public List<FrameEventDetails> GenerateAndSubmitFrames(UploadAssetResult assetInfo)
        {
            List<FrameEventDetails> frameEventsList = new List<FrameEventDetails>();

            try
            {
                _videoPublishUri = assetInfo.VideoFilePath ?? assetInfo.StreamingUrlDetails.DownloadUri;

                _videoName = AppendTimeStamp(assetInfo.VideoName);

                if (!string.IsNullOrEmpty(assetInfo.VideoName))
                {
                    _videoContainerName = assetInfo.VideoName;
                    string[] containerName = _videoContainerName.Split('.');
                    _videoContainerName = containerName[0].ToString();
                }
                else
                    _videoContainerName = this._amsConfig.BlobContainerName;

                _blobContainerName = Regex.Replace(_videoContainerName, @"[^0-9a-zA-Z]+", "a");
                _blobContainerName = AppendTimeStamp(_blobContainerName.ToLower());

                _container = _blobClient.GetContainerReference(_blobContainerName);
                _container.CreateIfNotExists();
                _container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                PopulateFrameEvents(assetInfo.ModeratedJson, frameEventsList);

                return GenerateAndUploadFrameImages(frameEventsList);
            }
            catch (Exception ex)
            {
                throw new FrameGenerationException()
                {
                    ReviewId = string.Empty,
                    VideoName = assetInfo.VideoName,
                    AssetId = assetInfo.AssetId,
                    ErrorTitle = Constants.ErrorTitle,
                    ErrorReason = ex.Message
                };
            }
        }

        #endregion

        #region FrameImage Generation

        /// <summary>
        /// Generates frames based on moderated json source.
        /// </summary>
        /// <param name="moderatedJsonstring">moderatedJsonstring</param>
        /// <param name="resultEventDetailsList">resultEventDetailsList</param>

        private void PopulateFrameEvents(string moderatedJsonstring, List<FrameEventDetails> resultEventDetailsList)
        {
            if (UploadAssetResult.V2JSONPath != null)
            {
                try
                {
                    using (var streamReader = new StreamReader(UploadAssetResult.V2JSONPath))
                    {
                        string jsonv2 = streamReader.ReadToEnd();
                        moderatedJsonstring = jsonv2;
                    }

                }
                catch
                {
                    Console.WriteLine("Json file associated with video is not present. V2 Json needs to be in the same folder as video with same name with .json extension.");
                }
                var moderatedJsonV2 = JsonConvert.DeserializeObject<VideoModerationResult>(moderatedJsonstring);

                if (moderatedJsonV2.Shots != null)
                {
                    double ticks = Convert.ToDouble(moderatedJsonV2.TimeScale);
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
                                            eventDetailsObj.FrameName = _videoName.Split('.')[0] + "_" + frameCount + ".png";
                                            resultEventDetailsList.Add(eventDetailsObj);

                                        }

                                        //for (int i = 0; i < clip.Frames.Count(); i++, index++)
                                        //{
                                        //	FrameEventDetails frameEventDetails = new FrameEventDetails();
                                        //	frameEventDetails.TimeStamp = clip.Frames[i].TimeStamp;
                                        //	frameEventDetails.IsAdultContent = clip.Frames[i].IsAdultContent;
                                        //	frameEventDetails.AdultConfidence = clip.Frames[i].AdultConfidence.ToString();
                                        //	frameEventDetails.Index = clip.Frames[i].Index;
                                        //	frameEventDetails.TimeScale = timescale;
                                        //	frameEventDetails.FrameOrderId = index.ToString();
                                        //	frameEventDetails.FrameName = _videoName + "_" + index + ".png";
                                        //	frameEventDetails.IsRacyContent = clip.Frames[i].IsRacyContent;
                                        //	frameEventDetails.RacyConfidence = clip.Frames[i].RacyConfidence;
                                        //	resultEventDetailsList.Add(frameEventDetails);

                                        //}
                                        // }
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
                                        eventDetailsObj.FrameName = _videoName + "_" + frameCount + ".png";
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
        ///  GetGeneratedFrameList method used for Generating Frames using Moderated Json 
        /// </summary>
        /// <param name="eventsList">resultDownloaddetailsList</param>
        private List<FrameEventDetails> GenerateAndUploadFrameImages(List<FrameEventDetails> eventsList)
        {
            #region frameCreation

            string frameStorageLocalPath = this._amsConfig.FfmpegFramesOutputPath + _videoName.Split('.')[0];
            Directory.CreateDirectory(frameStorageLocalPath);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n Frames Creation in progress");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            #region Check FFMPEG.Exe

            string ffmpegBlobUrl;

            if (File.Exists(this._amsConfig.FfmpegExecutablePath))
            {
                ffmpegBlobUrl = this._amsConfig.FfmpegExecutablePath;
            }
            else
            {
                DownloadFileFromBlob(this._amsConfig.BlobFile, this._amsConfig.FfmpegExecutablePath);
                ffmpegBlobUrl = this._amsConfig.FfmpegExecutablePath;
            }

            #endregion
            List<string> args = new List<string>();
            var timeScale = eventsList.First().TimeScale;
            StringBuilder sb = new StringBuilder();
            int frameCounter = 0;
            foreach (var frame in eventsList)
            {
                TimeSpan ts = TimeSpan.FromSeconds(Convert.ToDouble(frame.TimeStamp / timeScale));
                var line = "-ss " + ts + " -i " + _videoPublishUri + " -map " + frameCounter + ":v -frames:v 1 " + frameStorageLocalPath + "\\" + frame.FrameName + " ";
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



            //         int seqCount = eventsList.Count() % 5;
            //for (int i = 0; i < (eventsList.Count() - seqCount) && (eventsList.Count() - seqCount) >= 0; i = i + 5)
            //{
            //             Parallel.Invoke(
            //				() => CreateTaskProcess(eventsList[i].TimeStamp, frameStorageLocalPath, eventsList[i].TimeScale, eventsList[i].FrameName, ffmpegBlobUrl),
            //				() => CreateTaskProcess(eventsList[i + 1].TimeStamp, frameStorageLocalPath, eventsList[i + 1].TimeScale, eventsList[i + 1].FrameName, ffmpegBlobUrl),
            //				() => CreateTaskProcess(eventsList[i + 2].TimeStamp, frameStorageLocalPath, eventsList[i + 2].TimeScale, eventsList[i + 2].FrameName, ffmpegBlobUrl),
            //				() => CreateTaskProcess(eventsList[i + 3].TimeStamp, frameStorageLocalPath, eventsList[i + 3].TimeScale, eventsList[i + 3].FrameName, ffmpegBlobUrl),
            //				() => CreateTaskProcess(eventsList[i + 4].TimeStamp, frameStorageLocalPath, eventsList[i + 4].TimeScale, eventsList[i + 4].FrameName, ffmpegBlobUrl)
            //			  );
            //}

            //if (eventsList.Count() - seqCount > 0)
            //{
            //	Task[] tasks = new Task[seqCount];
            ////	int counter;
            //	for (int i = (eventsList.Count() - seqCount), j = 0; i < eventsList.Count(); i++, j++)
            //	{
            //		int counter = i;
            //		tasks[j] = new Task(() => CreateTaskProcess(eventsList[counter].TimeStamp, frameStorageLocalPath, eventsList[counter].TimeScale, eventsList[counter].FrameName, ffmpegBlobUrl));
            //	}

            //	foreach (Task task in tasks)
            //	{
            //		task.Start();
            //	}

            //	Task.WaitAll(tasks);
            //}
            sw.Stop();
            Console.WriteLine("\n Elapsed time: {0}", sw.Elapsed);
            using (var stw = new StreamWriter("AmsPerf.txt", true))
            {
                stw.WriteLine("Frame Creation Elapsed time: {0}", sw.Elapsed);
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(" Frames(" + eventsList.Count() + ") created successfully");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n Uploading Frames to BLOB in progress");

            //Parallel.For(0, eventsList.Count(), i =>
            //{
            //	AddFrameToBlobGenerationProcess(eventsList[i], frameStorageLocalPath + "\\" + eventsList[i].FrameName);
            //});

            Parallel.ForEach(eventsList,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                evnt => AddFrameToBlobGenerationProcess(evnt, frameStorageLocalPath + "\\" + evnt.FrameName));

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(" Frames(" + eventsList.Count() + ") uploaded to BLOB successfully ");

            if (Directory.Exists(frameStorageLocalPath))
            {
                Directory.Delete(frameStorageLocalPath, true);
            }

            #endregion

            return eventsList;
        }

        /// <summary>
        /// Upload frames to blob
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private string AddFrameToBlobGenerationProcess(FrameEventDetails frame, string imagePath)
        {

            FileInfo fileInfo = new FileInfo(imagePath);


            if (fileInfo != null && fileInfo.Length > 0)
            {
                byte[] imageData = null;
                long imageFileLength = fileInfo.Length;
                using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader br = new BinaryReader(fs);
                    imageData = br.ReadBytes((int)imageFileLength);
                }

                using (Stream stream = new MemoryStream(imageData))
                {
                    CloudBlockBlob blockBlob = _container.GetBlockBlobReference(Path.GetFileName(imagePath));
                    blockBlob.UploadFromStream(stream);
                    frame.PrimaryUri = blockBlob.StorageUri.PrimaryUri.ToString();
                }
            }

            return frame.PrimaryUri;
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
            var process = System.Diagnostics.Process.Start(processStartInfo);
            process.WaitForExit();
        }

        /// <summary>
        /// Download ffmpeg exe to local
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="path"></param>
        private void DownloadFileFromBlob(string fileName, string path)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this._amsConfig.BlobConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(this._amsConfig.BlobContainerForFfmpeg);
            container.CreateIfNotExists();
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            Stream stream = new MemoryStream();
            blockBlob.DownloadToStream(stream);
            stream.Position = 0;
            if (stream != null)
            {
                FileStream fileStream = File.Create(path);
                stream.Position = 0;
                stream.CopyTo(fileStream);
                fileStream.Close();
            }
        }

        private string AppendTimeStamp(string fileName)
        {
            return string.Concat(
                Path.GetFileNameWithoutExtension(fileName),
                DateTime.Now.ToString("yyMMddHHMMssfff"),
                Path.GetExtension(fileName)
                );
        }

        #endregion


    }
}
