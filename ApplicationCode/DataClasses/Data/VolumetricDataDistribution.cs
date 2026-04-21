using System;

namespace Registration.ApplicationCode.DataClasses.Data;

/// <summary>
/// This class is supposed to keep the information about number distribution
/// This information is then used for computing feature vectors
/// Min number is expected to be 0, since the input values are always considered positive including 0
/// </summary>
public class VolumetricDataDistribution
{

	private int numberOfValues;
	private int minValue;
	private int maxValue;

	private int[] numberOfOccurences;

	private double[] distributions;

	public VolumetricDataDistribution()
	{
		this.numberOfValues = 0;
		this.minValue = int.MaxValue;
		this.maxValue = int.MinValue;

		numberOfOccurences = new int[0];
	}

	public void AddValue(int value)
	{
		if(value > maxValue)
		{
			minValue = Math.Min(minValue, value);

			int[] tempCopy = new int[value - minValue + 1];
			CopyArray(numberOfOccurences, tempCopy, 0);
			numberOfOccurences = tempCopy;
			maxValue = value;
		}

		else if(value < minValue)
		{
            maxValue = Math.Max(maxValue, value);

            int[] tempCopy = new int[maxValue - value + 1];
            CopyArray(numberOfOccurences, tempCopy, minValue-value);
            numberOfOccurences = tempCopy;
            minValue = value;
        }

        numberOfOccurences[value - minValue]++;
		numberOfValues++;
	}

	private void CopyArray(int[] sourceArray, int[] destinationArray, int offset)
	{
		if (offset < 0)
			throw new Exception("Offset cannot be negative");

		if ((sourceArray.Length + offset) > destinationArray.Length)
			throw new Exception("Destination array isn't long enough");

		for (int i = 0; i < sourceArray.Length; i++)
			destinationArray[i+offset] = sourceArray[i];
	}

	private void CreateDistributionArray()
	{
		distributions = new double[numberOfOccurences.Length];

		double previousOccurences = 0;
		int previousNonZeroIndex = 0;
		for(int i = 0; i<numberOfOccurences.Length; i++)
		{
			if (numberOfOccurences[i] != 0)
			{
                previousOccurences += numberOfOccurences[i];
                distributions[i] = previousOccurences / (double)(numberOfValues);
				previousNonZeroIndex = i;
            }
			else
			{
				int nextNonZeroIndex = FindNextNonZeroOccurence(i + 1);
				double percentage = (double)(i - previousNonZeroIndex) / (nextNonZeroIndex - previousNonZeroIndex);
				distributions[i] = (previousOccurences + percentage * numberOfOccurences[nextNonZeroIndex])/numberOfValues;
			}
		}
	}

	private int FindNextNonZeroOccurence(int startIndex)
	{
		for(int i = startIndex; i<numberOfOccurences.Length; i++)
		{
			if (numberOfOccurences[i] != 0)
				return i;
		}

		throw new Exception("Reached end of the array.");
	}

	public double GetDistributionPercentage(double value)
	{
		if (distributions == null)
            CreateDistributionArray();

		int smallerIndex = (int)Math.Floor(value);
		int biggerIndex = (int)Math.Ceiling(value);

		if (biggerIndex == smallerIndex)
			return distributions[biggerIndex - minValue];

		return (value - smallerIndex) * (distributions[biggerIndex-minValue] - distributions[smallerIndex-minValue]) + distributions[smallerIndex-minValue];
    }

    public double MinValue { get => minValue; }
    public double MaxValue { get => maxValue; }
}

