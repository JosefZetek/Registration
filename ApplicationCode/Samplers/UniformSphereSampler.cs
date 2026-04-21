using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Samplers;

public class UniformSphereSampler
    {
        private static double RADIUS;
        private static double SPACING;

        private static List<Point3D>? distributedPoints;


        public static void SampleSphere(double radius, double spacing)
        {
            RADIUS = radius;
            SPACING = spacing;
            distributedPoints = InitializeDistributedPoints();
        }
        
        public static void SampleSphere()
        {
            RADIUS = Constants.RADIUS;
            SPACING = Constants.SPACING;
            distributedPoints = InitializeDistributedPoints();
        }
        

        #region Public Methods - Distributed Points Calculation

        /// <summary>
        /// Method returns uniformly distributed points around the center point
        /// </summary>
        /// <param name="data">Data to check the bounds of an object</param>
        /// <param name="centerPoint">Center point</param>
        /// <returns>Returns list of points satisfying the condition</returns>
        public static List<Point3D> GetDistributedPoints(AData data, Point3D centerPoint)
        {
            if (distributedPoints == null)
                SampleSphere();
                
            List<Point3D> resultPoints = new List<Point3D>();

            foreach (Point3D currentPoint in distributedPoints!)
            {
                if (data.PointWithinBounds(currentPoint.X + centerPoint.X, currentPoint.Y + centerPoint.Y, currentPoint.Z + centerPoint.Z))
                    resultPoints.Add(new Point3D(
                        currentPoint.X + centerPoint.X,
                        currentPoint.Y + centerPoint.Y,
                        currentPoint.Z + centerPoint.Z
                    ));
            }

            return resultPoints;
        }
        
        public static List<Point3D> GetDistributedPoints(Matrix<double> matrix)
        {
            if (distributedPoints == null || distributedPoints.Count == 0)
                SampleSphere();

            List<Point3D> transformedPoints = new List<Point3D>();
            Matrix<double> inverseMatrix = matrix.Transpose();

            foreach (Point3D currentPoint in distributedPoints!)
                transformedPoints.Add(currentPoint.Rotate(inverseMatrix));
            
            return transformedPoints;
        }
        

        #endregion

        #region Public Methods - Sampled Points for Spherical Harmonics

        public static List<(double theta, double phi)> GetSampledPoints(int count)
        {
            var points = new List<(double theta, double phi)>();
            double goldenRatio = (1 + Math.Sqrt(5)) / 2;

            for (int i = 0; i < count; i++)
            {
                double y = 1 - (i / (double)(count - 1)) * 2;  // y goes from 1 to -1
                double radius = Math.Sqrt(1 - y * y);  // radius at y

                double theta = Math.Acos(y);
                double phi = 2 * Math.PI * i / goldenRatio;  // golden angle increment

                points.Add((theta, phi));
            }
            return points;
        }

        #endregion

        #region Private Methods - Points Precalculation


        /// <summary>
        /// Method initializes uniformly distributed points
        /// closer than RADIUS from the origin with specified spacing
        /// </summary>
        /// <returns>Returns list of points</returns>
        private static List<Point3D> InitializeDistributedPoints()
        {
            List<Point3D> points = new List<Point3D>();
            double rSquared = RADIUS * RADIUS;

            for (double x = -RADIUS; x <= RADIUS; x += SPACING)
            {
                for (double y = -RADIUS; y <= RADIUS; y += SPACING)
                {
                    for (double z = -RADIUS; z <= RADIUS; z += SPACING)
                    {
                        if ((x * x + y * y + z * z) <= rSquared)
                        {
                            points.Add(new Point3D(x, y, z));
                        }
                    }
                }
            }

            return points;
        }

        #endregion
    }
