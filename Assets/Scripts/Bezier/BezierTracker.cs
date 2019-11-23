﻿using System.Collections;
using UnityEngine;

public class BezierTracker : MonoBehaviour
{
    // Variables to adjust the target position, rotation, and look ahead
    [SerializeField] private float targetOffset = 2;
    [SerializeField] private float targetFactor = 0.5f;
    [SerializeField] private float speedOffset = 3;
    [SerializeField] private float speedFactor = 0.5f;
    [SerializeField] private float lookAheadOffset = 15;

    [SerializeField] private Transform target;

    private float progressDistance;
    private BezierCircuit.TrackPoint progressPoint;
    private Vector3 lastPosition;

    public BezierCircuit circuit;
    public Vector3 targetLookAhead;
    public float speed = 0;

    public bool isMultiplayer = false;
    public Vector3 serverPlayerPosition;
    public Vector3 serverTargetPosition;

    private void Awake()
    {
        // May not need anymore (Use editor for singleplayer, use spawm manager for multiplayer)
        // Find the bezier circuit in the scene
        // circuit = GameObject.FindGameObjectWithTag("Circuit").GetComponent<BezierCircuit>();

        if(isMultiplayer == true)
        {
            RealTimeClient.Instance.StatsUpdate += GetPositions;
            StartCoroutine("UpdateServerStats");
        }

        if(circuit == null)
        {
            Debug.Log("Failed to find circuit gameobject, possible tag missing.");
        }

        // Makes sure everything is inital
        Reset();
    }

    // Resets the progressDistnace to 0
    public void Reset()
    {
        progressDistance = 0;
    }

    private void FixedUpdate()
    {
        // Get a speed based off how much is traveled between frames
        if(Time.fixedDeltaTime > 0)
        {
            speed = Mathf.Lerp(speed, (lastPosition - transform.position).magnitude / Time.fixedDeltaTime,
                               Time.fixedDeltaTime);
        }

        // Change the target position
        target.position = 
            circuit.GetTrackPoint(progressDistance + targetOffset + targetFactor * speed).position;

        // Change the target rotation
        target.rotation =
            Quaternion.LookRotation(
                circuit.GetTrackPoint(progressDistance + speedOffset + speedFactor * speed).direction);

        // Save the look ahead for animation purposes
        targetLookAhead =
            circuit.GetTrackPoint(progressDistance + targetOffset + lookAheadOffset + targetFactor * speed).position;

        // Get the current point on the track and the amount changed
        progressPoint = circuit.GetTrackPoint(progressDistance);
        Vector3 progressDelta = progressPoint.position - transform.position;

        // REORDER LATER TOMORROW
        // Compare progressDelta with server progressDelta
        if(isMultiplayer == true)
        {
            Vector3 serverProgressDelta = serverTargetPosition - serverPlayerPosition;
            if (Vector3.Distance(progressDelta, serverProgressDelta) > 10) //Distance
            {
                transform.position = serverPlayerPosition;
                progressPoint = circuit.GetTrackPoint(serverProgressDelta.magnitude);
                progressDelta = serverProgressDelta;
            }
        }

        // Checks if less than 0 and increment the progressDistance
        if(Vector3.Dot(progressDelta, progressPoint.direction) < 0)
        {
            progressDistance += progressDelta.magnitude * 0.5f;
        }

        // Mod the float as it will keep increase if not
        progressDistance = Mathf.Repeat(progressDistance, circuit.lengths[circuit.bezierCurves.Length - 1]);

        // Save the last position
        lastPosition = transform.position;
    }
    
    private void GetPositions(object sender, StatsUpdateEventArgs e)
    {
        Vector3 newPlayerPos = new Vector3();
        Vector3 newTargetPos = new Vector3();

        for(int i = 0; i < 3; i++)
        {
            newPlayerPos[i] = e.playerPosition[i];
            newTargetPos[i] = e.targetPosition[i];
        }

        serverPlayerPosition = newPlayerPos;
        serverTargetPosition = newTargetPos;
    }

    IEnumerator UpdateServerStats()
    {
        while (true)
        {
            float[] playerPos = { transform.position.x, transform.position.y, transform.position.z };
            float[] targetPos = { progressPoint.position.x, progressPoint.position.y, progressPoint.position.z };

            RealTimeClient.Instance.UpdateStats(Bike.Instance.Count, Bike.Instance.RPM, playerPos, targetPos);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Draw a line to the target position
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);

            // Draw a line to the target lookAhead
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(target.position, targetLookAhead);
        }
    }
}
