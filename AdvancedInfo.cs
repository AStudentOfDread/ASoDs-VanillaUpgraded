﻿using SFS.UI;
using SFS.World;
using System;
using UnityEngine;
using SFS;
using SFS.Variables;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace ASoD_s_VanillaUpgrades
{

    public class AdvancedInfo : MonoBehaviour
    {
        public static Rect windowRect = new Rect((float)WindowManager.settings["advancedInfo"]["x"], (float)WindowManager.settings["advancedInfo"]["y"], 150f, 220f);

        public static Rocket currentRocket;

        public double apoapsis;

        public double periapsis;

        public double angle;

        public double displayEcc;

        public static bool disableKt;

        public static AdvancedInfo instance;

        public void Awake()
        {
            instance = this;
        }

        public static string displayify(double value)
        {
            if (double.IsNegativeInfinity(value))
            {
                return "Escape";
            }
            if (double.IsNaN(value))
            {
                return "Error";
            }
            if (value < 10000)
            {
                return value.Round(0.1).ToString(1, true) + "m";
            }
            else
            {
                if (value > 100000000 && (bool)Config.settings["mmUnits"])
                {
                    return (value / 1000000).Round(0.1).ToString(1, true) + "Mm";
                }
                return (value / 1000).Round(0.1).ToString(1, true) + "km";
            }
        }

        public void windowFunc(int windowID)
        {
            GUI.Label(new Rect(10f, 20f, 160f, 20f), "Apoapsis:");
            GUI.Label(new Rect(10f, 40f, 160f, 20f), displayify(apoapsis));
            GUI.Label(new Rect(10f, 70f, 160f, 20f), "Periapsis:");
            GUI.Label(new Rect(10f, 90f, 160f, 20f), displayify(periapsis));
            GUI.Label(new Rect(10f, 120f, 160f, 25f), "Eccentricity:");
            GUI.Label(new Rect(10f, 140f, 160f, 25f), displayifyEcc(displayEcc));
            GUI.Label(new Rect(10f, 170f, 160f, 25f), "Angle:");
            GUI.Label(new Rect(10f, 190f, 160f, 20f), angle.Round(0.1).ToString(1, true) + "°");

            GUI.DragWindow();
        }

        public string displayifyEcc(double value)
        {
            if (value > 1000000 || double.IsNaN(value)) return "Error";
            return value.Round(0.001).ToString(3, true);
        }
        public void Update()
        {
            
            if (!(bool)Config.settings["allowTimeSlowdown"] && TimeDecelMain.timeDecelIndex != 0)
            {
                WorldTime.main.SetState(1, true, false);
                TimeDecelMain.timeDecelIndex = 0;
            }

            if (TimeDecelMain.timeDecelIndex != 0 && WorldTime.main.timewarpIndex.timewarpIndex != 0)
            {
                TimeDecelMain.timeDecelIndex = 0;
            }

            if (PlayerController.main.player.Value == null)
            {
                currentRocket = null;
                return;
            }


            currentRocket = (PlayerController.main.player.Value as Rocket);


            if (Main.menuOpen || !(bool)Config.settings["showAdvanced"] || VideoSettingsPC.main.uiOpacitySlider.value == 0) return;


            var sma = currentRocket.location.planet.Value.mass / -(2.0 * (Math.Pow(currentRocket.location.velocity.Value.magnitude, 2.0) / 2.0 - currentRocket.location.planet.Value.mass / currentRocket.location.Value.Radius));
            Double3 @double = Double3.Cross(currentRocket.location.position, currentRocket.location.velocity);
            Double2 double2 = (Double2)(Double3.Cross((Double3)currentRocket.location.velocity.Value, @double) / currentRocket.location.planet.Value.mass) - currentRocket.location.position.Value.normalized;
            var ecc = double2.magnitude;
            displayEcc = ecc;


            apoapsis = (Kepler.GetApoapsis(sma, ecc) - currentRocket.location.planet.Value.Radius);
            periapsis = (Kepler.GetPeriapsis(sma, ecc) - currentRocket.location.planet.Value.Radius);

            if (apoapsis == double.PositiveInfinity)
            {

                if (currentRocket.physics.location.velocity.Value.normalized.magnitude > 0)
                {
                    apoapsis = double.NegativeInfinity;
                }
                else
                {
                    apoapsis = 0;
                }

            }
            if (periapsis < 0) { periapsis = 0; }




            var trueAngle = currentRocket.partHolder.transform.eulerAngles.z;

            if (trueAngle > 180) { angle = 360 - trueAngle; }
            if (trueAngle < 180) { angle = -trueAngle; }
        }

        public static void StopTimewarp(bool showmsg)
        {
            if (WorldTime.main.timewarpIndex.timewarpIndex == 0 && TimeDecelMain.timeDecelIndex == 0) return;

            WorldTime.main.timewarpIndex.timewarpIndex = 0;
            WorldTime.main.SetState(1, true, false);
            TimeDecelMain.timeDecelIndex = 0;
            TimewarpToClass.timewarpTo = false;
            if (showmsg)
            {
                MsgDrawer.main.Log("Time acceleration stopped");
            }

        }

        public static void Throttle01()
        {
            if (currentRocket == null) return;
            currentRocket.throttle.throttlePercent.Value = 0.0005f;
        }

        public string throttle = "0";
        public void OnGUI()
        {
            if (PlayerController.main.player.Value == null)
            {
                currentRocket = null;
                return;
            }

            if (Main.menuOpen || !(bool)Config.settings["showAdvanced"] || VideoSettingsPC.main.uiOpacitySlider.value == 0 || currentRocket == null) return;

            Rect oldRect = windowRect;
            GUI.color = Config.windowColor;
            windowRect = GUI.Window(WindowManager.GetValidID(), windowRect, new GUI.WindowFunction(windowFunc), "Advanced");
            windowRect = WindowManager.ConfineRect(windowRect);
            if (oldRect != windowRect) WindowManager.settings["advancedInfo"]["x"] = windowRect.x; WindowManager.settings["advancedInfo"]["y"] = windowRect.y;
        }
    }
}
