using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToGoalAgent : Agent
{
    [SerializeField] Transform rewardPosition;
    public Transform targetTransform;
    private Quaternion oldRotation;
    public bool foundTarget;
    [SerializeField] private MeshRenderer groundMesh;
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;

    [SerializeField] RayPerceptionSensorComponent3D rayPerceptionSensor;

    private float previousDistanceToTarget;
    private int stepCount;
    private int rotSpeed = 60;
    private float speed = 2f;
    private Vector3 directionToTarget;
    private float distanceToTarget;
    private float angleToTarget;
    private float targetDetectionRange = 5f;
    private float targetDetectionAngle = 25f;
    private float movementPenalty = -0.001f;
    private float notFoundPenalty = -0.001f;
    private float targetDetectionReward = 0.01f;
    private float backwardPenalty = -0.001f;
    private float stepReward = 0.001f;
    private float distanceRewardScale = 0.01f;
    private int maxSteps = 1500;

    public override void OnEpisodeBegin()
    {
        targetTransform = null;
        foundTarget = false;
        previousDistanceToTarget = float.MaxValue;
        stepCount = 0;

        float randomYaw = Random.Range(0f, 360f);
        Vector3 eulerRotation = new Vector3(0f, randomYaw, 0f);
        transform.rotation = Quaternion.Euler(eulerRotation);

        transform.localPosition = new Vector3(Random.Range(-6f, 6f), 1.5f, Random.Range(-11f, 0f));
        rewardPosition.localPosition = new Vector3(Random.Range(-6f, 6f), 1.5f, Random.Range(-11f, 0f));
    }


public override void CollectObservations(VectorSensor sensor)
{
    oldRotation = transform.rotation;
    foundTarget = false;
    targetTransform = null;

    if (!foundTarget)
    {
        RayPerceptionOutput rayPerceptionOutput = rayPerceptionSensor.RaySensor.RayPerceptionOutput;
        var rayOutputs = rayPerceptionOutput.RayOutputs;

        for (int i = 0; i < rayOutputs.Length; i++)
        {
            if (rayOutputs[i].HasHit)
            {
                if (rayOutputs[i].HitGameObject.tag == "Reward")
                {
                    targetTransform = rayOutputs[i].HitGameObject.transform;
                    foundTarget = true;
                }
            }
        }
    }
    else
    {
        if (Vector3.Distance(targetTransform.localPosition, transform.localPosition) < targetDetectionRange &&
        Mathf.Abs(Vector3.SignedAngle(transform.forward, directionToTarget, transform.up)) <= targetDetectionAngle)
        {
            AddReward(targetDetectionReward / (stepCount + 20));
            foundTarget = true;
        }
        else
        {
            // Add small reward for looking in the direction of the target
            float lookAtTargetReward = 0.001f;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            if (angleToTarget < 90f)
            {
                AddReward(lookAtTargetReward);
            }
        }
        sensor.AddObservation(targetTransform.localPosition);
    }

    sensor.AddObservation(transform.localPosition);
    sensor.AddObservation(transform.rotation);
}
    public override void OnActionReceived(ActionBuffers vectorAction)
    {
        #region axes for input
        var rotation = vectorAction.ContinuousActions[0];
        var fwdAxes = vectorAction.ContinuousActions[1];
        var lateralAxes = vectorAction.ContinuousActions[2];
        #endregion

        if (foundTarget)
        {
            directionToTarget = targetTransform.localPosition - transform.localPosition;
            distanceToTarget = Vector3.Distance(targetTransform.localPosition, transform.localPosition);

            float currentDistanceToTarget = distanceToTarget;

            float distanceChange = previousDistanceToTarget - currentDistanceToTarget;

            float progress = distanceChange / previousDistanceToTarget;

            AddReward(-distanceToTarget);
        }
        else
        {
            AddReward(notFoundPenalty * (stepCount + 1));
        }

        #region Perform rotation and translation
        float rotationAmount = rotation * rotSpeed * Time.deltaTime;
        transform.Rotate(0, rotationAmount, 0);

        float moveAmount = fwdAxes * speed * Time.deltaTime;
        transform.Translate(0, 0, moveAmount, Space.Self);

        float lateralMoveAmount = lateralAxes * speed * Time.deltaTime;
        transform.Translate(lateralMoveAmount, 0, 0, Space.Self);
        #endregion

        // Check if moving backward and apply penalty
        if (Vector3.Dot(transform.forward, directionToTarget) < 0)
        {
            AddReward(backwardPenalty);
        }

        if (foundTarget)
        {
            angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, transform.up);

            if (Mathf.Abs(angleToTarget) <= 25f && Vector3.Dot(transform.forward, directionToTarget) > 0f)
            {
                AddReward(targetDetectionReward);
            }
        }

        AddReward(movementPenalty);
        AddReward(stepReward/(StepCount+1));
        stepCount++;

        // End the episode if max steps reached
        if (stepCount >= maxSteps)
        {
            EndEpisode();
        }
    }


    private void OnTriggerEnter(Collider other)
    {

        if (other.TryGetComponent<Goal>(out Goal goal))
        {

            AddReward(+1f);
            groundMesh.material = winMaterial;
            EndEpisode();
        }


        if (other.TryGetComponent<Wall>(out Wall wall))
        {
            AddReward(-1f);
            groundMesh.material = loseMaterial;
            EndEpisode();
        }
    }
}