using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WolfShooter.BaseEntities
{
    public class Texture
    {
        private readonly Color[,] finalPixels;
        public Bitmap Image { get; }

        public Texture(Bitmap image)
        {
            Image = image;
            finalPixels = new Color[image.Width, image.Height];
        }

        public Texture(string fileDir)
        {
            Image = new Bitmap(fileDir);
            finalPixels = new Color[Image.Width, Image.Height];
        }

        public Color GetPixel(int x, int y) => finalPixels[x, y];

        public void InitializeColorArray()
        {
            var bitmapData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly,
                Image.PixelFormat);
            var bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(Image.PixelFormat) / 8;
            var byteCount = bitmapData.Stride * Image.Height;
            var pixels = new byte[byteCount];
            var ptrFirstPixel = bitmapData.Scan0;
            
            var heightInPixels = bitmapData.Height;
            var widthInBytes = bitmapData.Width * bytesPerPixel;

            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

            for (var y = 0; y < heightInPixels; y++)
            { 
                var currentLine = y * bitmapData.Stride;
                var buffX = 0;
                for (var x = 0; x < widthInBytes; x += bytesPerPixel)
                {
                    var color = Color.FromArgb(pixels[currentLine + x + 2],
                        pixels[currentLine + x + 1], pixels[currentLine + x]);
                    finalPixels[buffX, y] = color;
                    buffX++;
                }
            }

        }
    }
}
