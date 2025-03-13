using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageHistogram
{
    internal class ImageAnalysisResult
    {
        public string FileName { get; set; }
        public bool IsDefectedInName { get; set; }
        public bool DetectedByAlgorithm { get; set; }
        public bool IsCorrect { get; set; }
    }
}
