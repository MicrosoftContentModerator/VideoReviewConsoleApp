using System.Collections.Generic;

namespace Microsoft.ContentModerator.BusinessEntities.Entities
{
  
    /// <summary>
    /// Represents a Json object after Video Moderation
    /// </summary>
    public class VideoModerationResult
    {
        /// <summary>
        /// Gets or Sets the Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or Sets the Timescale
        /// </summary>
        public string TimeScale { get; set; }

        /// <summary>
        /// Gets or Sets the Offset
        /// </summary>
        public string Offset { get; set; }
        /// <summary>
        /// Gets or Sets the Framerate
        /// </summary>
        public string FrameRate { get; set; }

        /// <summary>
        /// Gets or Sets the Width
        /// </summary>
        public string Width { get; set; }

        /// <summary>
        /// Gets or Sets the Height
        /// </summary>
        public string Height { get; set; }

        /// <summary>
        /// Gets or Sets the Fragments Details
        /// </summary>
        public List<Fragments> Fragments { get; set; }

        /// <summary>
        /// Gets or Sets the Json of Moderated Video
        /// </summary>
        public string ModeratedJson { get; set; }


	    //v2
	    public List<Shot> Shots { get; set; }
	}

    /// <summary>
    /// Represents a Video fragment.
    /// </summary>
    public class Fragments
    {
        /// <summary>
        /// Gets or Sets the Start of Frame
        /// </summary>
        public string Start { get; set; }

        /// <summary>
        /// Gets or Sets the Frame Duration
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// Gets or Sets the Frame Interval
        /// </summary>
        public string Interval { get; set; }

        /// <summary>
        /// Gets or Sets the Events of Frame
        /// </summary>
        public List<List<FrameEventDetails>> Events { get; set; }
    }

    /// <summary>
    /// Represents a Frame Event details
    /// </summary>
    public class FrameEventDetails
    {

        /// <summary>
        /// Gets or Sets the Interval of Event
        /// </summary>

        public string Interval { get; set; }

        /// <summary>
        /// Gets or Sets the IsAdultContent of Event
        /// </summary>
        public bool IsAdultContent { get; set; }

        /// <summary>
        /// Gets or Sets the AdultConfidence of Event
        /// </summary>
        public string AdultConfidence { get; set; }
		/// <summary>
		/// 
		/// </summary>
	    public bool IsRacyContent { get; set; }
		/// <summary>
		/// 
		/// </summary>
	    public string RacyConfidence { get; set; }
		/// <summary>
		///  Gets or Sets the Index of Event
		/// </summary>
		public int Index { get; set; }

        /// <summary>
        /// Gets or Sets the Timestamp of Event
        /// </summary>
        public int TimeStamp { get; set; }

        /// <summary>
        /// Gets or Sets the Timescale of Event
        /// </summary>
        public int TimeScale { get; set; }
        /// <summary>
        /// Gets or Sets the  BlobStorage PrimaryUri
        /// </summary>        
        public string PrimaryUri { get; set; }

        /// <summary>
        /// Gets or Sets the  BlobStorage SecondryUri
        /// </summary>
        public string SecondryUri { get; set; }

        /// <summary>
        /// Gets or Sets the FrameName
        /// </summary>       
        public string FrameName { get; set; }

        /// <summary>
        /// Gets or Sets the frame order id.
        /// </summary>
        public string FrameOrderId { get; set; }

    }


	public class Clip
	{
		public int Start { get; set; }
		public int Duration { get; set; }
		public int Interval { get; set; }
		public List<FrameEventDetails> Frames { get; set; }
	}

	public class Shot
	{
		public int Start { get; set; }
		public int Duration { get; set; }
		public int Interval { get; set; }
		public int Clipscount { get; set; }
		public List<Clip> Clips { get; set; }
	}
}
