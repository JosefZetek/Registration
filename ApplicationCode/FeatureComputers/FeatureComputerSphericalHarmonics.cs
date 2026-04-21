using System;
using System.Collections.Generic;
using System.Numerics;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.Samplers;

namespace Registration.ApplicationCode.FeatureComputers
{
    public class FeatureComputerSphericalHarmonics : AFeatureComputer
    {
        private readonly int _lMax;
        private readonly double _radius;
        private readonly int _sampleCount;

        public override int NumberOfFeatures => _lMax + 1;

        public FeatureComputerSphericalHarmonics(int lMax, double radius, int sampleCount)
        {
            _lMax = lMax;
            _radius = radius;
            _sampleCount = sampleCount;
        }

        public override void ComputeFeatureVector(AData data, Point3D p, double[] array, int startIndex)
        {
            CheckArrayDimensions(array, startIndex);
            var powerSpectrum = CalculatePowerSpectrum(data, p);

            for (int l = 0; l <= _lMax; l++)
            {
                array[startIndex + l] = powerSpectrum[l];
            }
        }

        public override FCConfiguration GetConfiguration()
        {
            var config = new FCConfiguration
            {
                FeatureComputerType = FCType.SPHERICAL_HARMONICS_FEATURE_COMPUTER
            };
            config.AddParameter("lMax", _lMax);
            config.AddParameter("radius", _radius);
            config.AddParameter("sampleCount", _sampleCount);
            return config;
        }

        private double[] CalculatePowerSpectrum(AData data, Point3D center)
        {
            var powerSpectrum = new double[_lMax + 1];
            var coefficients = new Complex[_lMax + 1, 2 * _lMax + 1];

            // Sample points on the sphere
            var pointsOnSphere = UniformSphereSampler.GetSampledPoints(_sampleCount);

            foreach (var (theta, phi) in pointsOnSphere)
            {
                var p = new Point3D(
                    center.X + _radius * Math.Sin(theta) * Math.Cos(phi),
                    center.Y + _radius * Math.Sin(theta) * Math.Sin(phi),
                    center.Z + _radius * Math.Cos(theta)
                );

                double value = data.GetValue(p);

                for (int l = 0; l <= _lMax; l++)
                {
                    for (int m = -l; m <= l; m++)
                    {
                        var ylm = SphericalHarmonic(l, m, theta, phi);
                        coefficients[l, m + _lMax] += value * Complex.Conjugate(ylm);
                    }
                }
            }
            
            double areaElement = 4.0 * Math.PI / _sampleCount;
            for (int l = 0; l <= _lMax; l++)
            {
                for (int m = -l; m <= l; m++)
                {
                    coefficients[l, m + _lMax] *= areaElement;
                }
            }

            for (int l = 0; l <= _lMax; l++)
            {
                double power = 0;
                for (int m = -l; m <= l; m++)
                {
                    power += coefficients[l, m + _lMax].Magnitude * coefficients[l, m + _lMax].Magnitude;
                }
                powerSpectrum[l] = power;
            }

            return powerSpectrum;
        }

        private Complex SphericalHarmonic(int l, int m, double theta, double phi)
        {
            double factor = Math.Sqrt((2 * l + 1) / (4 * Math.PI) * Factorial(l - Math.Abs(m)) / Factorial(l + Math.Abs(m)));
            var plm = AssociatedLegendrePolynomial(l, Math.Abs(m), Math.Cos(theta));
            
            if (m < 0)
            {
                plm *= Math.Pow(-1, -m) * Factorial(l + m) / Factorial(l - m);
            }

            return Complex.FromPolarCoordinates(factor * plm, m * phi);
        }

        private double AssociatedLegendrePolynomial(int l, int m, double x)
        {
            if (m < 0 || m > l)
            {
                throw new ArgumentOutOfRangeException(nameof(m));
            }

            double pmm = 1.0;
            if (m > 0)
            {
                double sign = (m % 2 == 0) ? 1.0 : -1.0;
                pmm = sign * DoubleFactorial(2 * m - 1) * Math.Pow(1 - x * x, m / 2.0);
            }

            if (l == m)
            {
                return pmm;
            }

            double pmm1 = x * (2 * m + 1) * pmm;
            if (l == m + 1)
            {
                return pmm1;
            }

            double pll = 0;
            for (int i = m + 2; i <= l; i++)
            {
                pll = (x * (2 * i - 1) * pmm1 - (i + m - 1) * pmm) / (i - m);
                pmm = pmm1;
                pmm1 = pll;
            }
            return pll;
        }

        private long Factorial(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (n == 0) return 1;
            long result = 1;
            for (int i = 1; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }
        
        private long DoubleFactorial(int n)
        {
            if (n < -1)
                throw new ArgumentOutOfRangeException(nameof(n));
            
            if (n == 0 || n == -1)
                return 1;

            long result = 1;
            
            for (int i = n; i >= 1; i -= 2)
                result *= i;
            
            return result;
        }

        public override void GetWeights(double[] weights, int offset)
        {
            for (int i = 0; i <= _lMax; i++)
            {
                weights[offset + i] = 1.0;
            }
        }
    }
}
