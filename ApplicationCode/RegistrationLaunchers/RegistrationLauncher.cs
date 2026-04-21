using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using DataView;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Density;
using Registration.ApplicationCode.FeatureComputers;
using Registration.ApplicationCode.FeatureNormalization;
using Registration.ApplicationCode.Matching;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.RotationComputers;
using Registration.ApplicationCode.Samplers;
using Registration.ApplicationCode.TransformationDistanceMetrics;

namespace Registration.ApplicationCode.RegistrationLaunchers;

public class RegistrationLauncher : IRegistrationLauncher
{
    /* Registration blocks */
    private ISampler _sampler;
    private AFeatureComputer _featureComputer;
    private IMatcher _matcher;
    private ATransformer _transformer;

    public static Transform3D? EXPECTED_TRANSFORMATION;
    public static double MAX_SPACING = 1.0;

    public static double MIN_SPACING = 1.0;

    private readonly Action? _actionIncreaseFeatureProgressBar;

    #region Constructors

    public RegistrationLauncher()
    {
        /* Initialization of registration modules */
        UniformSphereSampler.SampleSphere();
        _sampler = new SamplerPercentile(0.1);
        _matcher = new Matcher();
        // _matcher = new PositionMatcher();
        _transformer = new UniformRotationComputerPCA();

        // var config = RootConfigurationElement.Deserialize("/Users/pepazetek/Desktop/Tests/Config/config_FullFeatureSet.json");
        // this._featureComputer = config?.GetCompoundFeatureComputer();
        
        // List<double> quantiles = new List<double>();
        // for (int i = 0; i <= 100; i += 1)
        // {
        //     quantiles.Add(i/100.0);
        // }
        //     
        //

        this._featureComputer = new FeatureComputerISOCurvature(0.01, 1);
        
        // this._featureComputer = new CompoundFeatureComputer(new AFeatureComputer[]
        // {
        //     new FeatureComputerPointValue(),
        //     new FeatureComputerISOCurvature(0.01, 3),
        //     new FeatureComputerPCALength(),
        //     new FeatureComputerGradient(0.01, 3),
        //     new FeatureComputerQuantiles(new double[] {0, 0.25, 0.5, 0.75, 1}),
        // });

        // this._featureComputer = new FeatureComputerISOCurvature(0.1, 3);
    }

    public void setParam(double t)
    {
        this._featureComputer = new FeatureComputerISOCurvature(t, 3);
    }

    public RegistrationLauncher(AFeatureComputer featureComputer)
    {
        /* Initialization of registration modules */
        UniformSphereSampler.SampleSphere();
        _sampler = new SamplerPercentile(0.1);
        _matcher = new Matcher();
        _transformer = new UniformRotationComputerPCA();

        this._featureComputer = featureComputer;
    }

    private static void SetSpacing(AData microData, AData macroData)
    {
        MAX_SPACING = Math.Max(microData.Spacings.Max(), macroData.Spacings.Max());
        MIN_SPACING = Math.Min(microData.Spacings.Min(), macroData.Spacings.Min());
    }

    #endregion
    
    #region Run Registration Methods

    public Transform3D RunRegistration(FilePathDescriptor microDataPath, FilePathDescriptor macroDataPath)
    {
        return RunRegistration(new VolumetricData(microDataPath), new VolumetricData(macroDataPath));
    }
    
