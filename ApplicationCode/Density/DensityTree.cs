using System.Collections.Generic;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Density;

public class DensityTree
{
    private const int LEAF_NODE_TRANSFORMATIONS = 1;

    private List<Transform3D> nodesTransformations;

    private double threshold;

    private DensityTree closeNode = null;
    private DensityTree farNode = null;

    public DensityTree(List<Transform3D> transformations)
    {
        this.nodesTransformations = new List<Transform3D>();
        List<int> remainingIndexes = CreateIndexes(transformations);
        ConstructTree(transformations, remainingIndexes);
    }

    private DensityTree(List<Transform3D> transformations, List<int> remainingIndexes)
    {
        this.nodesTransformations = new List<Transform3D>();
        ConstructTree(transformations, remainingIndexes);
    }

    private void ConstructTree(List<Transform3D> transformations, List<int> remainingIndexes)
    {
        // If this is the leaf node
        if (remainingIndexes.Count <= LEAF_NODE_TRANSFORMATIONS)
        {
            AddTransformationsToCurrentNode(transformations, remainingIndexes);
            return;
        }

        int PIVOT_INDEX = remainingIndexes[0];
        nodesTransformations.Add(transformations[PIVOT_INDEX]);
        remainingIndexes.RemoveAt(0);

        List<int> farTransformations = new List<int>();
        List<int> closeTransformations = new List<int>();

        DivideIntoLists(transformations, remainingIndexes, ref closeTransformations, ref farTransformations);

        if (closeTransformations.Count > 0)
            this.closeNode = new DensityTree(transformations, closeTransformations);

        if (farTransformations.Count > 0)
            this.farNode = new DensityTree(transformations, farTransformations);
    }

    private void DivideIntoLists(List<Transform3D> transformations, List<int> remainingIndexes, ref List<int> closeNodes, ref List<int> farNodes)
    {
        Transform3D referenceTransformation = this.nodesTransformations[0];

        List<double> transformationDistances = new List<double>();

        int currentTransformationIndex;
        for (int i = 0; i < remainingIndexes.Count; i++)
        {
            currentTransformationIndex = remainingIndexes[i];
            transformationDistances.Add(referenceTransformation.SqrtDistanceTo(transformations[currentTransformationIndex]));
        }
        
        QuickSelectClass testClass = new QuickSelectClass();
        this.threshold = testClass.QuickSelect(transformationDistances, transformationDistances.Count / 2);

        for (int i = 0; i < transformationDistances.Count; i++)
        {
            int originalIndex = remainingIndexes[i];
            if (transformationDistances[i] >= threshold)
                farNodes.Add(originalIndex);
            else
                closeNodes.Add(originalIndex);
        }
    }

    public void ProximityQuery(Transform3D queryPoint, double radius, List<Transform3D> result)
    {
        double distance = queryPoint.SqrtDistanceTo(this.nodesTransformations[0]);

        // Check if the current node is within the radius
        if (distance < radius)
        {
            result.AddRange(this.nodesTransformations);
        }

        // Recursively search the subtrees based on distance and threshold
        if (this.closeNode != null && (distance - radius) < this.threshold)
        {
            this.closeNode.ProximityQuery(queryPoint, radius, result);
        }

        if (this.farNode != null && (distance + radius) > this.threshold)
        {
            this.farNode.ProximityQuery(queryPoint, radius, result);
        }
    }

    private List<int> CreateIndexes(List<Transform3D> transformations)
    {
        List<int> remainingIndexes = new List<int>(transformations.Count);
        for (int i = 0; i < transformations.Count; i++)
            remainingIndexes.Add(i);

        return remainingIndexes;
    }

    private void AddTransformationsToCurrentNode(List<Transform3D> transformations, List<int> remainingIndexes)
    {
        foreach (int index in remainingIndexes)
            this.nodesTransformations.Add(transformations[index]);
    }

    public double GetMedianThreshold()
    {
        List<double> listOfThresholds = new List<double>();
        Queue<DensityTree> densityTrees = new Queue<DensityTree>();
        DensityTree currentDensityTree = this;

        bool isLeaf;

        do
        {
            isLeaf = true;

            if (currentDensityTree.farNode != null)
            {
                densityTrees.Enqueue(currentDensityTree.farNode);
                isLeaf = false;
            }

            if (currentDensityTree.closeNode != null)
            {
                densityTrees.Enqueue(currentDensityTree.closeNode);
                isLeaf = false;
            }

            if(!isLeaf)
                listOfThresholds.Add(currentDensityTree.threshold);

            currentDensityTree = densityTrees.Dequeue();
        } while (densityTrees.Count > 0);

        QuickSelectClass testClass = new QuickSelectClass();
        return testClass.QuickSelect(listOfThresholds, listOfThresholds.Count / 2);
    }


}