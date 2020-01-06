using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using NReco.VideoConverter;
using NReco.VideoInfo;
using System.IO;
using System.Diagnostics;
using OpenCvSharp.Extensions;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            await run();
        }


        private async void Button1_Click(object sender, EventArgs e)
        {
            await run();
        }


        private async Task run()
        {
            const string videoPath = "test.mp4";
            const string resultBmpPath = "result.bmp";

            var inputs = new List<Mat>();
            var resultPanorama = new Mat();
            var stitcher = OpenCvSharp.Stitcher.Create(Stitcher.Mode.Panorama);

            // プレビュー表示しながらStitch用の画像リスト(List<Mat>)保存
            // Streamで処理する方法ないのかな？　元動画が長いとキツい？
            foreach (var bmp in GetThumbnails(videoPath))
            {
                var oldImage = preview.Image;
                await Task.Delay(100);
                preview.Image = bmp;

                oldImage?.Dispose();

                inputs.Add(BitmapConverter.ToMat(bmp));
            }

            lblStatus.Text = "画像統合中・・・";

            // 画像リストからパノラマ画像作成
            var resultBmp = await Task.Run(() =>
            {
                stitcher.Stitch(inputs, resultPanorama);
                inputs.ForEach(x => x.Dispose());
                return resultPanorama.ToBitmap();
            });

            // 結果のプレビュー表示
            {
                var oldImage = resultPreview.Image;
                resultPreview.Image = resultBmp;
                oldImage?.Dispose();
            }
            lblStatus.Text = "";

            await Task.Run(() =>
            {
                resultPanorama.SaveImage(resultBmpPath);
                resultPanorama.Dispose();
            });
        }


        /// <summary>
        /// だいたい0.3秒おきの動画サムネイルを取得する
        /// </summary>
        /// <param name="videoPath">ビデオファイルパス</param>
        /// <returns></returns>
        public static IEnumerable<Bitmap> GetThumbnails(string videoPath)
        {
            // メタ情報で再生時間取得
            var ffprob = new NReco.VideoInfo.FFProbe();
            var info = ffprob.GetMediaInfo(videoPath);
            var duration = info.Duration;

            var videoConv = new NReco.VideoConverter.FFMpegConverter();

            // 最短0.3秒おき、最大100分割
            var skipSec = duration.TotalSeconds < 30 ?
                          0.3f : (float)(duration.TotalSeconds / 100);

            var frameSec = 0f;
            while((float)duration.TotalSeconds > frameSec)
            {
                // Bitmapリソースは受信側で開放する。
                var jpegStream = new MemoryStream();
                videoConv.GetVideoThumbnail(videoPath, jpegStream, frameSec);
                yield return (Bitmap)Image.FromStream(jpegStream);
                frameSec += skipSec;
            }
        }

    }
}
