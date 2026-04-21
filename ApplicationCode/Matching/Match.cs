using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

/// <summary>
/// 
/// </summary>
public class Match: IComparable<Match>
{
    private FeatureVector microFeatureVector;
    private FeatureVector macroFeatureVector;
    private double similarity;

    /// <summary>
    /// Invalid match
    /// </summary>
    /// <param name="f1"></param>
    public Match(FeatureVector f1)
    {
        this.microFeatureVector = f1;
        this.macroFeatureVector = new FeatureVector();
        this.Similarity = 0;
    }

    public Match(FeatureVector microFV, FeatureVector macroFV)
    {
        this.microFeatureVector = microFV;
        this.macroFeatureVector = macroFV;
        this.similarity = CalculateSimilarity(microFV, macroFV);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="f1"></param>
    /// <param name="f2"></param>
    /// <returns></returns>
    private double CalculateSimilarity(FeatureVector f1, FeatureVector f2)
    {
        double num = 0;
        double denom = f1.Magnitude() * f2.Magnitude();

        for (int i = 0; i < f1.GetNumberOfFeatures; i++)
        {
            num += f1.Features[i] * f2.Features[i];
        }

        double s = num / denom * 100;
        return (s < 0) ? 0 : s;
    }

    /// <summary>
    /// Creation of valid match
    /// </summary>
    /// <param name="microFeatureVector">Micro feature vector</param>
    /// <param name="macroFeatureVector">Macro feature vector</param>
    /// <param name="similarity">Similarity of feature vectors</param>
    public Match(FeatureVector microFeatureVector, FeatureVector macroFeatureVector, double similarity)
    {
        this.microFeatureVector = microFeatureVector;
        this.macroFeatureVector = macroFeatureVector;
        this.Similarity = similarity;
    }

    public FeatureVector microFV { get => microFeatureVector; }
    public FeatureVector macroFV { get => macroFeatureVector; }
    public double Similarity { get => similarity; set => similarity = value; }

    public int CompareTo(Match other)
    {
        return other.similarity.CompareTo(this.similarity);
    }

    public override string ToString()
    {
        return $"Match contains:\nMicro feature vector:\n{microFeatureVector}\nMacro feature vector:\n{macroFeatureVector}";
    }
}