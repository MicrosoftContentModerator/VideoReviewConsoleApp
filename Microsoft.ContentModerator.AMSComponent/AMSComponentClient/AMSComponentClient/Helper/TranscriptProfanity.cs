using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ContentModerator.AMSComponentClient
{
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

}