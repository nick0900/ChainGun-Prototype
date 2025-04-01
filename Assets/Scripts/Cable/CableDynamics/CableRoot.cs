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

        [HideInInspector] public CableRoot root = null;

        [HideInInspector] public float tIdentityTail;
        [HideInInspector] public float tIdentityHead;
        [HideInInspector] public float tIdentityTailPrev;
        [HideInInspector] public float tIdentityHeadPrev;
        public Vector2 tangentOffsetHead;
        public Vector2 tangentOffsetTail;
        public Vector2 tangentPointHead;
        public Vector2 tangentPointTail;

        [HideInInspector] public float totalLambda = 0;
        [HideInInspector] public float inverseEffectiveMassDenominator = 0;

        public float storedLength = 0.0f;
        public float restLength = 1.0f;
        [HideInInspector] public float currentLength = 0;
        [HideInInspector] public float positionError = 0;
        [HideInInspector] public float segmentTension = 0.0f;

        [HideInInspector] public Vector2 cableUnitVector = Vector2.zero;

        [HideInInspector] public bool orientation = false;

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
            joint.tangentOffsetTail = joint.body.PointToShapeTangent(jointTail.body.transform.TransformPoint(jointTail.tangentOffsetHead), joint.orientation, cableHalfWidth, out joint.tIdentityTail);
        }
        if (jointHead.linkType == LinkType.Rolling)
        {
            CableMeshInterface.TangentAlgorithm(jointHead.body, joint.body, out jointHead.tangentOffsetTail, out joint.tangentOffsetHead, out jointHead.tIdentityTail, out joint.tIdentityHead, jointHead.orientation, joint.orientation, cableHalfWidth);
        }
        else
        {
            joint.tangentOffsetHead = joint.body.PointToShapeTangent(jointHead.body.transform.TransformPoint(jointHead.tangentOffsetTail), !joint.orientation, cableHalfWidth, out joint.tIdentityHead);
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
        Vector3 distVector = jointTail.tangentPointHead - joint.tangentPointTail;
        joint.currentLength = distVector.magnitude;
        joint.cableUnitVector = distVector.normalized;

        joint.positionError = joint.currentLength - joint.restLength;

        joint.segmentTension = TensionEstimation(joint);
    }

    static float TensionEstimation(Joint currentNode)
    {
        return Mathf.Max(currentNode.currentLength - currentNode.restLength, 0.0f);
    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
