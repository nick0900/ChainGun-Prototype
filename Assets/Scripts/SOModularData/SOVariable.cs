using System.Collections;
using System.Collections.Generic;
using UnityEngine;


abstract public class SOVariable<T> : ScriptableObject
{
    public T value;
}

[System.Serializable]
abstract public class SOVariableReference<T>
{
    public bool UseConstant = true;
    public T ConstantValue;
    public SOVariable<T> Variable;

    public T Value
    {
        get { return UseConstant ? ConstantValue : Variable.value; }
    }
}
