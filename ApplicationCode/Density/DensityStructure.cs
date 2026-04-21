using System;
using System.Collections.Generic;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Density;

public class DensityStructure
{
    private DensityTree rootNode;
    private List<Transform3D> transformations;

    private double threshold;
    private double spreadParameter;

    public DensityStructure(List<Transform3D> transformations, double threshold, double spreadParameter)
    {
        rootNode = new DensityTree(transformations);
        this.transformations = transformations;

        this.spreadParameter = spreadParameter;
        this.threshold = threshold;
    }

    public DensityStructure(List<Transform3D> transformations, double threshold)
    {
        rootNode = new DensityTree(transformations);
        this.transformations = transformations;

        this.spreadParameter = CalculateSpreadParameter(0.5);
        this.threshold = threshold;
    }

    public Transform3D TransformationsDensityFilter()
    {
        double maxDistance = Math.Sqrt(Math.Log(threshold) / (-spreadParameter));

        double bestDensity = 0;
        Transform3D bestTransformation = null;

        for (int i = 0; i < transformations.Count; i++)
        {
            List<Transform3D> result = FindPointsWithinRadius(transformations[i], maxDistance);

            double currentDensity = CalculateDensity(transformations[i], result);

            // UnityEngine.Debug.Log($"For transformation {i} the density is: {currentDensity}");

            if (bestDensity < currentDensity)
            {
                bestDensity = currentDensity;
                bestTransformation = transformations[i];
            }

        }

        return bestTransformation;
    }

    private double CalculateDensity(Transform3D referenceTransformation, List<Transform3D> transformations)
    {
        double currentDensity = 0;

        //Density is calculated like SUM of e^(-(spreadParameter * distance)^2) for all close transformations
        foreach (Transform3D currentTransformation in transformations)
            currentDensity += Math.Exp(-spreadParameter * Math.Pow(referenceTransformation.SqrtDistanceTo(currentTransformation), 2));

        return currentDensity;
    }

    /// <summary>
    /// Finds Transformations within Radius calculated as
    /// r = sqrt(-ln(threshold))/spreadParameter
    /// </summary>
    /// <param name="queryPoint">Transformation to find </param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private List<Transform3D> FindPointsWithinRadius(Transform3D queryPoint, double distance)
    {
        List<Transform3D> resultList = new List<Transform3D>();

        rootNode.ProximityQuery(queryPoint, distance, resultList);
        return resultList;
    }

    private double CalculateSpreadParameter(double medianValue)
    {
        return -Math.Log(Math.Max(Math.Min(medianValue, 1), 0))/Math.Pow(rootNode.GetMedianThreshold(), 2);
    }

    public double SpreadParameter { get => spreadParameter; }
    public double Threshold { get => threshold; }
}