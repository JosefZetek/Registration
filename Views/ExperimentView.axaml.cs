using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;
using Registration.ApplicationCode.TransformationDistanceMetrics;

namespace Registration.Views
{
    /// <summary>
    /// Runs the experiment configured in <see cref="ExperimentSetupDialog"/>: registers each
    /// microData{i} volume onto the macro volume, reports progress while computing, then shows
    /// the relative distance to the ground-truth transformation per test as a chart and lets
    /// the user export the results to CSV.
    /// </summary>
    public partial class ExperimentView : UserControl
    {
        private readonly ContentControl _mainContent;
        private readonly ExperimentConfiguration _configuration;

        private StackPanel _progressPanel;
        private TextBlock _testText;
        private TextBlock _stageText;
        private TextBlock _featureText;
        private TextBlock _summaryText;
        private ProgressBar _testProgressBar;
        private ProgressBar _featureProgressBar;
        private CartesianChart _resultsChart;
        private Button _exportButton;

        /* Feature-computation counters written by the launcher's worker threads and polled
           by a DispatcherTimer on the UI thread (same pattern as RegistrationView). */
        private long _featureProgressCount;
        private long _featureProgressMax;
        private volatile string _stageLabel = string.Empty;
        private DispatcherTimer? _progressTimer;

        private CancellationTokenSource? _runCancellation;

        /* One entry per finished test. A failed test is recorded as double.NaN so the
           CSV still lists it. */
        private readonly List<(string Macro, string Test, double Distance)> _results = new();

        public ExperimentView(ContentControl mainContent, ExperimentConfiguration configuration)
        {
            _mainContent = mainContent;
            _configuration = configuration;
            InitializeComponent();
            RunExperiments();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _progressPanel = this.FindControl<StackPanel>("ProgressPanel");
            _testText = this.FindControl<TextBlock>("TestText");
            _stageText = this.FindControl<TextBlock>("StageText");
            _featureText = this.FindControl<TextBlock>("FeatureText");
            _summaryText = this.FindControl<TextBlock>("SummaryText");
            _testProgressBar = this.FindControl<ProgressBar>("TestProgressBar");
            _featureProgressBar = this.FindControl<ProgressBar>("FeatureProgressBar");
            _resultsChart = this.FindControl<CartesianChart>("ResultsChart");
            _exportButton = this.FindControl<Button>("ExportButton");
        }

        private async void RunExperiments()
        {
            _runCancellation = new CancellationTokenSource();

            var progress = new RegistrationProgress
            {
                OnFeatureComputed = () => Interlocked.Increment(ref _featureProgressCount),
                OnFeatureBatchStart = count => Interlocked.Add(ref _featureProgressMax, count),
                OnStageChanged = (stage, total, name) => _stageLabel = $"Stage {stage}/{total}: {name}",
                CancellationToken = _runCancellation.Token,
            };

            int numberOfTests = _configuration.TotalTests;
            _testProgressBar.Maximum = numberOfTests;
            StartProgressTimer();

            try
            {
                await Task.Run(() =>
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                    int testNumber = 0;

                    foreach (ExperimentGroup group in _configuration.Groups)
                    {
                        progress.CancellationToken.ThrowIfCancellationRequested();

                        VolumetricData macro = new VolumetricData(
                            new FilePathDescriptor(group.MacroMhdPath, group.MacroRawPath));

                        foreach (ExperimentTestCase test in group.Tests)
                        {
                            progress.CancellationToken.ThrowIfCancellationRequested();

                            testNumber++;
                            int currentTest = testNumber;
                            string testLabel = $"{group.Name}: {test.Name}";
                            Dispatcher.UIThread.Post(() => OnTestStarted(currentTest, numberOfTests, testLabel));

                            /* Each test gets fresh feature counters so the bar restarts per test. */
                            Interlocked.Exchange(ref _featureProgressCount, 0);
                            Interlocked.Exchange(ref _featureProgressMax, 0);

                            double distance = double.NaN;

                            try
                            {
                                VolumetricData micro = new VolumetricData(new FilePathDescriptor(
                                    test.MicroMhdPath, test.MicroRawPath));

                                Transform3D expectedTransformation = TransformationIO.FetchTransformation(test.GroundTruthPath);

                                RegistrationLauncher registrationLauncher = new RegistrationLauncher(progress);
                                Transform3D resultTransformation = registrationLauncher.RunRegistration(micro, macro);

                                /* RunRegistration installs the metric for the current micro volume,
                                   but set it explicitly so the distance never depends on internals. */
                                Transform3D.SetTransformationDistance(new TransformationDistanceSeven(micro));

                                if (expectedTransformation != null && resultTransformation != null)
                                    distance = expectedTransformation.RelativeDistanceTo(resultTransformation);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine($"Test {testLabel} failed: {exception.Message}");
                            }

                            _results.Add((group.Name, test.Name, distance));
                            Dispatcher.UIThread.Post(() => OnTestFinished(currentTest));

                            /* Volumes are large; make sure the previous micro volume is released
                               before the next one is loaded. */
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                        }

                        macro = null;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                    }
                }, progress.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Experiment run cancelled.");
                return;
            }
            finally
            {
                StopProgressTimer();
                _runCancellation?.Dispose();
                _runCancellation = null;
            }

            _progressPanel.IsVisible = false;
            _exportButton.IsEnabled = true;

            var finished = _results.Where(r => !double.IsNaN(r.Distance)).ToList();
            _summaryText.Text = finished.Count > 0
                ? $"Finished {finished.Count}/{numberOfTests} tests, mean relative distance: {finished.Average(r => r.Distance):0.####}"
                : $"All {numberOfTests} tests failed.";
        }

