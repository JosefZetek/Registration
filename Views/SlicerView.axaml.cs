using System;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.DataClasses.Slicers;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Samplers;

namespace Registration.Views
{
    public partial class SlicerView : UserControl
    {
        private readonly ContentControl _mainContent;
        private ADataSlicer _dataSlicer;
        
        private Image SliceImageControl;
        
        private int _currentAxisIndex;
        private double _currentSliderValue;

        public SlicerView(ContentControl mainControl, AData imageData, ISampler sampler)
        {
            InitializeComponent();
            _mainContent = mainControl;
            _dataSlicer = new DataSlicer(imageData);
            UpdateSliceImage();
        } 
        
        public SlicerView(ContentControl mainControl, AData imageData)
        {
            InitializeComponent();
            _mainContent = mainControl;
            _dataSlicer = new DataSlicer(imageData);
            UpdateSliceImage();
        }

        public SlicerView(ContentControl mainContent, AData microData, AData macroData, Transform3D transformation)
        {
            InitializeComponent();
            _mainContent = mainContent;
            _dataSlicer = new TransformedDataSlicer(microData, macroData, transformation);
            UpdateSliceImage();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            SliceImageControl = this.FindControl<Image>("SliceImage");
            _currentAxisIndex = 0;
            _currentSliderValue = 0;
            
            if (SliceImageControl == null)
            {
                // Debug / log nebo vyhození výjimky pro rychlé zjištění problému
                System.Diagnostics.Debug.WriteLine("SliceImage not found - zkontrolujte x:Class a Build Action XAML.");
            }
        }

        #region Event Handlers
        
        private void OnAxisSelection_Changed(object? sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null)
                return;
            
            _currentAxisIndex = combo.SelectedIndex;
            UpdateSliceImage();
        }

        private void OnSliderValue_Changed(object? sender, RangeBaseValueChangedEventArgs e)
        {
            //download image from url and display it in SliceImage
            var slider = sender as Slider;
            if (slider == null)
                return;
            
            _currentSliderValue = slider.Value;
            UpdateSliceImage();
        }
        
        #endregion
        
        private void UpdateSliceImage()
        {
            if (SliceImageControl == null)
                return;
            
            int width = 500, height = 500;
            Color[][] imageData = _dataSlicer.Cut(_currentSliderValue, _currentAxisIndex, new CutResolution(width, height));

            int bytesPerPixel = 4; // BGRA
            byte[] pixels = new byte[width * height * bytesPerPixel];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = imageData[y][x];
                    int idx = (y * width + x) * bytesPerPixel;
                    pixels[idx + 0] = color.B;
                    pixels[idx + 1] = color.G;
                    pixels[idx + 2] = color.R;
                    pixels[idx + 3] = color.A;
                }
            }

            var writeable = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using (var fb = writeable.Lock())
            {
                // pokud je řádek bez padování, zkopírujeme najednou, jinak řádek po řádku
                if (fb.RowBytes == width * bytesPerPixel)
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
                }
                else
                {
                    int src = 0;
                    for (int y = 0; y < height; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixels, src, System.IntPtr.Add(fb.Address, y * fb.RowBytes), width * bytesPerPixel);
                        src += width * bytesPerPixel;
                    }
                }
            }
            SliceImageControl.Source = writeable;
        }

        private void DownloadImageFromUrl(string url)
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    using var response = await client.GetAsync(url).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    var bitmap = new Bitmap(stream);

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (SliceImageControl != null)
                        {
                            SliceImageControl.Source = bitmap;
                        }
                            
                        else
                        {
                            Console.WriteLine("SliceImage control is null.");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download image: {ex.Message}");
                }
            });
        }   
    }
}