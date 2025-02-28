using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class CableJoint : CableBase
{
    [SerializeField] private GameObject nodePrefab;

    int kin = 0;

    [HideInInspector] public float totalLambda = 0;
    [HideInInspector] public float lambda = 0;

    private float inverseEffectiveMassDenominator = 0;

    //private float slippingMass = 0;

    //private float slipVelocity = 0;

    //private float slipSign = 0;

    private bool slipping = false;

    [SerializeField] float slipVelocityThreshold = 0.01f;

    [HideInInspector] public float currentLength = 0;
    [HideInInspector] public float restLength = 1;
    [HideInInspector] public float positionError = 0;

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

    public override Vector2 NodePosition { get { return pulley != null ? (RB2D != null ? RB2D.worldCenterOfMass : pulley.PulleyCentreGeometrical) : this.transform.position; } }

    //temporary solution
    public Rigidbody2D rb2dEnd = null;
    public override Rigidbody2D RB2D { get { return rb2dEnd == null ? pulley.PulleyAttachedRigidBody : rb2dEnd; } }

    private float segmentTension = 0.0f;
    private float frictionFactor = 1.0f;
    private float SlipA = 0.0f;
    private float SlipB = 0.0f;
    private float slipSolveTolerance = 0.000001f;

    [HideInInspector] public int slipNodesCount = 0;

    void Awake()
    {
        node = this.GetComponent<CableJoint>();

        filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        currentLength = (CableStartPosition - CableEndPosition).magnitude;
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
    

    public void CableSegmentPreSlipUpdate()
    {
        //Updates tangents and handles stored length for each pulley
        UpdatePulley();
        //Calculates all the current lengths and initializes segments for solving
        InitializeNodes();
        
        //will be removed
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

        Vector2 centreOfMassOffset = (pulley.PulleyCentreGeometrical - NodePosition);
        this.tangentOffsetTail = this.tOffTail + centreOfMassOffset;
        this.tangentOffsetHead = this.tOffHead + centreOfMassOffset;

        this.tangentIdentityTail = this.tIdentityTail;
        this.tangentIdentityHead = this.tIdentityHead;
    }

    void InitializeNodes()
    {
        if (tail == null) return;

        Vector3 distVector = this.CableStartPosition - this.CableEndPosition;
        this.currentLength = distVector.magnitude;
        this.cableUnitVector = distVector.normalized;

        this.positionError = currentLength - restLength;

        this.segmentTension = TensionEstimation(this);
    }

    void FrictionFactorUpdate(float slipSign)
    {
        frictionFactor = this.pulley.FrictionFactor(slipSign, this.slipping, this.storedLength, CableWidth);
    }

    public void CableSlipConditionsUpdate()
    {
        if (tail == null) return;
        if (head != null)
        {
            float slidingCondition = 0.0f;
            float tension1 = this.segmentTension;
            float tension2 = head.node.segmentTension;
            
            if (tension1 <= tension2)
            {
                FrictionFactorUpdate(1.0f);
                slidingCondition = tension2 - tension1 * this.frictionFactor;
            }
            else
            {
                FrictionFactorUpdate(-1.0f);
                slidingCondition = tension1 * this.frictionFactor - tension2;
            }

            //print(tension1 + "   " + tension2 + "   " + this.frictionFactor + "   " + slidingCondition);

            if (slidingCondition > 0.0f)
            {
                if (!slipping)
                {
                    slipping = true;
                    FrictionFactorUpdate((tension1 <= tension2) ? 1.0f : -1.0f);
                }
            }
            else
            {
                if (slipping)
                {
                    slipping = false;
                    FrictionFactorUpdate((tension1 <= tension2) ? 1.0f : -1.0f);
                }
            }

        }
        else
        {
            slipping = false;
        }
    }

    public void CableConstraintsInitialization(ref CableBase slippingNodesStart, ref int slippingCount)
    {
        //every node is evaluated if they are slipping and may be considered within a slipping group
        if (slipping)
        {
            //the start of a new group of consecutive slipping nodes
            if (slippingNodesStart == null)
            {
                slippingNodesStart = this;
                slippingCount = 0;
            }
            else
            {
                slippingNodesStart.node.positionError += this.positionError;
            }
            slippingCount++;
        }
        else
        {
            //A group is complete and tensions may be balanced within the group
            //Recalculate the effective mass denominator for the whole group
            if (slippingNodesStart != null)
            {
                slippingNodesStart.node.positionError += this.positionError;
                slippingNodesStart.node.slipNodesCount = slippingCount;
                SlippingBalanceTension(slippingNodesStart);
                InverseMassDenominatorCalculationGroup(slippingNodesStart);

                slippingNodesStart = null;
            }
            else
            {
                InverseMassDenominatorCalculation();
            }
        }
        this.totalLambda = 0;
    }

    float FrictionCompounded(CableBase start, int index)
    {
        CableJoint currentNode = start.node;
        float ret = 1.0f;
        for (int i = 0; i < index + 1; i++)
        {
            ret *= currentNode.frictionFactor;
            currentNode = currentNode.head.node;
        }
        return ret;
    }

    float TensionEstimation(CableJoint currentNode)
    {
        return math.max(currentNode.currentLength - currentNode.restLength, 0.0f);
    }

    void SlippingBalanceTension(CableBase groupStart)
    {
        int count = groupStart.node.slipNodesCount;
        float startTension = groupStart.node.segmentTension;

        CableJoint currentNode = groupStart.node;
        float sumA = 0.0f;
        float sumB = 0.0f;
        for (int i = 0; i < count; i++)
        {

            currentNode = currentNode.head.node;
            currentNode.SlipA = FrictionCompounded(groupStart, i);
            currentNode.SlipB = currentNode.node.segmentTension - FrictionCompounded(groupStart, i) * startTension;

            sumA += currentNode.SlipA;
            sumB += currentNode.SlipB;
        }

        float startRestDelta = -sumB / (1 + sumA);
        groupStart.node.restLength += startRestDelta;

        currentNode = groupStart.node;
        for (int i = 0; i < count; i++)
        {
            currentNode = currentNode.head.node;
            currentNode.restLength += currentNode.SlipA * startRestDelta + currentNode.SlipB;
        }
    }

    void InverseMassDenominatorCalculation()
    {
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

            if (tail.RB2D.inertia != 0)
            {
                impulseRadius = Vector3.Cross(tail.tangentOffsetHead, cableUnitVector);

                inertiaTerm2 = Mathf.Pow(impulseRadius.z, 2) / tail.RB2D.inertia;
            }
        }

        //the mass projected along the cable direction that the impulse lambda must work against
        //larger masses result in a smaller denominator. a static object with infinite mass will give terms of zero
        //if both objects are static no impulse needs to be calculated
        inverseEffectiveMassDenominator = invMass1 + invMass2 + inertiaTerm1 + inertiaTerm2;
        inverseEffectiveMassDenominator = 1 / inverseEffectiveMassDenominator;
    }

    Vector2 M(int i, int max, CableBase node, CableBase rootNode)
    {
        if (i == 0)
        {
            return -node.head.node.cableUnitVector / node.RB2D.mass;
        }
        if (i == max)
        {
            return FrictionCompounded(rootNode, i - 2) * node.node.cableUnitVector / node.RB2D.mass;
        }
        return (FrictionCompounded(rootNode, i - 2) * node.node.cableUnitVector - FrictionCompounded(rootNode, i - 1) * node.head.node.cableUnitVector) / node.RB2D.mass;
    }
    Vector3 I(int i, int max, CableBase node, CableBase rootNode)
    {
        if (i == 0)
        {
            return -Vector3.Cross(node.node.tangentOffsetHead, node.head.node.cableUnitVector) / node.RB2D.inertia;
        }
        if (i == max)
        {
            return FrictionCompounded(rootNode, i - 2) * Vector3.Cross(node.node.tangentOffsetTail, node.node.cableUnitVector) / node.RB2D.inertia;
        }
        return (FrictionCompounded(rootNode, i - 2) * Vector3.Cross(node.node.tangentOffsetTail, node.node.cableUnitVector) - FrictionCompounded(rootNode, i - 1) * Vector3.Cross(node.node.tangentOffsetHead, node.head.node.cableUnitVector)) / node.RB2D.inertia;
    }
    void InverseMassDenominatorCalculationGroup(CableBase groupStart)
    {
        int slippingCount = groupStart.node.slipNodesCount;

        Vector2[] massDenominators = new Vector2[slippingCount + 2];
        Vector3[] inertiaDenominators = new Vector3[slippingCount + 2];
        CableBase current = groupStart.tail;
        //pre calculate the individual contributions of every body
        for (int i = 0; i <= slippingCount + 1; i++)
        {
            if (current.RB2D != null && !current.RB2D.isKinematic)
            {
                massDenominators[i] = M(i, slippingCount + 1, current, groupStart);
                if (current.RB2D.inertia != 0)
                {
                    inertiaDenominators[i] = I(i, slippingCount + 1, current, groupStart);
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
            current = current.head;
        }

        //the mass projected along the cable direction that the impulse lambda must work against
        //larger masses result in a smaller denominator. a static object with infinite mass will give terms of zero
        //if both objects are static no impulse needs to be calculated
        groupStart.node.inverseEffectiveMassDenominator = 0;
        current = groupStart;
        //summize the total projected constraint mass denominator
        for (int i = 1; i <= slippingCount + 1; i++)
        {
            groupStart.node.inverseEffectiveMassDenominator += Vector2.Dot(massDenominators[i] - massDenominators[i - 1], current.node.cableUnitVector)
                           + Vector3.Dot(inertiaDenominators[i], Vector3.Cross(current.tangentOffsetTail, current.node.cableUnitVector))
                           - Vector3.Dot(inertiaDenominators[i - 1], Vector3.Cross(current.tail.tangentOffsetHead, current.node.cableUnitVector));

            current = current.head;
        }
        groupStart.node.inverseEffectiveMassDenominator = 1 / groupStart.node.inverseEffectiveMassDenominator;
    }

    public void CableJointsSolve(CableBase start)
    {
        if (start == null) return;
        if (start.tail == null) return;

        if (start.node.slipping)
        {
            start.node.CableSlipGroupSolveConstrain();
            int count = start.node.slipNodesCount;
            for (int i = 0; i < count; i++)
            {
                start = start.head;
            }
        }
        else
        {
            start.node.CableSegmentSolveConstrain();
        }

        if (start.head != null)
        {
            start.head.node.CableJointsSolve(start.head);
        }
    }

    public void CableSegmentSolveConstrain()
    {
        if (positionError > 0 && inverseEffectiveMassDenominator > 0)
        {
            lambda = 0;

            //project the relative velocity of the two bodies along the cable direction
            Vector2 relVel = (tail.RB2D != null ? tail.RB2D.GetPointVelocity(CableStartPosition) : Vector2.zero) -
                             (this.RB2D != null ? this.RB2D.GetPointVelocity(CableEndPosition) : Vector2.zero);


            float velConstraintValue = Vector2.Dot(relVel, cableUnitVector);
            float velocitySteering = bias * positionError / Time.fixedDeltaTime;
            
            //impulse intensity:  
            lambda = -(velConstraintValue + velocitySteering) * inverseEffectiveMassDenominator;

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

    public void CableSlipGroupSolveConstrain()
    {
        if (positionError > 0 && inverseEffectiveMassDenominator > 0)
        {
            lambda = 0;

            //Sum the current velocity errors
            float velocityError = 0;
            CableBase segment = this;
            for (int i = 0; i < slipNodesCount + 1; i++)
            {
                velocityError += Vector2.Dot(segment.tail.RB2D.velocity - segment.RB2D.velocity, segment.node.cableUnitVector) +
                    segment.tail.RB2D.angularVelocity * Vector3.Cross(segment.tail.tangentOffsetHead, segment.node.cableUnitVector).z -
                    segment.RB2D.angularVelocity * Vector3.Cross(segment.tangentOffsetTail, segment.node.cableUnitVector).z;
                segment = segment.head;
            }

            float velocitySteering = bias * positionError / Time.fixedDeltaTime;

            //impulse intensity:  
            lambda = -(velocityError + velocitySteering) * inverseEffectiveMassDenominator;

            //accumulate and clamp impulse
            float tempLambda = totalLambda;
            totalLambda = Mathf.Min(0, totalLambda + lambda);
            lambda = totalLambda - tempLambda;

            //apply impulse
            segment = this;
            for (int i = 0; i < slipNodesCount + 1; i++)
            {
                float frictionFactorScalar = FrictionCompounded(this,i - 1);

                if (segment.RB2D != null && !segment.RB2D.isKinematic)
                {
                    segment.RB2D.velocity -= lambda * frictionFactorScalar * segment.node.cableUnitVector / segment.RB2D.mass;

                    if (segment.RB2D.inertia != 0)
                    {
                        segment.RB2D.angularVelocity -= Mathf.Rad2Deg * lambda * frictionFactorScalar * Vector3.Cross(segment.tangentOffsetTail, segment.node.cableUnitVector).z / segment.RB2D.inertia;
                    }
                }

                if (segment.tail.RB2D != null && !segment.tail.RB2D.isKinematic)
                {
                    segment.tail.RB2D.velocity += lambda * frictionFactorScalar * segment.node.cableUnitVector / segment.tail.RB2D.mass;

                    if (segment.tail.RB2D.inertia != 0)
                    {
                        segment.tail.RB2D.angularVelocity += Mathf.Rad2Deg * lambda * frictionFactorScalar * Vector3.Cross(segment.tail.tangentOffsetHead, segment.node.cableUnitVector).z / segment.tail.RB2D.inertia;
                    }
                }
                segment = segment.head;
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
        }
    }

    bool ConfigurePulley(CableMeshInterface hitPulley, out CableJoint newNode)
    {
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
}