        private void OnTestStarted(int testNumber, int numberOfTests, string testLabel)
        {
            _testText.Text = $"Running test {testNumber} / {numberOfTests} ({testLabel})";
            _testProgressBar.Value = testNumber - 1;
        }

        private void OnTestFinished(int testNumber)
        {
            _testProgressBar.Value = testNumber;
            UpdateChart();
        }

        /// <summary>
        /// When the view leaves the visual tree (user navigates away), cancel the run.
        /// </summary>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _runCancellation?.Cancel();
            base.OnDetachedFromVisualTree(e);
        }

        #region Feature Computation Progress

        private void StartProgressTimer()
        {
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

            if (max <= 0)
            {
                _featureProgressBar.IsIndeterminate = true;
                _featureText.Text = string.Empty;
                return;
            }

            _featureProgressBar.IsIndeterminate = false;
            _featureProgressBar.Maximum = max;
            _featureProgressBar.Value = Math.Min(value, max);
            _featureText.Text = $"Feature vectors: {value} / {max}";
        }

        private void StopProgressTimer()
        {
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _progressTimer.Tick -= OnProgressTick;
                _progressTimer = null;
            }

            _featureProgressBar.IsIndeterminate = false;
        }

        #endregion

        #region Results Chart & CSV Export

        private void UpdateChart()
        {
            /* NaN (failed test) entries stay in the CSV but are left out of the chart. */
            var plottable = _results.Where(r => !double.IsNaN(r.Distance)).ToList();

            _resultsChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Relative distance",
                    Values = plottable.Select(r => r.Distance).ToArray(),
                    /* Hovering a column shows which macro/test it belongs to. */
                    YToolTipLabelFormatter = point =>
                        $"{plottable[point.Index].Macro}: {plottable[point.Index].Test} = {point.Coordinate.PrimaryValue:0.####}",
                }
            };

            _resultsChart.XAxes = new[]
            {
                new Axis
                {
                    Labels = plottable.Select(r => $"{r.Macro}: {r.Test}").ToArray(),
                    LabelsRotation = plottable.Count > 6 ? 45 : 0,
                }
            };

            _resultsChart.YAxes = new[]
            {
                new Axis { Name = "Relative distance to ground truth", MinLimit = 0 }
            };
        }

        private async void OnExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var window = this.GetVisualRoot() as Window;
            if (window == null)
                return;

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export experiment results",
                SuggestedFileName = "experimentResults.csv",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } }
                },
            });

            if (file == null)
                return;

            string[][] rows = new string[_results.Count + 1][];
            rows[0] = new[] { "macro", "test", "relativeDistance" };

            for (int i = 0; i < _results.Count; i++)
            {
                rows[i + 1] = new[]
                {
                    _results[i].Macro,
                    _results[i].Test,
                    _results[i].Distance.ToString(CultureInfo.InvariantCulture),
                };
            }

            CSVWriter.WriteResult(file.Path.AbsolutePath, rows);
            Console.WriteLine($"Results exported to {file.Path.AbsolutePath}");
        }

        #endregion
    }
}
