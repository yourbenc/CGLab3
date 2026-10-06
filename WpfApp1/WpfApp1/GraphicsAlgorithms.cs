using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

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

        // ЗАДАНИЕ 1в: Выделение границы связной области
        public static List<Point> TraceBoundary(WriteableBitmap bitmap,int startX,int startY,Color boundaryColor)
        {
            // Список, куда будем записывать точки границы
            // в порядке их обхода
            List<Point> boundary = new List<Point>();

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // Проверяем, что начальная точка находится внутри изображения
            if (startX < 0 || startX >= width ||
                startY < 0 || startY >= height)
            {
                return boundary;
            }

            bitmap.Lock();

            try
            {
                // Проверяем, действительно ли начальная точка
                // принадлежит границе нужного цвета
                if (GetPixelFast(bitmap, startX, startY) != boundaryColor)
                    return boundary;

                // 8 соседей пикселя.
                // Порядок обхода — по часовой стрелке.
                int[] dx = { -1, 0, 1, 1, 1, 0, -1, -1 };
                int[] dy = { -1, -1, -1, 0, 1, 1, 1, 0 };

                Point start = new Point(startX, startY);

                // Текущая точка границы
                Point current = start;

                // Начинаем поиск соседей с точки слева
                // от начального пикселя
                Point backtrack = new Point(startX - 1, startY);

                // Первая точка после начальной.
                // Она понадобится для определения завершения обхода.
                Point? firstNext = null;

                // Добавляем начальную точку в список
                boundary.Add(start);

                // Защита от бесконечного цикла
                int maxSteps = width * height * 8;
                int steps = 0;

                while (steps < maxSteps)
                {
                    steps++;

                    // Определяем, с какого соседа начинать поиск
                    int backIndex = 0;

                    int backDx = (int)backtrack.X - (int)current.X;
                    int backDy = (int)backtrack.Y - (int)current.Y;

                    for (int i = 0; i < 8; i++)
                    {
                        if (dx[i] == backDx && dy[i] == backDy)
                        {
                            backIndex = i;
                            break;
                        }
                    }

                    bool found = false;
                    Point next = current;
                    Point newBacktrack = backtrack;

                    // Проверяем 8 соседних пикселей по часовой стрелке
                    for (int step = 1; step <= 8; step++)
                    {
                        int index = (backIndex + step) % 8;

                        int nx = (int)current.X + dx[index];
                        int ny = (int)current.Y + dy[index];

                        // Проверяем границы изображения
                        if (nx < 0 || nx >= width ||
                            ny < 0 || ny >= height)
                        {
                            continue;
                        }

                        // Если сосед имеет цвет границы,
                        // значит нашли следующую точку
                        if (GetPixelFast(bitmap, nx, ny) == boundaryColor)
                        {
                            next = new Point(nx, ny);

                            // Запоминаем пиксель перед найденным соседом.
                            // Отсюда начнём поиск на следующем шаге.
                            int previousIndex = (index + 7) % 8;

                            newBacktrack = new Point(
                                current.X + dx[previousIndex],
                                current.Y + dy[previousIndex]);

                            found = true;
                            break;
                        }
                    }

                    // Если соседняя точка границы не найдена,
                    // значит продолжать обход невозможно
                    if (!found)
                        break;

                    // Запоминаем первую точку после стартовой
                    if (firstNext == null)
                    {
                        firstNext = next;
                    }
                    else
                    {
                        // Если вернулись в начальную точку
                        // и снова собираемся идти в первую найденную точку,
                        // значит полный обход границы завершён
                        if (current == start && next == firstNext.Value)
                            break;
                    }

                    // Переходим к следующей точке
                    current = next;
                    backtrack = newBacktrack;

                    // Начальную точку второй раз в список не добавляем
                    if (current != start)
                    {
                        boundary.Add(current);
                    }
                }
            }
            finally
            {
                bitmap.Unlock();
            }

            // Возвращаем точки границы
            // в порядке их обхода
            return boundary;
        }

        // ЗАДАНИЕ 2: Целочисленный алгоритм Брезенхема
        public static void DrawLineBresenham(WriteableBitmap bitmap,int x0,int y0,int x1,int y1,Color color)
        {
            bitmap.Lock();

            try
            {
                // Разница по X
                int dx = Math.Abs(x1 - x0);

                // Разница по Y
                int dy = Math.Abs(y1 - y0);

                // Направление движения по X
                int sx = x0 < x1 ? 1 : -1;

                // Направление движения по Y
                int sy = y0 < y1 ? 1 : -1;

                // Ошибка алгоритма
                int err = dx - dy;

                while (true)
                {
                    // Рисуем текущий пиксель
                    SetPixelFast(bitmap, x0, y0, color);

                    // Если дошли до конечной точки
                    if (x0 == x1 && y0 == y1)
                        break;

                    // Удвоенная ошибка
                    int e2 = 2 * err;

                    // Решаем, нужно ли двигаться по X
                    if (e2 > -dy)
                    {
                        err -= dy;
                        x0 += sx;
                    }

                    // Решаем, нужно ли двигаться по Y
                    if (e2 < dx)
                    {
                        err += dx;
                        y0 += sy;
                    }
                }

                // Обновляем изображение
                bitmap.AddDirtyRect(
                    new Int32Rect(
                        0,
                        0,
                        bitmap.PixelWidth,
                        bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        // ЗАДАНИЕ 2: Алгоритм Ву
        public static void DrawLineWu(WriteableBitmap bitmap,int x0,int y0,int x1,int y1,Color color)
        {
            bitmap.Lock();

            try
            {
                // Проверяем, является ли линия более вертикальной, чем горизонтальной
                bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

                // Если линия крутая, меняем X и Y местами
                if (steep)
                {
                    Swap(ref x0, ref y0);
                    Swap(ref x1, ref y1);
                }

                // Всегда рисуем слева направо
                if (x0 > x1)
                {
                    Swap(ref x0, ref x1);
                    Swap(ref y0, ref y1);
                }

                // Разница координат
                double dx = x1 - x0;
                double dy = y1 - y0;

                // Наклон линии
                double gradient = dx == 0 ? 1 : dy / dx;

                // Текущее значение Y
                double y = y0;

                // Проходим от начала до конца линии
                for (int x = x0; x <= x1; x++)
                {
                    // Целая часть Y
                    int yInt = (int)Math.Floor(y);

                    // Дробная часть Y
                    double fraction = y - yInt;

                    if (steep)
                    {
                        // Для крутой линии координаты меняются местами

                        PlotWuPixel(
                            bitmap,
                            yInt,
                            x,
                            color,
                            1.0 - fraction);

                        PlotWuPixel(
                            bitmap,
                            yInt + 1,
                            x,
                            color,
                            fraction);
                    }
                    else
                    {
                        // Основной пиксель
                        PlotWuPixel(
                            bitmap,
                            x,
                            yInt,
                            color,
                            1.0 - fraction);

                        // Соседний пиксель
                        PlotWuPixel(
                            bitmap,
                            x,
                            yInt + 1,
                            color,
                            fraction);
                    }

                    // Переходим к следующему X
                    y += gradient;
                }

                bitmap.AddDirtyRect(
                    new Int32Rect(
                        0,
                        0,
                        bitmap.PixelWidth,
                        bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }
        // Рисует один пиксель с заданной интенсивностью
        private static void PlotWuPixel(
            WriteableBitmap bitmap,
            int x,
            int y,
            Color color,
            double intensity)
        {
            if (x < 0 || x >= bitmap.PixelWidth ||
                y < 0 || y >= bitmap.PixelHeight)
                return;

            // Ограничиваем интенсивность от 0 до 1
            intensity = Math.Max(0, Math.Min(1, intensity));

            // Получаем текущий цвет пикселя
            Color background = GetPixelFast(bitmap, x, y);

            // Смешиваем цвет линии с цветом фона
            byte r = (byte)(
                background.R * (1 - intensity) +
                color.R * intensity);

            byte g = (byte)(
                background.G * (1 - intensity) +
                color.G * intensity);

            byte b = (byte)(
                background.B * (1 - intensity) +
                color.B * intensity);

            Color result = Color.FromArgb(
                255,
                r,
                g,
                b);

            SetPixelFast(bitmap, x, y, result);
        }


        // Меняет два целых числа местами
        private static void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        // ЗАДАНИЕ 3: Градиентное окрашивание треугольника
        public static void DrawGradientTriangle(WriteableBitmap bitmap, Point p1, Color c1, Point p2, Color c2, Point p3, Color c3)
        {
            // bitmap.Lock(); ... try { SetPixelFast(...); } finally { bitmap.Unlock(); }
        }
        
    }
}