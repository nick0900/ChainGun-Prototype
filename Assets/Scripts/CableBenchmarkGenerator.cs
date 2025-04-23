using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableBenchmarkGenerator : MonoBehaviour
{
    [SerializeField] GameObject pulley1 = null;
    [SerializeField] CableRoot cable = null;

    [SerializeField] uint iterations = 1;
    [SerializeField] float spacing = 1.0f;
    private void Awake()
    {
        if (pulley1 == null) return;
        if (cable == null) return;

        CableMeshInterface cableEnd = cable.Joints[cable.Joints.Count - 1].body;
        cableEnd.transform.position += Vector3.right * spacing * 2 * iterations;

        for (int i = 0; i < iterations; i++)
        {
            GameObject newPulley = Instantiate(pulley1, cable.transform.position, pulley1.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 1).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2) + Vector3.down * spacing * 0.5f;
            CableRoot.Joint newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = false;
            cable.Joints.Insert(i * 4 + 1, newJoint);

            newPulley = Instantiate(pulley1, cable.transform.position, pulley1.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 2).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2) + Vector3.down * spacing * 1.5f;
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = true;
            cable.Joints.Insert(i * 4 + 2, newJoint);

            newPulley = Instantiate(pulley1, cable.transform.position, pulley1.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 3).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2 + 1.0f) + Vector3.down * spacing * 1.5f;
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = true;
            cable.Joints.Insert(i * 4 + 3, newJoint);

            newPulley = Instantiate(pulley1, cable.transform.position, pulley1.transform.rotation);
            newPulley.name = "pulley" + (i * 4 + 4).ToString();
            newPulley.transform.position += Vector3.right * spacing * (((float)i + 0.5f) * 2 + 1.0f) + Vector3.down * spacing * 0.5f;
            newJoint = new CableRoot.Joint();
            newJoint.linkType = CableRoot.LinkType.Rolling;
            newJoint.body = newPulley.GetComponent<CableMeshInterface>();
            newJoint.orientation = false;
            cable.Joints.Insert(i * 4 + 4, newJoint);
        }
    }
}
