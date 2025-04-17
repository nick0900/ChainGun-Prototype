using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static CableMeshInterface;
using static CableRoot;
using static UnityEngine.EventSystems.EventTrigger;

public class CableEngine : MonoBehaviour
{
    public uint SolverIterations = 10;
    public float SegmentsBias = 0.2f;
    public float ContactsBias = 0.7f;

    public List<CableMeshInterface> Bodies;
    public List<CableRoot> Cables;

    [System.Serializable]
    private class BodyAttachmentManifold
    {
        public CableMeshInterface body;
        public float greatestMargin;
        public List<(CableRoot.Joint joint, CableRoot root)> joints;
    }
    [SerializeField] private List<BodyAttachmentManifold> AttachedBodies;
    [SerializeField] private List<BodyAttachmentManifold> FreeBodies;

    private void OnEnable()
    {
        AttachedBodies = new List<BodyAttachmentManifold>();
        FreeBodies = new List<BodyAttachmentManifold>();

        foreach (CableRoot cable in Cables)
        {
            for (int i = 0; i < cable.Joints.Count; i++)
            {
                cable.Joints[i].id = CableRoot.GetNewJointID;
                CableRoot.Joint joint = cable.Joints[i];
                if (joint.body == null)
                {
                    print("Warning " + this.name + ": Joint " + i + " in cable root " + cable.name + " missing body");
                    continue;
                }
                if (Bodies.FindIndex(x => x == joint.body) == -1)
                {
                    print("Warning " + this.name + ": Body " + joint.body.name + " not added to cable engine beforehand. Body has been added from joint " + i + " cable " + cable.name);
                    Bodies.Add(joint.body);
                }


                int index = AttachedBodies.FindIndex(x => x.body == joint.body);
                if (index == -1)
                {
                    BodyAttachmentManifold bodyAttachment = new BodyAttachmentManifold();
                    bodyAttachment.body = joint.body;
                    bodyAttachment.joints = new List<(CableRoot.Joint joint, CableRoot root)>();
                    bodyAttachment.joints.Add((joint, cable));
                    bodyAttachment.greatestMargin = cable.CableHalfWidth * 2;
                    AttachedBodies.Add(bodyAttachment);
                }
                else
                {
                    BodyAttachmentManifold bodyAttachment = AttachedBodies[i];
                    bodyAttachment.joints.Add((joint, cable));
                    if (bodyAttachment.greatestMargin < cable.CableHalfWidth * 2)
                        bodyAttachment.greatestMargin = cable.CableHalfWidth * 2;
                    AttachedBodies[i] = bodyAttachment;
                }
            }
            CableRoot.SetRestlengthsAsCurrent(cable);
        }

        foreach (CableMeshInterface body in Bodies)
        {
            if (AttachedBodies.FindIndex(x => x.body == body) == -1)
            {
                BodyAttachmentManifold bodyAttachment = new BodyAttachmentManifold();
                bodyAttachment.body = body;
                FreeBodies.Add(bodyAttachment);
            }
        }
    }

    struct NearContact
    {
        public BodyAttachmentManifold b1;
        public BodyAttachmentManifold b2;
        public float margin;
    }
    List<NearContact> NearContacts = new List<NearContact>();

    struct PotentialPinchManifold
    {
        public BodyAttachmentManifold attach1;
        public BodyAttachmentManifold attach2;
        public CablePinchManifold manifold;
    }
    private List<PotentialPinchManifold> PotentialManifolds = new List<PotentialPinchManifold>();

    private List<CablePinchManifold> ContactConstraints = new List<CablePinchManifold>();
    struct SegmentHit
    {
        public CableRoot cable;
        public CableRoot.Joint joint;
        public CableMeshInterface body;
    }

    private List<SegmentHit> SegmentHits = new List<SegmentHit>();

    private List<(CableRoot.Joint joint, int index, CableRoot cable)> SegmentConstraints = new List<(CableRoot.Joint joint, int index, CableRoot cable)>();

    public bool DebugRenderContacts = false;

    public bool SolveContactConstraints = true;
    public bool PinchIntersections = true;

