using UnityEngine;

[System.Serializable]
public class GameTimeStamp
{
    public int year;
    public enum Season
    {
        Summer,
        Autumn,
        Winter,
        Spring
    }

    public Season season;

    public enum DayOfTheWeek
    {
        Saturday,
        Sunday,
        Monday,
        Teusday,
        Wednesday,
        Thursday,
        Friday
    }

    public int day;
    public int hour;
    public int minute;

    public GameTimeStamp(int year, Season season, int day, int hour, int minute)
    {
        this.year = year;
        this.season = season;
        this.day = day;
        this.hour = hour;
        this.minute = minute;
    }

    public GameTimeStamp(GameTimeStamp timeStamp)
    {
        this.year = timeStamp.year;
        this.season = timeStamp.season;
        this.day = timeStamp.day;
        this.hour = timeStamp.hour;
        this.minute = timeStamp.minute;
    }

    public void UpdateClock()
    {
        minute++;

        if (minute >= 60) {
            minute = 0;
            hour++;
        }

        if (hour >= 24) 
        {
            hour = 0;

            day++;
        }

        if (day >= 30) 
        {
            day = 1;
            if(season == Season.Spring)
            {
                season = Season.Summer;
                year++;
            }
            else
            {
                season++;
            }
          
        }

    }

    public DayOfTheWeek GetDayOfTheWeek()
    {
        int daysPassed = YearsToDays(year) + SeasonsToDays(season) + day;

        //Remainder
        int dayIndex = daysPassed % 7;

        return (DayOfTheWeek)dayIndex;
    }

    public static int HourToMinute(int hour) 
    {


        return hour * 60;
    }

    public static int DayToHour(int days)
    {
        return days * 24;
    }

    public static int SeasonsToDays(Season season)
    {
        int seasonIndex = (int)season;
        return seasonIndex * 30;
    }

    public static int YearsToDays(int years)
    {
        return years * 4 * 30;
    }

    public static int ToAbsoluteDay(GameTimeStamp t) => YearsToDays(t.year) + SeasonsToDays(t.season) + t.day;

    //Calculate the difference between two timestamps.
    public static int CompareTimestamps(GameTimeStamp timestamp1, GameTimeStamp timestamp2)
    {
        int timestamp1Hours = DayToHour(YearsToDays(timestamp1.year)) + DayToHour(SeasonsToDays(timestamp1.season)) + DayToHour(timestamp1.day) + timestamp1.hour;
        int timestamp2Hours = DayToHour(YearsToDays(timestamp2.year)) + DayToHour(SeasonsToDays(timestamp2.season)) + DayToHour(timestamp2.day) + timestamp2.hour;
        int difference = timestamp1Hours - timestamp2Hours;

        return Mathf.Abs(difference);
    }
}
