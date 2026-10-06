using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap _bitmap;
        private WriteableBitmap _patternBitmap;
        private bool _isDrawing = false;
        private Point _lastMousePos;

        public MainWindow()
        {
            InitializeComponent();

            int width = 800;
            int height = 600;
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            CanvasImage.Source = _bitmap;

            ClearCanvas();
        }

        private void ClearCanvas()
        {
            _bitmap.Lock(); // Блокируем ОДИН раз на всю операцию
            try
            {
                for (int y = 0; y < _bitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < _bitmap.PixelWidth; x++)
                    {
                        GraphicsAlgorithms.SetPixelFast(_bitmap, x, y, Colors.White);
                    }
                }
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock(); // Гарантированно разблокируем даже при ошибке
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e) => ClearCanvas();

        private void LoadPattern_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(openFileDialog.FileName);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    _patternBitmap = new WriteableBitmap(bitmapImage);
                    PatternStatus.Text = $"Загружен: {Path.GetFileName(openFileDialog.FileName)}\nРазмер: {_patternBitmap.PixelWidth}x{_patternBitmap.PixelHeight}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        private void CanvasImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(CanvasImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;

            if (ModeDraw.IsChecked == true)
            {
                _isDrawing = true;
                _lastMousePos = pos;

                _bitmap.Lock();
                try
                {
                    GraphicsAlgorithms.SetPixelFast(_bitmap, x, y, Colors.Black);
                    _bitmap.AddDirtyRect(new Int32Rect(x, y, 1, 1));
                }
                finally { _bitmap.Unlock(); }
            }
            else if (ModeFillColor.IsChecked == true)
            {
                Color fillColor = Colors.LightGreen;
                // Внутри этого метода уже есть правильная блокировка
                GraphicsAlgorithms.FloodFillSpanColor(_bitmap, x, y, fillColor);
            }
            else if (ModeFillPattern.IsChecked == true)
            {
                if (_patternBitmap == null)
                {
                    MessageBox.Show("Сначала загрузите рисунок для заливки!");
                    return;
                }
                // Внутри этого метода уже есть правильная блокировка
                GraphicsAlgorithms.FloodFillSpanPattern(_bitmap, x, y, _patternBitmap);
            }
        }

        private void CanvasImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && ModeDraw.IsChecked == true)
            {
                Point pos = e.GetPosition(CanvasImage);
                int x = (int)pos.X;
                int y = (int)pos.Y;
                int lastX = (int)_lastMousePos.X;
                int lastY = (int)_lastMousePos.Y;

                int minX = Math.Min(lastX, x);
                int minY = Math.Min(lastY, y);
                int maxX = Math.Max(lastX, x);
                int maxY = Math.Max(lastY, y);

                _bitmap.Lock();
                try
                {
                    DrawLineSimpleFast(lastX, lastY, x, y, Colors.Black);
                    _bitmap.AddDirtyRect(new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
                }
                finally
                {
                    _bitmap.Unlock();
                }

                _lastMousePos = pos;
            }
        }

        private void CanvasImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;
        }

        // Быстрая временная реализация для рисования мышью (без внутренних блокировок)
        private void DrawLineSimpleFast(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                GraphicsAlgorithms.SetPixelFast(_bitmap, x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
    }
}