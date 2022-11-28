using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeSet<T> : ScriptableObject
{
    private List<T> Set = new List<T>();

    public T this[int index]
    {
        get => Set[index];
    }

    public IEnumerator GetEnumerator()
    {
        return Set.GetEnumerator();
    }

    public int Count { get => Set.Count; }

    public void Add(T t)
    {
        if (!Set.Contains(t)) Set.Add(t);
    }

    public void Remove(T t)
    {
        if (Set.Contains(t)) Set.Remove(t);
    }
}

public abstract class SetRegistor<T> : MonoBehaviour
{
    public bool getObject = true;
    public T item;
    public RuntimeSet<T> set;
    private void OnEnable()
    {
        if (set != null)
        {
            set.Add(getObject ? GetComponent<T>() : item);
        }
    }

    private void OnDisable()
    {
        if (set != null)
        {
            set.Remove(getObject ? GetComponent<T>() : item);
        }
    }
}
