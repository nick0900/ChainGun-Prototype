using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableBenchmarkGenerator : MonoBehaviour
{
    enum BenchmarkType
    {
        Hilbert,
        Falling
    }
    [SerializeField] BenchmarkType benchmarkType;

    [SerializeField] GameObject pulley = null;
    [SerializeField] CableRoot cable = null;
    [SerializeField] GameObject fallingPulley = null;
    [SerializeField] CableEngine engine = null;

    [SerializeField] uint iterations = 1;
    [SerializeField] float spacing = 1.0f;
    [SerializeField] float fallHeight = 1.0f;

    private void Awake()
    {
        if (engine == null) return;
        if (engine.enabled) engine.enabled = false;
        switch (benchmarkType)
        {
            case BenchmarkType.Hilbert:
                Hilbert();
                break;
            case BenchmarkType.Falling:
                Falling();
                break;
        }
        engine.enabled = true;
    }

    void Hilbert()
    {
        if (pulley == null) return;
        if (cable == null) return;

        CableMeshInterface cableEnd = cable.Joints[cable.Joints.Count - 1].body;
        cableEnd.transform.position += Vector3.right * spacing * 2 * iterations;

        for (int i = 0; i < iterations; i++)
        {
            GameObject newPulley = Instantiate(pulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 1).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2) + Vector3.down * spacing * 0.5f;
            CableRoot.Joint newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = false;
            cable.Joints.Insert(i * 4 + 1, newJoint);

            newPulley = Instantiate(pulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 2).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2) + Vector3.down * spacing * 1.5f;
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = true;
            cable.Joints.Insert(i * 4 + 2, newJoint);

            newPulley = Instantiate(pulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 3).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2 + 1.0f) + Vector3.down * spacing * 1.5f;
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = true;
            cable.Joints.Insert(i * 4 + 3, newJoint);

            newPulley = Instantiate(pulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 4).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2 + 1.0f) + Vector3.down * spacing * 0.5f;
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = false;
            cable.Joints.Insert(i * 4 + 4, newJoint);
        }
    }

    void Falling()
    {
        if (pulley == null) return;
        if (cable == null) return;
        if (fallingPulley == null) return;

        CableMeshInterface cableEnd = cable.Joints[cable.Joints.Count - 1].body;
        cableEnd.transform.position += Vector3.right * spacing * 2 * iterations;

        for (int i = 0; i < iterations; i++)
        {
            GameObject newPulley = Instantiate(pulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "pulley" + (i * 2 + 1).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2);
            CableRoot.Joint newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = false;
            cable.Joints.Insert(i * 2 + 1, newJoint);

            newPulley = Instantiate(pulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "pulley" + (i * 2 + 2).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2 + 1.0f);
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = true;
            cable.Joints.Insert(i * 2 + 2, newJoint);

            newPulley = Instantiate(fallingPulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "falling pulley" + (i * 2 + 1).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2) + Vector3.up * fallHeight;
            engine.Bodies.Add(newPulley.GetComponent<CableMeshInterface>());

            newPulley = Instantiate(fallingPulley, cable.transform.position, pulley.transform.rotation);
            newPulley.name = "falling pulley" + (i * 2 + 2).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2 + 1.0f) + Vector3.up * fallHeight;
            engine.Bodies.Add(newPulley.GetComponent<CableMeshInterface>());
        }
    }
}