    static uint Framecount = 0;
    void FixedUpdate()
    {
        Framecount++;
        UpdateSegments(in Cables);

        RemoveJoints(ref Cables, ref AttachedBodies, ref FreeBodies);

        SegmentHits.Clear();
        SegmentsIntersections(in Bodies, in Cables, ref SegmentHits);
        AddJoints(in SegmentHits, ref AttachedBodies, ref FreeBodies);

        if (PinchIntersections)
        {
            NearContacts.Clear();
            PotentialManifolds.Clear();
            ContactConstraints.Clear();
            PinchBroadPhase(in AttachedBodies, in FreeBodies, ref NearContacts);
            PinchNarrowPhase(in NearContacts, ref PotentialManifolds);
            ConfirmPinchContacts(in PotentialManifolds, ref ContactConstraints, ref AttachedBodies, ref FreeBodies);
        }

        UpdateSlippingConditions(in Cables);

        SegmentConstraints.Clear();
        InitializeSegmentConstraints(in Cables, ref SegmentConstraints);

        PinchInitializeContactConstraints(ref ContactConstraints);

        Solver(in SegmentConstraints, ref ContactConstraints, SolverIterations, SegmentsBias, ContactsBias, SolveContactConstraints);
    }

    static void UpdateSegments(in List<CableRoot> cables)
    {
        foreach (CableRoot cable in cables)
        {
            for (int i = 1; i < cable.Joints.Count; i++)
            {
                CableRoot.UpdateSegment(cable.Joints[i], cable.Joints[i - 1], cable.CableHalfWidth);
            }
            
            if (cable.Looping)
            {
                CableRoot.UpdateSegment(cable.Joints[0], cable.Joints[cable.Joints.Count - 1], cable.CableHalfWidth);
            }
        }
    }

    static void PinchBroadPhase(in List<BodyAttachmentManifold> attachedBodies, in List<BodyAttachmentManifold> freeBodies, ref List<NearContact> nearContacts)
    {
        for (int i = 0; i < attachedBodies.Count; i++)
        {
            CableMeshInterface b1 = attachedBodies[i].body;
            if (b1.CableMeshPrimitiveType == CMPrimitives.Point) continue;
            bool isStatic1 = (b1.PulleyAttachedRigidBody == null) || (b1.PulleyAttachedRigidBody.isKinematic);
            Bounds aabb1 = b1.PulleyBounds;
            float margin1 = attachedBodies[i].greatestMargin;

            for (int j = i + 1; j < attachedBodies.Count; j++)
            {
                CableMeshInterface b2 = attachedBodies[j].body;
                if (b2.CableMeshPrimitiveType == CMPrimitives.Point) continue;
                bool isStatic2 = (b2.PulleyAttachedRigidBody == null) || (b2.PulleyAttachedRigidBody.isKinematic);
                Bounds aabb2 = b2.PulleyBounds;
                float margin = Mathf.Max(margin1, attachedBodies[j].greatestMargin);

                //if (isStatic1 && isStatic2) continue;

                if (CableMeshInterface.AABBMarginCheck(aabb1, aabb2, margin))
                {
                    NearContact contact = new NearContact();
                    contact.b1 = attachedBodies[i];
                    contact.b2 = attachedBodies[j];
                    contact.margin = margin;
                    nearContacts.Add(contact);
                }
            }

            for (int j = 0; j < freeBodies.Count; j++)
            {
                CableMeshInterface b2 = freeBodies[j].body;
                bool isStatic2 = (b2.PulleyAttachedRigidBody == null) || (b2.PulleyAttachedRigidBody.isKinematic);
                Bounds aabb2 = b2.PulleyBounds;

                //if (isStatic1 && isStatic2) continue;

                if (CableMeshInterface.AABBMarginCheck(aabb1, aabb2, margin1))
                {
                    NearContact contact = new NearContact();
                    contact.b1 = attachedBodies[i];
                    contact.b2 = freeBodies[j];
                    contact.margin = margin1;
                    nearContacts.Add(contact);
                }
            }
        }
    }

    static void PinchNarrowPhase(in List<NearContact> nearContacts, ref List<PotentialPinchManifold> manifolds)
    {
        foreach (NearContact near in nearContacts)
        {
            PotentialPinchManifold manifold = new PotentialPinchManifold();
            manifold.manifold = CableMeshInterface.GJKIntersection(near.b1.body, near.b2.body, near.margin);
            if (manifold.manifold.hasContact)
            {
                manifold.attach1 = near.b1;
                manifold.attach2 = near.b2;
                manifolds.Add(manifold);
            }
        }
    }

