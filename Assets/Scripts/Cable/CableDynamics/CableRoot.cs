using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using static CableRoot;

public class CableRoot : MonoBehaviour
{
    [System.Serializable]
    public enum LinkType
    {
        Rolling,
        Point,
        Hybrid
    }

    static private uint IdGenerator = 0;
    static public uint GetNewJointID 
    {  
        get 
        {
            IdGenerator++;
            return IdGenerator; 
        } 
    }

    [System.Serializable]
    public class Joint
    {
        public uint id = 0;

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
        public float currentLength = 0;
        [HideInInspector] public float positionError = 0;
        [HideInInspector] public float segmentTension = 0.0f;

        [HideInInspector] public Vector2 cableUnitVector = Vector2.zero;

        public bool orientation = false;
        public int startingLoops = 0;

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


    static public Joint AddJoint(CableRoot root, Joint segment, CableMeshInterface body)
    {
        int segmentIndex = root.Joints.FindIndex(x => x == segment);
        Joint jointHead = segment;
        Joint jointTail = root.Joints[segmentIndex - 1];
        Joint jointNew = new Joint();

        jointNew.id = GetNewJointID;
        jointNew.linkType = LinkType.Rolling;
        jointNew.body = body;
        jointNew.orientation = body.Orientation(jointTail.tangentPointHead, jointHead.tangentPointTail);

        // Recalculate all relevant tangents
        float initialRestLength = jointHead.restLength;

        if (jointTail.linkType == LinkType.Rolling)
        {
            CableMeshInterface.TangentAlgorithm(jointNew.body, jointTail.body, out jointNew.tangentOffsetTail, out jointTail.tangentOffsetHead, out jointNew.tIdentityTail, out jointTail.tIdentityHead, jointNew.orientation, jointTail.orientation, root.CableHalfWidth);
            float dist = jointTail.body.ShapeSurfaceDistance(jointTail.tIdentityHeadPrev, jointTail.tIdentityHead, jointTail.orientation, root.CableHalfWidth, true);

            jointTail.storedLength += dist;
            initialRestLength -= dist;

            Vector2 geometricalCenter = jointTail.body.PulleyCentreGeometrical;
            jointTail.tangentPointHead = geometricalCenter + jointTail.tangentOffsetHead;
            Vector2 COMOffset = geometricalCenter - jointTail.body.CenterOfMass;
            jointTail.tangentOffsetHead += COMOffset;

            jointTail.tIdentityHeadPrev = jointTail.tIdentityHead;
        }
        else
        {
            jointNew.tangentOffsetTail = jointNew.body.PointToShapeTangent(jointTail.tangentPointHead, jointNew.orientation, root.CableHalfWidth, out jointNew.tIdentityTail);
        }

        if (jointHead.linkType == LinkType.Rolling)
        {
            CableMeshInterface.TangentAlgorithm(jointHead.body, jointNew.body, out jointHead.tangentOffsetTail, out jointNew.tangentOffsetHead, out jointHead.tIdentityTail, out jointNew.tIdentityHead, jointHead.orientation, jointNew.orientation, root.CableHalfWidth);
            float dist = jointHead.body.ShapeSurfaceDistance(jointHead.tIdentityTailPrev, jointHead.tIdentityTail, jointHead.orientation, root.CableHalfWidth, true);

            jointHead.storedLength -= dist;
            initialRestLength += dist;

            Vector2 geometricalCenter = jointHead.body.PulleyCentreGeometrical;
            jointHead.tangentPointTail = geometricalCenter + jointHead.tangentOffsetTail;
            Vector2 COMOffset = geometricalCenter - jointHead.body.CenterOfMass;
            jointHead.tangentOffsetTail += COMOffset;

            jointHead.tIdentityTailPrev = jointHead.tIdentityTail;
        }
        else
        {
            jointNew.tangentOffsetHead = jointNew.body.PointToShapeTangent(jointHead.tangentPointTail, !jointNew.orientation, root.CableHalfWidth, out jointNew.tIdentityHead);
        }
        if (!(jointNew.orientation ^ (Vector2.SignedAngle(jointNew.tangentOffsetTail, jointNew.tangentOffsetHead) < 0)))
        {
            Debug.LogError("fuuuuuuuuuck!!");
        }
            /*
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
            */
            jointNew.storedLength = jointNew.body.ShapeSurfaceDistance(jointNew.tIdentityTail, jointNew.tIdentityHead, jointNew.orientation, root.CableHalfWidth, false);
        initialRestLength -= jointNew.storedLength;

        {
            Vector2 geometricalCenter = jointNew.body.PulleyCentreGeometrical;
            jointNew.tangentPointTail = geometricalCenter + jointNew.tangentOffsetTail;
            jointNew.tangentPointHead = geometricalCenter + jointNew.tangentOffsetHead;
            Vector2 COMOffset = geometricalCenter - jointNew.body.CenterOfMass;
            jointNew.tangentOffsetTail += COMOffset;
            jointNew.tangentOffsetHead += COMOffset;

            jointNew.tIdentityTailPrev = jointNew.tIdentityTail;
            jointNew.tIdentityHeadPrev = jointNew.tIdentityHead;
        }

        // re-initialize segments
        Vector3 distVector = jointNew.tangentPointTail - jointTail.tangentPointHead;
        jointNew.currentLength = distVector.magnitude;
        jointNew.cableUnitVector = distVector.normalized;
        distVector = jointHead.tangentPointTail - jointNew.tangentPointHead;
        jointHead.currentLength = distVector.magnitude;
        jointHead.cableUnitVector = distVector.normalized;

        // Adjust rest lengths so that tensions are equal:
        float tension = initialRestLength / (jointNew.currentLength + jointHead.currentLength);
        jointNew.restLength = jointNew.currentLength * tension;
        jointHead.restLength = jointHead.currentLength * tension;

        // complete segment initialization
        jointNew.positionError = jointNew.currentLength - jointNew.restLength;
        jointNew.segmentTension = TensionEstimation(jointNew);
        jointHead.positionError = jointHead.currentLength - jointHead.restLength;
        jointHead.segmentTension = TensionEstimation(jointHead);


        root.Joints.Insert(segmentIndex, jointNew);
        return jointNew;
    }

