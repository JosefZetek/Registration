using System;
using System.Collections.Generic;
using System.Linq;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

/// <summary>
/// K-Dimensional Tree for FeatureVector proximity queries
/// </summary>
public class KDTree
{
    /// <summary>
    /// Node in K-Dimensional Tree
    /// </summary>
    private class Node
    {
        public Node left, right;
        public int point;
        public int axis;
    }

    private Node root;
    private FeatureVector[] fVectors;
    private FeatureVector searchFeatureVector;
    
    // We use a Max-Priority Queue to keep track of the N closest points.
    // The "Priority" is the distance (double).
    // .NET 6+ PriorityQueue defaults to Min-Heap, so we pass negative distance or a custom comparer.
    private PriorityQueue<int, double> neighbors;
    private int maxNeighbors;

    public KDTree(FeatureVector[] fVectors)
    {
        if (fVectors == null || fVectors.Length == 0)
            throw new ArgumentException("Feature vectors array cannot be null or empty");
        
        this.fVectors = fVectors;
        int[] indices = Enumerable.Range(0, fVectors.Length).ToArray();
        root = BuildTree(indices, 0);
    }

    /// <summary>
    /// Finds the N nearest neighbors to the given feature vector
    /// </summary>
    public int[] FindNearest(FeatureVector featureVector, int n)
    {
        if (featureVector == null) throw new ArgumentNullException(nameof(featureVector));
        if (n <= 0) return Array.Empty<int>();

        this.searchFeatureVector = featureVector;
        this.maxNeighbors = n;
        
        // We use a custom comparer for Max-Priority (farthest distance first)
        this.neighbors = new PriorityQueue<int, double>(Comparer<double>.Create((a, b) => b.CompareTo(a)));

        if (root != null)
            SearchSubtree(root);

        // Extract indices from the queue
        int[] result = new int[neighbors.Count];
        int i = neighbors.Count - 1;
        while (neighbors.Count > 0)
        {
            result[i--] = neighbors.Dequeue();
        }
        return result;
    }

    /// <summary>
    /// Builds a KD-Tree recursively
    /// </summary>
    /// <param name="indices">Array of point indices to include in this subtree</param>
    /// <param name="depth">Current depth in the tree (determines axis)</param>
    /// <returns>Root node of the constructed subtree</returns>
    private Node BuildTree(int[] indices, int depth)
    {
        if (indices == null || indices.Length == 0)
            return null;

        int axis = depth % fVectors[0].GetNumberOfFeatures;
        
        Node node = new Node
        { 
            axis = axis
        };

        if (indices.Length == 1)
        {
            node.point = indices[0];
            return node;
        }

        // Find median using quickselect
        int medianIndex = indices.Length / 2;
        QuickSelect(indices, 0, indices.Length - 1, medianIndex, axis);
        
        node.point = indices[medianIndex];

        // Build left subtree with points before median
        if (medianIndex > 0)
        {
            int[] leftIndices = new int[medianIndex];
            Array.Copy(indices, 0, leftIndices, 0, medianIndex);
            node.left = BuildTree(leftIndices, depth + 1);
        }

        // Build right subtree with points after median
        if (medianIndex < indices.Length - 1)
        {
            int rightLength = indices.Length - medianIndex - 1;
            int[] rightIndices = new int[rightLength];
            Array.Copy(indices, medianIndex + 1, rightIndices, 0, rightLength);
            node.right = BuildTree(rightIndices, depth + 1);
        }

        return node;
    }
    /// <summary>
    /// Searches subtree for nearest neighbors using recursive DFS with pruning
    /// </summary>
    /// <param name="node">Current node to search</param>
    private void SearchSubtree(Node node)
    {
        if (node == null) return;

        FeatureVector nodePoint = fVectors[node.point];
        double distSq = searchFeatureVector.DistTo2(nodePoint);

        // 1. If we haven't found N points yet, just add it.
        // 2. If this point is closer than the farthest point in our current set, swap them.
        if (neighbors.Count < maxNeighbors)
        {
            neighbors.Enqueue(node.point, distSq);
        }
        else if (distSq < neighbors.PeekPriority()) // PeekPriority looks at the farthest distance
        {
            neighbors.Dequeue();
            neighbors.Enqueue(node.point, distSq);
        }

        double axisDiff = searchFeatureVector.Features[node.axis] - nodePoint.Features[node.axis];
        double axisDiffSq = axisDiff * axisDiff;

        Node firstSubtree = axisDiff < 0 ? node.left : node.right;
        Node secondSubtree = axisDiff < 0 ? node.right : node.left;

        // Search the closer side
        SearchSubtree(firstSubtree);

        // PRUNING: Only search the other side if the distance to the splitting plane 
        // is smaller than the farthest distance in our "best" list.
        if (neighbors.Count < maxNeighbors || axisDiffSq < neighbors.PeekPriority())
        {
            SearchSubtree(secondSubtree);
        }
    }

    /// <summary>
    /// Quickselect algorithm to find the k-th smallest element
    /// Partitions the array so that the element at position k is in its final sorted position
    /// </summary>
    /// <param name="indices">Array of point indices</param>
    /// <param name="left">Left boundary of the partition</param>
    /// <param name="right">Right boundary of the partition</param>
    /// <param name="k">Target position (median index)</param>
    /// <param name="axis">Axis along which to compare values</param>
    private void QuickSelect(int[] indices, int left, int right, int k, int axis)
    {
        while (left < right)
        {
            int pivotIndex = Partition(indices, left, right, axis);
            
            if (pivotIndex == k)
                return;
            else if (k < pivotIndex)
                right = pivotIndex - 1;
            else
                left = pivotIndex + 1;
        }
    }

    /// <summary>
    /// Partitions array segment around a pivot element
    /// </summary>
    /// <param name="indices">Array of point indices</param>
    /// <param name="left">Left boundary</param>
    /// <param name="right">Right boundary</param>
    /// <param name="axis">Axis along which to compare values</param>
    /// <returns>Final position of the pivot element</returns>
    private int Partition(int[] indices, int left, int right, int axis)
    {
        // Choose the rightmost element as pivot
        double pivotValue = fVectors[indices[right]].Features[axis];
        int storeIndex = left;

        // Move all elements smaller than pivot to the left
        for (int i = left; i < right; i++)
        {
            if (fVectors[indices[i]].Features[axis] < pivotValue)
            {
                Swap(indices, i, storeIndex);
                storeIndex++;
            }
        }

        // Place pivot in its final position
        Swap(indices, storeIndex, right);
        return storeIndex;
    }

    /// <summary>
    /// Swaps two elements in an array
    /// </summary>
    private void Swap(int[] array, int i, int j)
    {
        int temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }
}

/// <summary>
/// Helper extension for .NET PriorityQueue to look at the priority of the top element
/// </summary>
public static class PriorityQueueExtensions
{
    public static TPriority PeekPriority<TElement, TPriority>(this PriorityQueue<TElement, TPriority> queue)
    {
        queue.TryPeek(out _, out TPriority priority);
        return priority;
    }
}