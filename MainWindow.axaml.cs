using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.FeatureNormalization;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;
using Registration.ApplicationCode.Test;
using Registration.ApplicationCode.TransformationDistanceMetrics;
using Registration.Views;

namespace Registration;

public partial class MainWindow : Window
{
    
    private const double SidebarWidth = 300;
    
    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = new RegistrationView(MainContent);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        //set US locale
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        // RegistrationLauncher.EXPECTED_TRANSFORMATION = TransformationIO.FetchTransformation("/Users/pepazetek/Desktop/Tests/Trup/microData1.txt");
        
        
        // HeadlessRegistrationTest.Run(new string[0]);


        // var dataObject = new VolumetricData(new FilePathDescriptor("/Users/pepazetek/Desktop/Tests/Jatra/microData1.mhd", "/Users/pepazetek/Desktop/Tests/Jatra/microData1.raw"));
        // RegistrationLauncher rl = new RegistrationLauncher();
        // rl.FeatureVarianceInDifferentRegions(dataObject);
        // FeatureNormalizer globalFeatureNormalizer = rl.GetFeatureNormalizer(dataObject);
        // //
        // Console.WriteLine($"variance: {rl.FeatureDifferenceMean(dataObject, globalFeatureNormalizer, 1)}");
        // var transform3D = TransformationIO.FetchTransformation("/Users/pepazetek/Desktop/Tests/Jatra/microData1.txt");

        // DenseMatrix 3x3-Double
        // 0,980517  -0,196401  0,00358879
        // 0,177678   0,878956   -0,442568
        // 0,0837666   0,434583    0,896728
        // DenseVector 3-Double
        // 13,5806
        // 18,86
        // 14,8816
        // Matrix<double> matrix = Matrix<double>.Build.DenseOfArray(new double[,]
        // {
        //     {0.980517, -0.196401, 0.00358879},
        //     { 0.177678, 0.878956, -0.442568 },
        //     { 0.0837666, 0.434583, 0.896728}
        // });
        //
        // Vector<double> vector = Vector<double>.Build.DenseOfArray(new double[] {13.5806, 18.86, 14.8816});
        //
        //
        // Transform3D.SetTransformationDistance(new TransformationDistanceSeven(dataObject));
        //
        // Transform3D transformation1 = new Transform3D(matrix, vector);
        // Console.WriteLine(transformation1.RelativeDistanceTo(transform3D));
        // Console.WriteLine(transformation1.RelativeDistanceTo(transformation1));



        // var rl = new RegistrationLauncher();
        //
        // var dataObject = new VolumetricData(new FilePathDescriptor("/Users/pepazetek/Desktop/Tests/Jatra/macroData.mhd", "/Users/pepazetek/Desktop/Tests/Jatra/macroData.raw"));

        // Console.WriteLine($"difference mean: {rl.FeatureDifferenceMean(dataObject, globalFeatureNormalizer, 1.0)}");    
        // rl.FeatureVarianceInDifferentRegions(dataObject, 20);

        // //

        // // // double threshold = 0.03 * Math.Sqrt(dataObject.MaxValueX * dataObject.MaxValueX + dataObject.MaxValueY * dataObject.MaxValueY + dataObject.MaxValueZ * dataObject.MaxValueZ);
        // // // Console.WriteLine($"Threshold: {threshold}");
        // // // return;
        // //


        // int numberOfReferenceParams = 10;
        // string[][] dataToSave = new string[numberOfReferenceParams+1][];
        //
        // dataToSave[0] = ["Param", "Closer", "Further"];
        //
        // for (int i = 0; i < numberOfReferenceParams; i++)
        // {
        //     double param = (i + 1) * 0.05;
        //     rl.setParam(param);
        //     //func bellow is 5000 fv calc per call
        //     var (closer, further) = rl.FeatureVarianceTest(dataObject, globalFeatureNormalizer);
        //
        //     dataToSave[i+1] = [param.ToString(CultureInfo.InvariantCulture), closer.ToString(), further.ToString()];
        //
        //     Console.WriteLine($"Finished {i}/{numberOfReferenceParams}");
        // }
        //
        // CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/feature_variance_test_curvature_advanced_3.csv", dataToSave);
    }

    private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
    {
        Sidebar.Width = Sidebar.Width == 0 ? SidebarWidth : 0;
    }

    private void OnRegistration_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = new RegistrationView(MainContent);
    }

    private void OnSlicer_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = new SlicerSetupView(MainContent);
    }

    private void OnAlignment_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = new AlignmentView(MainContent);
    }

    private async void OnExperiment_Click(object? sender, RoutedEventArgs e)
    {
        var setupDialog = new ExperimentSetupDialog();
        var configuration = await setupDialog.ShowDialog<ExperimentConfiguration?>(this);

        if (configuration == null)
            return;

        MainContent.Content = new ExperimentView(MainContent, configuration);
    }
}
