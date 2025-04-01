using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CableMeshInterface;
using static CableRoot;

public class CableEngine : MonoBehaviour
{
    public List<CableMeshInterface> Bodies;
    public List<CableRoot> Cables;

    private struct AttachedJoint
    {
        public CableRoot.Joint joint;
        public CableRoot root;
    }
    private struct BodyAttachmentManifold
    {
        public CableMeshInterface body;
        public float greatestMargin;
        public List<AttachedJoint> joints;
    }
    private List<BodyAttachmentManifold> AttachedBodies;
    private List<BodyAttachmentManifold> FreeBodies;

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
                    bodyAttachment.joints.Add(attachedJoint);
                    bodyAttachment.greatestMargin = cable.CableHalfWidth;
                    AttachedBodies.Add(bodyAttachment);
                }
                else
                {
                    BodyAttachmentManifold bodyAttachment = AttachedBodies[i];
                    bodyAttachment.joints.Add(attachedJoint);
                    if (bodyAttachment.greatestMargin < cable.CableHalfWidth)
                        bodyAttachment.greatestMargin = cable.CableHalfWidth;
                    AttachedBodies[i] = bodyAttachment;
                }
            }
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

    public bool DebugRenderContacts = false;
    void Start()
    {
        NearContacts = new List<NearContact>();
        Manifolds = new List<PotentialPinchManifold>();
    }

    void FixedUpdate()
    {
        UpdateCables(in Cables);

        NearContacts.Clear();
        Manifolds.Clear();

        PinchBroadPhase(in AttachedBodies, in FreeBodies, ref NearContacts);
        PinchNarrowPhase(in NearContacts, ref Manifolds);
        ConfirmPinchContacts(ref Manifolds);


    }

    static void PinchBroadPhase(in List<BodyAttachmentManifold> attachedBodies, in List<BodyAttachmentManifold> freeBodies, ref List<NearContact> nearContacts)
    {
        for (int i = 0; i < attachedBodies.Count; i++)
        {
            CableMeshInterface b1 = attachedBodies[i].body;
            bool isStatic1 = (b1.PulleyAttachedRigidBody == null) || (b1.PulleyAttachedRigidBody.isKinematic);
            Bounds aabb1 = b1.PulleyBounds;
            float margin1 = attachedBodies[i].greatestMargin;

            for (int j = i + 1; j < attachedBodies.Count; j++)
            {
                CableMeshInterface b2 = attachedBodies[j].body;
                bool isStatic2 = (b2.PulleyAttachedRigidBody == null) || (b2.PulleyAttachedRigidBody.isKinematic);
                Bounds aabb2 = b2.PulleyBounds;
                float margin = Mathf.Max(margin1, attachedBodies[j].greatestMargin);

                if (isStatic1 && isStatic2) continue;

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

    static void UpdateCables(in List<CableRoot> cables)
    {
        foreach  (CableRoot cable in cables)
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
