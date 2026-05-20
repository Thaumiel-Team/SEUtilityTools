using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SEUtilityTools.API.Extensions
{
    public static class ImageExtensions
    {
        public static Bitmap Round(this Bitmap image, int percentage)
        {
            ArgumentNullException.ThrowIfNull(image);

            if (percentage < 0 || percentage > 50)
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be 0-50");

            double width = image.Size.Width;
            double height = image.Size.Height;
            double radius = Math.Min(width, height) * (percentage / 100.0);

            RenderTargetBitmap rtb = new(image.PixelSize, image.Dpi);

            using (DrawingContext ctx = rtb.CreateDrawingContext())
            {
                StreamGeometry geometry = new();
                using (StreamGeometryContext geoCtx = geometry.Open())
                {
                    geoCtx.BeginFigure(new Point(0, radius), isFilled: true);

                    geoCtx.ArcTo(new Point(radius, 0), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                    geoCtx.LineTo(new Point(width - radius, 0));

                    geoCtx.ArcTo(new Point(width, radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                    geoCtx.LineTo(new Point(width, height - radius));

                    geoCtx.ArcTo(new Point(width - radius, height), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                    geoCtx.LineTo(new Point(radius, height));

                    geoCtx.ArcTo(new Point(0, height - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                    geoCtx.EndFigure(isClosed: true);
                }


                Rect bounds = new(0, 0, width, height);
                using (ctx.PushGeometryClip(geometry))
                {
                    ctx.DrawImage(image, bounds, bounds);
                }
            }

            return rtb;
        }

        public static Bitmap Scale(this Bitmap image, int percentage)
        {
            ArgumentNullException.ThrowIfNull(image);

            if (percentage <= 0)
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be greater than 0");

            int newWidth = image.PixelSize.Width * percentage / 100;
            int newHeight = image.PixelSize.Height * percentage / 100;

            return image.CreateScaledBitmap(new PixelSize(newWidth, newHeight), BitmapInterpolationMode.HighQuality);
        }
    }
}