    static void ConfirmPinchContacts(in List<PotentialPinchManifold> manifolds, ref List<CablePinchManifold> contactConstraints, ref List<BodyAttachmentManifold> attachedBodies, ref List<BodyAttachmentManifold> freeBodies)
    {
        foreach (PotentialPinchManifold manifold in manifolds)
        {
            float maxWidth = 0.0f;
            List<(CableRoot.Joint joint, CableRoot root)> jointsA = new List<(CableRoot.Joint joint, CableRoot cable)>();
            List<(CableRoot.Joint joint, CableRoot root)> jointsB = new List<(CableRoot.Joint joint, CableRoot cable)>();
            CablePinchManifold reversedManifold = ReversedManifold(manifold.manifold);
            // gather all joints from body B
            if (manifold.attach2.joints != null)
                foreach (var entry in manifold.attach2.joints)
                {
                    if (entry.root.CableHalfWidth * 2 >= manifold.manifold.distance)
                        jointsB.Add(entry);
                }
            // Check for transition pinch joints on body A.
            // if there are, removes the connected joint in jointsB as it needs not be checked more
            // if not a transition pinch joint, add it to jointsA for further evaluation
            if (manifold.attach1.joints != null)
                foreach ((CableRoot.Joint joint, CableRoot root) in manifold.attach1.joints)
                {
                    if (root.CableHalfWidth * 2 >= manifold.manifold.distance)
                    {
                        if (CableRoot.EvaluateTransitionPinchJoint(root, joint, manifold.manifold))
                        {
                            maxWidth = Mathf.Max(maxWidth, root.CableHalfWidth * 2);
                        }
                        else
                        {
                            jointsA.Add((joint, root));
                        }
                    }
                }
            // Check for transition pinch joints on body B.
            // if there are, removes the connected joint in jointsA as it needs not be checked more
            // if not a transition pinch joint, evaluate if it is on the stored cable section of a body and add new joints
            List<CableRoot.Joint> newJoints;
            foreach ((CableRoot.Joint joint, CableRoot root) in jointsB)
            {
                if (CableRoot.EvaluateTransitionPinchJoint(root, joint, in reversedManifold))
                {
                    maxWidth = Mathf.Max(maxWidth, root.CableHalfWidth * 2);
                }
                else if (CableRoot.EvaluatePinchJoint(root, joint, in reversedManifold, out newJoints))
                {
                    RegisterJoints(in newJoints, manifold.attach1, manifold.attach2, root, ref attachedBodies, ref freeBodies);
                    maxWidth = Mathf.Max(maxWidth, root.CableHalfWidth * 2);
                }
            }
            // Evaluate any remaining joints from A if they are on the stored cable section of a body and add new joints
            foreach ((CableRoot.Joint joint, CableRoot root) in jointsA)
            {
                if (CableRoot.EvaluatePinchJoint(root, joint, in manifold.manifold, out newJoints))
                {
                    RegisterJoints(in newJoints, manifold.attach1, manifold.attach2, root, ref attachedBodies, ref freeBodies);
                    maxWidth = Mathf.Max(maxWidth, root.CableHalfWidth * 2);
                }
            }

            // If there exists any pinch joints for this manifold, add a single contact constraint for the widest cable
            if (maxWidth > 0.0f)
            {
                CablePinchManifold temp = manifold.manifold;
                temp.depth = maxWidth - temp.distance;
                contactConstraints.Add(temp);
            }
        }
    }

    static void RegisterJoints(in List<CableRoot.Joint> joints, BodyAttachmentManifold bodyA, BodyAttachmentManifold bodyB, CableRoot root, ref List<BodyAttachmentManifold> attachedBodies, ref List<BodyAttachmentManifold> freeBodies)
    {
        foreach (CableRoot.Joint joint in joints)
        {
            BodyAttachmentManifold attachment;
            if (bodyA.body == joint.body)
            {
                attachment = bodyA;
            }
            else
            {
                attachment = bodyB;
            }

            if (attachment.joints != null)
            {
                attachment.joints.Add((joint, root));
                if (root.CableHalfWidth * 2 > attachment.greatestMargin)
                    attachment.greatestMargin = root.CableHalfWidth * 2;
            }
            else
            {
                freeBodies.RemoveAt(freeBodies.FindIndex(x => x.body == attachment.body));

                attachment.joints = new List<(CableRoot.Joint joint, CableRoot root)>();
                attachment.joints.Add((joint, root));
                attachment.greatestMargin = root.CableHalfWidth * 2;

                attachedBodies.Add(attachment);
            }
        }
    }

    static public CablePinchManifold ReversedManifold(in CablePinchManifold manifold)
    {
        CablePinchManifold reversed = manifold;
        reversed.normal *= -1;
        reversed.bodyA = manifold.bodyB;
        reversed.bodyB = manifold.bodyA;
        reversed.contact1.A = manifold.contact1.B;
        reversed.contact1.B = manifold.contact1.A;
        if (reversed.contactCount == 2)
        {
            reversed.contact2 = reversed.contact1;
            reversed.contact1.A = manifold.contact2.B;
            reversed.contact1.B = manifold.contact2.A;
        }
        return reversed;
    }

