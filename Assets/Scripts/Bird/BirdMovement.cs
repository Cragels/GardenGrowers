using UnityEngine;
using System.Collections.Generic;
//using System;
using UnityEngine.UIElements;

public class BirdMovment : MonoBehaviour
{
    public float radius = 5f;
    public Transform centerPoint;
    public int speed;

    public float angle = 5f;

    public BirdEnum currentState;
    public BirdEnum previousState;


    public float searchDelay = 5f;
    public string plantTag = "Plants";
    private float idleTimer;
    private PlotScript targetPlot;
    private Transform targetTransform;

    public float eatDuration = 5f;
    private float eatTimer;

    public Transform player;
    public float fleeTriggerDistance = 4f;
    public float fleeClearDistance = 6f;
    public float fleeSpeed = 6f;
    private BirdEnum stateBeforeFlee;


    void Awake()
    {
        currentState = BirdEnum.Idle;
        currentState = BirdEnum.Idle;
        idleTimer = 0f;
    }

    void Update()
    {
        if (currentState != BirdEnum.Flee && player != null &&
            Vector3.Distance(transform.position, player.position) < fleeTriggerDistance)
        {
            stateBeforeFlee = currentState;
            currentState = BirdEnum.Flee;
        }

        bool enteredThisFrame = currentState != previousState;
        previousState = currentState;

        switch (currentState)
        {
            case BirdEnum.Idle:
                if (enteredThisFrame) OnEnterIdle();
                birdIdle();
                break;

            case BirdEnum.Target:
                birdTarget();
                break;

            case BirdEnum.Eat:
                if (enteredThisFrame) eatTimer = 0f;
                birdEat();
                break;

            case BirdEnum.Flee:
                birdFlee();
                break;
        }
    }

    /*public void switchState()
    {
        switch (currentState)
        {
            case BirdEnum.Idle:
                birdIdle();
                break;

            case BirdEnum.Target:
                birdTarget();
                break;

            case BirdEnum.Eat:
                //Eat code TODO;
                break;

            case BirdEnum.Flee:
                birdFlee();
                break;
        }
    }*/

    private void OnEnterIdle()
    {
        idleTimer = 0f;
        targetPlot = null;
        targetTransform = null;
    }

    public void birdIdle() // Circle above 
    {
        //int angle = 0;
        float idleTargetX;
        float idleTargetZ;
        //float DistanceClose;

        //bool close = true;


        //StartCoroutine(idleMove());

        float rad = angle * Mathf.Deg2Rad;

        idleTargetX = centerPoint.position.x + radius * Mathf.Cos(rad);

        idleTargetZ = centerPoint.position.z + radius * Mathf.Sin(rad);

        Vector3 idleTarget = new Vector3(idleTargetX, transform.position.y, idleTargetZ);

        Vector3 idlePrevious = transform.position;

        transform.position = Vector3.MoveTowards(transform.position, idleTarget, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, idleTarget) < 0.1f)
        {
            angle += 10f;
            if (angle >= 360f) angle = 0f;
        }

        idleTimer += Time.deltaTime;          
        if (idleTimer >= searchDelay)     
        {
            TryFindPlant();    
        }

    }

    //public IEnumerator idleMove()
    //{

    //}

    private void TryFindPlant()
    {
        GameObject[] plants = GameObject.FindGameObjectsWithTag(plantTag);
        if (plants.Length == 0)
        {
            idleTimer = 0f; 
            return;
        }

        GameObject chosen = plants[Random.Range(0, plants.Length)];
        PlotScript plot = chosen.GetComponentInParent<PlotScript>();
        if (plot == null)
        {
            idleTimer = 0f;
            return;
        }

        targetPlot = plot;
        targetTransform = chosen.transform;
        currentState = BirdEnum.Target;
    }

    private void birdTarget()
    {
        if (!TargetStillValid())
        {
            currentState = BirdEnum.Idle;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetTransform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetTransform.position) < 0.3f)
        {
            currentState = BirdEnum.Eat;
        }
    }

    private void birdEat()
    {
        if (!TargetStillValid())
        {
            currentState = BirdEnum.Idle;
            return;
        }

        eatTimer += Time.deltaTime;
        if (eatTimer >= eatDuration)
        {
            targetPlot.OnPlantEaten();
            targetPlot = null;
            targetTransform = null;
            currentState = BirdEnum.Idle;
        }
    }

    private void birdFlee()
    {
        if (player == null)
        {
            currentState = stateBeforeFlee;
            return;
        }

        Vector3 away = (transform.position - player.position);
        away.y = 0f;
        away = away.sqrMagnitude > 0.001f ? away.normalized : transform.forward;

        transform.position += away * fleeSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, player.position) > fleeClearDistance)
        {
            // Resume the interrupted behaviour if the target plant is still valid,
            // otherwise fall back to idle.
            if ((stateBeforeFlee == BirdEnum.Target || stateBeforeFlee == BirdEnum.Eat) && TargetStillValid())
                currentState = BirdEnum.Target; // re-approach; eat timer restarts naturally
            else
                currentState = BirdEnum.Idle;
        }
    }

    
    private bool TargetStillValid() //checking
    {
        if (targetPlot == null || targetTransform == null)
            return false;

        return targetPlot.plotStatus == PlotScript.PlotStatus.Planted ||
               targetPlot.plotStatus == PlotScript.PlotStatus.Watered;
    }
}



