using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    public enum ForwardAxis { ZPlus, ZMinus, XPlus, XMinus }

    [Header("Move")]
    [SerializeField] float maxForwardSpeed = 18f;
    [SerializeField] float maxReverseSpeed = 6f;
    [SerializeField] float acceleration = 12f;
    [SerializeField] float brakeForce = 18f;
    [SerializeField] float handbrakeDrag = 4f;

    [Header("Steer")]
    [SerializeField] float maxSteerAngleDeg = 28f;
    [SerializeField] float steerResponsiveness = 6f;
    [SerializeField] float steerStability = 3f;

    [Header("Grip / Damping")]
    [SerializeField] float lateralFriction = 6f;
    [SerializeField] float baseLinearDamping = 0.05f;
    [SerializeField] float angularDampingOnGround = 2f;

    [Header("Grounding")]
    [SerializeField] Transform groundRayOrigin;
    [SerializeField] float groundRayLength = 1.2f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Orientation")]
    [SerializeField] ForwardAxis forwardAxis = ForwardAxis.ZPlus;

    [Header("Center Of Mass")]
    [SerializeField] Transform com;

    [Header("Inputs")]
    [SerializeField] KeyCode handbrakeKey = KeyCode.Space;

    [Header("Control")]
    public bool controlsEnabled = false;

    Rigidbody rb;
    float steerInput;
    float throttleInput;

    public void SetControlEnabled(bool on) => controlsEnabled = on;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = baseLinearDamping;
        rb.angularDamping = angularDampingOnGround;
        if (com) rb.centerOfMass = transform.InverseTransformPoint(com.position);
    }

    void Update()
    {
        if (!controlsEnabled) { steerInput = 0f; throttleInput = 0f; return; }
        steerInput = Input.GetAxisRaw("Horizontal");
        throttleInput = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        bool grounded = CheckGrounded();

        Vector3 fwd, right, up;
        GetBasis(out fwd, out right, out up);

        Vector3 v = rb.linearVelocity;
        float vF = Vector3.Dot(v, fwd);
        float vR = Vector3.Dot(v, right);
        float vU = Vector3.Dot(v, up);

        if (grounded)
        {
            bool handbrake = Input.GetKey(handbrakeKey);

            float maxF = (throttleInput >= 0f) ? maxForwardSpeed : maxReverseSpeed;
            float speedRatio = Mathf.InverseLerp(0f, maxF, Mathf.Abs(vF));
            float accelScale = Mathf.Lerp(1f, 0.25f, speedRatio);

            float desiredAcc = 0f;
            if (!handbrake)
            {
                desiredAcc = throttleInput * acceleration * accelScale;
                if (Mathf.Sign(throttleInput) != Mathf.Sign(vF) && Mathf.Abs(vF) > 0.5f)
                    desiredAcc -= Mathf.Sign(vF) * brakeForce;
            }
            else
            {
                ApplyExtraDrag(handbrakeDrag);
            }

            rb.AddForce(fwd * desiredAcc, ForceMode.Acceleration);

            v = rb.linearVelocity;
            vF = Vector3.Dot(v, fwd);
            vR = Vector3.Dot(v, right);
            vU = Vector3.Dot(v, up);

            float capF = maxForwardSpeed;
            float capR = maxReverseSpeed;
            if (vF > capF) vF = Mathf.Lerp(vF, capF, 0.2f);
            if (vF < -capR) vF = Mathf.Lerp(vF, -capR, 0.2f);
            rb.linearVelocity = fwd * vF + right * vR + up * vU;

            rb.AddForce(-right * vR * lateralFriction, ForceMode.Acceleration);

            float steerAngleNow = maxSteerAngleDeg * steerInput *
                                  Mathf.Lerp(1f, 0.4f, Mathf.InverseLerp(0f, maxForwardSpeed, Mathf.Abs(vF)));
            float sign = (Mathf.Abs(vF) < 0.1f) ? 1f : Mathf.Sign(vF);
            float yawDelta = steerAngleNow * steerResponsiveness * sign * Time.fixedDeltaTime;
            Quaternion q = Quaternion.AngleAxis(yawDelta, up);
            rb.MoveRotation(rb.rotation * Quaternion.Slerp(Quaternion.identity, q, Time.fixedDeltaTime * steerStability));
        }
        else
        {
            rb.linearDamping = baseLinearDamping * 0.5f;
        }
    }

    bool CheckGrounded()
    {
        if (!groundRayOrigin) groundRayOrigin = transform;
        float radius = 0.45f;
        Vector3 origin = groundRayOrigin.position + Vector3.up * 0.2f;
        bool hit = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit _, groundRayLength, groundMask, QueryTriggerInteraction.Ignore);
        rb.angularDamping = hit ? angularDampingOnGround : 0.1f;
        rb.linearDamping = baseLinearDamping;
        return hit;
    }

    void ApplyExtraDrag(float extra)
    {
        if (rb.linearVelocity.sqrMagnitude < 0.0001f) return;
        Vector3 brake = -rb.linearVelocity.normalized * extra;
        rb.AddForce(brake, ForceMode.Acceleration);
    }

    void GetBasis(out Vector3 fwd, out Vector3 right, out Vector3 up)
    {
        up = transform.up;
        switch (forwardAxis)
        {
            case ForwardAxis.ZPlus: fwd = transform.forward; right = transform.right; break;
            case ForwardAxis.ZMinus: fwd = -transform.forward; right = -transform.right; break;
            case ForwardAxis.XPlus: fwd = transform.right; right = -transform.forward; break;
            case ForwardAxis.XMinus: fwd = -transform.right; right = transform.forward; break;
            default: fwd = transform.forward; right = transform.right; break;
        }
    }
}
