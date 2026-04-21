using System;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Samplers;

/// <summary>
/// This sampler outputs identical points for both micro and macro data
/// </summary>
public class SamplerFake : ISampler
{
    private AData microData, macroData;

    private Random random;
    private Point3D[] samePointArray;

    private double randomIncrement;

    private Transform3D expectedTransformation;

    private bool arrayFilled = false;

    /// <summary>
    /// Constructor for fake sampler, generating given amount of same points
    /// </summary>
    /// <param name="microData">Instance of micro data</param>
    /// <param name="macroData">Instance of macro data</param>
    /// <param name="samePointsCount">Number of same points</param>
    /// <param name="randomIncrement">Max difference for same points in each of the axis</param>
    /// <param name="expectedTransformation">Transformation aligning micro data onto macro data</param>
	public SamplerFake(AData microData, AData macroData, int samePointsCount, double randomIncrement, Transform3D expectedTransformation)
	{
        this.microData = microData;
        this.macroData = macroData;

        this.randomIncrement = randomIncrement;

        this.expectedTransformation = expectedTransformation;
        samePointArray = new Point3D[Math.Max(0, samePointsCount)];
        InitializeDefaultValues();
	}

    private void InitializeDefaultValues()
    {
        int seed = 100;
        random = new Random(seed);
    }

    private void FindSamePoints()
    {
        if (arrayFilled)
            return;

        /* Generating amount of same points */
        for (int i = 0; i < samePointArray.Length; i++)
            samePointArray[i] = new Point3D(
                microData.MaxValueX * random.NextDouble(),
                microData.MaxValueY * random.NextDouble(),
                microData.MaxValueZ * random.NextDouble()
            );

        arrayFilled = true;
    }

    public Point3D[] Sample(AData d, int count)
    {
        FindSamePoints();

        if (d == microData)
            return GenerateMicroPoints(count);

        return GenerateMacroPoints(count);
            
        //ShuffleArray(sampledPoints);
        

    }

    private void ShuffleArray(Point3D[] point)
    {
        for(int i = 0; i<point.Length; i++)
        {
            Point3D temp = point[i];
            int randomIndex = random.Next(point.Length);
            point[i] = point[randomIndex];
            point[randomIndex] = temp;
        }
    }

    private Point3D ShiftPointRandomly(Point3D point)
    {
        return new Point3D(
            point.X + random.NextDouble() * randomIncrement,
            point.Y + random.NextDouble() * randomIncrement,
            point.Z + random.NextDouble() * randomIncrement
        );
    }

    /// <summary>
    /// Transforms a given micro-level point to a corresponding macro-level point 
    /// aligned with a specific target transformation.
    /// </summary>
    /// <param name="point">The micro-level point to transform for alignment with the macro-level coordinate system.</param>
    /// <returns>The corresponding macro-level point after applying the expected transformation.</returns>
    private Point3D ConvertPointToMacro(Point3D point)
    {
        return point.ApplyRotationTranslation(expectedTransformation);
    }

    private Point3D[] GenerateMicroPoints(int count)
    {
        Point3D[] resultArray = new Point3D[count];

        int numberOfSamePoints = Math.Min(samePointArray.Length, count);

        /* Same point generation */
        for (int i = 0; i < numberOfSamePoints; i++)
            resultArray[i] = ShiftPointRandomly(samePointArray[i]);

        int numberOfComplements = count - numberOfSamePoints;

        /* Rest of the points generation */
        for (int i = 0; i < numberOfComplements; i++)
            resultArray[i + numberOfSamePoints] = new Point3D(
                microData.MaxValueX * random.NextDouble(),
                microData.MaxValueY * random.NextDouble(),
                microData.MaxValueZ * random.NextDouble()
            );

        return resultArray;
    }

    private Point3D[] GenerateMacroPoints(int count)
    {
        Point3D[] resultArray = new Point3D[count];

        int numberOfSamePoints = Math.Min(samePointArray.Length, count);

        /* Same point generation */
        for (int i = 0; i < numberOfSamePoints; i++)
            resultArray[i] = ShiftPointRandomly(ConvertPointToMacro(samePointArray[i]));

        int numberOfComplements = count - numberOfSamePoints;

        /* Rest of the points generation */
        for (int i = 0; i < numberOfComplements; i++)
            resultArray[i + numberOfSamePoints] = new Point3D(
                macroData.MaxValueX * random.NextDouble(),
                macroData.MaxValueY * random.NextDouble(),
                macroData.MaxValueZ * random.NextDouble()
            );

        return resultArray;
    }
}

