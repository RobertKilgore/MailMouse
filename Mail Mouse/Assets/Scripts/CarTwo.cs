using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class CarTwo : MonoBehaviour
{

    [SerializeField] Transform frontLeftMesh, frontRightMesh, backLeftMesh, backRightMesh;
    [SerializeField] WheelCollider frontLeftColl, frontRightColl, backLeftColl, backRightColl;
    [SerializeField] float motorForce = 1500f, brakeForce = 3000f, steerAngle = 30f;

    private float Throttle, Steering;
    private bool IsBraking;

    public void OnMovement(InputAction.CallbackContext context)
    {

        Vector2 Input = context.ReadValue<Vector2>();

        Steering = Input.x;
        Throttle = Input.y;

    }


    public void OnBraking(InputAction.CallbackContext context)
    {
        IsBraking = context.ReadValueAsButton();
    }

    private void FixedUpdate()
    {
        ApplyTorque();
        ApplySteering();
        ApplyBraking();
        UpdateWheelMeshes();
    }

    void ApplyTorque()
    {
        backLeftColl.motorTorque = Throttle * motorForce;
        backRightColl.motorTorque = Throttle * motorForce;

    }
    void ApplySteering()
    {
        frontLeftColl.steerAngle = Steering * steerAngle;
        frontRightColl.steerAngle = Steering * steerAngle;
    }
    void ApplyBraking()
    {
        float brake = IsBraking ? brakeForce : 0;
        frontLeftColl.brakeTorque = brake;
        frontRightColl.brakeTorque = brake;
        backLeftColl.brakeTorque = brake;
        backRightColl.brakeTorque = brake;


    }
    void UpdateWheelMeshes()
    {
        UpdateWheel(backLeftColl, backLeftMesh);
        UpdateWheel(backRightColl, backRightMesh);
        UpdateWheel(frontLeftColl, frontLeftMesh);
        UpdateWheel(frontRightColl, frontRightMesh);
    }
    void UpdateWheel(WheelCollider col, Transform trans)
    {
        Vector3 pos;
        Quaternion rot;

        col.GetWorldPose(out pos, out rot);

        trans.position = pos;
        trans.rotation = rot;


    }


}






