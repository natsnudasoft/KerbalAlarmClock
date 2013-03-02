﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine;
using KSP;

namespace KerbalAlarmClock
{
    /// <summary>
    /// A class to store the UT of events and get back useful data
    /// </summary>
    public class KerbalTime
    {
        //really there are 31,446,925.9936 seconds in a year, use 365*24 so the reciprocal math 
        //  to go to years and get back to full days isnt confusing
        const double HoursPerYearEarth = 365 * 24;


        #region "Constructors"
        public KerbalTime()
        { }
        public KerbalTime(double NewUT)
        {
            UT = NewUT;
        }
        #endregion

        /// <summary>
        /// Build the UT from raw values
        /// </summary>
        /// <param name="Years"></param>
        /// <param name="Days"></param>
        /// <param name="Hours"></param>
        /// <param name="Minutes"></param>
        /// <param name="Seconds"></param>
        public void BuildUT(double Years, double Days, double Hours, double Minutes, double Seconds)
        {
            UT = Seconds +
                Minutes * 60 +
                Hours * 60 * 60 +
                Days * 24 * 60 * 60 +
                Years * HoursPerYearEarth * 60 * 60;
        }

        #region "Properties"

        //Stores the Universal Time in game seconds
        public double UT;

        //readonly props that resolve from UT
        public long Second
        {
            get { return Convert.ToInt64(Math.Truncate(UT % 60)); }
        }
        public long Minute
        {
            get { return Convert.ToInt64(Math.Truncate((UT / 60) % 60)); }
        }

        private double HourRaw { get { return UT / 60 / 60; } }
        public long HourEarth
        {
            get { return Convert.ToInt64(Math.Truncate(HourRaw % 24)); }
        }

        public long DayEarth
        {
            get { return Convert.ToInt64(Math.Truncate(((HourRaw % HoursPerYearEarth) / 24))); }
        }

        public long YearEarth
        {
            get { return Convert.ToInt64(Math.Truncate((HourRaw / HoursPerYearEarth))); }
        }
        #endregion

        #region "String Formatting"
        public string IntervalString()
        {
            return IntervalString(6);
        }
        public string IntervalString(int segments)
        {
            string strReturn = "";

            if (UT < 0) strReturn += "+ ";

            int intUsed = 0;

            if (intUsed < segments && YearEarth != 0)
            {
                strReturn += string.Format("{0}y", Math.Abs(YearEarth));
                intUsed++;
            }

            if (intUsed < segments && (DayEarth != 0 || intUsed > 0))
            {
                if (intUsed > 0) strReturn += ", ";
                strReturn += string.Format("{0}d", Math.Abs(DayEarth));
                intUsed++;
            }

            if (intUsed < segments && (HourEarth != 0 || intUsed > 0))
            {
                if (intUsed > 0) strReturn += ", ";
                strReturn += string.Format("{0}h", Math.Abs(HourEarth));
                intUsed++;
            }
            if (intUsed < segments && (Minute != 0 || intUsed > 0))
            {
                if (intUsed > 0) strReturn += ", ";
                strReturn += string.Format("{0}m", Math.Abs(Minute));
                intUsed++;
            }
            if (intUsed < segments)// && (Second != 0 || intUsed > 0))
            {
                if (intUsed > 0) strReturn += ", ";
                strReturn += string.Format("{0}s", Math.Abs(Second));
                intUsed++;
            }


            return strReturn;
        }

        public string DateString()
        {
            return string.Format("Year {0},Day {1}, {2:00}:{3:00}:{4:00}", YearEarth + 1, DayEarth + 1, HourEarth, Minute, Second);
        }

        public string IntervalStringLong()
        {
            string strReturn = "";
            if (UT < 0) strReturn += "+ ";
            strReturn += string.Format("{0} Years, {1} Days, {2:00}:{3:00}:{4:00}", Math.Abs(YearEarth), Math.Abs(DayEarth), Math.Abs(HourEarth), Math.Abs(Minute), Math.Abs(Second));
            return strReturn;
        }

        public string UTString()
        {
            string strReturn = "";
            if (UT < 0) strReturn += "+ ";
            strReturn += String.Format("{0:N0}s", Math.Abs(UT));
            return strReturn;
        }
        #endregion

        public override string ToString()
        {
            return IntervalStringLong();
        }
    }

    /// <summary>
    /// Class to hold an Alarm event
    /// </summary>
    public class KACAlarm
    {
        public string SaveName = "";                                    //Which Save File
        public string Name = "";                                        //Name of Alarm
        public Boolean Enabled = true;                                  //Whether it is enabled - not in use currently
        public KerbalTime AlarmTime = new KerbalTime();                 //UT of the alarm
        public Boolean HaltWarp = true;                                 //Whether the time warp will be halted at this event
        public Boolean PauseGame = false;                                //Whether the Game will be paused at this event
        public string Message = "";                                     //Some descriptive text

        //Dynamic props down here
        public KerbalTime Remaining = new KerbalTime();                 //UT value of how long till the alarm fires
        public Boolean WarpInfluence = false;                           //Whether the Warp setting is being influenced by this alarm

        public Boolean Triggered = false;                               //Has this alarm been triggered
        public Boolean Actioned = false;                                //Has the code actioned th alarm - ie. displayed its message

