using System.Collections.Generic;

namespace ImageShifter.Core
{
    public class ConversionResult
    {
        public int Total { get; set; }

        public int SuccessCount { get; set; }

        public int FailCount => Errors.Count;

        public List<string> Errors { get; } = new ();
    }
}