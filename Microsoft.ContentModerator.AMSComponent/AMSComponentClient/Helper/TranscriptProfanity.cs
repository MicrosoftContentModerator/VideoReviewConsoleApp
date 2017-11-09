using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    public class TranscriptScreenTextResult
    {
        public List<TranscriptProfanity> TranscriptProfanity { get; set; }
        public double OffensiveScore { get; set; }
        public double RacyScore { get; set; }
        public double AdultScore { get; set; }
        public bool OffensiveTag { get; set; }
        public bool RacyTag { get; set; }
        public bool AdultTag { get; set; }
    }
	public class TranscriptProfanity
	{

		public string TimeStamp { get; set; }
		public List<Terms> Terms { get; set; }
	}

	public class Terms
	{
		public int Index { get; set; }
		public string Term { get; set; }
	}
    public class CaptionScreentextResult
    {
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public List<string> Captions { get; set; }
    }
}