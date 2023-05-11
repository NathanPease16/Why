using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum FinchState
{
    InBox,
    LeavingBox,
    SearchingForTarget,
    GoingToTarget,
    GoingBehindTarget,
    PushingBox,


}

public class Finch : MonoBehaviour
{
    public LayerMask nonCubes;

    private const float DISTANCE_TOLERANCE = 0.1f;
    private const float BOX_SIZE = 10f;
    private const float MOVE_SPEED = 5f;
    private const float ROTATE_SPEED = 15f;
    private const float BOX_CORRECTION = 20f;
    private const float TARGET_SIZE = 1f;
    private const float TARGET_SIZE_TOLERANCE = 0.2f;
    private const float DISTANCE_FROM_OBJECT = 3;
    private const float TURN_ANGLE = 20;

    private Rigidbody rb;


    private float previousDistance = Mathf.Infinity;
    private float edgeA = -1;
    private float edgeB = -1;
    private float localTolerance;
    private float angle;
    private float estObjectSize;

    private Vector3 previousPosition;
    private float distanceTraveled;

    private int direction;

    FinchState state;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        state = FinchState.InBox;
    }

    void Update()
    {
        switch (state)
        {
            case FinchState.InBox:
            {
                if (GetDistance() > BOX_SIZE)
                {
                    transform.Rotate(Vector3.down * BOX_CORRECTION);
                    state = FinchState.LeavingBox;
                }
                else
                    Rotate();
                break;
            }
            case FinchState.LeavingBox:
            {
                if (CheckForEdge())
                    Move(MOVE_SPEED);
                else
                {
                    state = FinchState.SearchingForTarget;
                    Move(0);
                }
                break;
            }
            case FinchState.SearchingForTarget:
            {
                bool target = FindTarget();

                if (target)
                {
                    state = FinchState.GoingToTarget;
                    transform.Rotate(Vector3.up * (angle / 2));
                }

                break;
            }
            case FinchState.GoingToTarget:
            {
                if (GetDistance() <= DISTANCE_FROM_OBJECT)  
                {
                    Move(0);
                    transform.Rotate(Vector3.up * ((angle / 2) + TURN_ANGLE));
                    previousPosition = transform.position;
                    state = FinchState.GoingBehindTarget;
                }
                else
                    Move(MOVE_SPEED);

                break;
            }
            case FinchState.GoingBehindTarget:
            {   
                Move(MOVE_SPEED);
                distanceTraveled += Vector3.Distance(previousPosition, transform.position);

                if (distanceTraveled >= DISTANCE_FROM_OBJECT + TARGET_SIZE)
                {
                    Move(0);
                    //transform.Rotate(Vector3.down * (angle * (DISTANCE_FROM_OBJECT + TARGET_SIZE) * 2 + (TURN_ANGLE * 2)));
                    transform.Rotate(Vector3.down * (90 + TURN_ANGLE + angle));
                    state = FinchState.PushingBox;
                }

                previousPosition = transform.position;

                break;
            }
            case FinchState.PushingBox:
            {
                if (CheckForEdge())
                    Move(MOVE_SPEED);
                else
                {
                    //state = FinchState.SearchingForTarget;
                    Move(0);
                }

                break;
            }
        }
        
        Debug.DrawRay(transform.position, transform.forward * 500, Color.green);
    }

    private void Rotate()
    {
        transform.Rotate(Vector3.down * ROTATE_SPEED * Time.deltaTime);
    }

    private void Move(float speed)
    {
        rb.velocity = transform.forward * speed;
    }

    private float GetDistance()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, transform.forward, out hit);

        float dist = hit.distance;

        if (dist == 0 && hit.collider == null)
            dist = Mathf.Infinity;

        return dist;
    }

    private bool CheckForEdge()
    {
        Vector3 position = transform.position + (transform.forward/2);

        return Physics.Raycast(position, Vector3.down, .6f, nonCubes);
    }

    private bool FindTarget()
    {

        RaycastHit hit;
        Physics.Raycast(transform.position, transform.forward, out hit);

        float distance = hit.distance;

        if (distance == 0 && hit.collider == null)
            distance = Mathf.Infinity;

        float change = Mathf.Abs(previousDistance - distance);

        if (change < DISTANCE_TOLERANCE)
        {
            if (edgeA < 0)
            {
                edgeB = -1;
                angle = 0;
                edgeA = previousDistance;
            }
        }
        if (change > DISTANCE_TOLERANCE && edgeA > 0)
        {
            if (edgeB < 0)
                edgeB = previousDistance;
        }
        
        if (edgeB < 0)
        {
            Rotate();
            angle += (Vector3.up * ROTATE_SPEED * Time.deltaTime).y;
        }
        else
        {
            estObjectSize = Mathf.Sqrt(edgeA * edgeA + edgeB * edgeB - 2 * edgeA * edgeB * Mathf.Cos(Mathf.Deg2Rad * angle));
            float diff = edgeB - edgeA;

            estObjectSize -= 1.76310598891f * diff;

            previousDistance = Mathf.Infinity;
            edgeA = -1;
            edgeB = -1;

            Debug.Log("Estimated Size: " + estObjectSize);

            if (Mathf.Abs(TARGET_SIZE - estObjectSize) <= TARGET_SIZE_TOLERANCE)
                return true;
                
        }
        
        previousDistance = distance;

        return false;
    }
}
