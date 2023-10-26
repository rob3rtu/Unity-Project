using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

public class FlightPhysics : MonoBehaviour
{
    [Tooltip("The maximum thrust of the engine")]
    [SerializeField]
    private float _maxThrust;
    
    [Range(0,1)]
    [Tooltip("The fraction of the Maximum Thrust currently applied")]
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

    [Tooltip("Coefficinets for Angular Drag in the 3 main axes")]
    [SerializeField]
    private Vector3 _angularDragCoefficients;

    [Tooltip("Curve describing Lift Coefficient vs Angle of Attack")]
    [SerializeField]
    private AnimationCurve _liftCoefficientCurve;

    [Tooltip("Curve describing Drag Coefficient vs Speed")]
    [SerializeField]
    private AnimationCurve _dragCoefficientCurve;


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

    // Update is called once per frame
    //THIS IS TEMPORARY CODE FOR THE PURPOSE OF TESTING THE PHYSICS!!!!
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            _throttle = Mathf.Clamp01(_throttle + 0.001f);
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _throttle = Mathf.Clamp01(_throttle - 0.001f);
        }

        if (Input.GetKey(KeyCode.F))
        {
            _flap = 5f * Mathf.Deg2Rad;
        }
        else
        {
            _flap = 0f;
        }

        if(Input.GetKey(KeyCode.S)) {
            ApplyTorque(Vector3.left, 0.003f);
        }
        if (Input.GetKey(KeyCode.W))
        {
            ApplyTorque(Vector3.right, 0.003f);
        }
        if (Input.GetKey(KeyCode.A))
        {
            ApplyTorque(Vector3.down, 0.003f);
        }
        if (Input.GetKey(KeyCode.D))
        {
            ApplyTorque(Vector3.up, 0.003f);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            ApplyTorque(Vector3.forward, 0.01f);
        }
        if (Input.GetKey(KeyCode.E))
        {
            ApplyTorque(Vector3.back, 0.01f);
        }
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
        _rigidBody.AddRelativeForce(_throttle * _maxThrust * Vector3.forward);

    private void ApplyLift()
    {
        //Applying wing lift
        Vector3 wingLift = ComputeLiftAndInducedDrag(_angleOfAttack + _flap, Vector3.right, _liftCoefficientCurve, _wingPower, _inducedDragFactor);
        _rigidBody.AddRelativeForce(wingLift);
        
        //Applying rudder "lift"
        Vector3 rudderLift = ComputeLiftAndInducedDrag(_sideslipAngle, Vector3.up, _liftCoefficientCurve, _rudderPower, _inducedDragFactor);
        _rigidBody.AddRelativeForce(rudderLift);
    
    }

    private Vector3 ComputeLiftAndInducedDrag(float angle, Vector3 rightAxis, AnimationCurve coefficientCurve, float liftFactor, float inducedDragFactor)
    {
        //Determine the Lift Coefficient for the given Attack Angle
        float liftCoefficient = coefficientCurve.Evaluate(angle * Mathf.Rad2Deg);

        //We only consider the Velocity projection onto the plane normal to rightAxis
        Vector3 liftVelocity = Vector3.ProjectOnPlane(_velocity, rightAxis);
        
        //Lift is perpendicular to the movement direction
        Vector3 liftDirection = Vector3.Cross(liftVelocity, rightAxis).normalized;
        float liftMagnitude = liftCoefficient * liftVelocity.sqrMagnitude * liftFactor;

        Vector3 lift = liftMagnitude * liftDirection;

        //Induced Drag Coefficient can be approximated in terms of the Lift Coefficient
        float inducedDragCoefficient = liftCoefficient * liftCoefficient * _inducedDragFactor;
        
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

    //TEMP CODE
    private void ApplyTorque(Vector3 dir, float mult = 1f)
    {
        _rigidBody.AddRelativeTorque(dir * mult, ForceMode.VelocityChange);
    }

}
