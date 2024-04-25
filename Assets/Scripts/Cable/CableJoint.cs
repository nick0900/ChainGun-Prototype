using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class CableJoint : CableBase
{
    [SerializeField] private GameObject nodePrefab;

    int kin = 0;

    [HideInInspector] public float totalLambda = 0;
    [HideInInspector] public float lambda = 0;

    private float effectiveMassDenominator = 0;

    //private float slippingMass = 0;

    //private float slipVelocity = 0;

    //private float slipSign = 0;

    private bool slipping = false;

    [SerializeField] float slipVelocityThreshold = 0.01f;

    [HideInInspector] public float currentLength = 0;
    [HideInInspector] public float previousLength = 0;
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

    private float previousTrueTension = 0.0f;
    private float frictionFactor = 1.0f;
    private float SlipA = 0.0f;
    private float SlipB = 0.0f;
    private float estimatedTension = 0.0f;
    private float slipSolveTolerance = 0.000001f;

    [HideInInspector] public CableJoint slipEndNode = null;
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
    

    public void CableSegmentUpdate()
    {
        UpdatePulley();
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

    void InitializeNodes()
    {
        previousTrueTension = Mathf.Abs(totalLambda);
        if ((head != null) && (tail != null))
            print(this.totalLambda + " " + head.node.totalLambda + " " + (Mathf.Abs(this.totalLambda) - Mathf.Abs(head.node.totalLambda)));
        totalLambda = 0;

        if (tail == null) return;

        previousLength = currentLength;
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

        //if (head != null)
        //{
        //    this.slippingMass = (tail.RB2D.mass + head.RB2D.mass);
        //}

        estimatedTension = TensionEstimation(this);
    }

    void FrictionFactorUpdate(float slipSign)
    {
        switch (this.pulley.CableMeshPrimitiveType)
        {
            case CableMeshInterface.CMPrimitives.Circle:
                //!!!!!!!!!!! Current implementation works only for one wrap around pulley !!!!!!!!!!!!!!!!!!!!
                float wrapAngle = Vector2.SignedAngle(head.node.cableUnitVector, this.cableUnitVector);
                if (wrapAngle < 0) wrapAngle = 360.0f + wrapAngle;
                wrapAngle *= Mathf.Deg2Rad;

                frictionFactor = Mathf.Exp(slipSign * (slipping ? CableKineticFriction : CableStaticFriction) * wrapAngle);
                break;

            default:
                frictionFactor = 1.0f;
                break;
        }
    }

    public void CableSlipUpdate(ref CableBase slippingNodesStart, ref int slippingCount)
    {
        if (tail == null) return;
        if (head != null)
        {
            float slidingCondition = 0.0f;
            //The final impulses from previous timestep used for the most accurate determination of cable slipping.
            if (this.previousTrueTension <= head.node.previousTrueTension)
            {
                FrictionFactorUpdate(1.0f);
                slidingCondition = head.node.previousTrueTension - this.previousTrueTension * this.frictionFactor;
            }
            else
            {
                FrictionFactorUpdate(-1.0f);
                slidingCondition = this.previousTrueTension * this.frictionFactor - head.node.previousTrueTension;
            }

            slipping = slidingCondition > 0.0f;
        }
        else
        {
            slipping = false;
        }

        //every node is evaluated if they are slipping and may be considered within a slipping group
        if (slipping)
        {
            //the start of a new group of consecutive slipping nodes
            if (slippingNodesStart == null)
            {
                slippingNodesStart = this;
                slippingCount = 0;
            }
            slippingCount++;
        }
        else
        {
            //A group is complete and tensions may be balanced within the group
            if (slippingNodesStart != null)
            {
                slippingNodesStart.node.slipEndNode = this;
                slippingNodesStart.node.slipNodesCount = slippingCount;

                float invMass1 = 0;
                float invMass2 = 0;

                float inertiaTerm1 = 0;
                float inertiaTerm2 = 0;

                Vector3 impulseRadius = Vector3.zero;

                if (this.RB2D != null && !this.RB2D.isKinematic)
                {
                    invMass1 = 1.0f / this.RB2D.mass;


                    if (this.RB2D.inertia != 0)
                    {
                        impulseRadius = Vector3.Cross(this.tangentOffsetTail, this.cableUnitVector);

                        inertiaTerm1 = Vector3.Dot(impulseRadius, impulseRadius) / this.RB2D.inertia;
                    }
                }

                if (slippingNodesStart.tail.RB2D != null && !slippingNodesStart.tail.RB2D.isKinematic)
                {
                    invMass2 = 1.0f / slippingNodesStart.tail.RB2D.mass;

                    if (slippingNodesStart.tail.RB2D.inertia != 0)
                    {
                        impulseRadius = Vector3.Cross(slippingNodesStart.tail.tangentOffsetHead, slippingNodesStart.node.cableUnitVector);

                        inertiaTerm2 = Mathf.Pow(impulseRadius.z, 2) / slippingNodesStart.tail.RB2D.inertia;
                    }
                }

                //the mass projected along the cable direction that the impulse lambda must work against
                //larger masses result in a smaller denominator. a static object with infinite mass will give terms of zero
                //if both objects are static no impulse needs to be calculated
                slippingNodesStart.node.effectiveMassDenominator = invMass1 + invMass2 + inertiaTerm1 + inertiaTerm2;

                SlipSolve(slippingNodesStart, slippingCount);
                slippingNodesStart = null;
            }
        }
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
        return (currentNode.currentLength - currentNode.restLength) / currentNode.effectiveMassDenominator;
    }

    bool SlipSolutionTest(CableJoint start, int count)
    {
        CableJoint currentNode = start.node;
        for (int i = 0; i < count; i++)
        {
            if(Mathf.Abs(currentNode.head.node.estimatedTension - currentNode.estimatedTension * currentNode.frictionFactor) > slipSolveTolerance)
            {
                return false;
            }
            currentNode = currentNode.head.node;
        }
        return true;
    }

    void SlipSolve(CableBase start, int count)
    {
        int iterations = 0;
        //while (!SlipSolutionTest(start.node, count))
        while (iterations < 1)
        {
            iterations++;
            CableJoint currentNode = start.node;
            float sumA = 0.0f;
            float sumB = 0.0f;
            for (int i = 0; i < count; i++)
            {

                currentNode = currentNode.head.node;
                currentNode.SlipA = FrictionCompounded(start, i) * currentNode.effectiveMassDenominator / start.node.effectiveMassDenominator;
                currentNode.SlipB = currentNode.effectiveMassDenominator * (currentNode.estimatedTension - FrictionCompounded(start, i) * start.node.estimatedTension);

                sumA += currentNode.SlipA;
                sumB += currentNode.SlipB;
            }

            float startRestDelta = -sumB / (1 + sumA);
            start.node.restLength += startRestDelta;
            start.node.estimatedTension = TensionEstimation(start.node);

            currentNode = start.node;
            for (int i = 0; i < count; i++)
            {
                currentNode = currentNode.head.node;
                currentNode.restLength += currentNode.SlipA * startRestDelta + currentNode.SlipB;
                currentNode.estimatedTension = TensionEstimation(currentNode);
            }
        }
        //print("solved with " + iterations + " iterations");
    }

    public void CableJointsSolve(CableBase start)
    {
        if (start == null) return;

        if (start.node.slipping)
        {
            start.node.CableSlipGroupSolveConstrain();

            for (int i = 0; i < start.node.slipNodesCount; i++)
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
        if (tail == null) return;

        float positionError = currentLength - restLength;

        if (positionError > 0 && effectiveMassDenominator > 0)
        {
            lambda = 0;

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

    public void CableSlipGroupSolveConstrain()
    {
        float lengthSum = currentLength;
        float restSum = restLength;
        CableBase currentNode = head;
        for (int i = 0; i < slipNodesCount; i++)
        {
            lengthSum += currentNode.node.currentLength;
            restSum += currentNode.node.restLength;

            currentNode = currentNode.head;
        }

        float positionError = lengthSum - restSum;

        if (positionError > 0 && effectiveMassDenominator > 0)
        {
            lambda = 0;

            //project the relative velocity of the two bodies along the cable direction

            float projVel1 = Vector2.Dot((tail.RB2D != null ? tail.RB2D.GetPointVelocity(this.CableStartPosition) : Vector2.zero), this.cableUnitVector);

            float projVel2 = Vector2.Dot(slipEndNode.RB2D != null ? slipEndNode.RB2D.GetPointVelocity(slipEndNode.CableEndPosition) : Vector2.zero, slipEndNode.cableUnitVector);


            float velConstraintValue = projVel1 - projVel2;
            float velocitySteering = bias * positionError / Time.fixedDeltaTime;

            //impulse intensity:  
            lambda = -(velConstraintValue + velocitySteering) / effectiveMassDenominator;

            //accumulate and clamp impulse
            float tempLambda = totalLambda;
            totalLambda = Mathf.Min(0, totalLambda + lambda);
            lambda = totalLambda - tempLambda;

            //apply impulse
            if (slipEndNode.RB2D != null && !slipEndNode.RB2D.isKinematic)
            {
                slipEndNode.RB2D.velocity -= lambda * slipEndNode.cableUnitVector / slipEndNode.RB2D.mass;

                if (slipEndNode.RB2D.inertia != 0)
                {
                    slipEndNode.RB2D.angularVelocity -= Mathf.Rad2Deg * lambda * Vector3.Cross(slipEndNode.tangentOffsetTail, slipEndNode.cableUnitVector).z / slipEndNode.RB2D.inertia;
                }
            }

            if (tail.RB2D != null && !tail.RB2D.isKinematic)
            {
                tail.RB2D.velocity += lambda * this.cableUnitVector / tail.RB2D.mass;

                if (tail.RB2D.inertia != 0)
                {
                    tail.RB2D.angularVelocity += Mathf.Rad2Deg * lambda * Vector3.Cross(tail.tangentOffsetHead, this.cableUnitVector).z / tail.RB2D.inertia;
                }
            }
        }
    }

    public void CablePulleyTensionBalance()
    {
        if (!slipping) return;

        //calculate balancing impulse
        float lambdaErr = (this.lambda - this.frictionFactor * head.node.lambda) / (1 + this.frictionFactor);
        //print(lambdaErr);

        //uppdate total lambdas
        //this.totalLambda -= lambdaErr;
        //head.node.totalLambda += lambdaErr;

        //float tempLambda = this.totalLambda;
        //this.totalLambda = Mathf.Min(0, this.totalLambda + lambdaErrOrig);
        //float lambdaErr = totalLambda - tempLambda;

        //apply impulse
        if (tail.RB2D != null && !tail.RB2D.isKinematic)
        {
            tail.RB2D.velocity -= lambdaErr * this.cableUnitVector / tail.RB2D.mass;

            if (tail.RB2D.inertia != 0)
            {
                tail.RB2D.angularVelocity -= Mathf.Rad2Deg * lambdaErr * Vector3.Cross(tail.tangentOffsetHead, this.cableUnitVector).z / tail.RB2D.inertia;
            }
        }

        if (this.RB2D != null && !this.RB2D.isKinematic)
        {
            this.RB2D.velocity += lambdaErr * this.cableUnitVector / this.RB2D.mass;
            this.RB2D.velocity -= lambdaErr * head.node.cableUnitVector / this.RB2D.mass;

            if (this.RB2D.inertia != 0)
            {
                this.RB2D.angularVelocity += Mathf.Rad2Deg * lambdaErr * Vector3.Cross(this.tangentOffsetTail, this.cableUnitVector).z / this.RB2D.inertia;
                this.RB2D.angularVelocity -= Mathf.Rad2Deg * lambdaErr * Vector3.Cross(this.tangentOffsetTail, head.node.cableUnitVector).z / this.RB2D.inertia;
            }
        }

        //tempLambda = head.node.totalLambda;
        //head.node.totalLambda = Mathf.Min(0, this.totalLambda + lambdaErrOrig);
        //lambdaErr = totalLambda - tempLambda;

        if (head.RB2D != null && !head.RB2D.isKinematic)
        {
            head.RB2D.velocity += lambdaErr * head.node.cableUnitVector / head.RB2D.mass;

            if (head.RB2D.inertia != 0)
            {
                head.RB2D.angularVelocity += Mathf.Rad2Deg * lambdaErr * Vector3.Cross(head.tangentOffsetHead, head.node.cableUnitVector).z / head.RB2D.inertia;
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
}
