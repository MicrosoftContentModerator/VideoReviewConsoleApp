using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    public class TranscriptTerms
    {
        public int Index { get; set; }
        public int OriginalIndex { get; set; }
        public int ListId { get; set; }
        public string Term { get; set; }
    }

    public class Status
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public object Exception { get; set; }
    }

    public class TextScreen
    {
        public string OriginalText { get; set; }
        public string NormalizedText { get; set; }
        public object Misrepresentation { get; set; }
        public string Language { get; set; }
        public List<TranscriptTerms> Terms { get; set; }
        public Status Status { get; set; }
        public string TrackingId { get; set; }
        public Classification Classification { get; set; }
    }
    public class Classification
    {
        public Category1 Category1 { get; set; }
        public Category2 Category2 { get; set; }
        public Category3 Category3 { get; set; }
        public string ReviewRecommended { get; set; }
    }
    public class Category1
    {
        public double Score { get; set; }
    }
    public class Category2
    {
        public double Score { get; set; }
    }
    public class Category3
    {
        public double Score { get; set; }
    }
}
