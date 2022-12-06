using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class CableJoint : CableBase
{
    [SerializeField] private GameObject nodePrefab;

    int kin = 0;

    private Vector2 jacobian;
    private float k;

    private Vector2 impulse;
    private float lambda;

    private float totalLambda = 0;

    [HideInInspector] public float length = 0;
    [HideInInspector] public float restLength = 1;

    [HideInInspector] public bool orientation;

    private bool slipping = false;

    [SerializeField] private GameObject chainTriggerPrefab;

    private Transform triggerBox;

    private float triggerWidth = 1.0f;

    ContactFilter2D filter;

    LayerMask mask = 1 << 0;

    private double epsilon = 0.000001;

    [HideInInspector] public CableMeshInterface pulley;

    [HideInInspector] public Vector2 tOffTail = Vector2.zero;
    [HideInInspector] public Vector2 tOffHead = Vector2.zero;
    [HideInInspector] public float tIdentityTail = -1;
    [HideInInspector] public float tIdentityHead = -1;

    [HideInInspector] public float tangentIdentityTail = -1;
    [HideInInspector] public float tangentIdentityHead = -1;

    public override Vector2 NodePosition { get { return pulley != null ? pulley.PulleyCentreGeometrical : this.transform.position; } }

    //temporary solution
    public Rigidbody2D rb2dEnd = null;
    public override Rigidbody2D RB2D { get { return rb2dEnd == null ? pulley.PulleyAttachedRigidBody : rb2dEnd; } }

    void Awake()
    {
        node = this.GetComponent<CableJoint>();

        filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
    }

    public void Initilizebox(float width)
    {
        if (triggerBox != null)
        {
            Destroy(triggerBox.gameObject);
        }

        triggerBox = Instantiate(chainTriggerPrefab, this.NodePosition, this.transform.rotation).transform;
        triggerBox.GetComponent<CableHitbox>().CableNode = this;

        triggerWidth = width;
        if (triggerWidth < 0.001f)
        {
            triggerWidth = 0.001f;
        }
        if (triggerWidth > 0.999f)
        {
            triggerWidth = 0.999f;
        }
        TriggerBoxUpdate();
    }
    

    public void CableSegmentUpdate()
    {
        UpdatePulley();
        FrictionUpdate();
        InitializeNodes();
        TriggerBoxUpdate();
        NodeAdder();
        NodeRemover();
    }

    void TangentAlgorithm(CableMeshInterface pulley1, CableMeshInterface pulley2, out Vector2 tangent1, out Vector2 tangent2, out float tangentIdentity1, out float tangentIdentity2, bool orientation1, bool orientation2)
    {
        if ((pulley1.CableMeshPrimitiveType == CableMeshInterface.CMPrimitives.Circle) && (pulley2.CableMeshPrimitiveType == CableMeshInterface.CMPrimitives.Circle))
        {
            (pulley1 as CirclePulley).CircleToCircleTangent(orientation1, out tangent1, out tangentIdentity1, pulley2 as CirclePulley, orientation2, out tangent2, out tangentIdentity2, CableWidth);
            return;
        }

        bool alternator = false;

        tangent1 = Vector2.zero;
        tangent2 = Vector2.zero;
        tangentIdentity1 = 0;
        tangentIdentity2 = 0;

        float newIdentity1 = 0;
        float newIdentity2 = 0;
        Vector2 newTan1 = pulley1.RandomSurfaceOffset( ref newIdentity1, CableWidth);
        Vector2 newTan2 = Vector2.zero;

        do
        {
            if (alternator)
            {
                tangent2 = newTan2;
                tangentIdentity2 = newIdentity2;

                newTan1 = pulley1.PointToShapeTangent(pulley2.PulleyCentreGeometrical + newTan2, orientation1, CableWidth, out newIdentity1);

                alternator = false;
            }
            else
            {
                tangent1 = newTan1;
                tangentIdentity1 = newIdentity1;

                newTan2 = pulley2.PointToShapeTangent(pulley1.PulleyCentreGeometrical + newTan1, !orientation2, CableWidth, out newIdentity2);

                alternator = true;
            }

        } while ( alternator ? (tangent2 != newTan2) : (tangent1 != newTan1));
    }

    void UpdatePulley()
    {
        if (this.linkType != LinkType.Rolling) return;

        if (tail.linkType != LinkType.Rolling)
        {
            tOffTail = pulley.PointToShapeTangent(tail.NodePosition, this.orientation, CableWidth, out this.tIdentityTail);
        }
        if (head.linkType == LinkType.Rolling)
        {
            TangentAlgorithm(head.node.pulley, this.pulley, out head.node.tOffTail, out this.tOffHead, out head.node.tIdentityTail, out this.tIdentityHead, head.node.orientation, this.orientation);
        }
        else
        {
            tOffHead = pulley.PointToShapeTangent(head.NodePosition, !this.orientation, CableWidth, out this.tIdentityHead);
        }

        float distTail = this.pulley.ShapeSurfaceDistance(this.tangentIdentityTail, this.tIdentityTail, this.orientation, this.CableWidth, true);
        float distHead = this.pulley.ShapeSurfaceDistance(this.tangentIdentityHead, this.tIdentityHead, this.orientation, this.CableWidth, true);

        // Update stored lengths:
        this.storedLength -= distTail;
        this.storedLength += distHead;

        // Update rest lengths:
        this.restLength += distTail;
        head.node.restLength -= distHead;

        this.tangentOffsetTail = this.tOffTail;
        this.tangentOffsetHead = this.tOffHead;

        this.tangentIdentityTail = this.tIdentityTail;
        this.tangentIdentityHead = this.tIdentityHead;
    }

    void FrictionUpdate()
    {
        /*
        if (this.linkType != LinkType.Rolling || sm == null || sm.infiniteFriction) return;

        Quaternion currentRotation;

        if (hingeBody != null)
        {
            currentRotation = transform.rotation * Quaternion.Inverse(hingeBody.transform.rotation);
        }
        else
        {
            currentRotation = transform.rotation;
        }

        if (Vector2.Angle(previousHingeRotation * Vector2.right, currentRotation * Vector2.right) > 0.01f)
        {
            slipping = true;
        }
        else
        {
            slipping = false;
        }

        JointMotor2D hm2d = this.hj2d.motor;

        float maxTorque = (slipping ? sm.kineticFrictConst : sm.staticFrictConst) * Mathf.Exp(this.storedLength / this.pulleyRadius / Mathf.PI - 1) * this.pulleyRadius;

        hm2d.maxMotorTorque = maxTorque;

        this.hj2d.motor = hm2d;

        previousHingeRotation = currentRotation;
        */
    }

    void CableLengthUpdate()
    {
        Vector3 distVector = CableStartPosition - CableEndPosition;
        length = distVector.magnitude;
        jacobian = distVector / (length + 0.00001f);
    }

    void InitializeNodes()
    {
        totalLambda = 0;

        if (tail == null) return;

        CableLengthUpdate();

        invInertiaTensor = Matrix4x4.zero;
        tail.invInertiaTensor = Matrix4x4.zero;

        if (RB2D != null)
        {
            Vector3 invInertia1 = Quaternion.identity * new Vector3(RB2D.inertia > 0 ? 1.0f / RB2D.inertia : 0,
                                                                    RB2D.inertia > 0 ? 1.0f / RB2D.inertia : 0,
                                                                    RB2D.inertia > 0 ? 1.0f / RB2D.inertia : 0);

            Matrix4x4 m = Matrix4x4.Rotate(Quaternion.Euler(0, 0, RB2D.rotation));
            invInertiaTensor[0, 0] = invInertia1.x;
            invInertiaTensor[1, 1] = invInertia1.y;
            invInertiaTensor[2, 2] = invInertia1.z;
            invInertiaTensor[3, 3] = 1;
            invInertiaTensor = m * invInertiaTensor * m.transpose;
        }

        if (tail.RB2D != null)
        {
            Vector3 invInertia2 = Quaternion.identity * new Vector3(tail.RB2D.inertia > 0 ? 1.0f / tail.RB2D.inertia : 0,
                                                                    tail.RB2D.inertia > 0 ? 1.0f / tail.RB2D.inertia : 0,
                                                                    tail.RB2D.inertia > 0 ? 1.0f / tail.RB2D.inertia : 0);

            Matrix4x4 m2 = Matrix4x4.Rotate(Quaternion.Euler(0, 0, tail.RB2D.rotation));
            tail.invInertiaTensor[0, 0] = invInertia2.x;
            tail.invInertiaTensor[1, 1] = invInertia2.y;
            tail.invInertiaTensor[2, 2] = invInertia2.z;
            tail.invInertiaTensor[3, 3] = 1;
            tail.invInertiaTensor = m2 * tail.invInertiaTensor * m2.transpose;
        }

        float w1 = 0;
        float w2 = 0;

        invMass = 0;
        tail.invMass = 0;

        if (RB2D != null && !RB2D.isKinematic)
        {
            invMass = 1.0f / (RB2D.mass);

            impulseRadiusTail = CableEndPosition - RB2D.worldCenterOfMass;
            w1 = Vector3.Dot(Vector3.Cross(invInertiaTensor.MultiplyVector(Vector3.Cross(impulseRadiusTail, jacobian)), impulseRadiusTail), jacobian);
        }

        if (tail.RB2D != null && !tail.RB2D.isKinematic)
        {
            tail.invMass = 1.0f / (tail.RB2D.mass);

            tail.impulseRadiusHead = CableStartPosition - tail.RB2D.worldCenterOfMass;
            w2 = Vector3.Dot(Vector3.Cross(tail.invInertiaTensor.MultiplyVector(Vector3.Cross(tail.impulseRadiusHead, jacobian)), tail.impulseRadiusHead), jacobian);
        }

        k = invMass + tail.invMass + w1 + w2;
    }

    public void CableSegmentSolve()
    {
        if (tail == null) return;

        float c = length - restLength;
        impulse = Vector2.zero;
        lambda = 0;

        if (c > 0 && k > 0)
        {
            // calculate the relative velocity of both attachment points:
            Vector2 relVel = (tail.RB2D != null ? tail.RB2D.GetPointVelocity(CableStartPosition) : Vector2.zero) -
                             (this.RB2D != null ? this.RB2D.GetPointVelocity(CableEndPosition) : Vector2.zero);

            // velocity constraint: velocity along jacobian must be zero.
            float cDot = Vector2.Dot(relVel, jacobian);

            // calculate constraint force intensity:  
            lambda = (-cDot - c * bias / Time.fixedDeltaTime) / k;

            // accumulate and clamp impulse:
            float tempLambda = totalLambda;
            totalLambda = Mathf.Min(0, totalLambda + lambda);
            lambda = totalLambda - tempLambda;

            // apply impulse to both rigidbodies:
            impulse = jacobian * lambda;

            if (this.RB2D != null && !this.RB2D.isKinematic)
            {
                this.RB2D.velocity -= impulse * invMass;
                RB2D.angularVelocity -= Mathf.Rad2Deg * invInertiaTensor.MultiplyVector(Vector3.Cross(this.impulseRadiusTail, impulse)).z;
            }

            if (tail.RB2D != null && !tail.RB2D.isKinematic)
            {
                tail.RB2D.velocity += impulse * tail.invMass;
                tail.RB2D.angularVelocity += Mathf.Rad2Deg * tail.invInertiaTensor.MultiplyVector(Vector3.Cross(tail.impulseRadiusHead, impulse)).z;
            }
        }
    }

    public void TriggerBoxUpdate()
    {
        triggerBox.position = this.NodePosition + tangentOffsetTail;
        Vector2 cable = tail.NodePosition + tail.tangentOffsetHead - (Vector2)triggerBox.position;
        triggerBox.rotation = Quaternion.FromToRotation(Vector3.right, cable);
        triggerBox.localScale = new Vector3(cable.magnitude, CableWidth * triggerWidth, 1);
    }

    public void NodeAdder()
    {
        RaycastHit2D hit = Physics2D.CircleCast(this.NodePosition + this.tangentOffsetTail, CableWidth / 2 * triggerWidth, tail.NodePosition + tail.tangentOffsetHead - (this.NodePosition + this.tangentOffsetTail), length, mask);
        if (!hit) return;

        //print("cableHit");
        CableJoint newNode;

        if (ConfigurePulley(hit.collider.GetComponent<CableMeshInterface>(), out newNode))
        {
            AddBack(newNode);

            if (newNode.tail.linkType == LinkType.Rolling)
            {
                TangentAlgorithm(newNode.pulley, newNode.tail.node.pulley, out newNode.tangentOffsetTail, out newNode.tail.tangentOffsetHead, out newNode.tangentIdentityTail, out newNode.tail.node.tangentIdentityHead, newNode.orientation, newNode.tail.node.orientation);
            }
            else
            {
                newNode.tangentOffsetTail = newNode.pulley.PointToShapeTangent(newNode.tail.NodePosition, newNode.orientation, newNode.CableWidth, out newNode.tangentIdentityTail);
            }

            if (this.linkType == LinkType.Rolling)
            {
                TangentAlgorithm(this.pulley, newNode.pulley, out this.tangentOffsetTail, out newNode.tangentOffsetHead, out this.tangentIdentityTail, out newNode.tangentIdentityHead, this.orientation, newNode.orientation);
            }
            else
            {
                newNode.tangentOffsetHead = newNode.pulley.PointToShapeTangent(this.NodePosition, !newNode.orientation, newNode.CableWidth, out newNode.tangentIdentityHead);
            }

            newNode.storedLength = newNode.pulley.ShapeSurfaceDistance(newNode.tangentIdentityTail, newNode.tangentIdentityHead, newNode.orientation, newNode.CableWidth, false);

            newNode.InitializeNodes();

            this.InitializeNodes();

            float initialRestLength = this.restLength;

            // Adjust rest lengths so that tensions are equal:
            float tension = initialRestLength / (this.length + newNode.length);
            this.restLength = this.length * tension;
            newNode.restLength = newNode.length * tension;

            newNode.Initilizebox(this.triggerWidth);

            newNode.TriggerBoxUpdate();
            
            this.TriggerBoxUpdate();
        }
    }

    bool ConfigurePulley(CableMeshInterface hitPulley, out CableJoint newNode)
    {
        newNode = null;

        if ((hitPulley == null) || !hitPulley.MeshGenerated) return false;
        if ((this.linkType == LinkType.Rolling) && (hitPulley == this.pulley)) return false;
        if ((this.tail != null) && (this.tail.linkType == LinkType.Rolling) && (this.tail.node.pulley == hitPulley)) return false;

        newNode = ((GameObject)Instantiate(nodePrefab, hitPulley.PulleyCentreGeometrical, Quaternion.identity)).GetComponent<CableJoint>();

        newNode.pulley = hitPulley;

        newNode.orientation = newNode.pulley.Orientation(prevCableTailPosition, prevCableThisPosition);

        newNode.anchor = this.anchor;

        newNode.name = this.name + (++kin).ToString();

        newNode.linkType = LinkType.Rolling;

        return true;
    }

    void NodeRemover()
    {
        if (!RemoveCondition()) return;
        
        head.node.restLength += this.restLength + this.storedLength;

        CutChain();

        if (tail.linkType == LinkType.Rolling)
        {
            if (head.linkType == LinkType.Rolling)
            {
                TangentAlgorithm(head.node.pulley, tail.node.pulley, out head.tangentOffsetTail, out tail.tangentOffsetHead, out head.node.tangentIdentityTail, out tail.node.tangentIdentityHead, head.node.orientation, tail.node.orientation);
            }
            else
            {
                tail.tangentOffsetHead = tail.node.pulley.PointToShapeTangent(head.NodePosition, !tail.node.orientation, tail.CableWidth, out tail.node.tangentIdentityHead);
            }
        }
        else if(head.linkType == LinkType.Rolling)
        {
            head.tangentOffsetTail = head.node.pulley.PointToShapeTangent(tail.NodePosition, head.node.orientation, head.CableWidth, out head.node.tangentIdentityTail);
        }

        head.GetComponent<CableJoint>().InitializeNodes();

        Destroy(triggerBox.gameObject);
        Destroy(this.gameObject);
    }

    bool RemoveCondition()
    {
        if (this.linkType != LinkType.Rolling) return false;

        if (this.pulley.CableMeshPrimitiveType == CableMeshInterface.CMPrimitives.Circle)
        {
            return this.storedLength < 0;
        }

        if (this.storedLength >= pulley.SafeStoredLength) return false;

        Vector2 tailDirection = this.CableEndPosition - this.CableStartPosition;
        Vector2 headDirection = head.CableEndPosition - head.CableStartPosition;
        bool sign = Vector2.SignedAngle(tailDirection, headDirection) < 0;

        return sign == this.orientation;
    }
}
