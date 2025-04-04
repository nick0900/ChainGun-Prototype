using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableRoot : MonoBehaviour
{
    [System.Serializable]
    public enum LinkType
    {
        Rolling,
        Point,
        Hybrid
    }

    [System.Serializable]
    public class Joint
    {
        public LinkType linkType = LinkType.Rolling;

        public CableMeshInterface body = null;

        [HideInInspector] public float tIdentityTail;
        [HideInInspector] public float tIdentityHead;
        [HideInInspector] public float tIdentityTailPrev;
        [HideInInspector] public float tIdentityHeadPrev;
        public Vector2 tangentOffsetHead;
        public Vector2 tangentOffsetTail;
        [HideInInspector] public Vector2 tangentPointHead;
        [HideInInspector] public Vector2 tangentPointTail;

        [HideInInspector] public float totalLambda = 0;
        [HideInInspector] public float inverseEffectiveMassDenominator = 0;

        public float storedLength = 0.0f;
        public float restLength = 1.0f;
        [HideInInspector] public float currentLength = 0;
        [HideInInspector] public float positionError = 0;
        [HideInInspector] public float segmentTension = 0.0f;

        [HideInInspector] public Vector2 cableUnitVector = Vector2.zero;

        public bool orientation = false;

        [HideInInspector] public bool slipping = false;
        [HideInInspector] public float frictionFactor = 1.0f;
    }

    public float CableHalfWidth = 0.05f;
    
    //implement later
    public bool Looping = false;
    
    public List<Joint> Joints;

    static public void UpdatePulley(ref Joint joint, ref Joint jointTail, ref Joint jointHead, float cableHalfWidth)
    {
        if (joint.linkType != LinkType.Rolling) return;

        if (jointTail.linkType != LinkType.Rolling)
        {
            jointTail.tangentPointHead = jointTail.body.transform.TransformPoint(jointTail.tangentOffsetHead);
            jointTail.tangentPointTail = jointTail.tangentPointHead;
            joint.tangentOffsetTail = joint.body.PointToShapeTangent(jointTail.tangentPointHead, joint.orientation, cableHalfWidth, out joint.tIdentityTail);
        }
        if (jointHead.linkType == LinkType.Rolling)
        {
            CableMeshInterface.TangentAlgorithm(jointHead.body, joint.body, out jointHead.tangentOffsetTail, out joint.tangentOffsetHead, out jointHead.tIdentityTail, out joint.tIdentityHead, jointHead.orientation, joint.orientation, cableHalfWidth);
        }
        else
        {
            jointHead.tangentPointTail = jointHead.body.transform.TransformPoint(jointHead.tangentOffsetTail);
            jointHead.tangentPointHead = jointHead.tangentPointTail;
            joint.tangentOffsetHead = joint.body.PointToShapeTangent(jointHead.tangentPointTail, !joint.orientation, cableHalfWidth, out joint.tIdentityHead);
        }

        float distTail = joint.body.ShapeSurfaceDistance(joint.tIdentityTailPrev, joint.tIdentityTail, joint.orientation, cableHalfWidth, true);
        float distHead = joint.body.ShapeSurfaceDistance(joint.tIdentityHeadPrev, joint.tIdentityHead, joint.orientation, cableHalfWidth, true);

        // Update stored lengths:
        joint.storedLength -= distTail;
        joint.storedLength += distHead;

        // Update rest lengths:
        joint.restLength += distTail;
        jointHead.restLength -= distHead;

        Vector2 geometricalCenter = joint.body.PulleyCentreGeometrical;
        joint.tangentPointTail = geometricalCenter + joint.tangentOffsetTail;
        joint.tangentPointHead = geometricalCenter + joint.tangentOffsetHead;
        Vector2 COMOffset = geometricalCenter - joint.body.CenterOfMass;
        joint.tangentOffsetTail += COMOffset;
        joint.tangentOffsetHead += COMOffset;

        joint.tIdentityTailPrev = joint.tIdentityTail;
        joint.tIdentityHeadPrev = joint.tIdentityHead;
    }

    static public void InitializeSegment(ref Joint joint, in Joint jointTail)
    {
        Vector3 distVector = joint.tangentPointTail - jointTail.tangentPointHead;
        joint.currentLength = distVector.magnitude;
        joint.cableUnitVector = distVector.normalized;

        joint.positionError = joint.currentLength - joint.restLength;

        joint.segmentTension = TensionEstimation(joint);
    }

    static float TensionEstimation(Joint currentNode)
    {
        return Mathf.Max(currentNode.currentLength - currentNode.restLength, 0.0f);
    }


    static public void AddJoint(CableRoot root, Joint segment, CableMeshInterface body)
    {
        int segmentIndex = root.Joints.FindIndex(x => x == segment);
        Joint jointHead = segment;
        Joint jointTail = root.Joints[segmentIndex - 1];
        Joint jointNew = new Joint();

        jointNew.linkType = LinkType.Rolling;
        jointNew.body = body;

        /*
        newNode = null;

        if ((hitPulley == null) || !hitPulley.MeshGenerated) return false;
        if ((this.linkType == LinkType.Rolling) && (hitPulley == this.pulley)) return false;
        if ((this.tail != null) && (this.tail.linkType == LinkType.Rolling) && (this.tail.node.pulley == hitPulley)) return false;

        newNode = ((GameObject)Instantiate(nodePrefab, hitPulley.PulleyCentreGeometrical, Quaternion.identity, hitPulley.transform)).GetComponent<CableJoint>();

        newNode.pulley = hitPulley;

        newNode.orientation = newNode.pulley.Orientation(CableStartPosition, CableEndPosition);

        newNode.anchor = this.anchor;

        newNode.name = this.name + (++kin).ToString();

        newNode.linkType = LinkType.Rolling; 
        */

        /*
        float tailIdentity = 0.0f;
        Vector2 tailOffset = Vector2.zero;
        if (newNode.tail.linkType == LinkType.Rolling)
        {
            tailIdentity = newNode.tail.node.tangentIdentityHead;
            tailOffset = newNode.tail.tangentOffsetHead;
            TangentAlgorithm(newNode.pulley, newNode.tail.node.pulley, out newNode.tangentOffsetTail, out tailOffset, out newNode.tangentIdentityTail, out tailIdentity, newNode.orientation, newNode.tail.node.orientation);
        }
        else
        {
            newNode.tangentOffsetTail = newNode.pulley.PointToShapeTangent(newNode.tail.NodePosition, newNode.orientation, newNode.CableWidth, out newNode.tangentIdentityTail);
        }

        float headIdentity = 0.0f;
        Vector2 headOffset = Vector2.zero;
        if (this.linkType == LinkType.Rolling)
        {
            headIdentity = this.tangentIdentityTail;
            headOffset = this.tangentOffsetTail;
            TangentAlgorithm(this.pulley, newNode.pulley, out headOffset, out newNode.tangentOffsetHead, out headIdentity, out newNode.tangentIdentityHead, this.orientation, newNode.orientation);
        }
        else
        {
            newNode.tangentOffsetHead = newNode.pulley.PointToShapeTangent(this.NodePosition, !newNode.orientation, newNode.CableWidth, out newNode.tangentIdentityHead);
        }

        if (!(newNode.orientation ^ (Vector2.SignedAngle(newNode.tangentOffsetTail, newNode.tangentOffsetHead) < 0)))
        {
            newNode.CutChain();
            Destroy(newNode.gameObject);
            return;
        }
        else
        {
            if (newNode.tail.linkType == LinkType.Rolling)
            {
                newNode.tail.node.tangentIdentityHead = tailIdentity;
                newNode.tail.tangentOffsetHead = tailOffset;
            }
            if (this.linkType == LinkType.Rolling)
            {
                this.tangentIdentityTail = headIdentity;
                this.tangentOffsetTail = headOffset;
            }
        }
        newNode.storedLength = newNode.pulley.ShapeSurfaceDistance(newNode.tangentIdentityTail, newNode.tangentIdentityHead, newNode.orientation, newNode.CableWidth, false);

        newNode.InitializeNodes();

        this.InitializeNodes();

        float initialRestLength = this.restLength;

        // Adjust rest lengths so that tensions are equal:
        float tension = initialRestLength / (this.currentLength + newNode.currentLength);
        this.restLength = this.currentLength * tension;
        newNode.restLength = newNode.currentLength * tension;

        newNode.Initilizebox(this.triggerWidth);

        newNode.TriggerBoxUpdate();

        this.TriggerBoxUpdate();
        */

        root.Joints.Insert(segmentIndex, jointNew);
    }

    static public bool RemoveCondition(in Joint joint)
    {
        return joint.storedLength < 0.0f;
    }

    static public void RemoveJoint(in CableRoot root, in Joint joint)
    {

    }
}
