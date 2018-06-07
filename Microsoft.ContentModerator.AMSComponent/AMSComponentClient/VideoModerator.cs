using Microsoft.WindowsAzure.MediaServices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    /// Encapsulates all media releated operations such as upload a video into AMS,encoding, encrypting,moderation of a Video asset.
    /// </summary>
    public class VideoModerator
    {
        private CloudMediaContext _mediaContext;
        AmsConfigurations _amsConfigurations = null;
        private string _processedAssetName = null;
        IAsset asset;

        /// <summary>
        /// Instantiates a Video Moderator instance.
        /// </summary>
        public VideoModerator(AmsConfigurations configObj)
        {

            _amsConfigurations = configObj;
            InitializeMediaContext(configObj);
        }

        #region Asset Operations

        /// <summary>
        /// Initailzes Media Context.
        /// </summary>
        /// <param name="configObj">AMSConfigurations</param>
        private void InitializeMediaContext(AmsConfigurations configObj)
        {
            var tokenCredentials = new AzureAdTokenCredentials(configObj.AzureAdTenentName,
                new AzureAdClientSymmetricKey(configObj.ClientId, configObj.ClientSecret),
                AzureEnvironments.AzureCloudEnvironment);
            var tokenProvider = new AzureAdTokenProvider(tokenCredentials);
            _mediaContext = new CloudMediaContext(new Uri(configObj.MediaServiceRestApiEndpoint), tokenProvider);
        }

        /// <summary>
        /// Creates an Asset
        /// </summary>
        /// <param name="uploadVideoRequest">uploadVideoRequest</param>
        /// <returns>Returns a asset</returns>
        public IAsset CreateAsset(UploadVideoStreamRequest uploadVideoRequest)
        {
            _processedAssetName = uploadVideoRequest.VideoName;
            var assetName = Path.GetFileNameWithoutExtension(_processedAssetName);

            asset = _mediaContext.Assets.Create(assetName, AssetCreationOptions.None);
            var assetFile = asset.AssetFiles.Create(_processedAssetName);

            using (Stream stream = new MemoryStream(uploadVideoRequest.VideoStream))
            {
                var policy = _mediaContext.AccessPolicies.Create(
                    assetName,
                    TimeSpan.FromDays(this._amsConfigurations.StreamingUrlActiveDays),
                    AccessPermissions.List | AccessPermissions.Read);

                var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, policy);
                assetFile.Upload(stream);
                locator.Delete();
                policy.Delete();
            }

            return asset;
        }


        /// <summary>
        /// Encodes an Asset
        /// </summary>
        /// <param name="asset">asset </param>
        /// <param name="encoding">encoding request params </param>
        /// <returns>encoded asset</returns>
        private void ConfigureEncodeAssetTask(EncodingRequest encoding, IJob job)
        {
            AmsEncoding amsEncoding = encoding.EncodingBitrate;
            string encodingType = GetDescription(amsEncoding);
            IMediaProcessor processor = GetLatestMediaProcessorByName(_amsConfigurations.MediaProcessor);
            if (processor == null)
            {
                throw new Exception("Please check the configuration values, some configuration values are not matching.");
            }
            ITask task = job.Tasks.AddNew(_processedAssetName + " encoding task", processor, encodingType, TaskOptions.None);
            // Specify the input asset to be encoded.
            task.InputAssets.Add(asset);
            task.OutputAssets.AddNew(asset.Name + " media streaming", AssetCreationOptions.None);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputAsset"></param>
        /// <param name="outputFolder"></param>
        private void DownloadAssetToLocal(IAsset outputAsset, string outputFolder)
        {

            IAccessPolicy accessPolicy = _mediaContext.AccessPolicies.Create("File Download Policy", TimeSpan.FromDays(30), AccessPermissions.Read);
            ILocator locator = _mediaContext.Locators.CreateSasLocator(outputAsset, accessPolicy);
            BlobTransferClient blobTransfer = new BlobTransferClient
            {
                NumberOfConcurrentTransfers = 10,
                ParallelTransferThreadCount = 10
            };
            var downloadTasks = new List<Task>();
            foreach (IAssetFile outputFile in outputAsset.AssetFiles)
            {
                if (outputFile.MimeType == "text/vtt")
                {
                    string localDownloadPath = Path.Combine(outputFolder, outputFile.Name);
                    downloadTasks.Add(outputFile.DownloadAsync(Path.GetFullPath(localDownloadPath), blobTransfer, locator, CancellationToken.None));

                }
            }
            Task.WaitAll(downloadTasks.ToArray());
        }
        /// <summary>
        /// GetLatestMediaProcessorByName Method used for Getting Media Processor
        /// </summary>
        /// <param name="mediaProcessorName"></param>
        /// <returns>IMediaProcessor object</returns>
        private IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = this._mediaContext.MediaProcessors.Where(p => p.Name == mediaProcessorName).
            ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();
            if (processor == null)
            {
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));
            }
            return processor;
        }

        /// <summary>
        /// PublishAsset - Publishes the asset with global descriptor to enable the asset for streaming.
        /// </summary>
        /// <param name="asset">asset</param>
        /// <returns>Returns list of streaming uri's for all available streaming formats</returns>
        public PublishedUrlDetails PublishAsset(IAsset asset)
        {
            PublishedUrlDetails publishedUrls = new PublishedUrlDetails();
            var assetFile = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();

            IAccessPolicy policy = _mediaContext.AccessPolicies.Create("Streaming policy", TimeSpan.FromDays(365), AccessPermissions.Read);
            ILocator originLocator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, policy, DateTime.UtcNow.AddMinutes(-5));

            Uri smoothStreamingUri = asset.GetSmoothStreamingUri();
            Uri hlsUri = asset.GetHlsUri();
            Uri mpegDashUri = asset.GetMpegDashUri();
            Uri hlsv3Uri = asset.GetHlsv3Uri();

            publishedUrls.SmoothUrl = smoothStreamingUri != null ? smoothStreamingUri.ToString().Replace("http://", "https://") : " ";
            publishedUrls.Hlsv4Uri = hlsUri != null ? hlsUri.ToString().Replace("http://", "https://") : " ";
            publishedUrls.Hlsv3Uri = hlsv3Uri != null ? hlsv3Uri.ToString().Replace("http://", "https://") : " ";
            publishedUrls.MpegDashUri = mpegDashUri != null ? mpegDashUri.ToString().Replace("http://", "https://") : " ";

            if (assetFile != null)
            {
                publishedUrls.UrlWithOriginLocator = originLocator.Path.Replace("http://", "https://") + assetFile.Name +
                                                     "/manifest";
            }
            return publishedUrls;
        }

        /// <summary>
        /// PublishAssetGetURLs - Published the asset to AMS and returns the download Uri
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private string GenerateDownloadUrl(IAsset asset, string videoname)
        {
            List<Uri> progressiveDownloadUris = new List<Uri>();
            var ext = Path.GetExtension(videoname);

            _mediaContext.Locators.Create(LocatorType.OnDemandOrigin, asset, AccessPermissions.Read, TimeSpan.FromDays(_amsConfigurations.StreamingUrlActiveDays));
            _mediaContext.Locators.Create(LocatorType.Sas, asset, AccessPermissions.Read, TimeSpan.FromDays(_amsConfigurations.StreamingUrlActiveDays));
            IEnumerable<IAssetFile> assetFilesList = asset.AssetFiles.ToList().Where(af => af.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

            var assetFiles = assetFilesList as IAssetFile[] ?? assetFilesList.ToArray();
            if (assetFiles.Length > 0)
            {
                progressiveDownloadUris = assetFiles.Select(assetfile => assetfile.GetSasUri()).ToList();
            }

            return progressiveDownloadUris.Count > 0 ? progressiveDownloadUris[0].AbsoluteUri.ToString() : "";
        }

        /// <summary>
        /// Gets the description value of encoding enum
        /// </summary>
        /// <param name="value">value</param>
        /// <returns>description name of an enum value</returns>
        private string GetDescription(AmsEncoding value)
        {
            var encodingtype = value.GetType();
            var encoding = encodingtype.GetField(value.ToString());
            var descriptions = encoding.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            return descriptions.Length > 0 ? descriptions[0].Description : value.ToString();
        }

        public string GetCmDetail(IAsset asset)
        {
            string fileName = DownloadModeratedJsonFile(asset, this._amsConfigurations.ModeratedJsonOutputPath);
            string filePath = Path.Combine(this._amsConfigurations.ModeratedJsonOutputPath, fileName);

            var moderatedJson = System.IO.File.ReadAllText(filePath);
            string[] outfolderPath = Directory.GetFiles(this._amsConfigurations.ModeratedJsonOutputPath, fileName);

            if ((File.Exists(outfolderPath[0])))
            {
                File.Delete(outfolderPath[0]);
            }

            return moderatedJson;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uploadVideoRequest"></param>
        /// <param name="uploadResult"></param>
        /// <returns></returns>
        /// 
        public bool CreateAzureMediaServicesJobToModerateVideo(UploadVideoStreamRequest uploadVideoRequest, UploadAssetResult uploadResult)
        {
            asset = CreateAsset(uploadVideoRequest);
            uploadResult.VideoName = uploadVideoRequest.VideoName;
            // Encoding the asset , Moderating the asset, Generating transcript in parallel
            IAsset encodedAsset = null;
            //Creates the job for the tasks.
            IJob job = this._mediaContext.Jobs.Create("AMS Review Job");

            //Adding encoding task to job.
            ConfigureEncodeAssetTask(uploadVideoRequest.EncodingRequest, job);

            ConfigureContentModerationTask(job);

            //adding transcript task to job.
            if (uploadResult.GenerateVTT)
            {
                ConfigureTranscriptTask(job);
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            //submit and execute job.
            job.Submit();
            job.GetExecutionProgressTask(new CancellationTokenSource().Token).Wait();
            watch.Stop();
            Logger.Log($"AMS Job Elapsed Time: {watch.Elapsed}");

            if (job.State == JobState.Error)
            {
                throw new Exception("Video moderation has failed due to AMS Job error.");
            }

            UploadAssetResult result = uploadResult;
            encodedAsset = job.OutputMediaAssets[0];
            result.ModeratedJson = GetCmDetail(job.OutputMediaAssets[1]);
            // Check for valid Moderated JSON
            var jsonModerateObject = JsonConvert.DeserializeObject<VideoModerationResult>(result.ModeratedJson);

            if (jsonModerateObject == null)
            {
                return false;
            }
            if (uploadResult.GenerateVTT)
            {
                GenerateTranscript(job.OutputMediaAssets.Last());
            }

            uploadResult.StreamingUrlDetails = PublishAsset(encodedAsset);
            string downloadUrl = GenerateDownloadUrl(asset, uploadVideoRequest.VideoName);
            uploadResult.StreamingUrlDetails.DownloadUri = downloadUrl;
            uploadResult.VideoName = uploadVideoRequest.VideoName;
            uploadResult.VideoFilePath = uploadVideoRequest.VideoFilePath;
            return true;
        }

        #endregion


        #region Media Indexeer
        /// <summary>
        /// Generating transcript for video
        /// </summary>
        /// <param name="assetUri"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private void ConfigureTranscriptTask(IJob job)
        {
            string mediaProcessorName = _amsConfigurations.MediaIndexer2MediaProcessor;
            IMediaProcessor processor = _mediaContext.MediaProcessors.GetLatestMediaProcessorByName(mediaProcessorName);

            string configuration = File.ReadAllText(_amsConfigurations.MediaIndexerConfigurationJson);
            ITask task = job.Tasks.AddNew("AudioIndexing Task", processor, configuration, TaskOptions.None);
            task.InputAssets.Add(asset);
            task.OutputAssets.AddNew("AudioIndexing Output Asset", AssetCreationOptions.None);
        }

        #endregion

        #region Content Moderation

        /// <summary>
        /// GetContentModerationDetails used for getting moderated json of uploded video
        /// </summary>
        /// <param name="assetId">assetId</param>
        /// <returns></returns>
        private void ConfigureContentModerationTask(IJob job)
        {
            IMediaProcessor mp = _mediaContext.MediaProcessors.GetLatestMediaProcessorByName(this._amsConfigurations.ModerationProcessor);
            if (mp == null)
            {
                throw new Exception("Please check the configuration values, some configuration values are not matching.");
            }

            string moderationConfiguration = System.IO.File.ReadAllText(this._amsConfigurations.ModerationConfigurationJson);

            ITask contentModeratorTask = job.Tasks.AddNew(asset.Name + "_" + "Adult and racy classifier task", mp, moderationConfiguration, TaskOptions.None);

            contentModeratorTask.InputAssets.Add(asset);
            contentModeratorTask.OutputAssets.AddNew(asset.Name + "_" + "Adult and racy classifier output", AssetCreationOptions.None);

        }

        /// <summary>
        /// Downloads an asset into a file.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="outputDirectory"></param>
        /// <returns>string</returns>
        private string DownloadModeratedJsonFile(IAsset asset, string outputDirectory)
        {
            string fileName = string.Empty;

            foreach (IAssetFile file in asset.AssetFiles)
            {
                file.Download(Path.Combine(outputDirectory, file.Name));
                fileName = file.Name;
                break;
            }

            return fileName;
        }
        public bool GenerateTranscript(IAsset asset)
        {
            try
            {
                var outputFolder = this._amsConfigurations.FfmpegFramesOutputPath;
                IAsset outputAsset = asset;
                IAccessPolicy policy = null;
                ILocator locator = null;
                policy = _mediaContext.AccessPolicies.Create("My 30 days readonly policy", TimeSpan.FromDays(360), AccessPermissions.Read);
                locator = _mediaContext.Locators.CreateLocator(LocatorType.Sas, outputAsset, policy, DateTime.UtcNow.AddMinutes(-5));
                DownloadAssetToLocal(outputAsset, outputFolder);
                locator.Delete();
                return true;
            }
            catch
            {   //TODO:  Logging
                Console.WriteLine("Exception occured while generating index for video.");
                throw;
            }
        }
        #endregion

    }

}
