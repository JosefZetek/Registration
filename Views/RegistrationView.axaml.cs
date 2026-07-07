using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using System;
using System.IO;
using Registration.ApplicationCode.DataClasses.Data;

namespace Registration.Views
{
    public partial class RegistrationView : UserControl
    {
        private readonly ContentControl _mainContent;
        
        private IndicatorView _indicatorViewMicroMeta;
        private IndicatorView _indicatorViewMicroRaw;
        private IndicatorView _indicatorViewMacroMeta;
        private IndicatorView _indicatorViewMacroRaw;

        private StackPanel _progressPanel;
        private TextBlock _stageText;
        private TextBlock _progressText;
        private ProgressBar _featureProgressBar;

        /* Feature-computation progress counters. Written from the worker threads
           inside the registration launcher (via Interlocked) and polled by a
           DispatcherTimer on the UI thread, so the many thousands of per-feature
           updates never flood the UI dispatcher. */
        private long _featureProgressCount;
        private long _featureProgressMax;
        private bool _isRunning;

        /* Latest stage reported by the launcher (worker thread) -> rendered by the UI timer. */
        private volatile string _stageLabel = string.Empty;

        /* Cancels the current run when the view is closed / navigated away from. */
        private CancellationTokenSource? _runCancellation;

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

            _progressPanel = this.FindControl<StackPanel>("ProgressPanel");
            _stageText = this.FindControl<TextBlock>("StageText");
            _progressText = this.FindControl<TextBlock>("ProgressText");
            _featureProgressBar = this.FindControl<ProgressBar>("FeatureProgressBar");

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

        private async void OnRunButton_Click(object? sender, EventArgs e)
        {
            if (_isRunning)
                return;

            if (!CheckIfAllPathsSet())
            {
                Console.WriteLine("Not all paths have been set!");
                return;
            }

            _isRunning = true;
            _runCancellation = new CancellationTokenSource();

            // Progress callbacks are invoked from the launcher's worker threads.
            // Keep them lock-free: just bump shared counters / store the stage and
            // let the UI timer render them.
            var progress = new RegistrationProgress
            {
                OnFeatureComputed = () => Interlocked.Increment(ref _featureProgressCount),
                OnFeatureBatchStart = count => Interlocked.Add(ref _featureProgressMax, count),
                OnStageChanged = (stage, total, name) => _stageLabel = $"Stage {stage}/{total}: {name}",
                CancellationToken = _runCancellation.Token,
            };

            StartProgress();

            FilePathDescriptor microDataPath = new FilePathDescriptor(_microMetaPath, _microRawPath);
            FilePathDescriptor macroDataPath = new FilePathDescriptor(_macroMetaPath, _macroRawPath);

            VolumetricData microData = null;
            VolumetricData macroData = null;
            Transform3D transformation = null;
            bool cancelled = false;

            try
            {
                transformation = await Task.Run(() =>
                {
                    microData = new VolumetricData(microDataPath);
                    macroData = new VolumetricData(macroDataPath);

                    RegistrationLauncher registrationLauncher =
                        new RegistrationLauncher(progress);

                    return registrationLauncher.RunRegistration(microData, macroData);
                }, progress.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                Console.WriteLine("Registration cancelled.");
            }
            finally
            {
                StopProgress();
                _runCancellation?.Dispose();
                _runCancellation = null;
                _isRunning = false;
            }

            // The view may already have been navigated away from (that's what
            // triggered the cancellation) - don't push a result view in that case.
            if (cancelled)
                return;

            Console.WriteLine(transformation);
            _mainContent.Content = new SlicerView(_mainContent, microData, macroData, transformation);
        }

        /// <summary>
        /// When the view leaves the visual tree (user navigates away / closes it),
        /// cancel any in-flight registration so the heavy computation stops.
        /// </summary>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _runCancellation?.Cancel();
            base.OnDetachedFromVisualTree(e);
        }

        #region Feature Computation Progress

        private DispatcherTimer? _progressTimer;

        private void StartProgress()
        {
            Interlocked.Exchange(ref _featureProgressCount, 0);
            Interlocked.Exchange(ref _featureProgressMax, 0);
            _stageLabel = string.Empty;

            _featureProgressBar.Value = 0;
            _featureProgressBar.Maximum = 1;
            _featureProgressBar.IsIndeterminate = true;
            _stageText.Text = "Starting...";
            _progressText.Text = string.Empty;
            _progressPanel.IsVisible = true;

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _progressTimer.Tick += OnProgressTick;
            _progressTimer.Start();
        }

        private void OnProgressTick(object? sender, EventArgs e)
        {
            string stage = _stageLabel;
            if (!string.IsNullOrEmpty(stage))
                _stageText.Text = stage;

            long max = Interlocked.Read(ref _featureProgressMax);
            long value = Interlocked.Read(ref _featureProgressCount);

            // No feature batch has reported its size yet (e.g. sampling stage):
            // keep the bar indeterminate and show no count.
            if (max <= 0)
            {
                _featureProgressBar.IsIndeterminate = true;
                _progressText.Text = string.Empty;
                return;
            }

            _featureProgressBar.IsIndeterminate = false;
            _featureProgressBar.Maximum = max;
            _featureProgressBar.Value = Math.Min(value, max);
            _progressText.Text = $"Feature vectors: {value} / {max}";
        }

        private void StopProgress()
        {
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _progressTimer.Tick -= OnProgressTick;
                _progressTimer = null;
            }

            _featureProgressBar.IsIndeterminate = false;
            _progressPanel.IsVisible = false;
        }

        #endregion

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