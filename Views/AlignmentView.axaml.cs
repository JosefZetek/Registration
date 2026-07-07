using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Svg.Skia;
using Avalonia.VisualTree;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.Views
{
    /// <summary>
    /// Lets the user pick a micro volume, a macro volume and a transformation (.txt, the
    /// format written by TransformationIO) and shows the aligned overlay in the slicer -
    /// without running a registration first.
    /// </summary>
    public partial class AlignmentView : UserControl
    {
        private readonly ContentControl _mainContent;

        private IndicatorView _indicatorViewMicroMeta;
        private IndicatorView _indicatorViewMicroRaw;
        private IndicatorView _indicatorViewMacroMeta;
        private IndicatorView _indicatorViewMacroRaw;
        private IndicatorView _indicatorViewTransformation;

        private string _microMetaPath = string.Empty;
        private string _microRawPath = string.Empty;
        private string _macroMetaPath = string.Empty;
        private string _macroRawPath = string.Empty;
        private string _transformationPath = string.Empty;

        public AlignmentView(ContentControl mainContent)
        {
            InitializeComponent();
            _mainContent = mainContent;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _indicatorViewMicroMeta = this.FindControl<IndicatorView>("IndicatorMicroMeta");
            _indicatorViewMicroRaw = this.FindControl<IndicatorView>("IndicatorMicroRaw");
            _indicatorViewMacroMeta = this.FindControl<IndicatorView>("IndicatorMacroMeta");
            _indicatorViewMacroRaw = this.FindControl<IndicatorView>("IndicatorMacroRaw");
            _indicatorViewTransformation = this.FindControl<IndicatorView>("IndicatorTransformation");

            if (_indicatorViewMicroMeta == null ||
                _indicatorViewMicroRaw == null ||
                _indicatorViewMacroMeta == null ||
                _indicatorViewMacroRaw == null ||
                _indicatorViewTransformation == null)
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
            return !string.IsNullOrEmpty(_microMetaPath) &&
                   !string.IsNullOrEmpty(_microRawPath) &&
                   !string.IsNullOrEmpty(_macroMetaPath) &&
                   !string.IsNullOrEmpty(_macroRawPath) &&
                   !string.IsNullOrEmpty(_transformationPath);
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

        private async void OnSelectMicroMetaButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.MHD);
            if (receivedPath == null)
                return;

            _microMetaPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMicroMeta, Path.GetFileName(_microMetaPath), false);
        }

        private async void OnSelectMicroRawButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.RAW);
            if (receivedPath == null)
                return;

            _microRawPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMicroRaw, Path.GetFileName(_microRawPath), false);
        }

        private async void OnSelectMacroMetaButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.MHD);
            if (receivedPath == null)
                return;

            _macroMetaPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMacroMeta, Path.GetFileName(_macroMetaPath), false);
        }

        private async void OnSelectMacroRawButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.RAW);
            if (receivedPath == null)
                return;

            _macroRawPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMacroRaw, Path.GetFileName(_macroRawPath), false);
        }

        private async void OnSelectTransformationButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.TXT);
            if (receivedPath == null)
                return;

            _transformationPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewTransformation, Path.GetFileName(_transformationPath), false);
        }

        private void OnShowButton_Click(object? sender, EventArgs e)
        {
            if (!CheckIfAllPathsSet())
            {
                Console.WriteLine("Not all paths have been set!");
                return;
            }

            Transform3D transformation = TransformationIO.FetchTransformation(_transformationPath);
            if (transformation == null)
            {
                Console.WriteLine($"Could not parse a transformation from {_transformationPath}");
                return;
            }

            VolumetricData microData = new VolumetricData(new FilePathDescriptor(_microMetaPath, _microRawPath));
            VolumetricData macroData = new VolumetricData(new FilePathDescriptor(_macroMetaPath, _macroRawPath));

            _mainContent.Content = new SlicerView(_mainContent, microData, macroData, transformation);
        }

        private void OnClearButton_Click(object? sender, EventArgs e)
        {
            _microMetaPath = string.Empty;
            _microRawPath = string.Empty;
            _macroMetaPath = string.Empty;
            _macroRawPath = string.Empty;
            _transformationPath = string.Empty;

            UpdateIndicator(_indicatorViewMicroMeta, "Micro Meta Data", true);
            UpdateIndicator(_indicatorViewMicroRaw, "Micro Raw Data", true);
            UpdateIndicator(_indicatorViewMacroMeta, "Macro Meta Data", true);
            UpdateIndicator(_indicatorViewMacroRaw, "Macro Raw Data", true);
            UpdateIndicator(_indicatorViewTransformation, "Transformation", true);
        }

        #endregion
    }
}
