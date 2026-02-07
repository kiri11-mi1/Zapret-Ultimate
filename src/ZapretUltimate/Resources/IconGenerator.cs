using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace ZapretUltimate.Resources;

public static class IconGenerator
{
    public static void GenerateIcons()
    {
        var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        Directory.CreateDirectory(resourcesPath);

        GenerateAppIcon(Path.Combine(resourcesPath, "app.ico"));
        GenerateTrayIcon(Path.Combine(resourcesPath, "tray.ico"), Color.FromArgb(100, 149, 237));
        GenerateTrayIcon(Path.Combine(resourcesPath, "tray_active.ico"), Color.FromArgb(76, 175, 80));
    }

    private static void GenerateAppIcon(string path)
    {
        if (File.Exists(path)) return;

        var sizes = new[] { 16, 32, 48, 64, 128, 256 };
        var bitmaps = new List<Bitmap>();

        foreach (var size in sizes)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);

            var padding = size * 0.1f;
            var rect = new RectangleF(padding, padding, size - padding * 2, size - padding * 2);

            using var path2 = CreateShieldPath(rect);
            using var brush = new LinearGradientBrush(rect, Color.FromArgb(25, 118, 210), Color.FromArgb(21, 101, 192), 90f);
            g.FillPath(brush, path2);

            using var font = new Font("Segoe UI", size * 0.4f, FontStyle.Bold);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("Z", font, Brushes.White, new RectangleF(0, 0, size, size), sf);

            bitmaps.Add(bmp);
        }

        SaveAsIco(bitmaps, path);

        foreach (var bmp in bitmaps)
            bmp.Dispose();
    }

    private static void GenerateTrayIcon(string path, Color color)
    {
        if (File.Exists(path)) return;

        var sizes = new[] { 16, 32 };
        var bitmaps = new List<Bitmap>();

        foreach (var size in sizes)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);

            var padding = size * 0.1f;
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, padding, padding, size - padding * 2, size - padding * 2);

            using var font = new Font("Segoe UI", size * 0.35f, FontStyle.Bold);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("Z", font, Brushes.White, new RectangleF(0, 0, size, size), sf);

            bitmaps.Add(bmp);
        }

        SaveAsIco(bitmaps, path);

        foreach (var bmp in bitmaps)
            bmp.Dispose();
    }

    private static GraphicsPath CreateShieldPath(RectangleF rect)
    {
        var path = new GraphicsPath();
        var w = rect.Width;
        var h = rect.Height;
        var x = rect.X;
        var y = rect.Y;

        path.AddArc(x, y, w * 0.2f, w * 0.2f, 180, 90);
        path.AddArc(x + w - w * 0.2f, y, w * 0.2f, w * 0.2f, 270, 90);
        path.AddLine(x + w, y + h * 0.5f, x + w * 0.5f, y + h);
        path.AddLine(x + w * 0.5f, y + h, x, y + h * 0.5f);
        path.CloseFigure();

        return path;
    }

    private static void SaveAsIco(List<Bitmap> bitmaps, string path)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write((short)0);
        bw.Write((short)1);
        bw.Write((short)bitmaps.Count);

        var offset = 6 + bitmaps.Count * 16;
        var imageData = new List<byte[]>();

        foreach (var bmp in bitmaps)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var data = ms.ToArray();
            imageData.Add(data);

            bw.Write((byte)(bmp.Width == 256 ? 0 : bmp.Width));
            bw.Write((byte)(bmp.Height == 256 ? 0 : bmp.Height));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(data.Length);
            bw.Write(offset);

            offset += data.Length;
        }

        foreach (var data in imageData)
        {
            bw.Write(data);
        }
    }
}
