using DirectShowLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SplitFrame
{
    public partial class SplitFrameForm : Form
    {
        private string _videoFileName;

        private bool _canStep = false;
        /// <summary>
        /// 处理视频相关DirectShowLib接口
        /// </summary>
        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaCtrl = null;
        private IMediaEventEx mediaEvt = null;
        private IMediaPosition mediaPos = null;
        private IVideoFrameStep frameStep = null;
        private IVideoWindow videoWin = null;

        private const int WM_GRAPHNOTIFY = 0x00008001;
        private const int WS_CHILD = 0x40000000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;
        private const int WM_MOVE = 0x00000003;
        private const int EC_COMPLETE = 0x00000001;

        private GrabFrames _grabFrames;
        public delegate void ReportProgress(double progress);

        public SplitFrameForm()
        {
            InitializeComponent();
            this.progressBar.Visible = false;
        }

        // 
        //创建filter graph
        //
        private void InitInterfaces()
        {
            try
            {
                graphBuilder = (IGraphBuilder)new FilterGraph();
                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                mediaPos = (IMediaPosition)graphBuilder;
            }
            catch (Exception ee)
            {
                MessageBox.Show("Couldn't start");
            }
        }

        //
        //This method stop the filter graph and ensures that we stop
        //sending messages to our window
        //
        private void CloseInterfaces()
        {
            if (mediaCtrl != null)
            {
                mediaCtrl.StopWhenReady();
                mediaEvt.SetNotifyWindow((IntPtr)0, WM_GRAPHNOTIFY, (IntPtr)0);
            }
            mediaCtrl = null;
            mediaEvt = null;
            mediaPos = null;
            if (_canStep)
                frameStep = null;
            videoWin = null;
            if (graphBuilder != null)
                Marshal.ReleaseComObject(this.graphBuilder);
            graphBuilder = null;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = @"All Files (*.*) | *.*|AVI Files (*.avi)|*.avi|MOV Files (*.mov)|*.mov|MP4 Files (*.mp4)|*.mp4|FLV Files (*.flv)|*.flv";
            openFileDialog.InitialDirectory = System.Environment.CurrentDirectory;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                CloseInterfaces();
                _videoFileName = openFileDialog.FileName;
                this.txtSelectDirectory.Text = _videoFileName;
                InitInterfaces();
                var folder = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                SplitFrame(_videoFileName, folder);
            }
        }

        private void SplitFrame(string fileName, string folder)
        {
            _grabFrames = new GrabFrames(fileName);
            _grabFrames.StoragePath = folder;
            _grabFrames.ReportProgressHandler += ReportProgressHandler;
            this.progressBar.Visible = true;
            SetResult(false);
            this.btnSelect.Enabled = false;
        }

        private void ReportProgressHandler(double progress)
        {
            if (this.progressBar.InvokeRequired)
            {
                ReportProgress d = new ReportProgress(ReportProgressHandler);
                this.progressBar.Invoke(d, new object[] { progress });
            }
            else
            {
                int value = (int)((progress + 1) * 100 / (this._grabFrames.MediaInfo.Duration * this._grabFrames.MediaInfo.FPS));
                if (value >= 100)
                {
                    this.progressBar.Value = 0;
                    this.progressBar.Visible = false;
                    SetResult(true);
                    this.btnSelect.Enabled = true;
                }
                this.progressBar.Value = value;
            }
        }

        private void SetResult(bool isEnable)
        {
            this.linkLabel1.Visible = isEnable;
            if (isEnable)
                this.linkLabel1.Text = string.Format("{0}\\{1}", System.Environment.CurrentDirectory, _grabFrames.StoragePath);
            else
                this.linkLabel1.Text = "";
        }

        private void LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("Explorer.exe", _grabFrames.Folder);
            }
            catch (Exception ee)
            {
            }
        }
    }
}
