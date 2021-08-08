using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoSplitNet
{
    class Program
    {
        static void Main(string[] args)
        {
            var mp4FilePath = @"C:\Users\Alexandr\Downloads\videoplayback.mp4";
            var outputPath = @"C:\Users\Alexandr\Downloads\Pictures";

            //using (var vFReader = new VideoFileReader())
            //{
            //    vFReader.Open("video.mp4");
            //    for (int i = 0; i < vFReader.FrameCount; i++)
            //    {
            //        Bitmap bmpBaseOriginal = vFReader.ReadVideoFrame();
            //    }
            //    vFReader.Close();
            //}

            using (var engine = new Engine(mp4FilePath))
            {
                var mp4 = new MediaFile { Filename = mp4FilePath };

                engine.GetMetadata(mp4);

                var i = 0;
                while (i < mp4.Metadata.Duration.Seconds)
                {
                    var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(i) };
                    var outputFile = new MediaFile { Filename = string.Format("{0}\\image-{1}.jpeg", outputPath, i) };
                    engine.GetThumbnail(mp4, outputFile, options);
                    i++;
                }
            }
        }
    }
}
