using System;
using System.Collections.Generic;

namespace Registration.ApplicationCode.Other;

public class QuickSelectClass
{
    private Random random = new Random();

    public T QuickSelect<T>(List<T> list, int k) where T : IComparable<T>
    {
        List<T> tempArray = new List<T>();
        for (int i = 0; i < list.Count; i++)
            tempArray.Add(list[i]);


        int left = 0, right = tempArray.Count - 1;

        while (left <= right)
        {
            int pivotIndex = random.Next(left, right + 1);
            (int low, int high) = PartitionThreeWay(tempArray, left, right, pivotIndex);

            if (k >= low && k <= high)
                return tempArray[k]; // Found the median efficiently
            else if (k < low)
                right = low - 1; // Search left
            else
                left = high + 1; // Search right
        }

        throw new InvalidOperationException("QuickSelect failed.");
    }

    private (int, int) PartitionThreeWay<T>(List<T> list, int left, int right, int pivotIndex) where T : IComparable<T>
    {
        T pivotValue = list[pivotIndex];
        int low = left, mid = left, high = right;

        while (mid <= high)
        {
            int cmp = list[mid].CompareTo(pivotValue);

            if (cmp < 0)  // Move smaller elements to the left
            {
                Swap(list, low, mid);
                low++; mid++;
            }
            else if (cmp > 0) // Move larger elements to the right
            {
                Swap(list, mid, high);
                high--;
            }
            else  // Equal elements stay in the middle
            {
                mid++;
            }
        }

        return (low, high); // Return range of pivot duplicates
    }

    private void Swap<T>(List<T> list, int i, int j)
    {
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
}