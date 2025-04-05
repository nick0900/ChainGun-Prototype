using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static CableMeshInterface;
using static CableRoot;

public class CableEngine : MonoBehaviour
{
    public List<CableMeshInterface> Bodies;
    public List<CableRoot> Cables;

    [System.Serializable]
    private struct AttachedJoint
    {
        public CableRoot.Joint joint;
        public CableRoot root;
    }
    [System.Serializable]
    private struct BodyAttachmentManifold
    {
        public CableMeshInterface body;
        public float greatestMargin;
        public List<AttachedJoint> joints;
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

                AttachedJoint attachedJoint = new AttachedJoint();
                attachedJoint.joint = joint;
                attachedJoint.root = cable;

                int index = AttachedBodies.FindIndex(x => x.body == joint.body);
                if (index == -1)
                {
                    BodyAttachmentManifold bodyAttachment = new BodyAttachmentManifold();
                    bodyAttachment.body = joint.body;
                    bodyAttachment.joints = new List<AttachedJoint>();
                    bodyAttachment.joints.Add(attachedJoint);
                    bodyAttachment.greatestMargin = cable.CableHalfWidth * 2;
                    AttachedBodies.Add(bodyAttachment);
                }
                else
                {
                    BodyAttachmentManifold bodyAttachment = AttachedBodies[i];
                    bodyAttachment.joints.Add(attachedJoint);
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
    List<NearContact> NearContacts;

    struct PotentialPinchManifold
    {
        public BodyAttachmentManifold attach1;
        public BodyAttachmentManifold attach2;
        public CablePinchManifold manifold;
    }
    private List<PotentialPinchManifold> Manifolds;

    struct SegmentHit
    {
        public CableRoot cable;
        public CableRoot.Joint joint;
        public CableMeshInterface body;
    }

    private List<SegmentHit> SegmentHits;

    public bool DebugRenderContacts = false;
    void Start()
    {
        NearContacts = new List<NearContact>();
        Manifolds = new List<PotentialPinchManifold>();
        SegmentHits = new List<SegmentHit>();
    }

    void FixedUpdate()
    {
        UpdateCables(in Cables);

        NearContacts.Clear();
        Manifolds.Clear();

        PinchBroadPhase(in AttachedBodies, in FreeBodies, ref NearContacts);
        PinchNarrowPhase(in NearContacts, ref Manifolds);
        ConfirmPinchContacts(ref Manifolds);

        SegmentHits.Clear();
        SegmentsIntersections(in Bodies, in Cables, ref SegmentHits);
        SpliceJoints(ref Cables, in SegmentHits, ref AttachedBodies, ref FreeBodies);

    }

    static void UpdateCables(in List<CableRoot> cables)
    {
        foreach (CableRoot cable in cables)
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
                float margin = Mathf.Max(margin1, attachedBodies[j].greatestMargin);

                if (isStatic1 && isStatic2) continue;

                if (CableMeshInterface.AABBMarginCheck(aabb1, aabb2, Mathf.Max(margin1, margin)))
                {
                    NearContact contact = new NearContact();
                    contact.b1 = attachedBodies[i];
                    contact.b2 = freeBodies[j];
                    contact.margin = margin;
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

    static void ConfirmPinchContacts(ref List<PotentialPinchManifold> manifolds)
    {

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

    static void SpliceJoints(ref List<CableRoot> cables, in List<SegmentHit> segmentHits, ref List<BodyAttachmentManifold> attachedBodies, ref List<BodyAttachmentManifold> freeBodies)
    {
        // Create new Joints
        foreach (SegmentHit hit in segmentHits)
        {
            int i = attachedBodies.FindIndex(x => x.body == hit.body);
            if (i != -1)
            {
                BodyAttachmentManifold attachement = attachedBodies[i];

                AttachedJoint attachedJoint = new AttachedJoint();
                attachedJoint.joint = hit.joint;
                attachedJoint.root = hit.cable;
                attachement.joints.Add(attachedJoint);

                if (attachement.greatestMargin < hit.cable.CableHalfWidth * 2)
                {
                    attachement.greatestMargin = hit.cable.CableHalfWidth * 2;
                }

                attachedBodies[i] = attachement;
            }
            else
            {
                freeBodies.RemoveAt(freeBodies.FindIndex(x => x.body == hit.body));

                BodyAttachmentManifold attachement = new BodyAttachmentManifold();

                attachement.body = hit.body;
                attachement.joints = new List<AttachedJoint>();
                AttachedJoint attachedJoint = new AttachedJoint();
                attachedJoint.joint = hit.joint;
                attachedJoint.root = hit.cable;
                attachement.joints.Add(attachedJoint);
                attachement.greatestMargin = hit.cable.CableHalfWidth * 2;

                attachedBodies.Add(attachement);
            }
            CableRoot.AddJoint(hit.cable, hit.joint, hit.body);
        }

        // Remove Joints
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
                        attachment.joints.RemoveAt(attachment.joints.FindIndex(x => x.joint == joint));
                        if (attachment.greatestMargin <= cable.CableHalfWidth * 2)
                        {
                            attachment.greatestMargin = attachment.joints[0].root.CableHalfWidth * 2;
                            for (int j = 1; j < attachment.joints.Count; j++)
                            {
                                if (attachment.greatestMargin < attachment.joints[j].root.CableHalfWidth*2)
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

                    CableRoot.RemoveJoint(cable, joint);
                    i--;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
            foreach (PotentialPinchManifold manifold in Manifolds)
            {
                if (manifold.manifold.contactCount > 0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(manifold.manifold.contact1.A, 0.05f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(manifold.manifold.contact1.B, 0.05f);
                }

                if (manifold.manifold.contactCount == 2)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(manifold.manifold.contact2.A, 0.05f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(manifold.manifold.contact2.B, 0.05f);
                }

            }
    }
}