    static void PinchInitializeContactConstraints(ref List<CablePinchManifold> manifolds)
    {
        for (int i = 0; i < manifolds.Count; i++)
        {
            CablePinchManifold contactManifold = manifolds[i];
            CableMeshInterface.InitializeContactConstraint(ref contactManifold);
            manifolds[i] = contactManifold;
        }
    }

    static void SegmentsIntersections(in List<CableMeshInterface> bodies, in List<CableRoot> cables, ref List<SegmentHit> hits)
    {
        foreach(CableRoot cable in cables)
        {
            for (int i = 1; i < cable.Joints.Count; i++)
            {
                CableRoot.Joint joint = cable.Joints[i];
                CableRoot.Joint jointTail = cable.Joints[i - 1];
                foreach (CableMeshInterface body in bodies)
                {
                    if (body.CableMeshPrimitiveType == CMPrimitives.Point) continue;
                    if ((joint.body == body) || (jointTail.body == body)) continue;

                    Vector2 lineNormal = new Vector2(joint.cableUnitVector.y, -joint.cableUnitVector.x);

                    Vector2 bodyVector = body.PulleyCentreGeometrical - jointTail.tangentPointHead;

                    // Check if the body might overlapp the segment line
                    float d = Mathf.Abs(Vector2.Dot(bodyVector, lineNormal));
                    if (d > body.MaxExtent + cable.CableHalfWidth) continue;

                    // Check that the body is within the segment limits
                    d = Vector2.Dot(bodyVector, joint.cableUnitVector);
                    if (body.CableMeshPrimitiveType == CMPrimitives.Circle)
                    {
                        if ((d < 0.0f) || (d > joint.currentLength)) continue;
                    }
                    else
                    {
                        float limitMargin = body.MaxExtent + cable.CableHalfWidth;
                        if ((d < -limitMargin) || (d > joint.currentLength + limitMargin)) continue;
                        if (!CableMeshInterface.GJKCableSegmentIntersection(cable, joint, jointTail, body)) continue;
                    }

                    SegmentHit hit = new SegmentHit();
                    hit.cable = cable;
                    hit.joint = joint;
                    hit.body = body;
                    hits.Add(hit);
                    // only one intersection per cable segment will be processed per fixed frame
                    break;
                }
            }
        }
    }

    static void AddJoints(in List<SegmentHit> segmentHits, ref List<BodyAttachmentManifold> attachedBodies, ref List<BodyAttachmentManifold> freeBodies)
    {
        // Create new Joints
        foreach (SegmentHit hit in segmentHits)
        {
            CableRoot.Joint newJoint = CableRoot.AddJoint(hit.cable, hit.joint, hit.body);
            int i = attachedBodies.FindIndex(x => x.body == hit.body);
            if (i != -1)
            {
                BodyAttachmentManifold attachement = attachedBodies[i];

                attachement.joints.Add((newJoint, hit.cable));

                if (attachement.greatestMargin < hit.cable.CableHalfWidth * 2)
                {
                    attachement.greatestMargin = hit.cable.CableHalfWidth * 2;
                }
            }
            else
            {
                freeBodies.RemoveAt(freeBodies.FindIndex(x => x.body == hit.body));

                BodyAttachmentManifold attachement = new BodyAttachmentManifold();

                attachement.body = hit.body;
                attachement.joints = new List<(CableRoot.Joint joint, CableRoot root)>();
                attachement.joints.Add((newJoint, hit.cable));
                attachement.greatestMargin = hit.cable.CableHalfWidth * 2;

                attachedBodies.Add(attachement);
            }
        }
    }

