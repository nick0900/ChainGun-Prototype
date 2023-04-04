using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class CableJoint : CableBase
{
    [SerializeField] private GameObject nodePrefab;

    int kin = 0;

    [HideInInspector] public float totalLambda = 0;

    private float effectiveMassDenominator = 0;

    private float slippingMass = 0;

    private float slipVelocity = 0;

    private float slipSign = 0;

    private bool slipping = false;

    [SerializeField] float slipVelocityThreshold = 0.01f;

    [HideInInspector] public float currentLength = 0;
    [HideInInspector] public float restLength = 1;

    [HideInInspector] public Vector2 cableUnitVector = Vector2.zero;

    [HideInInspector] public bool orientation;

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
        if (DoSlipSimulation)
        {
            SlipUpdate();
        }
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

    void SlipUpdate()
    {
        if ((tail == null) || head == null) return;

        bool headBigger = this.totalLambda > head.node.totalLambda;

        float greaterLambda = headBigger ? -head.node.totalLambda : -this.totalLambda;

        float lesserLambda = !headBigger ? -head.node.totalLambda : -this.totalLambda;


        float angle = Vector2.SignedAngle(head.node.cableUnitVector, this.cableUnitVector);
        if (angle < 0) angle = 360.0f + angle;
        angle *= Mathf.Deg2Rad;

        float lambdaMaxStatic = lesserLambda * Mathf.Exp(CableStaticFriction * angle);
        
        if (!slipping)
        {
            if (greaterLambda > lambdaMaxStatic) slipping = true;
            slipSign = headBigger ? -1.0f : 1.0f;
        }

        if (slipping)
        {
            float frictionImpulse = 0.0f;

            if (CableStaticFriction != 0.0f) 
            {
                frictionImpulse = slipSign * (lambdaMaxStatic - lesserLambda) * CableKineticFriction / CableStaticFriction;
            }

            float slipImpulse = -this.totalLambda + head.node.totalLambda - frictionImpulse;

            slipVelocity += slipImpulse / slippingMass;

            if (slipVelocity * slipSign <= 0)
            {

                slipping = false;
                slipVelocity = 0.0f;
            }
            else
            {
                this.restLength += slipVelocity * Time.fixedDeltaTime;
                head.node.restLength -= slipVelocity * Time.fixedDeltaTime;
            }
        }
    }

    void InitializeNodes()
    {
        totalLambda = 0;

        if (tail == null) return;

        Vector3 distVector = CableStartPosition - CableEndPosition;
        currentLength = distVector.magnitude;
        cableUnitVector = distVector.normalized;

        float invMass1 = 0;
        float invMass2 = 0;

        float inertiaTerm1 = 0;
        float inertiaTerm2 = 0;

        Vector3 impulseRadius = Vector3.zero;

        if (RB2D != null && !RB2D.isKinematic)
        {
            invMass1 = 1.0f / RB2D.mass;


            if (RB2D.inertia != 0)
            {
                impulseRadius = Vector3.Cross(this.tangentOffsetTail, cableUnitVector);

                inertiaTerm1 = Vector3.Dot(impulseRadius, impulseRadius) / RB2D.inertia;
            }
        }

        if (tail.RB2D != null && !tail.RB2D.isKinematic)
        {
            invMass2 = 1.0f / tail.RB2D.mass;

            if ( tail.RB2D.inertia != 0)
            {
                impulseRadius = Vector3.Cross(tail.tangentOffsetHead, cableUnitVector);

                inertiaTerm2 = Mathf.Pow(impulseRadius.z, 2) / tail.RB2D.inertia;
            }
        }

        //the mass projected along the cable direction that the impulse lambda must work against
        //larger masses result in a smaller denominator. a static object with infinite mass will give terms of zero
        //if both objects are static no impulse needs to be calculated
        effectiveMassDenominator = invMass1 + invMass2 + inertiaTerm1 + inertiaTerm2;

        if (head != null)
        {
            this.slippingMass = (tail.RB2D.mass + head.RB2D.mass);
        }
    }

    public void CableSegmentSolveConstrain()
    {
        if (tail == null) return;

        float positionError = currentLength - restLength;

        if (positionError > 0 && effectiveMassDenominator > 0)
        {
            float lambda = 0;

            //project the relative velocity of the two bodies along the cable direction
            Vector2 relVel = (tail.RB2D != null ? tail.RB2D.GetPointVelocity(CableStartPosition) : Vector2.zero) -
                             (this.RB2D != null ? this.RB2D.GetPointVelocity(CableEndPosition) : Vector2.zero);


            float velConstraintValue = Vector2.Dot(relVel, cableUnitVector);
            float velocitySteering = bias * positionError / Time.fixedDeltaTime;

            //impulse intensity:  
            lambda = -(velConstraintValue + velocitySteering) / effectiveMassDenominator;

            //accumulate and clamp impulse
            float tempLambda = totalLambda;
            totalLambda = Mathf.Min(0, totalLambda + lambda);
            lambda = totalLambda - tempLambda;

            //apply impulse
            if (this.RB2D != null && !this.RB2D.isKinematic)
            {
                this.RB2D.velocity -= lambda * cableUnitVector / this.RB2D.mass;

                if (this.RB2D.inertia != 0)
                {
                    this.RB2D.angularVelocity -= Mathf.Rad2Deg * lambda * Vector3.Cross(this.tangentOffsetTail, cableUnitVector).z / this.RB2D.inertia;
                }
            }

            if (tail.RB2D != null && !tail.RB2D.isKinematic)
            {
                tail.RB2D.velocity += lambda * cableUnitVector / tail.RB2D.mass;

                if (tail.RB2D.inertia != 0)
                {
                    tail.RB2D.angularVelocity += Mathf.Rad2Deg * lambda * Vector3.Cross(tail.tangentOffsetHead, cableUnitVector).z / tail.RB2D.inertia;
                }
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
        RaycastHit2D hit = Physics2D.CircleCast(this.NodePosition + this.tangentOffsetTail, CableWidth / 2 * triggerWidth, tail.NodePosition + tail.tangentOffsetHead - (this.NodePosition + this.tangentOffsetTail), currentLength, mask);
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
            float tension = initialRestLength / (this.currentLength + newNode.currentLength);
            this.restLength = this.currentLength * tension;
            newNode.restLength = newNode.currentLength * tension;

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

        head.node.InitializeNodes();

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

    private void OnDestroy()
    {
        Destroy(triggerBox.gameObject);
    }
}
