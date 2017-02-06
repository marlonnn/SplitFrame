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
        /// <summary>
        /// DirectShowLib 相关变量
        /// </summary>
        private IGraphBuilder graphBuilder;
        private ISampleGrabber sampleGrabber;
        public IBasicVideo basicVideo;
        private IMediaDet mediaDet;
        //文件名
        private string fileName;
        //文件存放路径
        private string storagePath;
        public string StoragePath
        {
            set { this.storagePath = value; }
            get { return this.storagePath; }
        }

        private Thread thread;

        //视频文件信息
        private MediaInfo mediaInfo;

        public MediaInfo MediaInfo { get { return this.mediaInfo; } }

        public delegate void ReportProgress(double progress);
        public ReportProgress ReportProgressHandler;

        private List<Frames> frames;

        public List<Frames> Frames { get { return this.frames; } }

        //提取的帧图存放的文件夹路径
        public string Folder;
        public GrabFrames(string fileName)
        {
            frames = new List<Frames>();
            this.fileName = fileName;
            thread = new Thread(new ThreadStart(this.Grab));
            thread.Start();
        }

        /// <summary>
        /// 获取视频流相关信息
        /// </summary>
        private void Grab()
        {
            try
            {
                mediaInfo = new MediaInfo();
                double fps, length;
                mediaDet = (IMediaDet)new MediaDet();
                mediaDet.put_Filename(fileName);

                mediaDet.get_StreamLength(out length);

                graphBuilder = (IGraphBuilder)new FilterGraph();
                sampleGrabber = (ISampleGrabber)new SampleGrabber();
                ConfigSampleGrabber(this.sampleGrabber);
                this.graphBuilder.AddFilter((IBaseFilter)sampleGrabber, "SampleGrabber");
                DsError.ThrowExceptionForHR(this.graphBuilder.RenderFile(fileName, null));
                basicVideo = this.graphBuilder as IBasicVideo;
                fps = getFramePerSecond();
                AMMediaType media = new AMMediaType();

                this.sampleGrabber.GetConnectedMediaType(media);
                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                {
                    throw new Exception("Format type incorrect");
                }

                double interval = 1 / fps;

                int videoWidth, videoHeight, videoStride;
                this.basicVideo.GetVideoSize(out videoWidth, out videoHeight);
                VideoInfoHeader videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                videoStride = videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);
                this.mediaInfo.MediaWidth = videoWidth;
                this.mediaInfo.MediaHeight = videoHeight;
                this.mediaInfo.MediaStride = videoStride;
                this.mediaInfo.MediaBitCount = videoInfoHeader.BmiHeader.BitCount;
                this.mediaInfo.FPS = fps;
                this.mediaInfo.Duration = length;
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
                //var v = frames.Count;
            }
            catch (Exception ee)
            {

            }

        }

        /// <summary>
        /// 获取帧率FPS
        /// </summary>
        /// <returns></returns>
        public double getFramePerSecond()
        {
            double result = 0;
            this.mediaDet.get_FrameRate(out result);
            if (result != 0)
                return result;

            this.basicVideo.get_AvgTimePerFrame(out result);
            if (result != 0)
                return 1 / result;

            if (result == 0)
                result = 25.0;

            return result;
        }

        /// <summary>
        /// 从视频流中获取帧图
        /// </summary>
        /// <param name="position"></param>
        private void SnapShot(double position)
        {
            try
            {
                int hr;
                IntPtr ip = IntPtr.Zero;
                int iBuffSize;
                hr = this.mediaDet.GetBitmapBits(position, out iBuffSize, ip, this.mediaInfo.MediaWidth, this.mediaInfo.MediaHeight);
                ip = Marshal.AllocCoTaskMem(iBuffSize);
                hr = this.mediaDet.GetBitmapBits(position, out iBuffSize, ip, this.mediaInfo.MediaWidth, this.mediaInfo.MediaHeight);
                //Bitmap bm = new Bitmap(this.mediaInfo.MediaWidth, this.mediaInfo.MediaHeight);

                Folder = string.Format("{0}\\{1}", System.Environment.CurrentDirectory, storagePath);
                if (!Directory.Exists(Folder))
                {
                    Directory.CreateDirectory(Folder);
                }

                try
                {
                    using (Bitmap bm = new Bitmap(
                        this.mediaInfo.MediaWidth,
                        this.mediaInfo.MediaHeight,
                        -this.mediaInfo.MediaStride,
                        PixelFormat.Format24bppRgb,
                        (IntPtr)(ip.ToInt32() + iBuffSize - this.mediaInfo.MediaStride)
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

        /// <summary>
        /// 初始化DirectShowLib相关接口变量
        /// </summary>
        /// <param name="sampGrabber"></param>
        private void ConfigSampleGrabber(ISampleGrabber sampGrabber)
        {
            AMMediaType media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            this.sampleGrabber.SetMediaType(media);
            DsUtils.FreeAMMediaType(media);
            media = null;
            int hr = sampGrabber.SetBufferSamples(true);
            DsError.ThrowExceptionForHR(hr);
        }
    }
}
