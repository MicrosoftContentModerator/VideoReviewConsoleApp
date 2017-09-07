using System;
using System.IO;
using System.Diagnostics;

namespace Microsoft.ContentModerator.AMSComponentClient
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
        public string CompressVideo(string videoPath)
        {
            string ffmpegBlobUrl;
            if (!ValidatePreRequisites())
            {
                Console.WriteLine("Configurations check failed. Please cross check the configurations!");
                throw new Exception();
            }

            if (File.Exists(_configObj.FfmpegExecutablePath))
            {
                ffmpegBlobUrl = this._configObj.FfmpegExecutablePath;
            }
            else
            {
                Console.WriteLine("ffmpeg.exe is missing. Please check the Lib folder");
                throw new Exception();
            }

            string videoFilePathCom = videoPath.Split('.')[0] + "_c.mp4";
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.FileName = ffmpegBlobUrl;
            processStartInfo.Arguments = "-i \"" + videoPath + "\" -vcodec libx264 -n -crf 32 -preset veryfast -vf scale=640:-1 -c:a aac -aq 1 -ac 2 -threads 0 \"" + videoFilePathCom + "\"";
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            process.Close();
            return videoFilePathCom;
        }

        private bool ValidatePreRequisites()
        {
            return _configObj.CheckValidations();
        }
    }
}
