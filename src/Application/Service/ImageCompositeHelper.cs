using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace ManjuCraft.Application.Service;

public static class ImageCompositeHelper
{
    public static (string compositeFilePath, int totalHeight) CreateCompositeImage(
        List<(string imagePath, string assetName)> assetsWithImages,
        string outputDir,
        string fileName)
    {
        Directory.CreateDirectory(outputDir);

        var loadedImages = new List<(Image img, string name)>();
        int maxWidth = 0;
        int totalHeight = 10;

        try
        {
            foreach (var asset in assetsWithImages)
            {
                if (!File.Exists(asset.imagePath))
                    continue;

                var img = Image.FromFile(asset.imagePath);
                loadedImages.Add((img, asset.assetName));

                if (img.Width > maxWidth)
                    maxWidth = img.Width;

                totalHeight += img.Height + 10;
            }

            if (loadedImages.Count == 0)
                throw new InvalidOperationException("没有可用的资产图片");

            totalHeight += 10;

            using var compositeBitmap = new Bitmap(maxWidth, totalHeight);
            using var g = Graphics.FromImage(compositeBitmap);
            g.Clear(Color.WhiteSmoke);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            using var font = new Font("SimSun", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.Black);
            using var bgBrush = new SolidBrush(Color.FromArgb(200, Color.White));
            using var borderPen = new Pen(Color.FromArgb(80, Color.Gray), 1);

            int yOffset = 10;
            foreach (var (img, name) in loadedImages)
            {
                var drawWidth = maxWidth;
                var drawHeight = (int)((float)img.Height / img.Width * drawWidth);

                g.DrawImage(img, new Rectangle(0, yOffset, drawWidth, drawHeight));
                g.DrawRectangle(borderPen, new Rectangle(0, yOffset, drawWidth - 1, drawHeight - 1));

                var textSize = g.MeasureString(name, font);
                var labelRect = new RectangleF(4, yOffset + 4, textSize.Width + 8, textSize.Height + 4);
                g.FillRectangle(bgBrush, labelRect);
                g.DrawString(name, font, brush, new PointF(8, yOffset + 6));

                yOffset += drawHeight + 10;
                img.Dispose();
            }

            var outputPath = Path.Combine(outputDir, fileName);
            compositeBitmap.Save(outputPath, ImageFormat.Png);

            return (outputPath, totalHeight);
        }
        catch
        {
            foreach (var (img, _) in loadedImages)
                img.Dispose();
            throw;
        }
    }
}
