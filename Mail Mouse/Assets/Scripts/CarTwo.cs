using UnityEngine;
using UnityEngine.InputSystem;

public class CarTwo : MonoBehaviour
{
    [Header("Wheel References")]
    [Tooltip("Visual wheel mesh for the front-left wheel.")]
    [SerializeField] private Transform frontLeftMesh, frontRightMesh, backLeftMesh, backRightMesh;
    [Tooltip("Physics wheel collider for the front-left wheel.")]
    [SerializeField] private WheelCollider frontLeftColl, frontRightColl, backLeftColl, backRightColl;

    [Header("Car Settings")]
    [Tooltip("How much force the rear wheels apply to drive the car forward.")]
    [SerializeField] private float motorForce = 3200f;
    [Tooltip("How strongly the brakes slow the car down.")]
    [SerializeField] private float brakeForce = 9000f;
    [Tooltip("Maximum steering angle for the front wheels.")]
    [SerializeField] private float maxSteerAngle = 20f;
    [Tooltip("Speed at which steering starts to reduce for stability.")]
    [SerializeField] private float maxSpeed = 55f;
    [Tooltip("How strong reverse drive is compared to forward drive.")]
    [SerializeField] private float reverseMultiplier = 0.9f;
    [Tooltip("How quickly input values respond and smooth out.")]
    [SerializeField] private float inputResponse = 14f;
    [Tooltip("How quickly steering changes from one angle to another.")]
    [SerializeField] private float steeringSpeed = 10f;
    [Tooltip("Amount of air/drag resistance applied to the car.")]
    [SerializeField] private float drag = 0.6f;
    [Tooltip("Extra rolling resistance that slows the car while moving.")]
    [SerializeField] private float rollingResistance = 0.2f;
    [Tooltip("Downforce coefficient to keep wheels grounded at high speed.")]
    [SerializeField] private float downforceCoefficient = 0.15f;

    private float throttleInput;
    private float steeringInput;
    private float smoothedThrottle;
    private float smoothedSteering;
    private bool isBraking;
    private Rigidbody carRigidbody;

    private void Awake()
    {
        // Find the rigidbody on this object first, then fall back to a parent rigidbody.
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody == null)
        {
            carRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    private void Start()
    {
        // Set up the wheel colliders with better grip and suspension values.
        ConfigureWheelColliders();
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        // Read the movement input and store it for the physics update.
        Vector2 input = context.ReadValue<Vector2>();
        steeringInput = Mathf.Clamp(input.x, -1f, 1f);
        throttleInput = Mathf.Clamp(input.y, -1f, 1f);
    }

    public void OnBraking(InputAction.CallbackContext context)
    {
        // Convert the brake input button into a simple true/false state.
        isBraking = context.ReadValueAsButton();
    }

    private void FixedUpdate()
    {
        // Smooth the input so the car feels less twitchy and more controlled.
        smoothedThrottle = Mathf.Lerp(smoothedThrottle, throttleInput, inputResponse * Time.fixedDeltaTime);
        smoothedSteering = Mathf.Lerp(smoothedSteering, steeringInput, inputResponse * Time.fixedDeltaTime);

        // Apply the movement systems in the correct order for physics stability.
        ApplyTorque();
        ApplySteering();
        ApplyBraking();
        ApplyDrag();
        ApplyDownforce();
        UpdateWheelMeshes();
    }

    private void ConfigureWheelColliders()
    {
        // Tune each wheel collider with the same settings for consistency.
        ConfigureSingleWheel(frontLeftColl);
        ConfigureSingleWheel(frontRightColl);
        ConfigureSingleWheel(backLeftColl);
        ConfigureSingleWheel(backRightColl);
    }

    private void ConfigureSingleWheel(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return;
        }

        // Increase the wheel grip so the car has better traction and resists skidding at speed.
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.stiffness = 2.5f;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = 2.2f;
        wheel.sidewaysFriction = sidewaysFriction;

        // Tune suspension for better weight transfer and ground contact at high speed.
        JointSpring suspensionSpring = wheel.suspensionSpring;
        suspensionSpring.spring = 55000f;
        suspensionSpring.damper = 7000f;
        wheel.suspensionSpring = suspensionSpring;
        wheel.suspensionDistance = 0.15f;
        wheel.radius = 0.34f;
    }

