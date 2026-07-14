using UnityEngine;
using UnityEngine.InputSystem;

public class CarArcade : MonoBehaviour
{
    [Header("Wheel References")]
    [SerializeField] private WheelCollider frontLeftColl, frontRightColl, backLeftColl, backRightColl;
    [SerializeField] private Transform frontLeftMesh, frontRightMesh, backLeftMesh, backRightMesh;

    [Header("Acceleration & Speed")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 120f;
    [SerializeField] private float brakePower = 80f;

    [Header("Steering")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float steerResponseSpeed = 15f;
    [SerializeField] private float speedSteerReduction = 0.3f; // Steering reduced at high speed

    [Header("Drift & Slide")]
    [SerializeField] private float driftFriction = 0.4f; // Lower = more slidey
    [SerializeField] private float normalFriction = 1.8f;
    [SerializeField] private float driftThreshold = 15f; // Angle to trigger drift

    [Header("Feel")]
    [SerializeField] private float downforce = 1f;
    [SerializeField] private float airDrag = 0.1f; // Light air resistance only

    private float throttleInput;
    private float steerInput;
    private float currentSteerAngle;
    private Rigidbody carRigidbody;
    private bool isBraking;
    private float previousSpeed;
    private float currentAcceleration;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody == null)
            carRigidbody = GetComponentInParent<Rigidbody>();
    }

    private void Start()
    {
        ConfigureWheels();
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        steerInput = input.x;
        throttleInput = input.y;
    }

    public void OnBraking(InputAction.CallbackContext context)
    {
        isBraking = context.ReadValueAsButton();
    }

    private void FixedUpdate()
    {
        if (carRigidbody == null)
            return;

        float currentSpeed = GetForwardSpeed();
        currentAcceleration = (currentSpeed - previousSpeed) / Time.fixedDeltaTime;
        previousSpeed = currentSpeed;
        
        ApplySteering(currentSpeed);
        ApplyThrottle(currentSpeed);
        ApplyBraking();
        ApplyDownforce();
        ApplyDrag();
        UpdateWheelMeshes();
    }

    private void ConfigureWheels()
    {
        foreach (WheelCollider wheel in new[] { frontLeftColl, frontRightColl, backLeftColl, backRightColl })
        {
            if (wheel == null) continue;

            // Forward friction - handles acceleration grip and rolling resistance
            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.stiffness = normalFriction;
            forward.asymptoteSlip = 0.5f;  // Rolling resistance kicks in
            forward.asymptoteValue = 0.3f; // Rolling resistance strength
            wheel.forwardFriction = forward;

            // Sideways friction - handles cornering grip and drifting
            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = normalFriction;
            wheel.sidewaysFriction = sideways;

            // Suspension for ground contact
            JointSpring suspension = wheel.suspensionSpring;
            suspension.spring = 35000f;
            suspension.damper = 4000f;
            suspension.targetPosition = 0.5f;
            wheel.suspensionSpring = suspension;
            wheel.suspensionDistance = 0.3f;
        }
    }

    private void ApplySteering(float currentSpeed)
    {
        // Smoothly interpolate steering
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, steerInput, steerResponseSpeed * Time.fixedDeltaTime);

        // Reduce steering at high speed (speed above ~60% max)
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / (maxSpeed * 0.6f));
        float steerReduction = Mathf.Lerp(1f, 1f - speedSteerReduction, speedFactor);
        float finalSteerAngle = currentSteerAngle * maxSteerAngle * steerReduction;

        frontLeftColl.steerAngle = finalSteerAngle;
        frontRightColl.steerAngle = finalSteerAngle;

        // Apply drift friction if sliding
        float slipAngle = Mathf.Abs(GetSlipAngle());
        float frictionAmount = slipAngle > driftThreshold ? driftFriction : normalFriction;

        foreach (WheelCollider wheel in new[] { frontLeftColl, frontRightColl, backLeftColl, backRightColl })
        {
            if (wheel == null) continue;
            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = frictionAmount;
            wheel.sidewaysFriction = sideways;
        }
    }

    private void ApplyThrottle(float currentSpeed)
    {
        float throttle = isBraking ? 0f : throttleInput;

        // Don't accelerate beyond max speed
        if (throttle > 0 && currentSpeed >= maxSpeed)
            throttle = 0f;
        else if (throttle < 0 && currentSpeed <= -maxSpeed * 0.6f)
            throttle = 0f;

        float motorTorque = throttle * acceleration * 100f;
        backLeftColl.motorTorque = motorTorque;
        backRightColl.motorTorque = motorTorque;
    }

    private void ApplyBraking()
    {
        if (!isBraking)
        {
            // No brakes - wheels handle deceleration via their friction curves
            frontLeftColl.brakeTorque = 0f;
            frontRightColl.brakeTorque = 0f;
            backLeftColl.brakeTorque = 0f;
            backRightColl.brakeTorque = 0f;
            return;
        }

        // Apply full brakes
        float brake = brakePower * 100f;
        frontLeftColl.brakeTorque = brake;
        frontRightColl.brakeTorque = brake;
        backLeftColl.brakeTorque = brake;
        backRightColl.brakeTorque = brake;
    }

    private void ApplyDownforce()
    {
        float speed = carRigidbody.linearVelocity.magnitude;
        carRigidbody.AddForce(Vector3.down * speed * downforce, ForceMode.Acceleration);
    }

    private void ApplyDrag()
    {
        if (carRigidbody == null)
            return;

        // Light air resistance only - wheels handle the rest via friction
        Vector3 airResistance = -carRigidbody.linearVelocity * airDrag;
        carRigidbody.AddForce(airResistance, ForceMode.Acceleration);
    }

    private float GetForwardSpeed()
    {
        if (carRigidbody == null)
            return 0f;

        Vector3 localVelocity = transform.InverseTransformDirection(carRigidbody.linearVelocity);
        return localVelocity.z;
    }

    private float GetSlipAngle()
    {
        if (carRigidbody == null)
            return 0f;

        Vector3 velocity = carRigidbody.linearVelocity;
        if (velocity.magnitude < 0.1f)
            return 0f;

        Vector3 forward = transform.forward;
        float slipAngle = Vector3.SignedAngle(forward, velocity, Vector3.up);
        return slipAngle;
    }

    private void UpdateWheelMeshes()
    {
        UpdateWheel(backLeftColl, backLeftMesh);
        UpdateWheel(backRightColl, backRightMesh);
        UpdateWheel(frontLeftColl, frontLeftMesh);
        UpdateWheel(frontRightColl, frontRightMesh);
    }

    private void UpdateWheel(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null)
            return;

        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    private void OnGUI()
    {
        float currentSpeed = GetForwardSpeed();


        string debugText = 
            $"Speed: {currentSpeed:F1}\n" +
            $"Acceleration: {acceleration:F1}";
        
        GUI.Label(new Rect(10, 10, 200, 100), debugText);
    }
}
