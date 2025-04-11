using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
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
        public float positionError = 0;
        [HideInInspector] public float segmentTension = 0.0f;

        [HideInInspector] public Vector2 cableUnitVector = Vector2.zero;

        public bool orientation = false;
        public int startingLoops = 0;

        [HideInInspector] public bool slipping = false;
        [HideInInspector] public float frictionFactor = 1.0f;
        [HideInInspector] public int slipJointsCount = 0;
        [HideInInspector] public float SlipA = 0.0f;
        [HideInInspector] public float SlipB = 0.0f;
    }

    public float CableHalfWidth = 0.05f;
    
    //implement later
    public bool Looping = false;
    
    public List<Joint> Joints;

    static public void UpdateSegment(Joint head, Joint tail, float cableHalfWidth)
    {
        if (head.linkType != LinkType.Rolling)
        {
            if (tail.linkType != LinkType.Rolling)
            {
                // update segment tail and head points
                head.tangentPointTail = head.body.transform.TransformPoint(head.tangentOffsetTail);
                tail.tangentPointHead = tail.body.transform.TransformPoint(tail.tangentOffsetHead);
            }
            else
            {
                // update segment tail and head points / tangents
                head.tangentPointTail = head.body.transform.TransformPoint(head.tangentOffsetTail);
                tail.tangentOffsetHead = tail.body.PointToShapeTangent(head.tangentPointTail, !tail.orientation, cableHalfWidth, out tail.tIdentityHead);

                // rolling update for tail with new tangent
                float dist = tail.body.ShapeSurfaceDistance(tail.tIdentityHeadPrev, tail.tIdentityHead, tail.orientation, cableHalfWidth, true);
                tail.storedLength += dist;
                head.restLength -= dist;
                tail.tangentPointHead = tail.body.PulleyCentreGeometrical + tail.tangentOffsetHead;
                tail.tangentOffsetHead = tail.tangentPointHead - tail.body.CenterOfMass;
                tail.tIdentityHeadPrev = tail.tIdentityHead;
            }
        }
        else
        {
            if (tail.linkType != LinkType.Rolling)
            {
                // update segment tail and head points / tangents
                tail.tangentPointHead = tail.body.transform.TransformPoint(tail.tangentOffsetHead);
                head.tangentOffsetTail = head.body.PointToShapeTangent(tail.tangentPointHead, head.orientation, cableHalfWidth, out head.tIdentityTail);

                // rolling update for head with new tangent
                float dist = head.body.ShapeSurfaceDistance(head.tIdentityTailPrev, head.tIdentityTail, head.orientation, cableHalfWidth, true);
                head.storedLength -= dist;
                head.restLength += dist;
                head.tangentPointTail = head.body.PulleyCentreGeometrical + head.tangentOffsetTail;
                head.tangentOffsetTail = head.tangentPointTail - head.body.CenterOfMass;
                head.tIdentityTailPrev = head.tIdentityTail;
            }
            else
            {
                // update segment tail and head tangents
                CableMeshInterface.TangentAlgorithm(head.body, tail.body, out head.tangentOffsetTail, out tail.tangentOffsetHead, out head.tIdentityTail, out tail.tIdentityHead, head.orientation, tail.orientation, cableHalfWidth);

                // rolling update for tail with new tangent
                float distTail = tail.body.ShapeSurfaceDistance(tail.tIdentityHeadPrev, tail.tIdentityHead, tail.orientation, cableHalfWidth, true);
                tail.storedLength += distTail;
                head.restLength -= distTail;
                tail.tangentPointHead = tail.body.PulleyCentreGeometrical + tail.tangentOffsetHead;
                tail.tangentOffsetHead = tail.tangentPointHead - tail.body.CenterOfMass;
                tail.tIdentityHeadPrev = tail.tIdentityHead;

                // rolling update for head with new tangent
                float distHead = head.body.ShapeSurfaceDistance(head.tIdentityTailPrev, head.tIdentityTail, head.orientation, cableHalfWidth, true);
                head.storedLength -= distHead;
                head.restLength += distHead;
                head.tangentPointTail = head.body.PulleyCentreGeometrical + head.tangentOffsetTail;
                head.tangentOffsetTail = head.tangentPointTail - head.body.CenterOfMass;
                head.tIdentityTailPrev = head.tIdentityTail;
            }
        }

        InitializeSegment(head, tail);
    }
    static public void InitializeSegment(Joint joint, Joint jointTail)
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
        return AddJoint(root, segment, body, body.Orientation(jointTail.tangentPointHead, jointHead.tangentPointTail));
    }

    static public Joint AddJoint(CableRoot root, Joint segment, CableMeshInterface body, bool orientation)
    {
        int segmentIndex = root.Joints.FindIndex(x => x == segment);
        Joint jointHead = segment;
        Joint jointTail = root.Joints[segmentIndex - 1];
        Joint jointNew = new Joint();

        jointNew.id = GetNewJointID;
        jointNew.linkType = LinkType.Rolling;
        jointNew.body = body;
        jointNew.orientation = orientation;

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
            Debug.LogError("faulty inverted pulley!");
        }
        
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

        InitializeSegment(jointHead, jointTail);
    }

    static public void SetRestlengthsAsCurrent(CableRoot cable)
    {
        for (int i = 1; i < cable.Joints.Count; i++)
        {
            CableRoot.UpdateSegment(cable.Joints[i], cable.Joints[i - 1], cable.CableHalfWidth);
        }

        if (cable.Looping)
        {
            CableRoot.UpdateSegment(cable.Joints[0], cable.Joints[cable.Joints.Count - 1], cable.CableHalfWidth);
        }

        for (int i = 1; i < cable.Joints.Count; i++)
        {
            Joint joint = cable.Joints[i];
            if (joint.linkType == LinkType.Rolling)
            {
                joint.storedLength = joint.body.ShapeSurfaceDistance(joint.tIdentityTail, joint.tIdentityHead, joint.orientation, cable.CableHalfWidth, false);
                joint.storedLength += joint.startingLoops * joint.body.LoopLength(cable.CableHalfWidth);
            }
            joint.restLength = joint.currentLength;
        }

        if (cable.Looping)
        {
            Joint joint = cable.Joints[0];
            if (joint.linkType == LinkType.Rolling)
            {
                joint.storedLength = joint.body.ShapeSurfaceDistance(joint.tIdentityTail, joint.tIdentityHead, joint.orientation, cable.CableHalfWidth, false);
                joint.storedLength += joint.startingLoops * joint.body.LoopLength(cable.CableHalfWidth);
            }
            joint.restLength = joint.currentLength;
        }
    }



    static void FrictionFactorUpdate(CableRoot cable, Joint joint, float slipSign)
    {
        joint.frictionFactor = joint.body.FrictionFactor(slipSign, joint.slipping, joint.storedLength, cable.CableHalfWidth);
    }

    static public void CableSlipConditionsUpdate(in CableRoot cable, in Joint joint, in Joint jointHead)
    {
        // didn't think I needed this
        if (joint.body.infiniteFriction)
        {
            joint.slipping = false;
            return;
        }

        float slidingCondition = 0.0f;
        float tension1 = joint.segmentTension;
        float tension2 = jointHead.segmentTension;

        if (tension1 <= tension2)
        {
            FrictionFactorUpdate(cable, joint, 1.0f);
            slidingCondition = tension2 - tension1 * joint.frictionFactor;
        }
        else
        {
            FrictionFactorUpdate(cable, joint, -1.0f);
            slidingCondition = tension1 * joint.frictionFactor - tension2;
        }

        //print(tension1 + "   " + tension2 + "   " + this.frictionFactor + "   " + slidingCondition);

        if (slidingCondition > 0.0f)
        {
            if (!joint.slipping)
            {
                joint.slipping = true;
                FrictionFactorUpdate(cable, joint, (tension1 <= tension2) ? 1.0f : -1.0f);
            }
        }
        else
        {
            if (joint.slipping)
            {
                joint.slipping = false;
                // probably can remove
                FrictionFactorUpdate(cable, joint, (tension1 <= tension2) ? 1.0f : -1.0f);
            }
        }
    }



    static public Joint JointConstraintInitialization(in CableRoot cable, int jointIndex, ref Joint slippingJointsStart, ref int constraintIndex, ref int slippingCount)
    {
        Joint constraint = null;
        Joint joint = cable.Joints[jointIndex];
        //every node is evaluated if they are slipping and may be considered within a slipping group
        if (joint.slipping)
        {
            //the start of a new group of consecutive slipping nodes
            if (slippingJointsStart == null)
            {
                slippingJointsStart = joint;
                constraintIndex = jointIndex;
                slippingCount = 0;
            }
            else
            {
                slippingJointsStart.positionError += joint.positionError;
            }
            slippingCount++;
        }
        else
        {
            //A group is complete and tensions may be balanced within the group
            //Recalculate the effective mass denominator for the whole group
            if (slippingJointsStart != null)
            {
                slippingJointsStart.positionError += joint.positionError;
                slippingJointsStart.slipJointsCount = slippingCount;
                SlippingBalanceTension(in cable, constraintIndex);
                if (slippingJointsStart.positionError > 0.0f)
                {
                    InverseMassDenominatorCalculationGroup(cable, slippingJointsStart, constraintIndex);
                    if (slippingJointsStart.inverseEffectiveMassDenominator > 0.0f) constraint = slippingJointsStart;
                }

                slippingJointsStart = null;
            }
            else
            {
                if (joint.positionError > 0.0f)
                {
                    constraintIndex = jointIndex;
                    InverseMassDenominatorCalculation(joint, cable.Joints[jointIndex - 1]);
                    if (joint.inverseEffectiveMassDenominator > 0.0f) constraint = joint;
                }
            }
        }
        // comment out for warm starting
        joint.totalLambda = 0;
        return constraint;
    }

    static float FrictionCompounded(in CableRoot cable, int startIndex, int endIndex)
    {
        float ret = 1.0f;
        for (int i = startIndex; i < endIndex + 1; i++)
        {
            ret *= cable.Joints[i].frictionFactor;
        }
        return ret;
    }

    static void SlippingBalanceTension(in CableRoot cable, int groupIndex)
    {
        int count = cable.Joints[groupIndex].slipJointsCount;
        float startTension = cable.Joints[groupIndex].segmentTension;

        float sumA = 0.0f;
        float sumB = 0.0f;
        for (int i = groupIndex; i < groupIndex + count; i++)
        {
            Joint joint = cable.Joints[i + 1];
            joint.SlipA = FrictionCompounded(cable, groupIndex, i);
            joint.SlipB = joint.segmentTension - FrictionCompounded(cable, groupIndex, i) * startTension;

            sumA += joint.SlipA;
            sumB += joint.SlipB;
        }

        float startRestDelta = -sumB / (1 + sumA);
        cable.Joints[groupIndex].restLength += startRestDelta;

        for (int i = groupIndex; i < groupIndex + count; i++)
        {
            Joint joint = cable.Joints[i + 1];
            joint.restLength += joint.SlipA * startRestDelta + joint.SlipB;
        }
    }
    
    static void InverseMassDenominatorCalculation(Joint segment, Joint segmentTail)
    {
        float invMass1 = 0;
        float invMass2 = 0;

        float inertiaTerm1 = 0;
        float inertiaTerm2 = 0;

        Vector3 impulseRadius = Vector3.zero;
        Rigidbody2D RB2D = segment.body.PulleyAttachedRigidBody;
        if (RB2D != null && !RB2D.isKinematic)
        {
            invMass1 = 1.0f / RB2D.mass;


            if (RB2D.inertia != 0)
            {
                impulseRadius = Vector3.Cross(segment.tangentOffsetTail, segment.cableUnitVector);

                inertiaTerm1 = (impulseRadius.z * impulseRadius.z) / RB2D.inertia;
            }
        }

        Rigidbody2D tailRB2D = segmentTail.body.PulleyAttachedRigidBody;
        if (tailRB2D != null && !tailRB2D.isKinematic)
        {
            invMass2 = 1.0f / tailRB2D.mass;

            if (tailRB2D.inertia != 0)
            {
                impulseRadius = Vector3.Cross(segmentTail.tangentOffsetHead, segment.cableUnitVector);

                inertiaTerm2 = (impulseRadius.z * impulseRadius.z) / tailRB2D.inertia;
            }
        }

        //the mass projected along the cable direction that the impulse lambda must work against
        //larger masses result in a smaller denominator. a static object with infinite mass will give terms of zero
        //if both objects are static no impulse needs to be calculated
        segment.inverseEffectiveMassDenominator = invMass1 + invMass2 + inertiaTerm1 + inertiaTerm2;
        segment.inverseEffectiveMassDenominator = 1 / segment.inverseEffectiveMassDenominator;
    }
    
    static Vector2 M(int i, int max, Joint joint, in CableRoot cable, int groupIndex)
    {
        if (i == 0)
        {
            Joint jointHead = cable.Joints[groupIndex + i];
            return -jointHead.cableUnitVector / joint.body.PulleyAttachedRigidBody.mass;
        }
        if (i == max)
        {
            return FrictionCompounded(cable, groupIndex, groupIndex + i - 2) * joint.cableUnitVector / joint.body.PulleyAttachedRigidBody.mass;
        }
        {
            Joint jointHead = cable.Joints[groupIndex + i];
            return (FrictionCompounded(cable, groupIndex, groupIndex + i - 2) * joint.cableUnitVector - FrictionCompounded(cable, groupIndex, i - 1) * jointHead.cableUnitVector) / joint.body.PulleyAttachedRigidBody.mass;
        }
    }
    static Vector3 I(int i, int max, Joint joint, in CableRoot cable, int groupIndex)
    {
        if (i == 0)
        {
            Joint jointHead = cable.Joints[groupIndex + i];
            return -Vector3.Cross(joint.tangentOffsetHead, jointHead.cableUnitVector) / joint.body.PulleyAttachedRigidBody.inertia;
        }
        if (i == max)
        {
            return FrictionCompounded(cable, groupIndex, groupIndex + i - 2) * Vector3.Cross(joint.tangentOffsetTail, joint.cableUnitVector) / joint.body.PulleyAttachedRigidBody.inertia;
        }
        {
            Joint jointHead = cable.Joints[groupIndex + i];
            return (FrictionCompounded(cable, groupIndex, groupIndex + i - 2) * Vector3.Cross(joint.tangentOffsetTail, joint.cableUnitVector) - FrictionCompounded(cable, groupIndex, groupIndex + i - 1) * Vector3.Cross(joint.tangentOffsetHead, jointHead.cableUnitVector)) / joint.body.PulleyAttachedRigidBody.inertia;
        }
    }
    static void InverseMassDenominatorCalculationGroup(CableRoot cable, CableRoot.Joint group, int groupIndex)
    {
        int slippingCount = group.slipJointsCount;

        Vector2[] massDenominators = new Vector2[slippingCount + 2];
        Vector3[] inertiaDenominators = new Vector3[slippingCount + 2];

        //pre calculate the individual contributions of every body
        for (int i = 0; i <= slippingCount + 1; i++)
        {
            Joint joint = cable.Joints[groupIndex + i - 1];
            Rigidbody2D RB2D = joint.body.PulleyAttachedRigidBody;
            if (RB2D != null && !RB2D.isKinematic)
            {
                massDenominators[i] = M(i, slippingCount + 1, joint, cable, groupIndex);
                if (RB2D.inertia != 0)
                {
                    inertiaDenominators[i] = I(i, slippingCount + 1, joint, cable, groupIndex);
                }
                else
                {
                    inertiaDenominators[i] = Vector3.zero;
                }
            }
            else
            {
                massDenominators[i] = Vector2.zero;
                inertiaDenominators[i] = Vector3.zero;
            }
        }

        //the mass projected along the cable direction that the impulse lambda must work against
        //larger masses result in a smaller denominator. a static object with infinite mass will give terms of zero
        //if both objects are static no impulse needs to be calculated
        group.inverseEffectiveMassDenominator = 0;

        //summize the total projected constraint mass denominator
        for (int i = 1; i <= slippingCount + 1; i++)
        {
            Joint joint = cable.Joints[groupIndex + i - 1];
            Joint jointTail = cable.Joints[groupIndex + i - 2];
            group.inverseEffectiveMassDenominator += Vector2.Dot(massDenominators[i] - massDenominators[i - 1], joint.cableUnitVector)
                           + Vector3.Dot(inertiaDenominators[i], Vector3.Cross(joint.tangentOffsetTail, joint.cableUnitVector))
                           - Vector3.Dot(inertiaDenominators[i - 1], Vector3.Cross(jointTail.tangentOffsetHead, joint.cableUnitVector));
        }
        group.inverseEffectiveMassDenominator = 1 / group.inverseEffectiveMassDenominator;
    }

    static public void SegmentConstraintSolve(Joint segment, Joint segmentTail, float bias)
    {
        Rigidbody2D RB2D = segment.body.PulleyAttachedRigidBody;
        Rigidbody2D tailRB2D = segmentTail.body.PulleyAttachedRigidBody;
        //project the relative velocity of the two bodies along the cable direction
        Vector2 relVel = (RB2D != null ? RB2D.GetPointVelocity(segment.tangentPointTail) : Vector2.zero) -
                         (tailRB2D != null ? tailRB2D.GetPointVelocity(segmentTail.tangentPointHead) : Vector2.zero);

        float velConstraintValue = Vector2.Dot(relVel, segment.cableUnitVector);

        float velocitySteering = bias * segment.positionError / Time.fixedDeltaTime;

        //impulse intensity:  
        float lambda = -(velConstraintValue + velocitySteering) * segment.inverseEffectiveMassDenominator;

        //accumulate and clamp impulse
        float tempLambda = segment.totalLambda;
        segment.totalLambda = Mathf.Min(0, segment.totalLambda + lambda);
        lambda = segment.totalLambda - tempLambda;

        //apply impulse
        if (RB2D != null && !RB2D.isKinematic)
        {
            RB2D.velocity += lambda * segment.cableUnitVector / RB2D.mass;

            if (RB2D.inertia != 0)
            {
                RB2D.angularVelocity += Mathf.Rad2Deg * lambda * Vector3.Cross(segment.tangentOffsetTail, segment.cableUnitVector).z / RB2D.inertia;
            }
        }

        if (tailRB2D != null && !tailRB2D.isKinematic)
        {
            tailRB2D.velocity -= lambda * segment.cableUnitVector / tailRB2D.mass;

            if (tailRB2D.inertia != 0)
            {
                tailRB2D.angularVelocity -= Mathf.Rad2Deg * lambda * Vector3.Cross(segmentTail.tangentOffsetHead, segment.cableUnitVector).z / tailRB2D.inertia;
            }
        }
    }

    static public void SlipGroupConstraintSolve(CableRoot.Joint group, int Groupindex, CableRoot cable, float bias)
    {
        //Sum the current velocity errors
        float velocityError = 0;
        for (int i = Groupindex; i < Groupindex + group.slipJointsCount + 1; i++)
        {
            Joint joint = cable.Joints[i];
            Joint jointTail = cable.Joints[i - 1];
            Rigidbody2D RB2D = joint.body.PulleyAttachedRigidBody;
            Rigidbody2D tailRB2D = jointTail.body.PulleyAttachedRigidBody;

            if (RB2D != null && !RB2D.isKinematic)
            {
                velocityError += Vector2.Dot(RB2D.velocity, joint.cableUnitVector) +
                    (RB2D.angularVelocity * Mathf.Deg2Rad) * Vector3.Cross(joint.tangentOffsetTail, joint.cableUnitVector).z;
            }
            if (tailRB2D != null && !tailRB2D.isKinematic)
            {
                velocityError -= Vector2.Dot(tailRB2D.velocity, joint.cableUnitVector) +
                    (tailRB2D.angularVelocity * Mathf.Deg2Rad) * Vector3.Cross(jointTail.tangentOffsetHead, joint.cableUnitVector).z;
            }
        }

        float velocitySteering = bias * group.positionError / Time.fixedDeltaTime;

        //impulse intensity:  
        float lambda = -(velocityError + velocitySteering) * group.inverseEffectiveMassDenominator;

        //accumulate and clamp impulse
        float tempLambda = group.totalLambda;
        group.totalLambda = Mathf.Min(0, group.totalLambda + lambda);
        lambda = group.totalLambda - tempLambda;

        //apply impulse
        for (int i = Groupindex; i < Groupindex + group.slipJointsCount + 1; i++)
        {
            Joint joint = cable.Joints[i];
            Joint jointTail = cable.Joints[i - 1];
            Rigidbody2D RB2D = joint.body.PulleyAttachedRigidBody;
            Rigidbody2D tailRB2D = jointTail.body.PulleyAttachedRigidBody;

            float frictionFactorScalar = FrictionCompounded(cable, Groupindex, i - 1);

            if (RB2D != null && !RB2D.isKinematic)
            {
                RB2D.velocity += lambda * frictionFactorScalar * joint.cableUnitVector / RB2D.mass;

                if (RB2D.inertia != 0)
                {
                    RB2D.angularVelocity += Mathf.Rad2Deg * lambda * frictionFactorScalar * Vector3.Cross(joint.tangentOffsetTail, joint.cableUnitVector).z / RB2D.inertia;
                }
            }

            if (tailRB2D != null && !tailRB2D.isKinematic)
            {
                tailRB2D.velocity -= lambda * frictionFactorScalar * joint.cableUnitVector / tailRB2D.mass;

                if (tailRB2D.inertia != 0)
                {
                    tailRB2D.angularVelocity -= Mathf.Rad2Deg * lambda * frictionFactorScalar * Vector3.Cross(jointTail.tangentOffsetHead, joint.cableUnitVector).z / tailRB2D.inertia;
                }
            }
        }
    }

    static public bool EvaluateTransitionPinchJoint(CableRoot cable, Joint joint, in CableMeshInterface.CablePinchManifold manifold)
    {
        bool ret = true;
        
        int i = cable.Joints.FindIndex(x => x.id == joint.id);
        if (i == -1) return false;

        if (i >= cable.Joints.Count - 1) ret = false;
        Joint jointHead = cable.Joints[i + 1];
        if (jointHead.body != manifold.bodyB) ret = false;
        if (jointHead.orientation == joint.orientation) ret = false;

        if (ret)
        {
            UpdatePinchedSegment(jointHead, joint, cable.CableHalfWidth, in manifold);
        }
        else if (i > 0)
        {
            // to signal cable engine to possibly not evaluate this joint further for pinching
            Joint jointTail = cable.Joints[i - 1];
            ret = (jointTail.body == manifold.bodyB) && (jointTail.orientation != joint.orientation);
        }

        return ret;
    }

    static public bool EvaluatePinchJoint(CableRoot cable, Joint joint, in CableMeshInterface.CablePinchManifold manifold)
    {
        if (!StoredCableIntersection(cable, joint, in manifold)) return false;

        //print("Super Pinch!!!");

        //int i = cable.Joints.FindIndex(x => x.id == joint.id);
        //AddJoint(cable, joint, manifold.bodyB, !joint.orientation);
        //Joint newJoint = cable.Joints[i];
        //AddJoint(cable, newJoint, joint.body, joint.orientation);

        //UpdatePinchedSegment(joint, newJoint, cable.CableHalfWidth, in manifold);
        //UpdatePinchedSegment(newJoint, cable.Joints[i], cable.CableHalfWidth, in manifold);

        return true;
    }

    static bool StoredCableIntersection(CableRoot cable, Joint joint, in CableMeshInterface.CablePinchManifold manifold)
    {
        if (joint.storedLength >= joint.body.LoopLength(cable.CableHalfWidth)) return true;

        float epsilon = 0.000001f;
        Vector2 p = new Vector2(manifold.normal.y, -manifold.normal.x);
        float tp = Vector2.Dot(p, joint.tangentOffsetTail);
        float hp = Vector2.Dot(p, joint.tangentOffsetHead);

        if (joint.orientation)
        {
            if (tp > -epsilon)
            {
                if (hp < epsilon)
                    return true;
                else
                {
                    float tn = Vector2.Dot(manifold.normal, joint.tangentOffsetTail);
                    float hn = Vector2.Dot(manifold.normal, joint.tangentOffsetHead);
                    if (tn - hn > 0.0f) return true;
                }
            }
            else if (tp < epsilon)
            {
                if (hp > epsilon)
                    return false;
                else
                {
                    float tn = Vector2.Dot(manifold.normal, joint.tangentOffsetTail);
                    float hn = Vector2.Dot(manifold.normal, joint.tangentOffsetHead);
                    if (hn - tn > 0.0f) return true;
                }
            }
        }
        else
        {
            if (tp < epsilon)
            {
                if (hp > -epsilon)
                    return true;
                else
                {
                    float tn = Vector2.Dot(manifold.normal, joint.tangentOffsetTail);
                    float hn = Vector2.Dot(manifold.normal, joint.tangentOffsetHead);
                    if (tn - hn > 0.0f) return true;
                }
            }
            else if (tp > -epsilon)
            {
                if (hp < -epsilon)
                    return false;
                else
                {
                    float tn = Vector2.Dot(manifold.normal, joint.tangentOffsetTail);
                    float hn = Vector2.Dot(manifold.normal, joint.tangentOffsetHead);
                    if (hn - tn > 0.0f) return true;
                }
            }
        }

        return false;
    }

    static void UpdatePinchedSegment(Joint joint, Joint jointTail, float cableHalfWidth, in CableMeshInterface.CablePinchManifold manifold)
    {
        Vector2 segmentUnitVector = new Vector2(manifold.normal.y, -manifold.normal.x) * (joint.orientation ? 1.0f : -1.0f);

        Vector2 pinchPointA;
        Vector2 pinchPointB;
        float newCurrentLength = 0.0f;

        if (manifold.contactCount == 2)
        {
            newCurrentLength = (manifold.contact1.A - manifold.contact2.A).sqrMagnitude;
            if (joint.orientation)
            {
                pinchPointA = manifold.contact1.A;
                pinchPointB = manifold.contact2.B;
            }
            else
            {
                pinchPointA = manifold.contact2.A;
                pinchPointB = manifold.contact1.B;
            }
        }
        else
        {
            pinchPointA = manifold.contact1.A;
            pinchPointB = manifold.contact1.B;
        }
        pinchPointA += manifold.normal * cableHalfWidth;
        pinchPointB -= manifold.normal * cableHalfWidth;
        newCurrentLength = Mathf.Sqrt((pinchPointB - jointTail.tangentPointHead).sqrMagnitude + newCurrentLength + (joint.tangentPointTail - pinchPointA).sqrMagnitude);

        //joint.tangentPointTail = pinchPointB;
        //joint.tangentOffsetTail = pinchPointB - joint.body.CenterOfMass;

        //jointTail.tangentPointHead = pinchPointA;
        //jointTail.tangentOffsetHead = pinchPointA - jointTail.body.CenterOfMass;

        joint.cableUnitVector = segmentUnitVector;
        joint.positionError -= joint.currentLength;
        joint.positionError += newCurrentLength;
        joint.currentLength = newCurrentLength;
        joint.segmentTension = TensionEstimation(joint);
    }
}