    private void ApplyTorque()
    {
        if (carRigidbody == null)
        {
            return;
        }

        float currentSpeed = GetForwardSpeed();
        float driveTorque = 0f;

        // Braking overrides throttle so the car stops cleanly instead of rolling backward.
        if (isBraking)
        {
            driveTorque = 0f;
        }
        else
        {
            driveTorque = smoothedThrottle * motorForce;
        }

        // Cap the speed so the car does not accelerate forever.
        if (smoothedThrottle > 0f && currentSpeed > maxSpeed)
        {
            driveTorque = 0f;
        }
        else if (smoothedThrottle < 0f && currentSpeed < -maxSpeed)
        {
            driveTorque = 0f;
        }

        // Apply reverse power at a slightly reduced strength.
        if (!isBraking && smoothedThrottle < 0f)
        {
            driveTorque *= reverseMultiplier;
        }

        backLeftColl.motorTorque = driveTorque;
        backRightColl.motorTorque = driveTorque;
    }

    private void ApplySteering()
    {
        // Apply steering angle smoothly without speed-based reduction.
        float targetAngle = smoothedSteering * maxSteerAngle;
        float smoothAngle = Mathf.Lerp(frontLeftColl.steerAngle, targetAngle, steeringSpeed * Time.fixedDeltaTime);

        frontLeftColl.steerAngle = smoothAngle;
        frontRightColl.steerAngle = smoothAngle;
    }

    private void ApplyBraking()
    {
        // Increase braking strength more at low speed so it feels sharper and more arcade-like.
        float currentSpeed = Mathf.Abs(GetForwardSpeed());
        float lowSpeedBoost = Mathf.Clamp01(1f - (currentSpeed / 12f));
        float brake = isBraking ? brakeForce * (1.2f + (1.4f * lowSpeedBoost)) : 0f;

        frontLeftColl.brakeTorque = brake;
        frontRightColl.brakeTorque = brake;
        backLeftColl.brakeTorque = brake;
        backRightColl.brakeTorque = brake;
    }

    private void ApplyDrag()
    {
        if (carRigidbody == null)
        {
            return;
        }

        // Add a small amount of drag so the car slows down more naturally.
        Vector3 dragForce = -carRigidbody.linearVelocity.normalized * drag;
        Vector3 rollingForce = -carRigidbody.linearVelocity * rollingResistance;
        carRigidbody.AddForce((dragForce + rollingForce) * Time.fixedDeltaTime, ForceMode.Acceleration);
    }

    private void ApplyDownforce()
    {
        if (carRigidbody == null)
        {
            return;
        }

        // Apply downforce proportional to speed squared to keep wheels grounded at high speed.
        float speed = carRigidbody.linearVelocity.magnitude;
        float downforce = speed * speed * downforceCoefficient;
        carRigidbody.AddForce(Vector3.down * downforce * Time.fixedDeltaTime, ForceMode.Acceleration);
    }

    private float GetForwardSpeed()
    {
        if (carRigidbody == null)
        {
            return 0f;
        }

        // Measure the car's forward speed in local space.
        Vector3 localVelocity = transform.InverseTransformDirection(carRigidbody.linearVelocity);
        return localVelocity.z;
    }

    private void UpdateWheelMeshes()
    {
        // Sync the visible wheel models to the wheel colliders so they rotate and move correctly.
        UpdateWheel(backLeftColl, backLeftMesh);
        UpdateWheel(backRightColl, backRightMesh);
        UpdateWheel(frontLeftColl, frontLeftMesh);
        UpdateWheel(frontRightColl, frontRightMesh);
    }

    private void UpdateWheel(WheelCollider col, Transform trans)
    {
        if (col == null || trans == null)
        {
            return;
        }

        // Get the collider's current world position and rotation and copy them to the mesh.
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);

        trans.position = pos;
        trans.rotation = rot;
    }
}


