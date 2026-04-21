using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Avalonia.Threading;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.TransformationDistanceMetrics;

namespace Registration.Views
{
    public partial class ProgressView : UserControl
    {
        private readonly ContentControl _mainContent;
        
        private ProgressBar _numberOfTestsProgressBar;
        private ProgressBar _featureProgressBar;

        public ProgressView(ContentControl mainContent)
        {
            InitializeComponent();
            _mainContent = mainContent;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _numberOfTestsProgressBar = this.FindControl<ProgressBar>("NumberOfTests");
            _featureProgressBar = this.FindControl<ProgressBar>("FeatureComputation");
            RunExperiments();
        }

        private void SetProgressBars(int numberOfTests, int numberOfFeatures)
        {
            _numberOfTestsProgressBar.Minimum = 0;
            _numberOfTestsProgressBar.Maximum = numberOfTests;
            _numberOfTestsProgressBar.Value = 0;
            
            _featureProgressBar.Minimum = 0;
            _featureProgressBar.Maximum = numberOfFeatures;
            _featureProgressBar.Value = 0;
        }

        private void RunExperiments()
        {
            SetProgressBars(5, 200);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Constants.NUMBER_OF_POINTS_MACRO = 10_000;
            Constants.NUMBER_OF_POINTS_MICRO = 10_000;

            List<string[]> distances = new List<string[]>();

            string[] folders = new string[]
            {
                // "/Users/pepazetek/Desktop/Tests/Elipsoid/",
                "/Users/pepazetek/Desktop/Tests/Jatra/",
                // "/Users/pepazetek/Desktop/Tests/Trup/",
                // "/Users/pepazetek/Desktop/Tests/MRI/",
            };

            string[] configs = new string[]
            {
                "/Users/pepazetek/Desktop/Tests/Config/config_FullFeatureSet.json"
            };

            foreach (string config in configs)
            {
                // var rootConfigurationElement = RootConfigurationElement.Deserialize(config);
                // if (rootConfigurationElement == null)
                //     continue;

                // POSÍLÁME PŘÍMO UI-safe metodu (bez závorek)
                // RegistrationLauncher registrationLauncher = new RegistrationLauncher(
                    // rootConfigurationElement.GetCompoundFeatureComputer(),
                    // IncreaseFeatureProgressBar);
                    
                RegistrationLauncher registrationLauncher = new RegistrationLauncher();
                
                distances.Add(new string[] { config, "microIndex", "distance", "microVolume" });

                foreach (string folder in folders)
                {
                    FilePathDescriptor macroData = new FilePathDescriptor($"{folder}macroData.mhd", $"{folder}macroData.raw");
                    VolumetricData macro = new VolumetricData(macroData);

                    for (int i = 3; i <= 3; i++)
                    {
                        FilePathDescriptor microData = new FilePathDescriptor($"{folder}microData{i}.mhd", $"{folder}microData{i}.raw");
                        VolumetricData micro = new VolumetricData(microData);

                        Transform3D expectedTransformation = TransformationIO.FetchTransformation($"{folder}microData{i}.txt");
                        
                        // RegistrationLauncher.EXPECTED_TRANSFORMATION = expectedTransformation;
                        
                        Transform3D resultTransformation = registrationLauncher.RunRegistration(micro, macro);

                        Transform3D.SetTransformationDistance(new TransformationDistanceSeven(micro));

                        double distance = expectedTransformation.RelativeDistanceTo(resultTransformation);
                        
                        distances.Add(new string[]
                        {
                            folder, i.ToString(), $"{distance}",
                            (micro.Measures[0] * micro.Measures[1] * micro.Measures[2]).ToString()
                        });

		                Console.WriteLine($"{folder}, {i}");

                        micro = null;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                        // IncreaseTestProgressBar();
                    }

                    macro = null;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                }
                
                // Write distances to file
                CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/distancesFake_50000.csv", distances.ToArray());
            }
        }
    }
}
