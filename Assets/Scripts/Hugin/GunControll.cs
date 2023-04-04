using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunControll : MonoBehaviour
{
    [SerializeField] GunConnector connectorGun;

    [SerializeField] CableAnchor anchorGun;

    [SerializeField] AnchorEndConnector connectorNode;


    [SerializeField] int maxProjectiles = 10;

    [SerializeField] float totalChainLength = 2;

    [SerializeField] float triggerDelay = 0.3f;

    [SerializeField] float reloadTime = 0.5f;

    Projectile currentProjectile;

    private enum ProjAction
    {
        ToggleStick,
        Stick,
        Unstick,
        Detonate,
        Destroy
    }

    private class ProjQueue
    {
        private LinkedList<Projectile> list;

        private readonly int Capacity;

        public ProjQueue(int size)
        {
            list = new LinkedList<Projectile>();

            Capacity = size;
        }

        public void Enqueue(Projectile projectile)
        {
            // är kön full raderas äldsta projectilen
            if (list.Count == Capacity)
            {
                Dequeue().Remove();
            }
            
            list.AddFirst(projectile.LLN);
        }

        public Projectile Dequeue()
        {
            Projectile lastItem = list.Last.Value;
            list.RemoveLast();
            return lastItem;
        }

        public IEnumerator TriggerSuccessive(ProjAction action, float delay)
        {
            Projectile[] signalArr = new Projectile[list.Count];

            if (action == ProjAction.Destroy || action == ProjAction.Detonate)
            {
                int signals = list.Count;
                for (int i = 0; i < signals; i++)
                {
                    signalArr[i] = Dequeue();
                }
            }
            else
            {
                list.CopyTo(signalArr, 0);
            }

            foreach (Projectile proj in signalArr)
            {
                if (proj != null)
                {
                    switch (action)
                    {
                        case ProjAction.Destroy:
                            proj.Remove();
                            break;

                        case ProjAction.ToggleStick:
                            proj.ToggleSticky();
                            break;

                        case ProjAction.Detonate:
                            proj.Detonate();
                            break;
                    }
                    yield return new WaitForSeconds(delay);
                }
            }
        }
    }

    ProjQueue externalProjectiles;

    int projIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        externalProjectiles = new ProjQueue(maxProjectiles);
    }

    //inputs
    float shootAxis;
    float stickyAxis;
    float broadcastAxis;
    float chainToggleAxis;
    bool hasProjectile = false;
    bool hasConnectedProjectile = false;
    bool chainConnected = false;
    bool shootPressed = false;
    bool stickyPressed = false;
    bool chainTPressed = false;
    bool reloading = false;

    // Update is called once per frame
    void Update()
    {
        shootAxis = Input.GetAxis("Shoot");

        stickyAxis = Input.GetAxis("ToggleSticky");

        broadcastAxis = Input.GetAxis("Broadcast");

        chainToggleAxis = Input.GetAxis("ChainToggle");
    }

    private void FixedUpdate()
    {
        ChainHandler();
        ReloadHandler();
        ShootHandler();
        ProjectileController();
    }

    void ShootHandler()
    {
        // If the key has been pressed
        if (shootAxis > 0)
        {
            if (!shootPressed && hasProjectile)
            {
                print("shoot");
                hasConnectedProjectile = false;
                if (!chainConnected)
                {
                    hasProjectile = false;
                }

                DetachProjectile();

                shootPressed = true;
                shootAxis = 0;
            }
        }
        else if (shootPressed)
        {
            shootPressed = false;
        }
    }

    void ReloadHandler()
    {
        if (!hasProjectile && !reloading)
        {
            StartCoroutine(Reload());
            reloading = true;
        }
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(reloadTime);
        print("reload");
        NewProjectile();
        hasProjectile = true;
        hasConnectedProjectile = true;
        reloading = false;
    }

    void ProjectileController()
    {
        // If the key has been pressed
        if (stickyAxis > 0)
        {
            if (!stickyPressed)
            {
                if (broadcastAxis > 0)
                {
                    print("broadcast toggle sticky");
                    StartCoroutine(externalProjectiles.TriggerSuccessive(ProjAction.ToggleStick, triggerDelay));
                    stickyPressed = true;
                }
                else if (currentProjectile == null)
                {
                    stickyPressed = true;
                }
                else
                {
                    print("toggle sticky");
                    currentProjectile.ToggleSticky();
                    stickyPressed = true;
                }
            }
        }
        else if (stickyPressed)
        {
            stickyPressed = false;
        }
    }

    void ChainHandler()
    {
        if (chainToggleAxis > 0)
        {
            if (!chainTPressed)
            {
                print("switch chain");
                if (chainConnected)
                {
                    chainConnected = false;
                    if (hasProjectile && !hasConnectedProjectile)
                    {
                        DisconnectAnchor();
                        externalProjectiles.Enqueue(currentProjectile);

                        currentProjectile = null;
                        hasProjectile = false;
                    }
                }
                else
                {
                    chainConnected = true;
                }
                chainTPressed = true;
            }
        }
        else if (chainTPressed)
        {
            chainTPressed = false;
        }
    }

    void NewProjectile()
    {
        currentProjectile = ProjectilePool.ProjPool.Request(this.transform.position, this.transform.rotation, false);

        currentProjectile.name = (++projIndex).ToString();

        for (int i = 0; i < currentProjectile.transform.childCount; i++)
        {
            currentProjectile.transform.GetChild(i).name = currentProjectile.name + '.' + i.ToString();
        }

        currentProjectile.ConnectObject(connectorGun);

    }

    void DetachProjectile()
    {
        if (chainConnected)
        {
            ConnectAnchor();

            anchorGun.head.RB2D.AddForce((this.transform.rotation * Vector2.right) * 100.0f);
        }
        else
        {
            currentProjectile.ConnectObject(null);

            externalProjectiles.Enqueue(currentProjectile);

            currentProjectile = null;
        }
    }

    void ConnectAnchor()
    {
        anchorGun.head.RB2D.simulated = true;

        currentProjectile.ConnectObject(connectorNode);
    }

    void DisconnectAnchor()
    {
        currentProjectile.ConnectObject(null);

        anchorGun.head.ChainDestroy(anchorGun.head);

        //anchorGun.head.RB2D.simulated = false;

        //anchorGun.head.transform.position = this.transform.position;
    }
}
