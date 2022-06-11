using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private Queue<Projectile> projectilePool;

    [SerializeField] int startingPool = 5;

    public static ProjectilePool ProjPool;

    private void Awake()
    {
        if (ProjPool != null)
            GameObject.Destroy(ProjPool);
        else
            ProjPool = this;
    }

    void Start()
    {
        projectilePool = new Queue<Projectile>();
        for (int i = 0; i < startingPool; i++)
        {
            Projectile item = Instantiate(projectilePrefab, this.transform.position, this.transform.rotation).GetComponent<Projectile>();
            item.Freeze();
            projectilePool.Enqueue(item);
        }
    }

    public Projectile Request(Vector3 position, Quaternion rotation, bool unFrozen)
    {
        Projectile item;

        if (projectilePool.Count > 0)
        {
            item = projectilePool.Dequeue();

            item.transform.parent.position = position;
            item.transform.parent.rotation = rotation;
        }
        else
        {
            item = Instantiate(projectilePrefab, position, rotation).GetComponent<Projectile>();
            if (!unFrozen)
            {
                item.Freeze();
            }
        }

        if (unFrozen)
        {
            item.Unfreeze();
        }

        return item;
    }

    public void Store(Projectile item)
    {
        item.Freeze();

        item.transform.parent.position = this.transform.position;
        item.transform.parent.rotation = this.transform.rotation;

        projectilePool.Enqueue(item);
    }
}
