using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeFunctionality : ChainManager
{
    // gör om chainManager och NodeFunctionality till scriptable objects? Uppdastera också namn och skriv kommentarer

    [SerializeField] private GameObject nodePrefab;

    int kin = 0;

    private Vector2 jacobian;
    private float k;

    private Vector2 impulse;
    private float lambda;

    private float totalLambda = 0;

    [HideInInspector] public float chainWidth = 0.1f;

    [HideInInspector] public float length = 0;
    [HideInInspector] public float restLength = 1;

    [HideInInspector] public float pulleyRadius = 0;
    [HideInInspector] public bool orientation;

    [HideInInspector] public Vector2 tOffTail = Vector2.zero;

    [HideInInspector] public Vector2 tOffHead = Vector2.zero;

    [HideInInspector] public Quaternion previousRotation;

    [HideInInspector] public Quaternion previousHingeRotation;

    [HideInInspector] public CircleCollider2D previousPulley = null;

    private bool slipping = false;

    private Rigidbody2D hingeBody;

    private SurfaceMaster sm;

    [SerializeField] private GameObject chainTriggerPrefab;

    private Transform triggerBox;

    private float triggerWidth = 0.5f;

    ContactFilter2D filter;

    LayerMask mask = 1;

    private double epsilon = 0.000001;

    public ChainMesh CMesh;

    public Vector2 CMvertex;


    void Awake()
    {
        node = this.GetComponent<NodeFunctionality>();

        filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
    }

    public void Initilizebox(float width)
    {
        if (triggerBox != null)
        {
            Destroy(triggerBox.gameObject);
        }

        triggerBox = Instantiate(chainTriggerPrefab, this.transform.position, this.transform.rotation).transform;
        triggerBox.GetComponent<ChainTrigger>().ChainNode = this;

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

    void UpdatePulley()
    {
        if (this.linkType != LinkType.Rolling) return;

        if (tail.linkType != LinkType.Rolling)
        {
            TangentPointCircle(tail.transform.position, this.transform.position, this.pulleyRadius, this.orientation, out this.tOffTail);
        }
        if (head.linkType == LinkType.Rolling)
        {
            TangentCircleCircle(head.transform.position, head.node.pulleyRadius, head.node.orientation, out head.node.tOffTail,
                                this.transform.position, this.pulleyRadius, this.orientation, out this.tOffHead);
        }
        else
        {
            TangentPointCircle(head.transform.position, this.transform.position, this.pulleyRadius, !this.orientation, out this.tOffHead);
        }

        Vector2 currentTTail = (this.transform.rotation * Quaternion.Inverse(previousRotation)) * this.tangentOffsetTail;
        Vector2 currentTHead = (this.transform.rotation * Quaternion.Inverse(previousRotation)) * this.tangentOffsetHead;

        float distTail = CircleDistance(currentTTail, this.tOffTail, this.orientation, this.pulleyRadius);
        float distHead = CircleDistance(currentTHead, this.tOffHead, this.orientation, this.pulleyRadius);

        previousRotation = this.transform.rotation;

        // Update stored lengths:
        this.storedLength += distTail;
        this.storedLength -= distHead;

        // Update rest lengths:
        this.restLength -= distTail;
        head.node.restLength += distHead;

        this.tangentOffsetTail = this.tOffTail;
        this.tangentOffsetHead = this.tOffHead;
    }

    void TangentPointCircle(Vector2 P1, Vector2 P2, float r2, bool orientation2, out Vector2 tangentOffset2)
    {
        Vector2 d = P2 - P1;

        if (d.magnitude <= r2 - chainWidth/2)
        {
            //print("fuck");
            throw new System.Exception();
            //tangentOffset2 = Vector2.zero;
            //return;
        }

        float alpha = d.x >= 0 ? Mathf.Asin(d.y / d.magnitude) : Mathf.PI - Mathf.Asin(d.y / d.magnitude);

        float phi = Mathf.Asin((r2 - chainWidth/2) / d.magnitude);

        alpha = orientation2 ? alpha - Mathf.PI / 2 - phi : alpha + Mathf.PI / 2 + phi;

        tangentOffset2 = r2 * new Vector2(Mathf.Cos(alpha), Mathf.Sin(alpha));
    }

    void TangentCircleCircle(Vector2 P1, float r1, bool orientation1, out Vector2 tangentOffset1, Vector2 P2, float r2, bool orientation2, out Vector2 tangentOffset2)
    {
        Vector2 d = P2 - P1;

        bool sameOrientation = (orientation1 && orientation2) || !(orientation1 || orientation2);

        float r = sameOrientation ? r2 - r1 : r1 + r2 - chainWidth;

        if (d.magnitude <= r)
        {
            //tangentOffset1 = Vector2.zero;
            //tangentOffset2 = Vector2.zero;
            //return;
            throw new System.Exception();
        }

        if (sameOrientation)
        {
            if (r1 == r2)
            {
                d = Vector2.Perpendicular(d.normalized) * r1;

                if(!orientation1)
                {
                    d = -d;
                }

                tangentOffset1 = d;
                tangentOffset2 = d;
            }
            else
            {
                Vector2 tangentIntersection = (P2 * (r1 - chainWidth/2) - P1 * (r2 - chainWidth/2)) / (r1 - r2);

                TangentPointCircle(tangentIntersection, P1, r1, !orientation1, out tangentOffset1);
                TangentPointCircle(tangentIntersection, P2, r2, !orientation2, out tangentOffset2);
            }
        }
        else
        {
            Vector2 tangentIntersection = (P2*(r1 - chainWidth/2) + P1*(r2 - chainWidth/2)) / (r1 + r2 - chainWidth);

            TangentPointCircle(tangentIntersection, P1, r1, orientation1, out tangentOffset1);
            TangentPointCircle(tangentIntersection, P2, r2, !orientation2, out tangentOffset2);
        }
    }

    float CircleDistance(Vector2 p1, Vector2 p2, bool orientation, float radious)
    {
        if (orientation)
        {
            Vector2 aux = p1;
            p1 = p2;
            p2 = aux;
        }

        float theta = Mathf.Atan2(p1.x * p2.y - p1.y * p2.x, p1.x * p2.x + p1.y * p2.y);

        return radious * theta;
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
        Vector3 distVector = tail.transform.position + (Vector3)tail.tangentOffsetHead - this.transform.position - (Vector3)this.tangentOffsetTail;
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

        if (rb2d != null)
        {
            Vector3 invInertia1 = Quaternion.identity * new Vector3(rb2d.inertia > 0 ? 1.0f / rb2d.inertia : 0,
                                                                    rb2d.inertia > 0 ? 1.0f / rb2d.inertia : 0,
                                                                    rb2d.inertia > 0 ? 1.0f / rb2d.inertia : 0);

            Matrix4x4 m = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rb2d.rotation));
            invInertiaTensor[0, 0] = invInertia1.x;
            invInertiaTensor[1, 1] = invInertia1.y;
            invInertiaTensor[2, 2] = invInertia1.z;
            invInertiaTensor[3, 3] = 1;
            invInertiaTensor = m * invInertiaTensor * m.transpose;
        }

        if (tail.rb2d != null)
        {
            Vector3 invInertia2 = Quaternion.identity * new Vector3(tail.rb2d.inertia > 0 ? 1.0f / tail.rb2d.inertia : 0,
                                                                    tail.rb2d.inertia > 0 ? 1.0f / tail.rb2d.inertia : 0,
                                                                    tail.rb2d.inertia > 0 ? 1.0f / tail.rb2d.inertia : 0);

            Matrix4x4 m2 = Matrix4x4.Rotate(Quaternion.Euler(0, 0, tail.rb2d.rotation));
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

        if (rb2d != null && !rb2d.isKinematic)
        {
            invMass = 1.0f / (rb2d.mass);

            impulseRadiusTail = (Vector2)this.transform.position + tangentOffsetTail - rb2d.worldCenterOfMass;
            w1 = Vector3.Dot(Vector3.Cross(invInertiaTensor.MultiplyVector(Vector3.Cross(impulseRadiusTail, jacobian)), impulseRadiusTail), jacobian);
        }

        if (tail.rb2d != null && !tail.rb2d.isKinematic)
        {
            tail.invMass = 1.0f / (tail.rb2d.mass);

            tail.impulseRadiusHead = (Vector2)tail.transform.position + tail.tangentOffsetHead - tail.rb2d.worldCenterOfMass;
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
            Vector2 relVel = (tail.rb2d != null ? tail.rb2d.GetPointVelocity(tail.transform.position) : Vector2.zero) -
                             (this.rb2d != null ? this.rb2d.GetPointVelocity(this.transform.position) : Vector2.zero);

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

            if (this.rb2d != null && !this.rb2d.isKinematic)
            {
                this.rb2d.velocity -= impulse * invMass;
                rb2d.angularVelocity -= Mathf.Rad2Deg * invInertiaTensor.MultiplyVector(Vector3.Cross(this.impulseRadiusTail, impulse)).z;
            }

            if (tail.rb2d != null && !tail.rb2d.isKinematic)
            {
                tail.rb2d.velocity += impulse * tail.invMass;
                tail.rb2d.angularVelocity += Mathf.Rad2Deg * tail.invInertiaTensor.MultiplyVector(Vector3.Cross(tail.impulseRadiusHead, impulse)).z;
            }
        }
    }

    public void TriggerBoxUpdate()
    {
        triggerBox.position = this.transform.position + (Vector3)tangentOffsetTail;
        Vector2 cable = tail.transform.position + (Vector3)tail.tangentOffsetHead - triggerBox.position;
        triggerBox.rotation = Quaternion.FromToRotation(Vector3.right, cable);
        triggerBox.localScale = new Vector3(cable.magnitude, chainWidth * triggerWidth, 1);
    }

    public void NodeAdder()
    {
        RaycastHit2D hit = Physics2D.CircleCast(this.transform.position + (Vector3)this.tangentOffsetTail, chainWidth / 2 * triggerWidth, tail.transform.position + (Vector3)tail.tangentOffsetHead - (this.transform.position + (Vector3)this.tangentOffsetTail), length, mask);
        if (!hit) return;

        NodeFunctionality newNode;

        if (ConfigurePulley(hit, out newNode))
        {
            AddBack(newNode);

            if (newNode.GetTail().linkType == LinkType.Rolling)
            {
                TangentCircleCircle(newNode.transform.position, newNode.pulleyRadius, newNode.orientation, out newNode.tangentOffsetTail,
                                    newNode.tail.transform.position, newNode.tail.node.pulleyRadius, newNode.tail.node.orientation, out newNode.tail.tangentOffsetHead);
            }
            else
            {
                TangentPointCircle(newNode.tail.transform.position, newNode.transform.position, newNode.pulleyRadius, newNode.orientation, out newNode.tangentOffsetTail);
            }

            if (this.linkType == LinkType.Rolling)
            {
                TangentCircleCircle(this.transform.position, this.pulleyRadius, this.orientation, out this.tangentOffsetTail,
                                    newNode.transform.position, newNode.pulleyRadius, newNode.orientation, out newNode.tangentOffsetHead);
            }
            else
            {
                TangentPointCircle(this.transform.position, newNode.transform.position, newNode.pulleyRadius, !newNode.orientation, out newNode.tangentOffsetHead);
            }

            newNode.storedLength = 0;

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

    bool ConfigurePulley(RaycastHit2D hit, out NodeFunctionality newNode)
    {
        //bool orientation = Vector2.SignedAngle((Vector2)tail.transform.position - (Vector2)this.transform.position + this.tangentOffsetTail, (Vector2)hit.transform.position + hit.offset - (Vector2)this.transform.position - this.tangentOffsetTail) < 0;
        newNode = null;
        Collider2D coll = hit.collider;

        if (!CMDictionary.CMD.dictionary.ContainsKey(coll))
        {
            return false;
        }
        ChainMesh CMesh = CMDictionary.CMD.dictionary[coll];

        bool orientation = GetOrientation(hit, CMesh);

        print(orientation);

        if (coll is CircleCollider2D)
        {
            CircleCollider2D circleColl = coll.GetComponent<CircleCollider2D>();

            if (circleColl == this.previousPulley || (tail.linkType != LinkType.AnchorStart && circleColl == tail.node.previousPulley))
            {
                newNode = null;
                return false;
            }

            newNode = ((GameObject)Instantiate(nodePrefab, hit.transform.position + (Vector3)circleColl.offset, Quaternion.identity)).GetComponent<NodeFunctionality>();

            newNode.previousPulley = circleColl;

            newNode.pulleyRadius = circleColl.radius + chainWidth / 2;
        }
        else
        {
            bool onSame = this.CMesh == CMesh;

            if (onSame)
            {
                orientation = this.orientation;
            }
            else if (tail.node != null)
            {
                onSame = tail.node.CMesh == CMesh;
                if(onSame)
                {
                    orientation = tail.node.orientation;
                }
            }

            orientation = !orientation;

            Vector2 tangent;

            /*
            if (!FindVertex(CMesh, onSame, ref orientation, out tangent, coll.transform.position, coll.transform.rotation))
            {
                return false;
            }
            

            newNode = ((GameObject)Instantiate(nodePrefab, tangent, Quaternion.identity)).GetComponent<NodeFunctionality>();

            newNode.pulleyRadius = chainWidth/2 + 0.001f;

            newNode.CMesh = CMesh;
            newNode.CMvertex = tangent;
            */
        }

        newNode.orientation = !orientation;

        newNode.GetComponent<CircleCollider2D>().radius = newNode.pulleyRadius + chainWidth / 2;

        newNode.transform.GetChild(0).transform.localScale = new Vector3(newNode.pulleyRadius * 2, newNode.pulleyRadius * 2, 0.1f);

        newNode.slipping = false;

        newNode.previousRotation = Quaternion.identity;

        newNode.chainWidth = this.chainWidth;

        newNode.hingeBody = coll.attachedRigidbody;

        newNode.sm = coll.GetComponent<SurfaceMaster>();
        if (newNode.hingeBody != null)
        {
            newNode.previousHingeRotation = newNode.transform.rotation * Quaternion.Inverse(newNode.hingeBody.transform.rotation);
        }
        else
        {
            newNode.previousHingeRotation = newNode.transform.rotation;
        }

        newNode.name = this.name + (++kin).ToString();

        newNode.linkType = LinkType.Rolling;

        return true;
    }

    bool GetOrientation(RaycastHit2D hit, ChainMesh CMesh)
    {
        Vector2 chainPoint = (((Vector2)tail.transform.position + tail.tangentOffsetHead) - ((Vector2)this.transform.position + this.tangentOffsetTail)) * hit.fraction;

        Vector2 chainPointPrev = ((tail.prevPos + tail.prevTangentHead) - (this.prevPos + this.prevTangentTail)) * hit.fraction;

        Vector2 hitPointPrev = hit.point;

        if (CMesh is ChainMeshDynamic)
        {
            Vector2 hitOffset = hit.point - (Vector2)CMesh.transform.position;

            hitPointPrev = ((ChainMeshDynamic)CMesh).prevRot * (hitOffset + (Vector2)((ChainMeshDynamic)CMesh).prevPos);
        }

        Vector2 hitDirection = (chainPoint - chainPointPrev) - (hit.point - hitPointPrev);

        print(hitDirection.normalized);

        return Vector2.SignedAngle((Vector2)tail.transform.position - (Vector2)this.transform.position, hitDirection) > 0;
    }

    void NodeRemover()
    {
        if (storedLength >= 0) return;

        if (CircleDistance(tangentOffsetHead, tangentOffsetTail, orientation, pulleyRadius) >= 0) return;

        if (tail.linkType == LinkType.AnchorStart)
        {
            head.node.previousPulley = null;
        }
        else
        {
            head.node.previousPulley = tail.node.previousPulley;
        }
        
        head.node.restLength += this.restLength + this.storedLength;

        CutChain();

        if (tail.linkType == LinkType.Rolling)
        {
            if (head.linkType == LinkType.Rolling)
            {
                TangentCircleCircle(head.transform.position, head.node.pulleyRadius, head.node.orientation, out head.tangentOffsetTail,
                                    tail.transform.position, tail.node.pulleyRadius, tail.node.orientation, out tail.tangentOffsetHead);
            }
            else
            {
                TangentPointCircle(head.transform.position, tail.transform.position, tail.node.pulleyRadius, !tail.node.orientation, out tail.tangentOffsetHead);
            }
        }
        else if(head.linkType == LinkType.Rolling)
        {
            TangentPointCircle(tail.transform.position, head.transform.position, head.node.pulleyRadius, head.node.orientation, out head.tangentOffsetTail);
        }

        head.GetComponent<NodeFunctionality>().InitializeNodes();

        Destroy(triggerBox.gameObject);
        Destroy(this.gameObject);
    }
}