        //Details of the alarm message
        public Rect AlarmWindow;                                        
        public int AlarmWindowID=0;
        public Boolean AlarmWindowClosed = false;

        //Details of the alarm message
        public Boolean EditWindowOpen=false;                                        
        
        #region "Constructors"
        public KACAlarm()
        {
            if (KACBehaviour.IsFlightMode)
                SaveName = HighLogic.CurrentGame.Title;
        }
        public KACAlarm(double UT)
        {
            if (KACBehaviour.IsFlightMode)
                SaveName = HighLogic.CurrentGame.Title;
            AlarmTime.UT = UT;
        }
        public KACAlarm(double UT,string NewName,string NewMessage,Boolean NewHaltWarp,Boolean NewPause)
        {
            if (KACBehaviour.IsFlightMode)
                SaveName = HighLogic.CurrentGame.Title;
            AlarmTime.UT = UT;
            Remaining.UT = AlarmTime.UT - Planetarium.GetUniversalTime();
            Name = NewName;
            Message = NewMessage;
            HaltWarp = NewHaltWarp;
            PauseGame = NewPause;
        }
        #endregion

        /// <summary>
        /// Convert properties for the save file to a single string for storage
        /// </summary>
        /// <returns>CSV string of persistant properties</returns>
        public string SerializeString()
        {
            return KACUtils.PipeSepVariables(SaveName, Name, Enabled, AlarmTime.UT, HaltWarp, PauseGame, Message);
        }

        /// <summary>
        /// Basically deserializing the alarm
        /// </summary>
        /// <param name="AlarmDetails"></param>
        public void LoadFromString(string AlarmDetails)
        {
            string[] vars = AlarmDetails.Split("|".ToCharArray());

            SaveName = vars[0];
            Name = vars[1];
            Enabled = Convert.ToBoolean(vars[2]);
            AlarmTime.UT = Convert.ToDouble(vars[3]);
            HaltWarp = Convert.ToBoolean(vars[4]);
            if (vars.Length == 6)
                Message = vars[5];
            else
            {
                PauseGame = Convert.ToBoolean(vars[5]);
                Message = vars[6];
            }
        }

        public static int SortByUT(KACAlarm c1, KACAlarm c2)
        {
            return c1.Remaining.UT.CompareTo(c2.Remaining.UT);
        }
    }

    /// <summary>
    /// Extended List class to deal with multiple save files"/>
    /// </summary>
    public class KACAlarmList : List<KACAlarm>
    {
        /// <summary>
        /// How many alarms in the supplied save file
        /// </summary>
        /// <param name="SaveName"></param>
        /// <returns></returns>
        public Int64 CountInSave(string SaveName)
        {
            long lngReturn=0;

            foreach (KACAlarm tmpAlarm in this)
            {
                if (tmpAlarm.SaveName.ToLower() == SaveName.ToLower())
                    lngReturn++;
            }

            return lngReturn;
        }

        /// <summary>
        /// Are there any alarms for this save file that are in the future and not already actioned
        /// </summary>
        /// <param name="SaveName"></param>
        /// <returns></returns>
        public Boolean ActiveEnabledFutureAlarms(string SaveName)
        {
            Boolean blnReturn = false;
            foreach (KACAlarm tmpAlarm in this)
            {
                if (tmpAlarm.AlarmTime.UT > Planetarium.GetUniversalTime() && tmpAlarm.Enabled && !tmpAlarm.Actioned && (tmpAlarm.SaveName.ToLower() == SaveName.ToLower()))
                {
                    blnReturn = true;
                }
            }
            return blnReturn;
        }

        /// <summary>
        /// Get a filtered list of alarms for a specirfic save file
        /// </summary>
        /// <param name="SaveName"></param>
        /// <returns></returns>
        public KACAlarmList BySaveName(string SaveName)
        {
            KACAlarmList lstreturn = new KACAlarmList();

            foreach (KACAlarm tmpAlarm in this)
            {
                if (tmpAlarm.SaveName.ToLower() == SaveName.ToLower())
                    lstreturn.Add(tmpAlarm);
            }

            return lstreturn;
        }

        /// <summary>
        /// Get the Alarm object from the Unity Window ID
        /// </summary>
        /// <param name="windowID"></param>
        /// <returns></returns>
        public KACAlarm GetByWindowID(Int32 windowID)
        {
            KACAlarm alarmReturn=null;
            foreach (KACAlarm tmpAlarm in this)
            {
                if (tmpAlarm.AlarmWindowID == windowID)
                    alarmReturn=tmpAlarm;
            }
            return alarmReturn;
        }

        public Boolean PauseAlarmOnScreen(string SaveName)
        {
            Boolean blnReturn = false;
            foreach (KACAlarm tmpAlarm in this)
            {
                if ((tmpAlarm.SaveName.ToLower() == SaveName.ToLower()) && tmpAlarm.AlarmWindowID!=0 && !tmpAlarm.AlarmWindowClosed )
                {
                    blnReturn = true;
                    break;
                }
            }
            return blnReturn;
        }
    }
    public class KACVesselSOI
    {
        public string Name;
        public string SOIName;
        //public String SOINew;
        //public Boolean SOIChanged { get { return (SOILast != SOINew); } }

        //public KACVesselSOI() { }
        public KACVesselSOI(String VesselName, String SOIBody)
        {
            Name = VesselName;
            SOIName = SOIBody;
        }
    }
}