    public (List<FeatureVector>, List<FeatureVector>) GetShiftedFeatureVectors(AData dataObject, Point3D originalPoint, Random random, double thresholdDistance)
    {
        int numberOfPoints = 1_000;
        
        List<FeatureVector> closerFeatureVectors = new List<FeatureVector>();
        List<FeatureVector> furtherFeatureVectors = new List<FeatureVector>();
        
        Point3D[] closerPoints = new Point3D[numberOfPoints/2];
        Point3D[] furtherPoints = new Point3D[numberOfPoints - closerPoints.Length];

        for (int i = 0; i < closerPoints.Length; i++)
        { 
            //generate random theta and phi for spherical coordinates
            double theta = random.NextDouble() * 2 * Math.PI;
            double phi = Math.Acos(2 * random.NextDouble() - 1);

            double minR = 0;
            double maxR = thresholdDistance;

            // Uniformly distribute within the volume of the spherical shell (bin)
            double u = random.NextDouble();
            double distance = Math.Pow(u * (Math.Pow(maxR, 3) - Math.Pow(minR, 3)) + Math.Pow(minR, 3), 1.0 / 3.0);

            //convert spherical coordinates to cartesian coordinates
            double shiftX = distance * Math.Sin(phi) * Math.Cos(theta);
            double shiftY = distance * Math.Sin(phi) * Math.Sin(theta);
            double shiftZ = distance * Math.Cos(phi);

            Point3D shiftedPoint = new Point3D(
                originalPoint.X + shiftX,
                originalPoint.Y + shiftY,
                originalPoint.Z + shiftZ
            );
                
            closerPoints[i] = shiftedPoint;
        }
        
        for (int i = 0; i < closerPoints.Length; i++)
        { 
            //generate random theta and phi for spherical coordinates
            double theta = random.NextDouble() * 2 * Math.PI;
            double phi = Math.Acos(2 * random.NextDouble() - 1);

            double minR = thresholdDistance;
            double maxR = 3 * thresholdDistance;

            // Uniformly distribute within the volume of the spherical shell (bin)
            double u = random.NextDouble();
            double distance = Math.Pow(u * (Math.Pow(maxR, 3) - Math.Pow(minR, 3)) + Math.Pow(minR, 3), 1.0 / 3.0);

            //convert spherical coordinates to cartesian coordinates
            double shiftX = distance * Math.Sin(phi) * Math.Cos(theta);
            double shiftY = distance * Math.Sin(phi) * Math.Sin(theta);
            double shiftZ = distance * Math.Cos(phi);

            Point3D shiftedPoint = new Point3D(
                originalPoint.X + shiftX,
                originalPoint.Y + shiftY,
                originalPoint.Z + shiftZ
            );
                
            furtherPoints[i] = shiftedPoint;
        }
        
        closerFeatureVectors.AddRange(CalculateFeatureVectors(dataObject, _featureComputer, closerPoints));
        furtherFeatureVectors.AddRange(CalculateFeatureVectors(dataObject, _featureComputer, furtherPoints));

        return (closerFeatureVectors, furtherFeatureVectors);
    }

