using System;

namespace Registration.ApplicationCode.Other.FCConfiguration;

[Serializable]
public enum ParameterType
{
    String,
    Double,
    Int,
    Bool,
    DoubleArray,
    IntArray,
    BoolArray
}