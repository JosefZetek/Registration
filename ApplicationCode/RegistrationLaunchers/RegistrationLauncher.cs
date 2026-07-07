using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DataView;
using Registration.ApplicationCode.Approximation;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Density;
using Registration.ApplicationCode.FeatureComputers;
using Registration.ApplicationCode.FeatureNormalization;
using Registration.ApplicationCode.Matching;
using Registration.ApplicationCode.Other;
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

    /* Optional progress/cancellation hooks. Never null internally so callers can be
       terse; a run without reporting uses an empty RegistrationProgress. */
    private readonly RegistrationProgress _progress;

    #region Constructors

    public RegistrationLauncher(RegistrationProgress? progress = null)
    {
        /* Initialization of registration modules */
        UniformSphereSampler.SampleSphere();
        
        //Use Segmented sampling with MatcherLowe (points are not sampled in the same positions
        //Use SamplerPercentile with PositionMatcher (points may be sampled in the same position, matcher uses position variance) 

        // Option A
        // _sampler = new SamplerSegmented(42, 0.1);
        // _matcher = new MatcherLowe(0.8);
        
        //Option B
        _sampler = new SamplerPercentile(0.1);
        _matcher = new PositionMatcher();
        
        _transformer = new UniformRotationComputerPCA();

        _progress = progress ?? new RegistrationProgress();

        // Curvature and Gradient rely on the same volume approximation. Sharing a single
        // approximation computer lets it be computed once per point and reused by both,
        // instead of each feature computer recomputing it (roughly halves that cost).
        var sharedApproximation = new ApproximationComputerLinear(0.15);
        this._featureComputer = new CompoundFeatureComputer(new AFeatureComputer[]
        {
            // new FeatureComputerPointValue(),
            new FeatureComputerISOCurvature(0.05, sharedApproximation),
            new FeatureComputerPCALength(),
            new FeatureComputerGradient(0.05, sharedApproximation),
            new FeatureComputerShapeIndex(0.05, sharedApproximation),
            new FeatureComputerQuantiles(new double[] { 0.05, 0.25, 0.5, 0.75, 0.95 }),
        });


        // this._featureComputer = new FeatureComputerISOCurvature(0.1, 3);
    }

    public void setParam(double t)
    {
        this._featureComputer = new FeatureComputerISOCurvature(t, 3);
    }

    public RegistrationLauncher(AFeatureComputer featureComputer, RegistrationProgress? progress = null)
    {
        /* Initialization of registration modules */
        UniformSphereSampler.SampleSphere();
        _sampler = new SamplerSegmented(42, 0.1);
        _matcher = new MatcherLowe(0.5);
        _transformer = new UniformRotationComputerPCA();
        
        // _sampler = new SamplerPercentile(0.1);
        // _matcher = new PositionMatcher();

        this._featureComputer = featureComputer;

        _progress = progress ?? new RegistrationProgress();
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

    public Transform3D RunRegistration(AData microData, AData macroData)
    {
        SetSpacing(microData, macroData);
        //Sets Locale to US
        //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

        //----------------------------------------FEATURE VECTOR CALCULATION------------------------------------------------

        // this.featureComputer = new FakeFeatureComputer(EXPECTED_TRANSFORMATION);
        // var fc = (FakeFeatureComputer)featureComputer;

        /* No 4x oversampling anymore: the self-matching saliency stage that consumed the
           surplus was replaced by Lowe's ratio test inside the matcher. */
        _progress.ReportStage(1, "Sampling micro points");
        Point3D[] pointsMicro = _sampler.Sample(microData, Constants.NUMBER_OF_POINTS_MICRO);
        _progress.CancellationToken.ThrowIfCancellationRequested();

        // fc.SetTransformedSampling(true);
        Console.WriteLine("Computing micro feature vectors.");
        _progress.ReportStage(2, "Computing micro feature vectors");
        List<FeatureVector> featureVectorsMicro = CalculateFeatureVectors(microData, _featureComputer, pointsMicro);

        _progress.ReportStage(3, "Sampling macro points");
        Point3D[] pointsMacro = _sampler.Sample(macroData, Constants.NUMBER_OF_POINTS_MACRO);
        _progress.CancellationToken.ThrowIfCancellationRequested();

        // fc.SetTransformedSampling(false);
        Console.WriteLine("Computing macro feature vectors.");
        _progress.ReportStage(4, "Computing macro feature vectors");
        List<FeatureVector> featureVectorsMacro = CalculateFeatureVectors(macroData, _featureComputer, pointsMacro);

        Console.WriteLine($"Number of feature vectors micro: {featureVectorsMicro.Count}");
        Console.WriteLine($"Number of feature vectors macro: {featureVectorsMacro.Count}");

        //----------------------------------------FEATURE VECTOR NORMALIZATION------------------------------------------------
        _progress.ReportStage(5, "Normalizing & weighting features");
        /* Rank based normalization: each feature maps to [0, 1] by its rank within its own
           set, which is robust to outliers and aligns micro/macro feature distributions. */
        FeatureNormalizerRank featureNormalizer = new FeatureNormalizerRank(featureVectorsMicro, featureVectorsMacro);
        featureVectorsMicro = featureNormalizer.NormalizeList(featureVectorsMicro, true);
        featureVectorsMacro = featureNormalizer.NormalizeList(featureVectorsMacro, false);

        double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
        this._featureComputer.GetWeights(featureWeights, 0);

        foreach (FeatureVector featureVector in featureVectorsMicro)
        {
            for (int i = 0; i < featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        foreach (FeatureVector featureVector in featureVectorsMacro)
        {
            for (int i = 0; i < featureVector.Features.Length; i++)
                featureVector.Features[i] *= featureWeights[i];
        }

        //----------------------------------------SETUP TRANSFORMATION METRICS------------------------------------------------
        ITransformationDistance transformationDistance = new TransformationDistanceSeven(microData);
        Transform3D.SetTransformationDistance(transformationDistance);

        //----------------------------------------MATCHES-------------------------------------------------

        _progress.CancellationToken.ThrowIfCancellationRequested();
        _progress.ReportStage(6, "Matching feature vectors");

        Console.WriteLine("Matching.");
        Console.WriteLine($"Micro {featureVectorsMicro.Count}, macro {featureVectorsMacro.Count}");
        
        /* Segmented sampling gives non-repeated points, so unstable correspondences are
           rejected by Lowe's ratio test inside the matcher (threshold 1.0 keeps every
           match that passes the ratio test). */
        Match[] matches = _matcher.Match(featureVectorsMicro.ToArray(), featureVectorsMacro.ToArray(), 1.0);

        //------------------------------------GET TRANSFORMATION -----------------------------------------
        Console.WriteLine($"Computing {matches.Length} transformations.\n");
        _progress.ReportStage(7, "Computing transformations");
        List<Transform3D> transformations = CalculateTransformations(microData, macroData, matches);
        Console.WriteLine($"Number of transformations: {transformations.Count}");
        _progress.CancellationToken.ThrowIfCancellationRequested();
        _progress.ReportStage(8, "Density filtering");
        DensityStructure densityStructure = new DensityStructure(transformations, 0.1);
        Transform3D tr = densityStructure.TransformationsDensityFilter();

        /* The density winner is the transformation of a single match; a least-squares
           rigid fit over all matches consistent with it averages that noise out. */
        double macroDiagonal = Math.Sqrt(
            macroData.MaxValueX * macroData.MaxValueX +
            macroData.MaxValueY * macroData.MaxValueY +
            macroData.MaxValueZ * macroData.MaxValueZ);
        // tr = TransformationRefiner.Refine(matches, tr, macroDiagonal);

        
        // if (EXPECTED_TRANSFORMATION != null && tr != null)
        // {
        //     Console.WriteLine($"Transformation distance: {EXPECTED_TRANSFORMATION.RelativeDistanceTo(tr)}");
        //
        //     List<Point2D> distances = new List<Point2D>(matches.Length);
        //     foreach (Match match in matches)
        //     {
        //         distances.Add(new Point2D(
        //                 match.microFV.DistTo(match.macroFV),
        //                 match.microFV.Point.ApplyRotationTranslation(EXPECTED_TRANSFORMATION).Distance(match.macroFV.Point)
        //             )
        //         );
        //     }
        //     
        //     CSVWriter.WriteResult("/Users/pepazetek/Desktop/MatchDistances.csv", "Feature Distance", "Real Distance", distances);
        // }

        return tr;
    }

    #endregion
    
    #region Main Algorithm Calcucation Methods

    private static bool HasFiniteFeatures(FeatureVector featureVector)
    {
        for (int i = 0; i < featureVector.Features.Length; i++)
        {
            if (double.IsNaN(featureVector.Features[i]) || double.IsInfinity(featureVector.Features[i]))
                return false;
        }

        return true;
    }

    private List<FeatureVector> CalculateFeatureVectors(AData data, AFeatureComputer featureComputer, Point3D[] sampledPoints)
    {
        object lockObj = new object();

        /* Let any listener size its progress bar for this batch before we start. */
        _progress.OnFeatureBatchStart?.Invoke(sampledPoints.Length);

        CancellationToken cancellationToken = _progress.CancellationToken;

        List<FeatureVector> featureVectors = new List<FeatureVector>();
        Thread[] threads = new Thread[Environment.ProcessorCount];
        int chunkSize = (sampledPoints.Length / Environment.ProcessorCount) + 1;

        for (int t = 0; t < Environment.ProcessorCount; t++)
        {
            int start = t * chunkSize;
            int end = Math.Min(start + chunkSize, sampledPoints.Length);

            threads[t] = new Thread(() =>
            {
                List<FeatureVector> localFeatures = new List<FeatureVector>();
                for (int i = start; i < end; i++)
                {
                    /* Bail out early if the run was cancelled (e.g. the view was closed). */
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    FeatureVector fv = featureComputer.ComputeFeatureVector(data, sampledPoints[i]);

                    /* Vectors with NaN/Inf features (flat neighborhood, vanishing gradient, ...)
                       are dropped here so they cannot poison the normalization statistics. */
                    if (HasFiniteFeatures(fv))
                        localFeatures.Add(fv);

                    _progress.OnFeatureComputed?.Invoke();
                }
                
                lock (lockObj)
                {
                    featureVectors.AddRange(localFeatures);
                }
            });

            threads[t].Start();
        }

        for (int t = 0; t < threads.Length; t++)
        {
            threads[t].Join();
        }

        cancellationToken.ThrowIfCancellationRequested();

        return featureVectors;
    }

    private List<Transform3D> CalculateTransformations(AData microData, AData macroData, Match[] matches)
    {
        CancellationToken cancellationToken = _progress.CancellationToken;

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
                    if (cancellationToken.IsCancellationRequested)
                        break;

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

        return transformations;
    }

    #endregion

    #region Testing Methods

    /// <summary>
    /// Calculates average difference of feature vectors distanced maximum `radius` units 
    /// </summary>
    /// <param name="dataObject"></param>
    /// <param name="globalFeatureNormalizer"></param>
    /// <param name="radius"></param>
    /// <param name="numberOfReferencePoints"></param>
    /// <param name="pointsPerBin"></param>
    /// <returns></returns>
    public double FeatureDifferenceMean(AData dataObject, FeatureNormalizer globalFeatureNormalizer, double radius, int numberOfReferencePoints = 20, int pointsPerBin = 100)
    {
        Point3D[] sampledPoints = _sampler.Sample(dataObject, 10_000);
        Random random = new Random();
        
        List<double> distances = new List<double>();
        
        for (int i = 0; i < numberOfReferencePoints; i++)
        {
            Point3D referencePoint = sampledPoints[i];
            FeatureVector referenceFV = _featureComputer.ComputeFeatureVector(dataObject, referencePoint);
            
            Console.WriteLine($"Processing {i+1}/{numberOfReferencePoints}");

            double minR = 0;
            double maxR = radius;

            List<Point3D> points = new List<Point3D>();
            
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
                
                points.Add(shiftedPoint);
            }
            
            List<FeatureVector> featureVectors = CalculateFeatureVectors(dataObject, _featureComputer, points.ToArray());

            List<FeatureVector> allFV = new List<FeatureVector> { referenceFV };
            allFV.AddRange(featureVectors);
            
            allFV = globalFeatureNormalizer.NormalizeList(allFV, true);

            double[] featureWeights = new double[this._featureComputer.NumberOfFeatures];
            this._featureComputer.GetWeights(featureWeights, 0);

            foreach (FeatureVector fv in allFV)
            {
                for (int j = 0; j < fv.Features.Length; j++)
                    fv.Features[j] *= featureWeights[j];
            }

            FeatureVector normalizedRefFV = allFV[0];
            for (int j = 1; j < allFV.Count; j++)
            {
                distances.Add(normalizedRefFV.DistTo(allFV[j]));
            }
        }
        
        return distances.Count > 0 ? distances.Average() : 0.0;
    }

    /// <summary>
    /// Outputs data showing for a given distance (x axis) how does the variance (feature vector distance between center point and surrounding points) differ 
    /// </summary>
    public void FeatureVarianceInDifferentRegions(AData dataObject, int numberOfReferencePoints = 20)
    {
        SetSpacing(dataObject, dataObject);
        
        Point3D[] sampledPoints = _sampler.Sample(dataObject, 10_000);
        List<FeatureVector> globalFV = CalculateFeatureVectors(dataObject, _featureComputer, sampledPoints);
        
        FeatureNormalizer globalFeatureNormalizer = new FeatureNormalizer(globalFV, globalFV);
        
        Random random = new Random();

        int minBinDistance = 0;
        int numberOfBins = 20;
        int pointsPerBin = 100;
        double maxDistance = 2;
        double binStep = maxDistance / numberOfBins;

        List<string[]> csvData = new List<string[]>();
        
        string[] header = new string[numberOfBins + 1];
        header[0] = "RefPointIdx";
        for (int b = 0; b < numberOfBins; b++)
            header[b + 1] = $"Bin_{(minBinDistance + (b * binStep)):0.00}-{(minBinDistance + ((b + 1) * binStep)):0.00}";
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
                double minR = minBinDistance + b * binStep;
                double maxR = minBinDistance + (b + 1) * binStep;

                List<Point3D> points = new List<Point3D>();
                
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

                    /* Shifted points can land outside the volume; skip them so
                       border-clamped values do not distort the variance statistics. */
                    if (!dataObject.PointWithinBounds(shiftedPoint))
                        continue;

                    points.Add(shiftedPoint);
                }

                List<FeatureVector> binFeatureVectors = CalculateFeatureVectors(dataObject, _featureComputer, points.ToArray());

                List<FeatureVector> allFV = new List<FeatureVector> { referenceFV };
                allFV.AddRange(binFeatureVectors);
                
                allFV = globalFeatureNormalizer.NormalizeList(allFV, true);

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

        CSVWriter.WriteResult("/Users/pepazetek/Desktop/Tests/varianceDifferentRegionsEditedSampling5.csv", csvData.ToArray());
    }
    
    /// <summary>
    /// Calculates average variance for both neighborhoods (closer < threshold, further > threshold) for a given number of reference points
    /// </summary>
    /// <param name="dataObject"></param>
    /// <param name="globalFeatureNormalizer"></param>
    /// <param name="numberOfReferencePoints"></param>
    /// <param name="threshold">Threshold between close and far neighborhood</param>
    /// <returns></returns>
    public (double, double) FeatureVarianceTest(AData dataObject, FeatureNormalizer globalFeatureNormalizer, int numberOfReferencePoints = 10, double threshold = 1)
    {
        SetSpacing(dataObject, dataObject);

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
            
            allFV = globalFeatureNormalizer.NormalizeList(allFV, true);
            
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
    
    /// <summary>
    /// Feature normalizer for data across whole object
    /// </summary>
    /// <param name="dataObject"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public FeatureNormalizer GetFeatureNormalizer(AData dataObject, int points = 10_000)
    {
        Point3D[] sampledPoints = _sampler.Sample(dataObject, points);
        List<FeatureVector> fv = CalculateFeatureVectors(dataObject, _featureComputer, sampledPoints);
        return new FeatureNormalizer(fv, fv);
    }
    
    /// <summary>
    /// Divides area into closer and futher neighborhood, returns two lists of FVs for closer and further neighborhood
    /// </summary>
    /// <param name="dataObject"></param>
    /// <param name="originalPoint"></param>
    /// <param name="random"></param>
    /// <param name="thresholdDistance"></param>
    /// <returns></returns>
    private (List<FeatureVector>, List<FeatureVector>) GetShiftedFeatureVectors(AData dataObject, Point3D originalPoint, Random random, double thresholdDistance)
    {
        int numberOfPoints = 500;
        
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
        
        for (int i = 0; i < furtherPoints.Length; i++)
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

    /// <summary>
    /// Calculates distances (real and fv) between referenceFV and shiftedFVs 
    /// </summary>
    /// <param name="referenceFV">Feature Vector thats typically in the middle</param>
    /// <param name="shiftedFVs">Feature vectors deviating from the middle (reference) one</param>
    /// <returns>Returns list of points where x = fvDistance, y = real distance</returns>
    private List<Point2D> CalculatedDistance(FeatureVector referenceFV, List<FeatureVector> shiftedFVs)
    {
        List<Point2D> distances = new List<Point2D>();

        List<FeatureVector> referenceFeatureVectors = new List<FeatureVector>();
        referenceFeatureVectors.Add(referenceFV);

        //Normalization
        FeatureNormalizer featureNormalizer = new FeatureNormalizer(shiftedFVs, referenceFeatureVectors);
        shiftedFVs = featureNormalizer.NormalizeList(shiftedFVs, true);
        referenceFeatureVectors = featureNormalizer.NormalizeList(referenceFeatureVectors, false);

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
    /// Calculates variance between passed lists and referenceFV 
    /// </summary>
    /// <param name="referenceFv">Reference feature vector</param>
    /// <param name="closerFV">List of feature vectors closer to reference FV</param>
    /// <param name="furtherFV">List of feature vectors further to reference FV</param>
    /// <returns>Returns (closerVariance, furtherVariance)</returns>
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
    
    
    /// <summary>
    /// Method returning two closest feature vectors (hypothesis was if there is a bigger leap (Lowe's ratio, used in registration SIFT algorithm), the match is stable)
    /// Current problem is that the two closest matches might not satisfy the condition of having a bigger leap between them, but that might be caused because they lie
    /// in the same position
    /// CONCLUSION: This approach might work, but different sampling method needs to be used (uniform is not suitable because the spacing would have to be very small, but say splitting
    /// the volume into segments and sampling only points with highest variance withing each segment might work with Lowe's ratio test + standard matcher (position matcher expects
    /// that the valid (hopefully accurate) matches are sampled multiple times. Regardless of this, it might still work, however it would be very inefficient)
    /// </summary>
    /// <param name="dataObject"></param>
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
        featureVectorsMicro = featureNormalizer.NormalizeList(featureVectorsMicro, true);
        featureVectorsMacro = featureNormalizer.NormalizeList(featureVectorsMacro, false);

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

    #endregion
}
