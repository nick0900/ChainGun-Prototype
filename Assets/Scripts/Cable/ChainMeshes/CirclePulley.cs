using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CirclePulley : ChainMesh
{

    [SerializeField] CircleCollider2D data = null;

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

    public override bool MeshGenerated { get { return data != null; } }

    public override bool Errornous
    {
        get
        {
            return (MeshGenerated && (data.radius <= 0)) ||
                (MeshGenerated && !submesh && !CMDictionary.CMD.IsRegistered(data, this)) ||
                (MeshGenerated && submesh && CMDictionary.CMD.IsRegistered(data, this)) ||
                (!MeshGenerated && CMDictionary.CMD.IsRegistered(this));
        }
    }

    public override void GenerateChainMesh()
    {
        if (submesh) return;

        data = GetComponent<CircleCollider2D>();

        if (data == null) return;

        CMDictionary.CMD.Register(data, this);

        PrintErrors();
    }
    public override void RemoveChainMesh()
    {
        if (data != null)
        {
            CMDictionary.CMD.Unregister(data);
        }

        data = null;
    }

    public override void GenerateSubMesh(ChainMesh root)
    {
        if (submesh) return;

        data = GetComponent<CircleCollider2D>();

        if (data == null) return;

        CMDictionary.CMD.Register(data, root);

        PrintErrors();
    }

    public override void ForceGenerateSubMesh(ChainMesh root)
    {
        if (!ProtectSettings)
        {
            submesh = true;
        }

        GenerateSubMesh(root);
    }

    public override bool PrintErrors()
    {
        bool error = false;

        if (MeshGenerated && (data.radius <= 0))
        {
            error = true;
            print(this + "/Error: radious of circular pulley must be greater than 0");
        }

        if (MeshGenerated && !submesh && !CMDictionary.CMD.IsRegistered(data, this))
        {
            if (!CMDictionary.CMD.IsRegistered(data))
            {
                print(this + "/Error: collider:" + data + " not registered");
            }
            else
            {
                print(this + "/Error: collider:" + data + " registered to another chainmesh");
            }
            error = true;
        }

        if (MeshGenerated && submesh && CMDictionary.CMD.IsRegistered(data, this))
        {
            print(this + "/Error: collider:" + data + " registered to this chainmesh in spite of being a submesh");
            error = true;
        }

        if (!MeshGenerated && CMDictionary.CMD.IsRegistered(this))
        {
            error = true;
            print(this + "/Error: Unknown collider is registered to chainmesh");
        }

        return error;
    }

    public override bool CorrectErrors()
    {
        bool errorsFixed = true;

        if (MeshGenerated && (data.radius <= 0))
        {
            print(this + "/Error: radious of circular pulley must be greater than 0");
            print(this + "/FixFailed: no automatic fix implemented");
            errorsFixed = false;
        }

        if (MeshGenerated && !submesh && !CMDictionary.CMD.IsRegistered(data, this))
        {
            if (!CMDictionary.CMD.IsRegistered(data))
            {
                print(this + "/Error: collider:" + data + " not registered");
            }
            else
            {
                print(this + "/Error: collider:" + data + " registered to another chainmesh");
            }

            CMDictionary.CMD.Register(data, this);

            print(this + "/Fix: re registering");
            if (!CMDictionary.CMD.IsRegistered(data, this))
            {
                print("Failed");
                errorsFixed = false;
            }
            else
            {
                print("Succeded");
            }
        }

        if (MeshGenerated && submesh && CMDictionary.CMD.IsRegistered(data, this))
        {
            print(this + "/Error: collider:" + data + " registered to this chainmesh in spite of being a submesh");
            print(this + "/Fix: removing chainmesh");

            RemoveChainMesh();
        }

        if (!MeshGenerated && CMDictionary.CMD.IsRegistered(this))
        {
            errorsFixed = false;
            print(this + "/Error: Unknown collider is registered to chainmesh");
            print(this + "/FixFailed: No way of unregistering collider. global collider recalculation or manual removal of collider needed!");
        }

        return errorsFixed;
    }

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 tailDirection = tailPrevious - (Vector2)data.transform.position + data.offset;
        Vector2 headDirection = headPrevious - (Vector2)data.transform.position + data.offset;

        return Vector2.SignedAngle(tailDirection, headDirection) > 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, ref ChainJointCache cache)
    {
        Vector2 d = ((Vector2)data.transform.position) - point;

        if (d.magnitude <= data.radius - chainWidth / 2)
        {
            throw new System.Exception();
        }

        float alpha = d.x >= 0 ? Mathf.Asin(d.y / d.magnitude) : Mathf.PI - Mathf.Asin(d.y / d.magnitude);

        float phi = Mathf.Asin((data.radius - chainWidth / 2) / d.magnitude);

        alpha = orientation ? alpha - Mathf.PI / 2 - phi : alpha + Mathf.PI / 2 + phi;

        return data.radius * new Vector2(Mathf.Cos(alpha), Mathf.Sin(alpha));
    }

    public override void CreateChainCollider(float chainWidth)
    {
        throw new System.NotImplementedException();
    }
}
