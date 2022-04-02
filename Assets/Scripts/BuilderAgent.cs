using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BuilderAgent : Agent
{
    public Transform Builder;

    public GameObject Start;
    public GameObject Target;
    public GameObject supportPrefab;
    // public GameObject nodePrefab;
    public GameObject plankXPrefab;
    public GameObject plankZPrefab;

    GameObject currentClosestObject;
    GameObject objectPretender;
    GameObject lastObject = null;
    float distanceToTarget;
    float initialDistanceToTarget;

    List<GameObject> supports = new List<GameObject>(); 
    // List<GameObject> nodes = new List<GameObject>(); 
    List<GameObject> xPlanks = new List<GameObject>(); 
    List<GameObject> zPlanks = new List<GameObject>(); 

    void ResetEnv()
    {
        foreach(GameObject u in supports) { Destroy(u); }
        supports.Clear();

        // foreach(GameObject u in nodes) { Destroy(u); }
        // nodes.Clear();

        foreach(GameObject u in xPlanks) { Destroy(u); }
        xPlanks.Clear();

        foreach(GameObject u in zPlanks) { Destroy(u); }
        zPlanks.Clear();

        // Builder.localPosition = Start.transform.localPosition + new Vector3(0, 2f, 0);
        this.transform.localPosition = Start.transform.localPosition; //  + new Vector3(0, 2f, 0);

        currentClosestObject = Start;
        distanceToTarget = Vector3.Distance(currentClosestObject.transform.localPosition, Target.transform.localPosition);
        initialDistanceToTarget = distanceToTarget;
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        ResetEnv();
        // Debug.Log("New episode begins.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Target.transform.localPosition);
        sensor.AddObservation(this.transform.localPosition);
    }

    public GameObject DoAction(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var action = act[0];
        // var position = Builder.localPosition;
        var position = this.transform.localPosition;

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 0.5f;
                lastObject = null;
                break;
            case 2:
                dirToGo = transform.forward * -0.5f;
                lastObject = null;
                break;
            case 3:
                dirToGo = transform.right * -0.5f;
                lastObject = null;
                break;
            case 4:
                dirToGo = transform.right * 0.5f;
                lastObject = null;
                break;
            case 5:
                position.y = 0.5f;
                GameObject support = Instantiate(supportPrefab, position, Quaternion.identity);
                lastObject = support;
                supports.Add(support);
                // if (Vector3.Distance(currentClosestObject.transform.localPosition, support.transform.localPosition) < 3.0f) 
                // { 
                //     AddReward(0.01f); 
                // }
                break;
            // case 6:
            //     position.y = 1.05f;
            //     GameObject node = Instantiate(nodePrefab, position, Quaternion.identity);
            //     nodes.Add(node);
            //     return node;
            case 6:
                position.y = 1.05f;
                GameObject xPlank = Instantiate(plankXPrefab, position, Quaternion.identity);
                lastObject = xPlank;
                xPlanks.Add(xPlank);
                // if (Vector3.Distance(currentClosestObject.transform.localPosition, xPlank.transform.localPosition) < 3.0f) 
                // { 
                //     AddReward(0.01f); 
                //     // Rigidbody rb = xPlank.GetComponent<Rigidbody>();
                //     // Debug.Log($"Velocity: {rb.velocity.magnitude:F10}; " 
                //     //         + $"Angular velocity: {rb.angularVelocity.magnitude:F10}; "
                //     //         + $"Step count: {StepCount}; "
                //     //         );
                // }
                return xPlank;
            case 7:
                position.y = 1.05f;
                GameObject zPlank = Instantiate(plankZPrefab, position, Quaternion.identity);
                lastObject = zPlank;
                zPlanks.Add(zPlank);
                // if (Vector3.Distance(currentClosestObject.transform.localPosition, zPlank.transform.localPosition) < 3.0f) 
                // { 
                //     AddReward(0.01f); 
                // }
                return zPlank;
        }
        // Debug.Log($"Supports: {supports.Count}; Nodes: {nodes.Count}");
        if(action == 1 | action == 2 | action == 3| action == 4)
        {
            // Builder.transform.Translate(dirToGo);
            this.transform.Translate(dirToGo);
            // if (Builder.localPosition.x <= -5f | Builder.localPosition.x >= 5f | Builder.localPosition.z >= 5f | Builder.localPosition.z <= -5f)
            if (this.transform.localPosition.x <= -5f | this.transform.localPosition.x >= 5f | 
                this.transform.localPosition.z >= 5f | this.transform.localPosition.z <= -5f)
            {
                // Builder.localPosition = Start.transform.localPosition + new Vector3(0, 2f, 0);
                Debug.Log($"Out of bounds. Step count: {StepCount}");
                EndEpisode();
            }
        }
        return null;
    }
 
    bool AnyVelocity(List<GameObject> objects)
    {
        foreach(GameObject u in objects)
        {
            Rigidbody rb = u.GetComponent<Rigidbody>();
            if(rb.velocity.magnitude > 0.1f | rb.angularVelocity.magnitude > 0.1f)
            {
                return true;
            }
        }
        return false;
    }

    // Called every step of the engine. Here the agent takes an action.
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Debug.Log($"Cummulatibe reward: {GetCumulativeReward()}");

        SetReward(0.0f);
        if (StepCount > 1000)
        {
            Debug.Log($"Full episode. Step count: {StepCount}");
            EndEpisode();
        }
        // Check whether a new construct is stable
        if(AnyVelocity(supports))  // | AnyVelocity(nodes)
        { 
            // ResetEnv();
            Debug.Log($"A support collapsed. Step count: {StepCount}");
            EndEpisode(); 
            return;
        }
        if(AnyVelocity(xPlanks) | AnyVelocity(zPlanks))  // | AnyVelocity(nodes)
        { 
            // ResetEnv();
            Debug.Log($"A plank collapsed. Step count: {StepCount}");
            EndEpisode(); 
            return;
        }
        if(lastObject != null && Vector3.Distance(lastObject.transform.localPosition, currentClosestObject.transform.localPosition) <= 3.0f)
        {
            AddReward(0.01f);
            Debug.Log("Add a in range reward.");
            lastObject = null;
        }

        // Check if a new xPlank, or zPlank from previous step touches a currentClosestObject
        // If so, reassing a currentClosestObject and recalculate distance
        if (objectPretender)
        {
            Vector3 currentPos = currentClosestObject.transform.localPosition;
            Vector3 sizeCurrent = currentClosestObject.GetComponent<Renderer>().bounds.size; 
            float negXCurrent = currentPos.x - sizeCurrent.x / 2f;
            float posXCurrent = currentPos.x + sizeCurrent.x / 2f;
            float negZCurrent = currentPos.z - sizeCurrent.z / 2f;
            float posZCurrent = currentPos.z + sizeCurrent.z / 2f;

            Vector3 pretenderPos = objectPretender.transform.localPosition;
            Vector3 sizePretender = objectPretender.GetComponent<Renderer>().bounds.size; 
            float negXPretender = pretenderPos.x - sizePretender.x / 2f;
            float posXPretender = pretenderPos.x + sizePretender.x / 2f;
            float negZPretender = pretenderPos.z - sizePretender.z / 2f;
            float posZPretender = pretenderPos.z + sizePretender.z / 2f;

            bool xInLine = Mathf.Abs(posXCurrent - posXPretender) < 0.01f | Mathf.Abs(negXCurrent - negXPretender) < 0.01f ? true : false;
            bool zInLine = Mathf.Abs(posZCurrent - posZPretender) < 0.01f | Mathf.Abs(negZCurrent - negZPretender) < 0.01f ? true : false;

            bool xTouches = Mathf.Abs(posXCurrent - negXPretender) < 0.01f | Mathf.Abs(negXCurrent - posXPretender) < 0.01f ? true : false;
            bool zTouches = Mathf.Abs(posZCurrent - negZPretender) < 0.01f | Mathf.Abs(negZCurrent - posZPretender) < 0.01f ? true : false;

            if ((xInLine & zTouches) | (zInLine & xTouches))
            {
                AddReward(0.1f);
                float pretenderDistanceToTarget = Vector3.Distance(pretenderPos, Target.transform.localPosition);
                if (pretenderDistanceToTarget < 2.0f)
                {
                    AddReward(1.0f);
                    EndEpisode();
                    return;
                }
                else if (pretenderDistanceToTarget < distanceToTarget)
                {
                    currentClosestObject = objectPretender;
                    distanceToTarget = pretenderDistanceToTarget;
                    float reward = 1.0f - distanceToTarget / initialDistanceToTarget;
                    AddReward(reward);
                }
            }
        }

        // Move the agent using the action.
        objectPretender = DoAction(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKeyUp(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            discreteActionsOut[0] = 5;
        }
        else if (Input.GetKeyUp(KeyCode.X))
        {
            discreteActionsOut[0] = 6;
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            discreteActionsOut[0] = 7;
        }
        // else if (Input.GetKeyUp(KeyCode.V))
        // {
        //     discreteActionsOut[0] = 8;
        // }
    }
}
