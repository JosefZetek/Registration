using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Registration.ApplicationCode.Other;
using System.Threading.Tasks;
using Avalonia.Svg.Skia;
using System;
using System.IO;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Samplers;

namespace Registration.Views
{
    public partial class SlicerSetupView : UserControl
    {
        private readonly ContentControl _mainContent;
        
        private IndicatorView _indicatorViewMeta;
        private IndicatorView _indicatorViewRaw;

        private string _metaPath = string.Empty;
        private string _rawPath = string.Empty;

        public SlicerSetupView(ContentControl mainContent)
        {
            InitializeComponent(); 
            _mainContent = mainContent;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _indicatorViewMeta = this.FindControl<IndicatorView>("IndicatorMeta");
            _indicatorViewRaw = this.FindControl<IndicatorView>("IndicatorRaw");

            if(_indicatorViewMeta == null || _indicatorViewRaw == null)
            {
                throw new Exception("One or more IndicatorView controls could not be found. Please check the XAML definitions.");
            }
        }

        private async Task<IStorageFile?> LoadPathFromUser(FilePickerOpenOptions filePicker)
        {
            var window = this.GetVisualRoot() as Window;

            if (window == null)
                throw new Exception("Window could not be found");

            var picks = await window.StorageProvider.OpenFilePickerAsync(filePicker);

            return picks.Count == 0 ? null : picks[0];
        }

        private bool CheckIfAllPathsSet()
        {
            return !string.IsNullOrEmpty(_metaPath) &&
                   !string.IsNullOrEmpty(_rawPath);
        }

        private void UpdateIndicator(IndicatorView indicator, string path, bool empty)
        {
            if (empty)
            {
                indicator.Opacity = 0.5;
                indicator.Icon = null;
            }
            else
            {
                indicator.Opacity = 1;
                indicator.Icon = new SvgImage
                {
                    Source = SvgSource.Load("avares://Registration/Assets/Images/checkmark.svg"),
                };
            }

            indicator.Text = path;
        }

        #region Button Event Handlers

        private async void OnMetaButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.MHD);
            if (receivedPath == null)
                return;

            _metaPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMeta, Path.GetFileName(receivedPath.Path.AbsolutePath), false);
        }

        private async void OnRawButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.RAW);
            if (receivedPath == null)
                return;

            _rawPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewRaw, Path.GetFileName(receivedPath.Path.AbsolutePath), false);
        }

        private void OnClearButton_Click(object? sender, EventArgs e)
        {
            _metaPath = string.Empty;
            _rawPath = string.Empty;

            UpdateIndicator(_indicatorViewMeta, "Meta Data", true);
            UpdateIndicator(_indicatorViewRaw, "Raw Data", true);
        }

        private void OnSliceButton_Click(object? sender, EventArgs e)
        {
            if (!CheckIfAllPathsSet())
            {
                Console.WriteLine("Not all paths have been set!");
                return;
            }

            FilePathDescriptor filePathDescriptor = new FilePathDescriptor(_metaPath, _rawPath);
            VolumetricData slicedData = new VolumetricData(filePathDescriptor);

            _mainContent.Content = new SlicerView(_mainContent, slicedData, new SamplerPercentile(0.1));
            //_mainContent.Content = new SlicerView(_mainContent, slicedData);
        }
        
        #endregion

        
    }
}