    static public bool RemoveCondition(in Joint joint)
    {
        if (joint.body.CableMeshPrimitiveType == CableMeshInterface.CMPrimitives.Circle)
        {
            return joint.storedLength < 0.0f;
        }

        if (joint.storedLength > 0.00001f) return false;

        if (joint.orientation)
            return Vector2.SignedAngle(joint.tangentOffsetTail, joint.tangentOffsetHead) < 0.0f;
        return Vector2.SignedAngle(joint.tangentOffsetTail, joint.tangentOffsetHead) > 0.0f;
    }

    static public void RemoveJoint(CableRoot root, Joint joint)
    {
        int segmentIndex = root.Joints.FindIndex(x => x == joint);
        Joint jointHead = root.Joints[segmentIndex + 1];
        Joint jointTail = root.Joints[segmentIndex - 1];
        Joint jointOld = joint;
        root.Joints.RemoveAt(segmentIndex);

        jointHead.restLength += jointOld.restLength + jointOld.storedLength;

        if (jointTail.linkType == LinkType.Rolling)
        {
            if (jointHead.linkType == LinkType.Rolling)
            {
                CableMeshInterface.TangentAlgorithm(jointHead.body, jointTail.body, out jointHead.tangentOffsetTail, out jointTail.tangentOffsetHead, out jointHead.tIdentityTail, out jointTail.tIdentityHead, jointHead.orientation, jointTail.orientation, root.CableHalfWidth);
                float distHead = jointHead.body.ShapeSurfaceDistance(jointHead.tIdentityTailPrev, jointHead.tIdentityTail, jointHead.orientation, root.CableHalfWidth, true);
                float distTail = jointTail.body.ShapeSurfaceDistance(jointTail.tIdentityHeadPrev, jointTail.tIdentityHead, jointTail.orientation, root.CableHalfWidth, true);

                // Update stored lengths:
                jointHead.storedLength -= distHead;
                jointTail.storedLength += distTail;

                // Update rest lengths:
                jointHead.restLength += distHead;
                jointTail.restLength -= distTail;

                Vector2 geometricalCenter = jointHead.body.PulleyCentreGeometrical;
                jointHead.tangentPointTail = geometricalCenter + jointHead.tangentOffsetTail;
                Vector2 COMOffset = geometricalCenter - jointHead.body.CenterOfMass;
                jointHead.tangentOffsetTail += COMOffset;

                geometricalCenter = jointTail.body.PulleyCentreGeometrical;
                jointTail.tangentPointHead = geometricalCenter + jointTail.tangentOffsetHead;
                COMOffset = geometricalCenter - jointTail.body.CenterOfMass;
                jointTail.tangentOffsetHead += COMOffset;

                jointHead.tIdentityTailPrev = jointHead.tIdentityTail;
                jointTail.tIdentityHeadPrev = jointTail.tIdentityHead;
            }
            else
            {
                jointTail.tangentOffsetHead = jointTail.body.PointToShapeTangent(jointHead.tangentPointTail, !jointTail.orientation, root.CableHalfWidth, out jointTail.tIdentityHead);
                float distTail = jointTail.body.ShapeSurfaceDistance(jointTail.tIdentityHeadPrev, jointTail.tIdentityHead, jointTail.orientation, root.CableHalfWidth, true);

                // Update stored lengths:
                jointTail.storedLength += distTail;

                // Update rest lengths:
                jointTail.restLength -= distTail;

                Vector2 geometricalCenter = jointTail.body.PulleyCentreGeometrical;
                jointTail.tangentPointHead = geometricalCenter + jointTail.tangentOffsetHead;
                Vector2 COMOffset = geometricalCenter - jointTail.body.CenterOfMass;
                jointTail.tangentOffsetHead += COMOffset;

                jointTail.tIdentityHeadPrev = jointTail.tIdentityHead;
            }
        }
        else if (jointHead.linkType == LinkType.Rolling)
        {
            jointHead.tangentOffsetTail = jointHead.body.PointToShapeTangent(jointTail.tangentPointHead, jointHead.orientation, root.CableHalfWidth, out jointHead.tIdentityTail);
            float distHead = jointHead.body.ShapeSurfaceDistance(jointHead.tIdentityTailPrev, jointHead.tIdentityTail, jointHead.orientation, root.CableHalfWidth, true);

            // Update stored lengths:
            jointHead.storedLength -= distHead;

            // Update rest lengths:
            jointHead.restLength += distHead;

            Vector2 geometricalCenter = jointHead.body.PulleyCentreGeometrical;
            jointHead.tangentPointTail = geometricalCenter + jointHead.tangentOffsetTail;
            Vector2 COMOffset = geometricalCenter - jointHead.body.CenterOfMass;
            jointHead.tangentOffsetTail += COMOffset;

            jointHead.tIdentityTailPrev = jointHead.tIdentityTail;
        }

        InitializeSegment(ref jointHead, jointTail);
    }

