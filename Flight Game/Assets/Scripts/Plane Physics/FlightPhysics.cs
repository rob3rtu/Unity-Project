using System;
using UnityEngine;

public class FlightPhysics : MonoBehaviour
{
    [Tooltip("The maximum thrust generated by the engine")]
    [SerializeField]
    private float _maxEnginePower;

    [Range(0, 1)]
    [Tooltip("The fraction of the Engine Power currently applied")]
    [SerializeField]
    private float _throttle;

    [Tooltip("Parameter that affects Lift generated by the wings")]
    [SerializeField]
    private float _wingPower;

    [Tooltip("Parameter that affects 'Lift' generaed by the rudder")]
    [SerializeField]
    private float _rudderPower;

    [Tooltip("Parameter that affects Induced Drag, a type of Drag caused by Lift")]
    [SerializeField]
    private float _inducedDragFactor;

    [Tooltip("Coefficients for Angular Drag in the 3 main axes")]
    [SerializeField]
    private Vector3 _angularDragCoefficients;

    [Tooltip("Curve describing Lift Coefficient vs Angle of Attack")]
    [SerializeField]
    private AnimationCurve _liftCoefficientCurve;

    [Tooltip("Curve describing Drag Coefficient vs Speed")]
    [SerializeField]
    private AnimationCurve _dragCoefficientCurve;

    [Tooltip("The plane limits on steering")]
    [SerializeField]
    private Vector3 _steeringLimits;

    //Both velocity and angular velocity are in Local/Model Space
    //So if our object is normally _, but rotated |, UP is always (0,1,0) in Local Space, but (-1,0,0) in World Space
    private Vector3 _velocity;
    private Vector3 _angularVelocity;

    private float _angleOfAttack;
    private float _sideslipAngle;

    private Rigidbody _rigidBody;

    //TEMP FOR TESTING PURPOSES
    private float _flap;

    // Start is called before the first frame update
    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _velocity = Vector3.zero;
        _angularVelocity = Vector3.zero;
    }

    // FixedUpdate is called at a fixed rate
    void FixedUpdate()
    {
        DetermineState();

        ApplyThrust();
        ApplyLift();
        ApplyDrag();
    }

    public void AddThrottle(float amount)
    {
        _throttle = Mathf.Clamp01(_throttle + amount);
    }

    private void DetermineState()
    {
        //Transforming velocities to Local Space
        _velocity = transform.InverseTransformDirection(_rigidBody.velocity);
        _angularVelocity = transform.InverseTransformDirection(_rigidBody.angularVelocity);

        DetermineAngles();
    }

    private void DetermineAngles()
    {
        //Pitch angle
        _angleOfAttack = Mathf.Atan2(-_velocity.y, _velocity.z);
        //Yaw angle
        _sideslipAngle = Mathf.Atan2(_velocity.x, _velocity.z);
    }

    private void ApplyThrust() =>
          _rigidBody.AddRelativeForce(_throttle * _maxEnginePower * Vector3.forward);
    private void ApplyLift()
    {
        //Applying wing lift
        Vector3 wingLift = ComputeLiftAndInducedDrag(_angleOfAttack + _flap, Vector3.right, _liftCoefficientCurve, _wingPower, _inducedDragFactor);
        _rigidBody.AddRelativeForce(wingLift);

        //Applying rudder "lift"
        Vector3 rudderLift = ComputeLiftAndInducedDrag(_sideslipAngle, Vector3.up, _liftCoefficientCurve, _rudderPower, _inducedDragFactor);
        _rigidBody.AddRelativeForce(rudderLift);

    }
    /// <summary>
    /// Returns the combined effects of lift and induced drag
    /// </summary>
    /// <param name="angle">The angle of attack/sideslip angle of the plane</param>
    /// <param name="wingAxis">The direction of the 'wing', should be Vector3.right for wings and Vector3.up for rudder</param>
    /// <param name="coefficientCurve">The curve describing Lift Coefficient vs Angle</param>
    /// <param name="liftFactor">Controlls lift power</param>
    /// <param name="inducedDragFactor">Controlls induced drag power</param>
    /// <returns></returns>
    private Vector3 ComputeLiftAndInducedDrag(float angle, Vector3 wingAxis, AnimationCurve coefficientCurve, float liftFactor, float inducedDragFactor)
    {
        //Determine the Lift Coefficient for the given Attack Angle
        float liftCoefficient = coefficientCurve.Evaluate(angle * Mathf.Rad2Deg);

        //We only consider the Velocity projection onto the plane normal to wingAxis
        Vector3 liftVelocity = Vector3.ProjectOnPlane(_velocity, wingAxis);

        //Lift is perpendicular to the movement direction
        Vector3 liftDirection = Vector3.Cross(liftVelocity, wingAxis).normalized;
        float liftMagnitude = liftCoefficient * liftVelocity.sqrMagnitude * liftFactor;

        Vector3 lift = liftMagnitude * liftDirection;

        //Induced Drag Coefficient can be approximated in terms of the Lift Coefficient
        float inducedDragCoefficient = liftCoefficient * liftCoefficient * inducedDragFactor;

        Vector3 dragDirection = -liftVelocity.normalized;
        float dragMagnitude = inducedDragCoefficient * liftVelocity.sqrMagnitude;

        Vector3 drag = dragMagnitude * dragDirection;

        return lift + drag;
    }

    private void ApplyDrag()
    {
        //Applying Parasitic Drag
        float dragCoefficient = _dragCoefficientCurve.Evaluate(_velocity.magnitude);
        Vector3 parasiticDrag = dragCoefficient * _velocity.sqrMagnitude * -_velocity.normalized;
        _rigidBody.AddRelativeForce(parasiticDrag);

        //Applying Angular Drag
        Vector3 angularDrag = Vector3.Scale(_angularVelocity.sqrMagnitude * -_angularVelocity.normalized, _angularDragCoefficients);
        _rigidBody.AddRelativeTorque(angularDrag, ForceMode.Acceleration);
    }

    /// <summary>
    /// Applies a force to the object making it rotate along the Z axis
    /// </summary>
    /// <param name="magnitude">The strength of the force. If negative, the object rolls to the right</param>
    /// <returns></returns>
    public void ApplyRollTorque(float magnitude = 1f)
    {
        magnitude = Mathf.Clamp(magnitude, -_steeringLimits.z, _steeringLimits.z);
        _rigidBody.AddRelativeTorque(Vector3.forward * magnitude, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Applies a force to the object making it rotate along the X axis
    /// </summary>
    /// <param name="magnitude">The strength of the force. If negative, the object pitches down</param>
    /// <returns></returns>
    public void ApplyPitchTorque(float magnitude = 1f)
    {
        magnitude = Mathf.Clamp(magnitude, -_steeringLimits.x, _steeringLimits.x);
        _rigidBody.AddRelativeTorque(Vector3.right * magnitude, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Applies a force to the object making it rotate along the Y axis
    /// </summary>
    /// <param name="magnitude">The strength of the force. If negative, the object steers left</param>
    /// <returns></returns>
    public void ApplyYawTorque(float magnitude = 1f)
    {
        magnitude = Mathf.Clamp(magnitude, -_steeringLimits.y, _steeringLimits.y);
        _rigidBody.AddRelativeTorque(Vector3.up * magnitude, ForceMode.VelocityChange);
    }

    public void SetFlapRad(float angle = 0f)
    {
        _flap = angle;
    }
}
