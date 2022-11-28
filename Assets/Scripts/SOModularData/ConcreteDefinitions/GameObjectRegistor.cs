using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectRegistor : SetRegistor<GameObject> 
{
    private void OnEnable()
    {
        if (set != null)
        {
            set.Add(getObject ? this.gameObject : item);
        }
    }

    private void OnDisable()
    {
        if (set != null)
        {
            set.Remove(getObject ? this.gameObject : item);
        }
    }
}
