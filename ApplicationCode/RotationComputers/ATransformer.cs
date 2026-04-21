using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Matching;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Samplers;

namespace Registration.ApplicationCode.RotationComputers;

public abstract class ATransformer
{
    private object lockObj;

    public ATransformer(UniformSphereSampler uniformSphereSampler)
    {
        this.lockObj = new object();
    }

    public ATransformer()
    {
        this.lockObj = new object();
    }

    #region Public Method Appending Transformations

    /// <summary>
    /// Method appends transformations to the passed list
    /// </summary>
    /// <param name="m">Match for which the transformation gets calculated</param>
    /// <param name="dataMicro">Micro data instance</param>
    /// <param name="dataMacro">Macro data instance</param>
    public Transform3D[] GetTransformations(Match m, AData dataMicro, AData dataMacro)
    {

        Transform3D[] transformations;

        Matrix<double>[] rotationMatrices = GetRotationMatrices(dataMicro, dataMacro, m.microFV.Point, m.macroFV.Point);
        Vector<double> translationVector;

        if (rotationMatrices == null)
            return null; // No rotation matrices found, nothing to append

        transformations = new Transform3D[rotationMatrices.Length];

        for (int i = 0; i < rotationMatrices.Length; i++)
        {
            translationVector = GetTranslationVector(m, rotationMatrices[i]);
            transformations[i] = new Transform3D(rotationMatrices[i], translationVector);
        }

        return transformations;
    }

    #endregion

    #region Private Methods for transformation calculation
    protected abstract Matrix<double>[] GetRotationMatrices(AData dataMicro, AData dataMacro, Point3D pointMicro, Point3D pointMacro);

    private Vector<double> GetTranslationVector(Match m, Matrix<double> rotationMatrix)
    {
        Point3D pMicro = m.microFV.Point.Copy();
        Point3D pMacro = m.macroFV.Point.Copy();


        pMicro = pMicro.Rotate(rotationMatrix);

        Vector<double> translationVector = Vector<double>.Build.Dense(3);
        translationVector[0] = pMacro.X - pMicro.X; // real coordinates
        translationVector[1] = pMacro.Y - pMicro.Y;
        translationVector[2] = pMacro.Z - pMicro.Z;

        return translationVector;
    }

    #endregion
}
