using System;
using System.Collections.Generic;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Density;

public class DensityNaive
{

    private List<Transform3D> transformations;
    private double threshold;
    private double spreadParameter;

    public DensityNaive(List<Transform3D> transformations, double threshold, double spreadParameter)
    {
        this.transformations = transformations;
        this.threshold = threshold;
        this.spreadParameter = spreadParameter;
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

            if (bestDensity < currentDensity)
            {
                bestDensity = currentDensity;
                bestTransformation = transformations[i];
            }

        }

        return bestTransformation;
    }

    private List<Transform3D> FindPointsWithinRadius(Transform3D referenceTransformation, double maxDistance)
    {
        List<Transform3D> closeTransformations = new List<Transform3D>();

        for(int i = 0; i<transformations.Count; i++)
        {
            if (referenceTransformation.RelativeDistanceTo(transformations[i]) <= maxDistance)
                closeTransformations.Add(transformations[i]);
        }

        return closeTransformations;
    }

    private double CalculateDensity(Transform3D referenceTransformation, List<Transform3D> transformations)
    {
        double currentDensity = 0;

        //Density is calculated like SUM of e^(-(spreadParameter * distance)^2) for all close transformations
        foreach (Transform3D currentTransformation in transformations)
            currentDensity += Math.Exp(-spreadParameter * Math.Pow(referenceTransformation.RelativeDistanceTo(currentTransformation), 2));

        return currentDensity;
    }
}
