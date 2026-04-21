using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;
using System.Threading.Tasks;
using Avalonia.Svg.Skia;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.TransformationDistanceMetrics;

namespace Registration.Views
{
    public partial class RegistrationView : UserControl
    {
        private readonly ContentControl _mainContent;
        
        private IndicatorView _indicatorViewMicroMeta;
        private IndicatorView _indicatorViewMicroRaw;
        private IndicatorView _indicatorViewMacroMeta;
        private IndicatorView _indicatorViewMacroRaw;

        private string _microMetaPath = string.Empty;
        private string _macroMetaPath = string.Empty;
        private string _microRawPath = string.Empty;
        private string _macroRawPath = string.Empty;

        public RegistrationView(ContentControl mainContent)
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

            if(_indicatorViewMicroMeta == null ||
               _indicatorViewMicroRaw == null ||
               _indicatorViewMacroMeta == null ||
               _indicatorViewMacroRaw == null)
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
                   !string.IsNullOrEmpty(_macroMetaPath) &&
                   !string.IsNullOrEmpty(_microRawPath) &&
                   !string.IsNullOrEmpty(_macroRawPath);
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
            UpdateIndicator(_indicatorViewMicroMeta, Path.GetFileName(receivedPath.Path.AbsolutePath), false);
        }

        private async void OnSelectMicroRawButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.RAW);
            if (receivedPath == null)
                return;

            _microRawPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMicroRaw, Path.GetFileName(receivedPath.Path.AbsolutePath), false);
        }

        private async void OnSelectMacroMetaButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.MHD);
            if (receivedPath == null)
                return;

            _macroMetaPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMacroMeta, Path.GetFileName(receivedPath.Path.AbsolutePath), false);
        }

        private async void OnSelectMacroRawButton_Click(object? sender, EventArgs e)
        {
            var receivedPath = await LoadPathFromUser(FileExtensionPickers.RAW);
            if (receivedPath == null)
                return;

            _macroRawPath = receivedPath.Path.AbsolutePath;
            UpdateIndicator(_indicatorViewMacroRaw, Path.GetFileName(receivedPath.Path.AbsolutePath), false);
        }

        private void OnRunButton_Click(object? sender, EventArgs e)
        {
            if (!CheckIfAllPathsSet())
            {
                Console.WriteLine("Not all paths have been set!");
                return;
            }
            
            RegistrationLauncher registrationLauncher = new RegistrationLauncher();

            FilePathDescriptor microDataPath = new FilePathDescriptor(_microMetaPath, _microRawPath);
            FilePathDescriptor macroDataPath = new FilePathDescriptor(_macroMetaPath, _macroRawPath);
            
            VolumetricData microData = new VolumetricData(microDataPath);
            VolumetricData macroData = new VolumetricData(macroDataPath);

            Transform3D transformation = registrationLauncher.RunRegistration(microData, macroData);
            _mainContent.Content = new SlicerView(_mainContent, microData, macroData, transformation);
        }

        private void OnClearButton_Click(object? sender, EventArgs e)
        {
            _microMetaPath = string.Empty;
            _macroMetaPath = string.Empty;
            _microRawPath = string.Empty;
            _macroRawPath = string.Empty;

            UpdateIndicator(_indicatorViewMicroMeta, "Micro Meta Data", true);
            UpdateIndicator(_indicatorViewMicroRaw, "Micro Raw Data", true);
            UpdateIndicator(_indicatorViewMacroMeta, "Macro Meta Data", true);
            UpdateIndicator(_indicatorViewMacroRaw, "Macro Raw Data", true);

            //IndicatorMacroMeta.Icon = null;
            //IndicatorMacroMeta.Opacity = 0.5;
            Console.WriteLine("All paths have been cleared.");
        }
        #endregion
    }
}