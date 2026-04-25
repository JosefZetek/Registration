using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;
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
        
        var rl = new RegistrationLauncher();
        
        var dataObject = new VolumetricData(new FilePathDescriptor("/Users/pepazetek/Desktop/Tests/Jatra/macroData.mhd", "/Users/pepazetek/Desktop/Tests/Jatra/macroData.raw"));

        Console.WriteLine($"difference mean: {rl.FeatureDifferenceMean(dataObject, 1.0)}");
        // rl.FeatureVarianceInDifferentRegions(dataObject, 20);

        // //

        // // // double threshold = 0.03 * Math.Sqrt(dataObject.MaxValueX * dataObject.MaxValueX + dataObject.MaxValueY * dataObject.MaxValueY + dataObject.MaxValueZ * dataObject.MaxValueZ);
        // // // Console.WriteLine($"Threshold: {threshold}");
        // // // return;
        // //
        // int numberOfReferenceParams = 60;
        // string[][] dataToSave = new string[numberOfReferenceParams+1][];

        // dataToSave[0] = new string[] { "Param", "Closer", "Further" };

        // for (int i = 0; i < numberOfReferenceParams; i++)
        // {
        //     rl.setParam(i * 0.05);
        //     var (closer, further) = rl.FeatureVarianceTest(dataObject, 20, i * 0.05);

        //     dataToSave[i+1] = new string[] { (i * 0.05).ToString(), closer.ToString(), further.ToString() };

        //     Console.WriteLine($"Finished {i}/{numberOfReferenceParams}");
        // }

        // CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/feature_variance_test_curvature.csv", dataToSave);
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
        MainContent.Content = new RegistrationView(MainContent);
    }

    private void OnExperiment_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = new ProgressView(MainContent);
    }
}