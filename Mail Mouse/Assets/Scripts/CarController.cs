using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Setup")]
    [SerializeField] private WheelCollider frontLeftWheel;
    [SerializeField] private WheelCollider frontRightWheel;
    [SerializeField] private WheelCollider rearLeftWheel;
    [SerializeField] private WheelCollider rearRightWheel;

    [SerializeField] private Transform frontLeftVisual;
    [SerializeField] private Transform frontRightVisual;
    [SerializeField] private Transform rearLeftVisual;
    [SerializeField] private Transform rearRightVisual;

    [Header("Driving")]
    [SerializeField] private float motorForce = 1400f;
    [SerializeField] private float brakeForce = 2200f;
    [SerializeField] private float maxSteerAngle = 28f;
    [SerializeField] private float maxSpeed = 18f;
    [SerializeField] private float reverseMultiplier = 0.7f;
    [SerializeField] private float steeringResponse = 4f;
    [SerializeField] private float inputSmoothing = 7f;
    [SerializeField] private float downforce = 18f;
    [SerializeField] private float steeringReductionAtSpeed = 0.2f;
    [SerializeField] private float steeringReductionStartSpeed = 7f;
    [SerializeField] private float yawDamping = 0.98f;
    [SerializeField] private float coastingStopSpeed = 1.2f;
    [SerializeField] private float coastingDeceleration = 7f;

    [Header("Physics")]
    [SerializeField] private float drag = 0.2f;
    [SerializeField] private float angularDrag = 0.4f;
    [SerializeField] private float inputDeadZone = 0.05f;

    private Rigidbody carRigidbody;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction brakeAction;

    private float throttleInput;
    private float steeringInput;
    private float smoothedThrottle;
    private float smoothedSteering;
    private bool brakeHeld;
    private bool showDebugPanel;
    private Vector3 previousVelocity;
    private Vector3 currentAcceleration;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        if (carRigidbody == null)
        {
            Debug.LogWarning("CarController: Rigidbody is missing.", this);
            return;
        }

        carRigidbody.linearDamping = drag;
        carRigidbody.angularDamping = angularDrag;
        previousVelocity = Vector3.zero;
        currentAcceleration = Vector3.zero;
    }

    private void OnEnable()
    {
        BindInputActions();
    }

    private void OnDisable()
    {
        UnbindInputActions();
    }

    private void Start()
    {
        ConfigureWheels();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            showDebugPanel = !showDebugPanel;
        }

        if (playerInput == null || playerInput.actions == null)
        {
            ReadKeyboardFallback();
        }
        else if (moveAction == null)
        {
            ReadKeyboardFallback();
        }

        UpdateVisualWheels();
    }

    private void FixedUpdate()
    {
        if (carRigidbody != null)
        {
            Vector3 currentVelocity = carRigidbody.linearVelocity;
            currentAcceleration = (currentVelocity - previousVelocity) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            previousVelocity = currentVelocity;

            Vector3 angularVelocity = carRigidbody.angularVelocity;
            angularVelocity.y *= yawDamping;
            carRigidbody.angularVelocity = angularVelocity;
        }

        smoothedThrottle = Mathf.Lerp(smoothedThrottle, throttleInput, inputSmoothing * Time.fixedDeltaTime);
        smoothedSteering = Mathf.Lerp(smoothedSteering, steeringInput, inputSmoothing * Time.fixedDeltaTime);

        if (Mathf.Abs(smoothedThrottle) < inputDeadZone)
        {
            smoothedThrottle = 0f;
        }

        if (Mathf.Abs(smoothedSteering) < inputDeadZone)
        {
            smoothedSteering = 0f;
        }

        ApplySteering();
        ApplyDrive();
        ApplyBrakes();
        ApplyCoastingResistance();
        ApplyDownforce();
    }

    private void BindInputActions()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        moveAction = playerInput.actions.FindAction("Move");
        brakeAction = playerInput.actions.FindAction("Brake") ?? playerInput.actions.FindAction("Braking");

        if (moveAction != null)
        {
            moveAction.started += OnMove;
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
        }

        if (brakeAction != null)
        {
            brakeAction.started += OnBrake;
            brakeAction.performed += OnBrake;
            brakeAction.canceled += OnBrake;
        }
    }

    private void UnbindInputActions()
    {
        if (moveAction != null)
        {
            moveAction.started -= OnMove;
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }

        if (brakeAction != null)
        {
            brakeAction.started -= OnBrake;
            brakeAction.performed -= OnBrake;
            brakeAction.canceled -= OnBrake;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        steeringInput = Mathf.Clamp(input.x, -1f, 1f);
        throttleInput = Mathf.Clamp(input.y, -1f, 1f);

        if (Mathf.Abs(steeringInput) < inputDeadZone)
        {
            steeringInput = 0f;
        }

        if (Mathf.Abs(throttleInput) < inputDeadZone)
        {
            throttleInput = 0f;
        }
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        brakeHeld = context.ReadValueAsButton();
    }

    private void ReadKeyboardFallback()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Vector2 keyboardInput = Vector2.zero;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            keyboardInput.x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            keyboardInput.x += 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            keyboardInput.y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            keyboardInput.y -= 1f;
        }

        steeringInput = Mathf.Clamp(keyboardInput.x, -1f, 1f);
        throttleInput = Mathf.Clamp(keyboardInput.y, -1f, 1f);

        if (Mathf.Abs(steeringInput) < inputDeadZone)
        {
            steeringInput = 0f;
        }

        if (Mathf.Abs(throttleInput) < inputDeadZone)
        {
            throttleInput = 0f;
        }

        brakeHeld = keyboard.spaceKey.isPressed || keyboard.leftShiftKey.isPressed || keyboard.leftCtrlKey.isPressed;
    }

    private void ConfigureWheels()
    {
        ConfigureWheel(frontLeftWheel);
        ConfigureWheel(frontRightWheel);
        ConfigureWheel(rearLeftWheel);
        ConfigureWheel(rearRightWheel);
    }

    private void ConfigureWheel(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return;
        }

        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.stiffness = 3.5f;
        forwardFriction.extremumSlip = 0.4f;
        forwardFriction.extremumValue = 1.2f;
        forwardFriction.asymptoteSlip = 0.8f;
        forwardFriction.asymptoteValue = 1.0f;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = 3.2f;
        sidewaysFriction.extremumSlip = 0.35f;
        sidewaysFriction.extremumValue = 1.4f;
        sidewaysFriction.asymptoteSlip = 0.75f;
        sidewaysFriction.asymptoteValue = 1.2f;
        wheel.sidewaysFriction = sidewaysFriction;

        JointSpring spring = wheel.suspensionSpring;
        spring.spring = 55000f;
        spring.damper = 7000f;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;
    }

    private void ApplySteering()
    {
        float currentSpeed = Mathf.Abs(GetForwardSpeed());
        float speedFactor = Mathf.Clamp01((currentSpeed - steeringReductionStartSpeed) / Mathf.Max(maxSpeed - steeringReductionStartSpeed, 0.1f));
        float steeringScale = Mathf.Lerp(1f, steeringReductionAtSpeed, speedFactor);
        float effectiveSteer = smoothedSteering * maxSteerAngle * steeringScale;

        if (frontLeftWheel != null)
        {
            frontLeftWheel.steerAngle = Mathf.Lerp(frontLeftWheel.steerAngle, effectiveSteer, steeringResponse * Time.fixedDeltaTime);
        }

        if (frontRightWheel != null)
        {
            frontRightWheel.steerAngle = Mathf.Lerp(frontRightWheel.steerAngle, effectiveSteer, steeringResponse * Time.fixedDeltaTime);
        }
    }

    private void OnGUI()
    {
        if (!showDebugPanel)
        {
            return;
        }

        float forwardVelocity = GetForwardSpeed();
        float forwardAcceleration = GetForwardAcceleration();
        float speedMagnitude = carRigidbody != null ? carRigidbody.linearVelocity.magnitude : 0f;
        float steeringValue = smoothedSteering;
        float throttleValue = smoothedThrottle;
        bool isBraking = brakeHeld;

        GUI.Box(new Rect(16f, 16f, 320f, 180f), "Car Debug");
        GUI.Label(new Rect(28f, 40f, 290f, 20f), $"Forward Vel: {forwardVelocity:F2} m/s");
        GUI.Label(new Rect(28f, 64f, 290f, 20f), $"Forward Accel: {forwardAcceleration:F2} m/s^2");
        GUI.Label(new Rect(28f, 88f, 290f, 20f), $"Speed: {speedMagnitude:F2} m/s");
        GUI.Label(new Rect(28f, 112f, 290f, 20f), $"Throttle: {throttleValue:F2}");
        GUI.Label(new Rect(28f, 136f, 290f, 20f), $"Steer: {steeringValue:F2}");
        GUI.Label(new Rect(28f, 160f, 290f, 20f), $"Brake: {(isBraking ? "On" : "Off")}");
    }

    private void ApplyDrive()
    {
        if (carRigidbody == null)
        {
            return;
        }

        float currentSpeed = GetForwardSpeed();
        float desiredTorque = smoothedThrottle * motorForce;

        if (Mathf.Abs(smoothedThrottle) < inputDeadZone)
        {
            desiredTorque = 0f;
        }

        if (smoothedThrottle > 0f && currentSpeed >= maxSpeed)
        {
            desiredTorque = 0f;
        }
        else if (smoothedThrottle < 0f && currentSpeed <= -maxSpeed)
        {
            desiredTorque = 0f;
        }

        if (smoothedThrottle < 0f)
        {
            desiredTorque *= reverseMultiplier;
        }

        if (rearLeftWheel != null)
        {
            rearLeftWheel.motorTorque = desiredTorque;
        }

        if (rearRightWheel != null)
        {
            rearRightWheel.motorTorque = desiredTorque;
        }
    }

    private void ApplyBrakes()
    {
        float brakeTorque = brakeHeld ? brakeForce : 0f;

        if (frontLeftWheel != null)
        {
            frontLeftWheel.brakeTorque = brakeTorque;
        }

        if (frontRightWheel != null)
        {
            frontRightWheel.brakeTorque = brakeTorque;
        }

        if (rearLeftWheel != null)
        {
            rearLeftWheel.brakeTorque = brakeTorque;
        }

        if (rearRightWheel != null)
        {
            rearRightWheel.brakeTorque = brakeTorque;
        }
    }

    private void ApplyCoastingResistance()
    {
        if (carRigidbody == null)
        {
            return;
        }

        if (brakeHeld || Mathf.Abs(smoothedThrottle) > inputDeadZone)
        {
            return;
        }

        float forwardSpeed = Mathf.Abs(GetForwardSpeed());
        if (forwardSpeed <= coastingStopSpeed)
        {
            float decel = coastingDeceleration * Time.fixedDeltaTime;
            float newSpeed = Mathf.Max(0f, forwardSpeed - decel);
            float speedDirection = Mathf.Sign(GetForwardSpeed());
            if (speedDirection == 0f)
            {
                speedDirection = 1f;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(carRigidbody.linearVelocity);
            localVelocity.z = speedDirection * newSpeed;
            carRigidbody.linearVelocity = transform.TransformDirection(localVelocity);
            return;
        }

        if (forwardSpeed > coastingStopSpeed * 4f)
        {
            return;
        }

        float speedRatio = Mathf.Clamp01((forwardSpeed - coastingStopSpeed) / Mathf.Max((coastingStopSpeed * 4f) - coastingStopSpeed, 0.01f));
        float extraDecel = coastingDeceleration * (1f - speedRatio);
        float decelerationForce = extraDecel * Time.fixedDeltaTime;
        float newForwardSpeed = Mathf.Max(0f, forwardSpeed - decelerationForce);
        float coastingDirection = Mathf.Sign(GetForwardSpeed());
        if (coastingDirection == 0f)
        {
            coastingDirection = 1f;
        }

        Vector3 coastingLocalVelocity = transform.InverseTransformDirection(carRigidbody.linearVelocity);
        coastingLocalVelocity.z = coastingDirection * newForwardSpeed;
        carRigidbody.linearVelocity = transform.TransformDirection(coastingLocalVelocity);
    }

    private void ApplyDownforce()
    {
        if (carRigidbody == null)
        {
            return;
        }

        float speed = carRigidbody.linearVelocity.magnitude;
        carRigidbody.AddForce(-transform.up * downforce * speed, ForceMode.Acceleration);
    }

    private float GetForwardSpeed()
    {
        if (carRigidbody == null)
        {
            return 0f;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(carRigidbody.linearVelocity);
        return localVelocity.z;
    }

    private float GetForwardAcceleration()
    {
        if (carRigidbody == null)
        {
            return 0f;
        }

        Vector3 localAcceleration = transform.InverseTransformDirection(currentAcceleration);
        return localAcceleration.z;
    }

    private void UpdateVisualWheels()
    {
        UpdateWheelVisual(frontLeftWheel, frontLeftVisual);
        UpdateWheelVisual(frontRightWheel, frontRightVisual);
        UpdateWheelVisual(rearLeftWheel, rearLeftVisual);
        UpdateWheelVisual(rearRightWheel, rearRightVisual);
    }

    private void UpdateWheelVisual(WheelCollider wheelCollider, Transform visual)
    {
        if (wheelCollider == null || visual == null)
        {
            return;
        }

        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        visual.position = position;
        visual.rotation = rotation;
    }
}
