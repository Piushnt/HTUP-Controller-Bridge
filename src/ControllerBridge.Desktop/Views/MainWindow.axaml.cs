using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;

namespace ControllerBridge.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetApplicationIcon();
    }

    private void SetApplicationIcon()
    {
        try
        {
            int width = 32;
            int height = 32;
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var fb = bitmap.Lock())
            {
                int stride = fb.RowBytes;
                byte[] pixels = new byte[stride * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * 4;
                        float nx = (x - 16f) / 13f;
                        float ny = (y - 16f) / 10f;
                        float dist = (nx * nx) + (ny * ny);

                        if (dist <= 1.0f)
                        {
                            float t = (float)x / width;
                            byte r = (byte)(6 + t * (139 - 6));
                            byte g = (byte)(182 - t * (182 - 92));
                            byte b = (byte)(212 + t * (246 - 212));
                            byte a = 255;

                            // Small D-pad & ABXY button impressions in white
                            if ((x >= 8 && x <= 13 && y >= 14 && y <= 18) ||
                                (x >= 10 && x <= 11 && y >= 12 && y <= 20) ||
                                (x >= 19 && x <= 23 && y >= 13 && y <= 19))
                            {
                                r = 255; g = 255; b = 255;
                            }

                            pixels[index] = b;     // Blue
                            pixels[index + 1] = g; // Green
                            pixels[index + 2] = r; // Red
                            pixels[index + 3] = a; // Alpha
                        }
                    }
                }
                Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
            }
            this.Icon = new WindowIcon(bitmap);
        }
        catch { }
    }
}
