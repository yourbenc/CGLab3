using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public static class GraphicsAlgorithms
    {
        // ==========================================
        // ЗАДАНИЕ 1а: Рекурсивная заливка серий пикселей цветом
        // ==========================================
        public static void FloodFillSpanColor(WriteableBitmap bitmap, int startX, int startY, Color fillColor)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            if (startX < 0 || startX >= width || startY < 0 || startY >= height)
                return;

            // Блокируем холст ОДИН раз на всю операцию заливки
            bitmap.Lock();
            try
            {
                Color targetColor = GetPixelFast(bitmap, startX, startY);
                if (targetColor == fillColor)
                    return;

                FillSpanRecursive(bitmap, startX, startY, targetColor, fillColor, width, height);

                // Обновляем весь холст (можно оптимизировать, вычисляя реальные границы)
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                bitmap.Unlock(); // Гарантированная разблокировка
            }
        }

        private static void FillSpanRecursive(WriteableBitmap bitmap, int x, int y, Color targetColor, Color fillColor, int width, int height)
        {
            int xLeft = x;
            while (xLeft >= 0 && GetPixelFast(bitmap, xLeft, y) == targetColor)
                xLeft--;
            xLeft++;

            int xRight = x;
            while (xRight < width && GetPixelFast(bitmap, xRight, y) == targetColor)
                xRight++;
            xRight--;

            for (int i = xLeft; i <= xRight; i++)
                SetPixelFast(bitmap, i, y, fillColor);

            // Строка выше
            if (y > 0)
            {
                int xNew = xLeft;
                while (xNew <= xRight)
                {
                    while (xNew <= xRight && GetPixelFast(bitmap, xNew, y - 1) != targetColor)
                        xNew++;
                    if (xNew > xRight) break;

                    int xSpanRight = xNew;
                    while (xSpanRight <= xRight && GetPixelFast(bitmap, xSpanRight, y - 1) == targetColor)
                        xSpanRight++;

                    FillSpanRecursive(bitmap, xSpanRight - 1, y - 1, targetColor, fillColor, width, height);
                    xNew = xSpanRight + 1;
                }
            }

            // Строка ниже
            if (y < height - 1)
            {
                int xNew = xLeft;
                while (xNew <= xRight)
                {
                    while (xNew <= xRight && GetPixelFast(bitmap, xNew, y + 1) != targetColor)
                        xNew++;
                    if (xNew > xRight) break;

                    int xSpanRight = xNew;
                    while (xSpanRight <= xRight && GetPixelFast(bitmap, xSpanRight, y + 1) == targetColor)
                        xSpanRight++;

                    FillSpanRecursive(bitmap, xSpanRight - 1, y + 1, targetColor, fillColor, width, height);
                    xNew = xSpanRight + 1;
                }
            }
        }

        // ==========================================
        // ЗАДАНИЕ 1б: Рекурсивная заливка серий пикселей рисунком
        // ==========================================
        public static void FloodFillSpanPattern(WriteableBitmap bitmap, int startX, int startY, WriteableBitmap patternBitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            if (startX < 0 || startX >= width || startY < 0 || startY >= height || patternBitmap == null)
                return;

            bitmap.Lock();
            try
            {
                Color targetColor = GetPixelFast(bitmap, startX, startY);
                FillSpanPatternRecursive(bitmap, startX, startY, targetColor, patternBitmap, width, height);
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        private static void FillSpanPatternRecursive(WriteableBitmap bitmap, int x, int y, Color targetColor, WriteableBitmap patternBitmap, int width, int height)
        {
            int xLeft = x;
            while (xLeft >= 0 && GetPixelFast(bitmap, xLeft, y) == targetColor)
                xLeft--;
            xLeft++;

            int xRight = x;
            while (xRight < width && GetPixelFast(bitmap, xRight, y) == targetColor)
                xRight++;
            xRight--;

            int patternWidth = patternBitmap.PixelWidth;
            int patternHeight = patternBitmap.PixelHeight;

            for (int i = xLeft; i <= xRight; i++)
            {
                // Циклическое замещение для маленьких файлов, 1:1 для больших (без масштабирования)
                int px = i % patternWidth;
                int py = y % patternHeight;

                Color patternColor = GetPixelFast(patternBitmap, px, py);
                SetPixelFast(bitmap, i, y, patternColor);
            }

            // Строка выше
            if (y > 0)
            {
                int xNew = xLeft;
                while (xNew <= xRight)
                {
                    while (xNew <= xRight && GetPixelFast(bitmap, xNew, y - 1) != targetColor)
                        xNew++;
                    if (xNew > xRight) break;
                    int xSpanRight = xNew;
                    while (xSpanRight <= xRight && GetPixelFast(bitmap, xSpanRight, y - 1) == targetColor)
                        xSpanRight++;
                    FillSpanPatternRecursive(bitmap, xSpanRight - 1, y - 1, targetColor, patternBitmap, width, height);
                    xNew = xSpanRight + 1;
                }
            }

            // Строка ниже
            if (y < height - 1)
            {
                int xNew = xLeft;
                while (xNew <= xRight)
                {
                    while (xNew <= xRight && GetPixelFast(bitmap, xNew, y + 1) != targetColor)
                        xNew++;
                    if (xNew > xRight) break;
                    int xSpanRight = xNew;
                    while (xSpanRight <= xRight && GetPixelFast(bitmap, xSpanRight, y + 1) == targetColor)
                        xSpanRight++;
                    FillSpanPatternRecursive(bitmap, xSpanRight - 1, y + 1, targetColor, patternBitmap, width, height);
                    xNew = xSpanRight + 1;
                }
            }
        }

        // ==========================================
        // Быстрые методы доступа к пикселям (БЕЗ Lock/Unlock)
        // Вызывающий код ДОЛЖЕН сам оборачивать вызовы в Lock/Unlock
        // ==========================================
        public static Color GetPixelFast(WriteableBitmap bitmap, int x, int y)
        {
            if (x < 0 || x >= bitmap.PixelWidth || y < 0 || y >= bitmap.PixelHeight)
                return Colors.Transparent;

            int stride = bitmap.BackBufferStride;
            int index = y * stride + x * 4;

            byte b = Marshal.ReadByte(bitmap.BackBuffer, index);
            byte g = Marshal.ReadByte(bitmap.BackBuffer, index + 1);
            byte r = Marshal.ReadByte(bitmap.BackBuffer, index + 2);
            byte a = Marshal.ReadByte(bitmap.BackBuffer, index + 3);

            return Color.FromArgb(a, r, g, b);
        }

        public static void SetPixelFast(WriteableBitmap bitmap, int x, int y, Color color)
        {
            if (x < 0 || x >= bitmap.PixelWidth || y < 0 || y >= bitmap.PixelHeight)
                return;

            int stride = bitmap.BackBufferStride;
            int index = y * stride + x * 4;

            Marshal.WriteByte(bitmap.BackBuffer, index, color.B);
            Marshal.WriteByte(bitmap.BackBuffer, index + 1, color.G);
            Marshal.WriteByte(bitmap.BackBuffer, index + 2, color.R);
            Marshal.WriteByte(bitmap.BackBuffer, index + 3, color.A);
        }

        // ==========================================
        // ЗАГОТОВКИ ДЛЯ КОЛЛЕГ (ЗАДАНИЯ 1в, 2, 3)
        // ==========================================
        /*
        // ЗАДАНИЕ 1в: Выделение границы связной области
        public static List<Point> TraceBoundary(WriteableBitmap bitmap, int startX, int startY, Color boundaryColor)
        {
            // bitmap.Lock(); ... try { ... } finally { bitmap.Unlock(); }
            return new List<Point>();
        }

        // ЗАДАНИЕ 2: Целочисленный алгоритм Брезенхема
        public static void DrawLineBresenham(WriteableBitmap bitmap, int x0, int y0, int x1, int y1, Color color)
        {
            // bitmap.Lock(); ... try { SetPixelFast(...); } finally { bitmap.Unlock(); }
        }

        // ЗАДАНИЕ 2: Алгоритм Ву
        public static void DrawLineWu(WriteableBitmap bitmap, int x0, int y0, int x1, int y1, Color color)
        {
            // bitmap.Lock(); ... try { SetPixelFast(...); } finally { bitmap.Unlock(); }
        }

        // ЗАДАНИЕ 3: Градиентное окрашивание треугольника
        public static void DrawGradientTriangle(WriteableBitmap bitmap, Point p1, Color c1, Point p2, Color c2, Point p3, Color c3)
        {
            // bitmap.Lock(); ... try { SetPixelFast(...); } finally { bitmap.Unlock(); }
        }
        */
    }
}