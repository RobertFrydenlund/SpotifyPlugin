using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SpotifyPlugin
{
    class ImageManipulate
    {
        static void Generate(int x, int y, string path)
        {
            string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +  "/Rainmeter/SpotifyPlugin";
            Bitmap source = new Bitmap(path);
            int width = 20;
            int height = 200;

            for (int i = 0; i < 48; i++)
            {
                Bitmap CroppedImage = source.Clone(new System.Drawing.Rectangle(x + 25*i , y - 10*i, width, height), source.PixelFormat);
                CroppedImage.Save(rootPath + "/bar" + i, System.Drawing.Imaging.ImageFormat.Png);
            }

        }
    }
}
