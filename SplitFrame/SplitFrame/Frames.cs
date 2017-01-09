using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitFrame
{
    [Serializable]
    public class Frames
    {
        private double _time;

        private Image _image;

        private string _name;

        private int _frameName;
        public int FrameName
        {
            get { return this._frameName; }
        }

        public double Time
        {
            get { return this._time; }
        }

        public Image Image
        {
            get { return this._image; }
        }

        public Frames(double time, Image image, string name)
        {
            this._time = time;
            this._image = image;
            this._name = name;
            try
            {
                this._frameName = Int32.Parse(name);
            }
            catch (Exception e)
            {
                this._frameName = 0;
            }
        }
    }
}
