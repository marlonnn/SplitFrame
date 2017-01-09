using DirectShowLib;
using DirectShowLib.DES;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplitFrame
{
    public class GrabFrames
    {
        private IGraphBuilder _graphBuilder;
        private ISampleGrabber _sampleGrabber;
        public IBasicVideo _basicVideo;
        private IMediaDet _mediaDet;
        private string _fileName;
        private string _storagePath;
        public string StoragePath
        {
            set { this._storagePath = value; }
            get { return this._storagePath; }
        }

        private Thread _thread;

        private MediaInfo _mediaInfo;

        public MediaInfo MediaInfo { get { return this._mediaInfo; } }

        public delegate void ReportProgress(double progress);
        public ReportProgress ReportProgressHandler;

        private List<Frames> _frames;

        public List<Frames> Frames { get { return this._frames; } }

        public string Folder;
        public GrabFrames(string fileName)
        {
            _frames = new List<Frames>();
            _fileName = fileName;
            _thread = new Thread(new ThreadStart(this.Grab));
            _thread.Start();
        }

        private void Grab()
        {
            try
            {
                _mediaInfo = new MediaInfo();
                double fps, length;
                _mediaDet = (IMediaDet)new MediaDet();
                _mediaDet.put_Filename(_fileName);

                _mediaDet.get_StreamLength(out length);

                _graphBuilder = (IGraphBuilder)new FilterGraph();
                _sampleGrabber = (ISampleGrabber)new SampleGrabber();
                ConfigSampleGrabber(this._sampleGrabber);
                this._graphBuilder.AddFilter((IBaseFilter)_sampleGrabber, "SampleGrabber");
                DsError.ThrowExceptionForHR(this._graphBuilder.RenderFile(_fileName, null));
                _basicVideo = this._graphBuilder as IBasicVideo;
                fps = getFramePerSecond();
                AMMediaType media = new AMMediaType();

                this._sampleGrabber.GetConnectedMediaType(media);
                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                {
                    throw new Exception("Format type incorrect");
                }

                double interval = 1 / fps;

                int videoWidth, videoHeight, videoStride;
                this._basicVideo.GetVideoSize(out videoWidth, out videoHeight);
                VideoInfoHeader videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                videoStride = videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);
                this._mediaInfo.MediaWidth = videoWidth;
                this._mediaInfo.MediaHeight = videoHeight;
                this._mediaInfo.MediaStride = videoStride;
                this._mediaInfo.MediaBitCount = videoInfoHeader.BmiHeader.BitCount;
                this._mediaInfo.FPS = fps;
                this._mediaInfo.Duration = length;
                DsUtils.FreeAMMediaType(media);
                media = null;
                int index = 0;
                for (double i = 0; i < length; i = i + interval)
                {
                    SnapShot(index);
                    if (ReportProgressHandler != null)
                    {
                        ReportProgressHandler(index);
                    }
                    index++;
                }
                //var v = _frames.Count;
            }
            catch (Exception ee)
            {

            }

        }

        public double getFramePerSecond()
        {
            double result = 0;
            this._mediaDet.get_FrameRate(out result);
            if (result != 0)
                return result;

            this._basicVideo.get_AvgTimePerFrame(out result);
            if (result != 0)
                return 1 / result;

            if (result == 0)
                result = 25.0;

            return result;
        }

        private void SnapShot(double position)
        {
            try
            {
                int hr;
                IntPtr ip = IntPtr.Zero;
                int iBuffSize;
                hr = this._mediaDet.GetBitmapBits(position, out iBuffSize, ip, this._mediaInfo.MediaWidth, this._mediaInfo.MediaHeight);
                ip = Marshal.AllocCoTaskMem(iBuffSize);
                hr = this._mediaDet.GetBitmapBits(position, out iBuffSize, ip, this._mediaInfo.MediaWidth, this._mediaInfo.MediaHeight);
                //Bitmap bm = new Bitmap(this._mediaInfo.MediaWidth, this._mediaInfo.MediaHeight);

                Folder = string.Format("{0}\\{1}", System.Environment.CurrentDirectory, _storagePath);
                if (!Directory.Exists(Folder))
                {
                    Directory.CreateDirectory(Folder);
                }

                try
                {
                    using (Bitmap bm = new Bitmap(
                        this._mediaInfo.MediaWidth,
                        this._mediaInfo.MediaHeight,
                        -this._mediaInfo.MediaStride,
                        PixelFormat.Format24bppRgb,
                        (IntPtr)(ip.ToInt32() + iBuffSize - this._mediaInfo.MediaStride)
                        ))
                    {
                        bm.Save(string.Format("{0}\\{1}.png", Folder, position));
                    }
                    Marshal.FreeCoTaskMem(ip);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Could not convert bitmapbits to bitmap: " + e.Message);
                }
            }
            catch (Exception ee)
            {
            }
        }
        private void ConfigSampleGrabber(ISampleGrabber sampGrabber)
        {
            AMMediaType media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            this._sampleGrabber.SetMediaType(media);



            DsUtils.FreeAMMediaType(media);
            media = null;
            int hr = sampGrabber.SetBufferSamples(true);
            DsError.ThrowExceptionForHR(hr);
        }
    }
}
