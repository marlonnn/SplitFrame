using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitFrame
{
    /// <summary>
    /// 帧图相关类
    /// </summary>
    [Serializable]
    public class Frames
    {
        private double time;

        private Image image;

        private string name;

        private int frameName;
        public int FrameName
        {
            get { return this.frameName; }
        }

        public double Time
        {
            get { return this.time; }
        }

        public Image Image
        {
            get { return this.image; }
        }

        public Frames(double time, Image image, string name)
        {
            this.time = time;
            this.image = image;
            this.name = name;
            try
            {
                this.frameName = Int32.Parse(name);
            }
            catch (Exception e)
            {
                this.frameName = 0;
            }
        }
    }
}