    static void RemoveJoints(ref List<CableRoot> cables, ref List<BodyAttachmentManifold> attachedBodies, ref List<BodyAttachmentManifold> freeBodies)
    {
        foreach (CableRoot cable in cables)
        {
            for (int k = 0; k < cable.Joints.Count; k++)
            {
                CableRoot.Joint joint = cable.Joints[k];
                if (CableRoot.RemoveCondition(in joint))
                {
                    int i = attachedBodies.FindIndex(x => x.body == joint.body);
                    BodyAttachmentManifold attachment = attachedBodies[i];
                    if (attachment.joints.Count > 1)
                    {
                        attachment.joints.RemoveAt(attachment.joints.FindIndex(x => x.joint.id == joint.id));
                        if (attachment.greatestMargin <= cable.CableHalfWidth * 2)
                        {
                            attachment.greatestMargin = attachment.joints[0].root.CableHalfWidth * 2;
                            for (int j = 1; j < attachment.joints.Count; j++)
                            {
                                if (attachment.greatestMargin < attachment.joints[j].root.CableHalfWidth * 2)
                                {
                                    attachment.greatestMargin = attachment.joints[j].root.CableHalfWidth * 2;
                                }
                            }
                        }
                    }
                    else
                    {
                        attachedBodies.RemoveAt(i);
                        BodyAttachmentManifold bodyAttachment = new BodyAttachmentManifold();
                        bodyAttachment.body = joint.body;
                        freeBodies.Add(bodyAttachment);
                    }

                    CableRoot.Joint extraRemove;
                    CableRoot.RemoveJoint(cable, joint, out extraRemove);

                    if (extraRemove != null)
                    {
                        i = attachedBodies.FindIndex(x => x.body == extraRemove.body);
                        attachment = attachedBodies[i];
                        if (attachment.joints.Count > 1)
                        {
                            attachment.joints.RemoveAt(attachment.joints.FindIndex(x => x.joint.id == extraRemove.id));
                            if (attachment.greatestMargin <= cable.CableHalfWidth * 2)
                            {
                                attachment.greatestMargin = attachment.joints[0].root.CableHalfWidth * 2;
                                for (int j = 1; j < attachment.joints.Count; j++)
                                {
                                    if (attachment.greatestMargin < attachment.joints[j].root.CableHalfWidth * 2)
                                    {
                                        attachment.greatestMargin = attachment.joints[j].root.CableHalfWidth * 2;
                                    }
                                }
                            }
                        }
                        else
                        {
                            attachedBodies.RemoveAt(i);
                            BodyAttachmentManifold bodyAttachment = new BodyAttachmentManifold();
                            bodyAttachment.body = joint.body;
                            freeBodies.Add(bodyAttachment);
                        }
                    }
                }
            }
        }
    }

    static void UpdateSlippingConditions(in List<CableRoot> cables)
    {
        foreach (CableRoot cable in cables)
        {
            for (int i = 1; i < cable.Joints.Count - 1; i++)
            {
                CableRoot.CableSlipConditionsUpdate(cable, cable.Joints[i], cable.Joints[i + 1]);
            }
        }
    }

    static void InitializeSegmentConstraints(in List<CableRoot> cables, ref List<(CableRoot.Joint joint, int index, CableRoot cable)> constraints)
    {
        foreach(CableRoot cable in cables)
        {
            CableRoot.Joint groupStart = null;
            int constraintIndex = -1;
            int groupCount = 0;
            for (int i = 1; i < cable.Joints.Count; i++)
            {
                CableRoot.Joint constraintJoint = JointConstraintInitialization(in cable, i, ref groupStart, ref constraintIndex, ref groupCount);
                if (constraintJoint != null)
                    constraints.Add((constraintJoint, constraintIndex, cable));
            }
        }
    }

    static void Solver(in List<(CableRoot.Joint joint, int index, CableRoot cable)> segmentConstraints, ref List<CablePinchManifold> pinchConstraints, uint iterations, float segmentBias, float contactBias, bool solveContactConstraints)
    {
        for (int i = 0; i < iterations; i++)
        {
            foreach ((CableRoot.Joint joint, int index, CableRoot cable) in segmentConstraints)
            {
                if (joint.slipping)
                {
                    CableRoot.SlipGroupConstraintSolve(joint, index, cable, segmentBias);
                }
                else
                {
                    CableRoot.SegmentConstraintSolve(joint, cable.Joints[index - 1], segmentBias);
                }
            }

            if (solveContactConstraints)
                for (int j = 0; j < pinchConstraints.Count; j++)
                {
                    CablePinchManifold contactManifold = pinchConstraints[j];
                    CableMeshInterface.ContactConstraintSolve(ref contactManifold, contactBias);
                    pinchConstraints[j] = contactManifold;
                }
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && DebugRenderContacts)
            foreach (CablePinchManifold manifold in ContactConstraints)
            {
                if (manifold.contactCount > 0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(manifold.contact1.A, 0.05f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(manifold.contact1.B, 0.05f);
                }

                if (manifold.contactCount == 2)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(manifold.contact2.A, 0.05f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(manifold.contact2.B, 0.05f);
                }

            }
    }
}
