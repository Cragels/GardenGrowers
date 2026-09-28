using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Internal Clock")]
    [SerializeField] GameTimeStamp timeStamp;
    public float timeScale = 1f;

    [Header("Day And Night Cycle")]
    public Transform sunTransform;

    //Observer pattern
    List<ITimeTracker> listeners = new List<ITimeTracker>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        timeStamp = new GameTimeStamp(0, GameTimeStamp.Season.Summer, 1, 6, 0); 
        StartCoroutine(TimeUpdate());
    }
    
    IEnumerator TimeUpdate()
    {
        while (true) 
        {
            Tick();
            yield return new WaitForSeconds(1/timeScale);
         }
      


    }
    public void DebugAdvanceDay()
    {
        int startDay = GameTimeStamp.ToAbsoluteDay(timeStamp);
        while (GameTimeStamp.ToAbsoluteDay(timeStamp) == startDay)
        {
            Tick();
        }

        timeStamp.hour = 6;
        timeStamp.minute = 0;

        // Fire listeners once more so anything reading the corrected time
        // (UI clock, sun position) reflects 6am immediately, not hour 0
        foreach (ITimeTracker listener in listeners)
        {
            listener.ClockUpdate(timeStamp);
        }
        UpdateSunMovement();

    }

    public GameTimeStamp GetGameTimeStamp()
    {
        return new GameTimeStamp(timeStamp);
    }

    public void Tick()
    {
        timeStamp.UpdateClock();

        //Inform the listeners of the new time state
        foreach(ITimeTracker listener in listeners)
        {
            listener.ClockUpdate(timeStamp);
        }

        UpdateSunMovement();
    }

    void UpdateSunMovement()
    {
        int timeInMinutes = GameTimeStamp.HourToMinute(timeStamp.hour) + timeStamp.minute;

        //sun moves 15 degrees in an hour
        // .25 degrees in a minute
        // At midnight (00:00) , the angle of the sun should be -90

        float sunAngle = .25f * timeInMinutes - 90;

        sunTransform.eulerAngles = new Vector3(sunAngle, 0, 0);
    }

    //handle listeners

    //add objects to the list of listeners
    public void RegisterTracker(ITimeTracker listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterTracker(ITimeTracker listener) 
    { 
        listeners.Remove(listener);
    }

    public bool IsAtOrAfterHour(int hour)
    {
        return timeStamp.hour >= hour;
    }

}
