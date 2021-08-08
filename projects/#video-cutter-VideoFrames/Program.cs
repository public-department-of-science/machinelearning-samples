using System;
using System.Drawing;
using System.IO;

namespace VideoFrames
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var mp4FilePath = @"C:\Users\Alexandr\Downloads\videoplayback.mp4";
            var outputPath = @"C:\Users\Alexandr\Downloads\Pictures";

            TimeSpan timeOfFrame = new TimeSpan(0, 0, 1);

            //pick mp4 file
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");
            StorageFile pickedFile = await picker.PickSingleFileAsync();
            if (pickedFile == null)
            {
                return;
            }
            ///


            //Get video resolution
            List<string> encodingPropertiesToRetrieve = new List<string>();
            encodingPropertiesToRetrieve.Add("System.Video.FrameHeight");
            encodingPropertiesToRetrieve.Add("System.Video.FrameWidth");
            IDictionary<string, object> encodingProperties = await pickedFile.Properties.RetrievePropertiesAsync(encodingPropertiesToRetrieve);
            uint frameHeight = (uint)encodingProperties["System.Video.FrameHeight"];
            uint frameWidth = (uint)encodingProperties["System.Video.FrameWidth"];
            ///


            //Use Windows.Media.Editing to get ImageStream
            var clip = await MediaClip.CreateFromFileAsync(pickedFile);
            var composition = new MediaComposition();
            composition.Clips.Add(clip);

            var imageStream = await composition.GetThumbnailAsync(timeOfFrame, (int)frameWidth, (int)frameHeight, VideoFramePrecision.NearestFrame);
            ///


            //generate bitmap 
            var writableBitmap = new WriteableBitmap((int)frameWidth, (int)frameHeight);
            writableBitmap.SetSource(imageStream);


            //generate some random name for file in PicturesLibrary
            var saveAsTarget = await KnownFolders.PicturesLibrary.CreateFileAsync("IMG" + Guid.NewGuid().ToString().Substring(0, 4) + ".jpg");


            //get stream from bitmap
            Stream stream = writableBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)stream.Length];
            await stream.ReadAsync(pixels, 0, pixels.Length);

            using (var writeStream = await saveAsTarget.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, writeStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)writableBitmap.PixelWidth,
                    (uint)writableBitmap.PixelHeight,
                    96,
                    96,
                    pixels);
                await encoder.FlushAsync();

                using (var outputStream = writeStream.GetOutputStreamAt(0))
                {
                    await outputStream.FlushAsync();
                }
            }

            //using (var vFReader = new VideoFileReader())
            //{
            //    vFReader.Open("video.mp4");
            //    for (int i = 0; i < vFReader.FrameCount; i++)
            //    {
            //        Bitmap bmpBaseOriginal = vFReader.ReadVideoFrame();
            //    }
            //    vFReader.Close();
            //}

            //using (var engine = new Engine(mp4FilePath))
            //{
            //    var mp4 = new MediaFile { Filename = mp4FilePath };

            //    engine.GetMetadata(mp4);

            //    var i = 0;
            //    while (i < mp4.Metadata.Duration.Seconds)
            //    {
            //        var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(i) };
            //        var outputFile = new MediaFile { Filename = string.Format("{0}\\image-{1}.jpeg", outputPath, i) };
            //        engine.GetThumbnail(mp4, outputFile, options);
            //        i++;
            //    }
            //}
        }
    }
}
