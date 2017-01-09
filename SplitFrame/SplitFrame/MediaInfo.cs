using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitFrame
{
    public class MediaInfo
    {
        public int MediaWidth;
        public int MediaHeight;
        public int MediaStride;
        public int MediaBitCount;
        public double FPS;
        public double Duration;
        public string FileName;
        public string FilePath;

        public MediaInfo()
        {
            this.MediaWidth = 0;
            this.MediaHeight = 0;
            this.MediaStride = 0;
            this.MediaBitCount = 0;
            this.FPS = 0;
            this.Duration = 0;
            this.FileName = string.Empty;
            this.FilePath = string.Empty;
        }
    }
}