    private List<Point2D> CalculatedDistance(FeatureVector referenceFV, List<FeatureVector> shiftedFVs)
    {
        List<Point2D> distances = new List<Point2D>();

        List<FeatureVector> referenceFeatureVectors = new List<FeatureVector>();
        referenceFeatureVectors.Add(referenceFV);

        //Normalization
        FeatureNormalizer featureNormalizer = new FeatureNormalizer(shiftedFVs, referenceFeatureVectors);
        shiftedFVs = featureNormalizer.NormalizeList(shiftedFVs);
        referenceFeatureVectors = featureNormalizer.NormalizeList(referenceFeatureVectors);

        double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
        this._featureComputer.GetWeights(featureWeights, 0);

        foreach (FeatureVector featureVector in shiftedFVs)
        {
            for (int i = 0; i<featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        foreach (FeatureVector featureVector in referenceFeatureVectors)
        {
            for (int i = 0; i<featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        referenceFV = referenceFeatureVectors.First();
        
        //Distance calculation
        foreach (FeatureVector shiftedFV in shiftedFVs)
        {
            double realDistance = referenceFV.Point.Distance(shiftedFV.Point);
            double fvDistance = referenceFV.DistTo(shiftedFV);
            distances.Add(new Point2D(fvDistance, realDistance));
        }

        return distances;
    }

    /// <summary>
    /// Outputs data that showing for a given distance (x axis) how does the variance (feature vector distance between center point and surrounding points differ) 
    /// </summary>
    public void FeatureVarianceInDifferentRegions(AData dataObject, int numberOfReferencePoints = 20)
    {
        SetSpacing(dataObject, dataObject);
        double diag = Math.Sqrt(dataObject.MaxValueX * dataObject.MaxValueX + dataObject.MaxValueY * dataObject.MaxValueY + dataObject.MaxValueZ * dataObject.MaxValueZ);
        
        Point3D[] sampledPoints = _sampler.Sample(dataObject, 10_000);
        Random random = new Random();

        int numberOfBins = 10;
        int pointsPerBin = 100;
        double maxDistance = 1; 
        double binStep = maxDistance / numberOfBins;

        List<string[]> csvData = new List<string[]>();
        
        string[] header = new string[numberOfBins + 1];
        header[0] = "RefPointIdx";
        for (int b = 0; b < numberOfBins; b++)
            header[b + 1] = $"Bin_{b * binStep:0.00}-{(b + 1) * binStep:0.00}";
        csvData.Add(header);

        for (int i = 0; i < numberOfReferencePoints; i++)
        {
            Point3D referencePoint = sampledPoints[i];
            FeatureVector referenceFV = _featureComputer.ComputeFeatureVector(dataObject, referencePoint);

            string[] row = new string[numberOfBins + 1];
            row[0] = i.ToString();

            Console.WriteLine($"Processing {i+1}/{numberOfReferencePoints}");

            for (int b = 0; b < numberOfBins; b++)
            {
                double minR = b * binStep;
                double maxR = (b + 1) * binStep;
                
                List<FeatureVector> binFeatureVectors = new List<FeatureVector>();

                for (int p = 0; p < pointsPerBin; p++)
                {
                    double theta = random.NextDouble() * 2 * Math.PI;
                    double phi = Math.Acos(2 * random.NextDouble() - 1);
                    double u = random.NextDouble();
                    double distance = Math.Pow(u * (Math.Pow(maxR, 3) - Math.Pow(minR, 3)) + Math.Pow(minR, 3), 1.0 / 3.0);

                    double shiftX = distance * Math.Sin(phi) * Math.Cos(theta);
                    double shiftY = distance * Math.Sin(phi) * Math.Sin(theta);
                    double shiftZ = distance * Math.Cos(phi);

                    Point3D shiftedPoint = new Point3D(
                        referencePoint.X + shiftX,
                        referencePoint.Y + shiftY,
                        referencePoint.Z + shiftZ
                    );

                    binFeatureVectors.Add(_featureComputer.ComputeFeatureVector(dataObject, shiftedPoint));
                }

                List<FeatureVector> allFV = new List<FeatureVector> { referenceFV };
                allFV.AddRange(binFeatureVectors);

                FeatureNormalizer featureNormalizer = new FeatureNormalizer(allFV, allFV);
                allFV = featureNormalizer.NormalizeList(allFV);

                double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
                this._featureComputer.GetWeights(featureWeights, 0);

                foreach (FeatureVector fv in allFV)
                {
                    for (int j = 0; j < fv.Features.Length; j++)
                        fv.Features[j] *= featureWeights[j];
                }

                FeatureVector normalizedRefFV = allFV[0];
                List<double> distances = new List<double>();
                for (int j = 1; j < allFV.Count; j++)
                {
                    distances.Add(normalizedRefFV.DistTo(allFV[j]));
                }

                double avg = distances.Count > 0 ? distances.Average() : 0.0;
                double variance = distances.Count > 0 ? distances.Sum(v => Math.Pow(v - avg, 2)) / distances.Count : 0.0;
                
                row[b + 1] = variance.ToString();
            }

            csvData.Add(row);
        }

        CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/varianceDifferentRegions.csv", csvData.ToArray());
    }
    
    public (double, double) FeatureVarianceTest(AData dataObject, int numberOfReferencePoints, double paramSet)
    {
        SetSpacing(dataObject, dataObject);
        
        //Three percent of diagonal
        //double threshold = 0.03 * Math.Sqrt(dataObject.MaxValueX * dataObject.MaxValueX + dataObject.MaxValueY * dataObject.MaxValueY + dataObject.MaxValueZ * dataObject.MaxValueZ);

        double threshold = 1;

        // Sample at least 10 k points
        Point3D[] sampledPoints = _sampler.Sample(dataObject, 10_000);
        Random random = new Random();

        Point3D referencePoint;
        double closerAvg = 0;
        double furtherAvg = 0;

        List<Point2D> points = new List<Point2D>();
        for(int i = 0; i<numberOfReferencePoints; i++)
        {
            referencePoint = sampledPoints[i];
            FeatureVector referenceFV = _featureComputer.ComputeFeatureVector(dataObject, referencePoint);

            var (closerFV, furtherFV) = GetShiftedFeatureVectors(dataObject, referencePoint, random, threshold);
            
            // Normalize and apply weights
            List<FeatureVector> allFV = new List<FeatureVector> { referenceFV };
            allFV.AddRange(closerFV);
            allFV.AddRange(furtherFV);
            
            FeatureNormalizer featureNormalizer = new FeatureNormalizer(allFV, allFV);
            allFV = featureNormalizer.NormalizeList(allFV);
            
            double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
            this._featureComputer.GetWeights(featureWeights, 0);

            foreach (FeatureVector fv in allFV)
            {
                for (int j = 0; j < fv.Features.Length; j++)
                    fv.Features[j] *= featureWeights[j];
            }

            FeatureVector normalizedRefFV = allFV[0];
            List<FeatureVector> normalizedCloserFV = allFV.GetRange(1, closerFV.Count);
            List<FeatureVector> normalizedFurtherFV = allFV.GetRange(1 + closerFV.Count, furtherFV.Count);

            var (varianceCloser, varianceFurther) = CalculateVariance(normalizedRefFV, normalizedCloserFV, normalizedFurtherFV);
            
            points.Add(new Point2D(varianceCloser, varianceFurther));
            
            closerAvg += varianceCloser;
            furtherAvg += varianceFurther;
        }
        
        // CSVWriter.WriteResult($"/Users/pepazetek/Desktop/Tests/varianceWithParam{paramSet:E0}", "varCloser", "varFurther", points);
        
        // CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/varianceDataAfterParamModification.csv", "varCloser", "varFurther", points);
        
        closerAvg /= numberOfReferencePoints;
        furtherAvg /= numberOfReferencePoints;

        return (closerAvg, furtherAvg);
    }

    private (double varianceCloser, double varianceFurther) CalculateVariance(FeatureVector referenceFv, List<FeatureVector> closerFV, List<FeatureVector> furtherFV)
    {
        List<double> closerDistances = new List<double>();
        List<double> furtherDistances = new List<double>();

        for (int i = 0; i < closerFV.Count; i++)
        {
            closerDistances.Add(referenceFv.DistTo(closerFV[i]));
        }
        
        for(int i = 0; i<furtherFV.Count; i++)
        {
            furtherDistances.Add(referenceFv.DistTo(furtherFV[i]));
        }
        
        double CalculateVar(List<double> values)
        {
            if (values.Count == 0) return 0.0;
            double avg = values.Average();
            return values.Sum(v => Math.Pow(v - avg, 2)) / values.Count;
        }

        return (CalculateVar(closerDistances), CalculateVar(furtherDistances));
    }


    public void FeatureVectorTest(AData dataObject)
    {
        SetSpacing(dataObject, dataObject);
        //Sets Locale to US
        //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        
        //----------------------------------------FEATURE VECTOR CALCULATION------------------------------------------------
        
        // Point3D[] pointsSampled = _sampler.Sample(dataObject, Constants.NUMBER_OF_POINTS_MICRO + Constants.NUMBER_OF_POINTS_MACRO);
        Point3D[] pointsMicro = _sampler.Sample(dataObject, Constants.NUMBER_OF_POINTS_MICRO);
        Point3D[] pointsMacro =  _sampler.Sample(dataObject, Constants.NUMBER_OF_POINTS_MACRO);

        // fc.SetTransformedSampling(true);
        Console.WriteLine("Computing feature vectors micro.");
        List<FeatureVector> featureVectorsMicro = CalculateFeatureVectors(dataObject, _featureComputer, pointsMicro);

        Console.WriteLine("Computing feature vectors macro.");
        List<FeatureVector> featureVectorsMacro = CalculateFeatureVectors(dataObject, _featureComputer, pointsMacro);
        
        Console.WriteLine($"Number of feature vectors micro: {featureVectorsMicro.Count}");
        Console.WriteLine($"Number of feature vectors macro: {featureVectorsMacro.Count}");

        //----------------------------------------FEATURE VECTOR NORMALIZATION------------------------------------------------
        FeatureNormalizer featureNormalizer = new FeatureNormalizer(featureVectorsMicro, featureVectorsMacro);
        featureVectorsMicro = featureNormalizer.NormalizeList(featureVectorsMicro);
        featureVectorsMacro = featureNormalizer.NormalizeList(featureVectorsMacro);

        double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
        this._featureComputer.GetWeights(featureWeights, 0);

        foreach (FeatureVector featureVector in featureVectorsMicro)
        {
            for (int i = 0; i<featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        foreach (FeatureVector featureVector in featureVectorsMacro)
        {
            for (int i = 0; i<featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        Console.WriteLine("Matching.");

        FeatureVector[] featureVectorsMicroArray = featureVectorsMicro.ToArray();
        FeatureVector[] featureVectorsMacroArray = featureVectorsMacro.ToArray();

        KDTree tree = new KDTree(featureVectorsMicroArray);
        List<Match> matches = new List<Match>();

        double diag = Math.Sqrt(dataObject.MaxValueX * dataObject.MaxValueX + dataObject.MaxValueY * dataObject.MaxValueY + dataObject.MaxValueZ * dataObject.MaxValueZ);
        string[][] valuesToSave = new string[featureVectorsMacro.Count + 1][];

        valuesToSave[0] = new string[] {"fvDistanceClosest", "fvDistanceSecondClosest", "realDistanceClosest", "realDistanceSecondClosest"};

        for (int i = 0; i < featureVectorsMacro.Count; i++)
        {

            FeatureVector currentFeatureVector = featureVectorsMacroArray[i];

            var nearest = tree.FindNearest(currentFeatureVector, 2);
            
            if(nearest == null || nearest.Length == 0 || nearest[0] < 0 || nearest[1] < 0)
                continue;

            double fvDistanceClosest = currentFeatureVector.DistTo(featureVectorsMicroArray[nearest[0]]);
            double fvDistanceSecondClosest = currentFeatureVector.DistTo(featureVectorsMicroArray[nearest[1]]);

            double realDistanceClosest = currentFeatureVector.Point.Distance(featureVectorsMicroArray[nearest[0]].Point) / diag;
            double realDistanceSecondClosest = currentFeatureVector.Point.Distance(featureVectorsMicroArray[nearest[1]].Point) / diag;

            valuesToSave[i + 1] = new string[] {
                fvDistanceClosest.ToString(),
                fvDistanceSecondClosest.ToString(),
                realDistanceClosest.ToString(),
                realDistanceSecondClosest.ToString()
            };
        }

        CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/fvRatios.csv", valuesToSave);
    }
    
    public Transform3D RunRegistration(AData microData, AData macroData)
    {
        SetSpacing(microData, macroData);
        //Sets Locale to US
        //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        
        //----------------------------------------FEATURE VECTOR CALCULATION------------------------------------------------

        // this.featureComputer = new FakeFeatureComputer(EXPECTED_TRANSFORMATION);
        // var fc = (FakeFeatureComputer)featureComputer;
        
        Point3D[] pointsMicro = _sampler.Sample(microData, Constants.NUMBER_OF_POINTS_MICRO);

        // fc.SetTransformedSampling(true);
        Console.WriteLine("Computing micro feature vectors.");
        List<FeatureVector> featureVectorsMicro = CalculateFeatureVectors(microData, _featureComputer, pointsMicro);
        
        Point3D[] pointsMacro = _sampler.Sample(macroData, Constants.NUMBER_OF_POINTS_MACRO);

        // fc.SetTransformedSampling(false);
        Console.WriteLine("Computing macro feature vectors.");
        List<FeatureVector> featureVectorsMacro = CalculateFeatureVectors(macroData, _featureComputer, pointsMacro);

        Console.WriteLine($"Number of feature vectors micro: {featureVectorsMicro.Count}");
        Console.WriteLine($"Number of feature vectors macro: {featureVectorsMacro.Count}");

        //----------------------------------------FEATURE VECTOR NORMALIZATION------------------------------------------------
        FeatureNormalizer featureNormalizer = new FeatureNormalizer(featureVectorsMacro, featureVectorsMacro);
        featureVectorsMicro = featureNormalizer.NormalizeList(featureVectorsMicro);
        featureVectorsMacro = featureNormalizer.NormalizeList(featureVectorsMacro);

        double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
        this._featureComputer.GetWeights(featureWeights, 0);

        foreach (FeatureVector featureVector in featureVectorsMicro)
        {
            for (int i = 0; i<featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }
        
        foreach (FeatureVector featureVector in featureVectorsMacro)
        {
            for (int i = 0; i<featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        //----------------------------------------SETUP TRANSFORMATION METRICS------------------------------------------------
        ITransformationDistance transformationDistance = new TransformationDistanceSeven(microData);
        Transform3D.SetTransformationDistance(transformationDistance);

        //----------------------------------------MATCHES-------------------------------------------------
        Console.WriteLine("Matching.");
        Console.WriteLine($"Micro {featureVectorsMicro.Count}, macro {featureVectorsMacro.Count}");
        Match[] matches = _matcher.Match(featureVectorsMicro.ToArray(), featureVectorsMacro.ToArray(), Constants.THRESHOLD);
        
        //------------------------------------GET TRANSFORMATION -----------------------------------------
        Console.WriteLine($"Computing {matches.Length} transformations.\n");
        List<Transform3D> transformations = CalculateTransformations(microData, macroData, matches);
        Console.WriteLine($"Number of transformations: {transformations.Count}");
        DensityStructure densityStructure = new DensityStructure(transformations, 0.1);
        Transform3D tr = densityStructure.TransformationsDensityFilter();

        if (EXPECTED_TRANSFORMATION != null)
            Console.WriteLine($"Transformation distance: {EXPECTED_TRANSFORMATION.RelativeDistanceTo(tr)}");

        return tr;
    }

    #endregion
    
    #region Calculation Methods

    private List<FeatureVector> CalculateFeatureVectors(AData data, AFeatureComputer featureComputer, Point3D[] sampledPoints)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        object lockObj = new object();
        
        long originalMemoryUse = GC.GetTotalMemory(false);
        const long MEMORY_THRESHOLD = 2L * 1024L * 1024L * 1024L; // 2 GB

        List<FeatureVector> featureVectors = new List<FeatureVector>();
        Thread[] threads = new Thread[Environment.ProcessorCount];
        int chunkSize = (sampledPoints.Length / Environment.ProcessorCount) + 1;

        for (int t = 0; t < Environment.ProcessorCount; t++)
        {
            int start = t * chunkSize;
            int end = Math.Min(start + chunkSize, sampledPoints.Length);

            threads[t] = new Thread(() =>
            {
                Console.WriteLine("Thread started for points {0} to {1}", start, end);
                List<FeatureVector> localFeatures = new List<FeatureVector>();
                for (int i = start; i < end; i++)
                {
                    long currentMemoryUse = GC.GetTotalMemory(false);
                    if ((currentMemoryUse - originalMemoryUse) > MEMORY_THRESHOLD)
                    {
                        Console.WriteLine("Memory threshold exceeded, forcing garbage collection.");
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                        originalMemoryUse = GC.GetTotalMemory(false);
                    }

                    FeatureVector fv = featureComputer.ComputeFeatureVector(data, sampledPoints[i]);
                    localFeatures.Add(fv);
                    
                    _actionIncreaseFeatureProgressBar?.Invoke();
                }
                
                lock (lockObj)
                {
                    featureVectors.AddRange(localFeatures);
                }

                // Console.WriteLine("nazdar");
                // long currentMemoryUse = GC.GetTotalMemory(false);

                // Console.WriteLine($"Usage in GB: {currentMemoryUse / (1024.0 * 1024.0 * 1024.0)}");
                
                
                
            });

            threads[t].Start();
        }

        for (int t = 0; t < threads.Length; t++)
        {
            threads[t].Join();
        }

        
        stopwatch.Stop();
        Console.WriteLine($"Feature computation time: {stopwatch.ElapsedMilliseconds}");

        return featureVectors;
    }

    private List<Transform3D> CalculateTransformations(AData microData, AData macroData, Match[] matches)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        List<Transform3D> transformations = new List<Transform3D>();
        Thread[] threads = new Thread[Environment.ProcessorCount];
        object lockObj = new object();
        int chunkSize = (matches.Length / Environment.ProcessorCount) + 1;

        for (int t = 0; t < Environment.ProcessorCount; t++)
        {
            int localStart = t * chunkSize;
            int localEnd = Math.Min(localStart + chunkSize, matches.Length);

            threads[t] = new Thread(() =>
            {
                for (int i = localStart; i < localEnd; i++)
                {
                    Transform3D[] currentTransformations = _transformer.GetTransformations(matches[i], microData, macroData);
                    
                    if (currentTransformations == null)
                        continue;
                    
                    lock (lockObj)
                    {
                        foreach (Transform3D currentTransformation in currentTransformations)
                            transformations.Add(currentTransformation);
                    }
                }
            });

            threads[t].Start();
        }

        for (int t = 0; t < threads.Length; t++)
            threads[t].Join();

        stopwatch.Stop();
        Console.WriteLine($"Transformations computation time: {stopwatch.ElapsedMilliseconds}");

        return transformations;
    }

    #endregion
}
