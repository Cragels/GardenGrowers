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
    public float idleHeight = 20f;

    public BirdEnum currentState;
   // public BirdEnum previousState;


    public float searchDelay = 20f;
    public string plantTag = "Plants";
    private float idleTimer;
    private PlotScript targetPlot;
    private Transform targetTransform;

    public float eatDuration = 5f;
    private float eatTimer;

    Vector3 PlayerPosition;
    public float fleeTriggerDistance = 4f;
    //private BirdEnum stateBeforeFlee;
    public float FleeXDir;
    public float FleeZDir;

    public bool FleeTargeted;
    Vector3 fleeTarget;
    public float fleeDistance = 15f;

    private Vector3 lastTargetPos;

    void Awake()
    {
        currentState = BirdEnum.Idle;
        //previousState = BirdEnum.Idle;
        idleTimer = 0f;
    }

    void Update()
    {

        //bool enteredThisFrame = currentState != previousState;
        //previousState = currentState;

        switch (currentState)
        {
            case BirdEnum.Idle:
                birdIdle();
                break;

            case BirdEnum.Target:
                birdTarget();
                break;

            case BirdEnum.Eat:
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
        lastTargetPos = chosen.transform.position;
        currentState = BirdEnum.Target;
    }

    private void birdTarget()
    {
        if (!TargetStillValid())
        {
            StartFlee(lastTargetPos);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetTransform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetTransform.position) < 0.3f)
        {
            eatTimer = 0f;
            currentState = BirdEnum.Eat;
        }
    }

    private void birdEat()
    {
        
        if (!TargetStillValid())
        {
            StartFlee(lastTargetPos);
            return;
        }

        eatTimer += Time.deltaTime;
        if (eatTimer >= eatDuration)
        {
            Vector3 plantPos = targetTransform.position;
            targetPlot.OnPlantEaten();
            targetPlot = null;
            targetTransform = null;
            StartFlee(plantPos);
        }
    }

    private void StartFlee(Vector3 awayFrom)
    {
        Vector3 dir = transform.position - awayFrom;
        dir.y = 0f;

        // Player exactly on top of the bird: pick a random direction
        if (dir.sqrMagnitude < 0.001f)
        {
            Vector2 r = Random.insideUnitCircle.normalized;
            dir = new Vector3(r.x, 0f, r.y);
        }
        dir.Normalize();

        fleeTarget = transform.position + dir * fleeDistance;
        fleeTarget.y = centerPoint.position.y;

        FleeTargeted = true;
        eatTimer = 0f;
        targetPlot = null;
        targetTransform = null;
        currentState = BirdEnum.Flee;
    }

    private void birdFlee()
    {
        transform.position = Vector3.MoveTowards(transform.position, fleeTarget, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, fleeTarget) < 0.1f)
        {
            FleeTargeted = false;
            idleTimer = 0f;
            currentState = BirdEnum.Idle;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            StartFlee(other.transform.position);
    }


    /*private void birdFlee()
    {
        float fleeHeight = centerPoint.position.y;
        //float playerX = PlayerPosition.x;
        //float playerY = PlayerPosition.z;

        float currentBirdX = transform.position.x;
        float currentBirdZ = transform.position.z;

       
        if (FleeTargeted)
        {
            transform.position = Vector3.MoveTowards(transform.position, fleeTarget, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, fleeTarget) < 0.1f)
            {
                FleeTargeted = false;
                idleTimer = 0;
                currentState = BirdEnum.Idle;

            }
        }
        else 
        {
            fleeTarget = new Vector3(currentBirdX * FleeXDir, fleeHeight, currentBirdZ * FleeZDir);
            FleeTargeted = true;
        }



        }*/


    private bool TargetStillValid() //checking
    {
        if (targetPlot == null || targetTransform == null)
            return false;

        return targetPlot.plotStatus == PlotScript.PlotStatus.Planted ||
               targetPlot.plotStatus == PlotScript.PlotStatus.Watered;
    }

    /*public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerPosition = other.transform.position;

            float playerX = PlayerPosition.x;
            float playerZ = PlayerPosition.z;

            float birdX = transform.position.x;
            float birdZ = transform.position.z;

            float totalX = birdX - playerX;
            float totalZ = birdZ - playerZ;

            if (totalX > 0 && totalZ > 0) // These are for the bird to go away from the player
            {
                FleeXDir = -1.01f;
                FleeZDir = -1.01f;
            }
            else if (totalX > 0 && totalZ < 0)
            {
                FleeXDir = -1.01f;
                FleeZDir = 1.01f;
            }
            else if (totalX < 0 && totalZ > 0)
            {
                FleeXDir = 1.01f;
                FleeZDir = -1.01f;
            }
            else if (totalX < 0 && totalZ < 0)
            {
                FleeXDir = 1.01f;
                FleeZDir = 1.01f;
            }
           // else
           // {
             //   FleeXDir = 1f;
               // FleeZDir = 1f;
           // }


            currentState = BirdEnum.Flee;
            
        }
    }*/

}



