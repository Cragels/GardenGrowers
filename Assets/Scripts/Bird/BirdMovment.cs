using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UIElements;

public class BirdMovment : MonoBehaviour
{
    public float radius = 5f;
    public Transform centerPoint;
    public int speed;

    public float angle = 5f;

    public BirdEnum currentState;
    public BirdEnum previousState;

    void Awake()
    {
        currentState = BirdEnum.Idle;
        currentState = BirdEnum.Idle;

    }

    void Update()
    {
        if (currentState != previousState)
        {
            switchState();
        }

        if (currentState == previousState)
        {
            birdIdle();
        }
    }

    public void switchState()
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

            Vector3 idleTarget = new Vector3(idleTargetX,transform.position.y, idleTargetZ);

            Vector3 idlePrevious = transform.position;

            transform.position = Vector3.MoveTowards(transform.position, idleTarget, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, idleTarget) < 0.1f)
            {
                angle += 10f;
                if (angle >= 360f) angle = 0f;
            }

        
    }

    //public IEnumerator idleMove()
    //{

    //}

    public void birdTarget()
    {

    }

    public void birdFlee()
    {

    }
}