    static public void SetRestlengthsAsCurrent(CableRoot cable)
    {
        CableRoot.Joint joint;
        for (int i = 1; i < cable.Joints.Count - 1; i++)
        {
            joint = cable.Joints[i];
            CableRoot.Joint jointTail = cable.Joints[i - 1];
            CableRoot.Joint jointHead = cable.Joints[i + 1];

            CableRoot.UpdatePulley(ref joint, ref jointTail, ref jointHead, cable.CableHalfWidth);
            CableRoot.InitializeSegment(ref joint, jointTail);

            cable.Joints[i] = joint;
            cable.Joints[i - 1] = jointTail;
            cable.Joints[i + 1] = jointHead;
        }
        joint = cable.Joints[cable.Joints.Count - 1];
        CableRoot.InitializeSegment(ref joint, cable.Joints[cable.Joints.Count - 2]);
        cable.Joints[cable.Joints.Count - 1] = joint;

        for (int i = 1; i < cable.Joints.Count; i++)
        {
            joint = cable.Joints[i];
            if (joint.linkType == LinkType.Rolling)
            {
                joint.storedLength = joint.body.ShapeSurfaceDistance(joint.tIdentityTail, joint.tIdentityHead, joint.orientation, cable.CableHalfWidth, false);
                joint.storedLength += joint.startingLoops * joint.body.LoopLength(cable.CableHalfWidth);
            }
            joint.restLength = joint.currentLength;
        }
    }
}
