using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ContentModerator.BusinessEntities.Entities;
using Microsoft.ContentModerator.BusinessEntities.Enum;
using Microsoft.ContentModerator.RESTUtilityServices;
using Microsoft.ContentModerator.BusinessEntities;
using Microsoft.ContentModerator.MediaStorage;
using Microsoft.ContentModerator.FFMPEG;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.ContentModerator.AMSComponent
{
    /// <summary>
    /// Represents AMS component.
    /// </summary>
    public class AmsComponent
    {
        AmsConfigurations _configObj = null;
        public AmsComponent(AmsConfigurations obj)
        {
            if (obj != null)
                _configObj = obj;
        }

        /// <summary>
        /// Instantiates an instance of AMSComponent.
        /// </summary>
        public AmsComponent()
        {
            _configObj = GetConfiguartions();
        }

        private AmsConfigurations GetConfiguartions()
        {
            return new AmsConfigurations();
        }
        private string CompressVideo(string videoPath)
        {
            string ffmpegBlobUrl;
            if (File.Exists(_configObj.FfmpegExecutablePath))
            {
                ffmpegBlobUrl = this._configObj.FfmpegExecutablePath;
            }
            else
            {
                DownloadFileFromBlob(_configObj.BlobFile, this._configObj.FfmpegExecutablePath);
                ffmpegBlobUrl = _configObj.FfmpegExecutablePath;
            }
            string videoFilePathCom = videoPath.Split('.')[0] + "_c.mp4";
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.FileName = ffmpegBlobUrl;
            processStartInfo.Arguments = "-i " + videoPath + " -vcodec libx265 -n -crf 32 -preset veryfast -vf scale=640:-1 -c:a aac -aq 1 -ac 2 -threads 0 " + videoFilePathCom;
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            process.Close();
            return videoFilePathCom;
        }
        private void DownloadFileFromBlob(string fileName, string path)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this._configObj.BlobConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(this._configObj.BlobContainerForFfmpeg);
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
        public bool ProcessVideoModeration(string videoFilePath, string confidenceVal, ref string reviewId, bool generateVtt)
        {
            if (ValidatePreRequisites())
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Console.WriteLine("\nVideo review process started...");
                videoFilePath = CompressVideo(videoFilePath);
                sw.Stop();
                using (var stw = new StreamWriter("AmsPerf.txt", true))
                {
                    stw.WriteLine("\n Compression Elapsed Time: {0}", sw.Elapsed);
                }

                UploadVideoStreamRequest streamRequest =
                    new UploadVideoStreamRequest
                    {
                        VideoStream = File.ReadAllBytes(videoFilePath),
                        VideoName = Path.GetFileName(videoFilePath),
                        EncodingRequest = new EncodingRequest()
                        {
                            EncodingBitrate = AmsEncoding.AdaptiveStreaming
                        },
                        VideoFilePath = videoFilePath
                    };
                reviewId = UploadAndModerateVideoByStream(streamRequest, confidenceVal, generateVtt);
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool ValidatePreRequisites()
        {
            return _configObj.CheckValidations();
        }

        /// <summary>
        /// Uploads and Moderated video by video stream.
        /// </summary>
        /// <param name="request">UploadVideoStreamRequest</param>
        /// <param name="confidenceVal">Confidence Value for filter</param>
        /// <returns>>Returns review id</returns>
        public string UploadAndModerateVideoByStream(UploadVideoStreamRequest request, string confidenceVal, bool generateVtt)
        {
            VideoReviewApi reviewApIobj = new VideoReviewApi(this._configObj);
            VideoModerator videoModerator = new VideoModerator(this._configObj);
            UploadAssetResult assetResultObj = new UploadAssetResult();

            string reviewId = string.Empty;
            using (var sw = new StreamWriter("AmsPerf.txt", true))
            {
                sw.WriteLine("File Size:{0} MB", ((double)request.VideoStream.Length / 1024 / 1024).ToString());
            }


            if (videoModerator.UploadAndModerate(request, ref assetResultObj, generateVtt))
            {
                FrameGenerator framegenerator = new FrameGenerator(_configObj, confidenceVal);
                List<FrameEventDetails> frameEntityList = framegenerator.GenerateAndSubmitFrames(assetResultObj, ref reviewId);
                reviewApIobj.ProcessReviewAPI(assetResultObj, frameEntityList, reviewId);
            }

            return reviewId;
        }
        
    }
}
