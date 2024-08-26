﻿using HarmonyLib;
using SFS;
using SFS.Builds;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace VanillaUpgrades
{
    [HarmonyPatch(typeof(FlightInfoDrawer), "Update")]
    internal static class DisplayAccurateThrustAndLocalTWR
    {
        // This method modifies the way thrust and TWR are displayed on the flight information panel:
        // - Thrust now takes into account engine orientation and stretching - also works with boosters
        // - TWR is now the local Thrust-To-Weight ratio (takes into account the local gravity)
        private static void Postfix(ref TextAdapter ___thrustText, ref TextAdapter ___thrustToWeightText)
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                float mass = rocket.rb2d.mass;
                float localGravity = (float)(rocket.location.planet.Value.GetGravity(rocket.location.position.Value.magnitude));

                // Calculate thrust, taking into account engines state, stretching and orientation
                Vector2 thrust = new Vector2(0.0f, 0.0f);

                foreach (EngineModule engineModule in rocket.partHolder.GetModules<EngineModule>())
                {
                    if (engineModule.engineOn.Value)
                    {
                        Vector2 direction = (Vector2)engineModule.transform.TransformVector(engineModule.thrustNormal.Value);

                        // For cheaters...
                        float stretchFactor = 1.0f;
                        if (Base.worldBase.AllowsCheats) stretchFactor = direction.magnitude;

                        Vector2 engineThrust = engineModule.thrust.Value * direction.normalized * stretchFactor * engineModule.throttle_Out.Value;
                        thrust += engineThrust;
                    }
                }

                foreach (BoosterModule boosterModule in rocket.partHolder.GetModules<BoosterModule>())
                {
                    if(boosterModule.enabled)
                    {
                        Vector2 direction = (Vector2)boosterModule.transform.TransformVector(boosterModule.thrustVector.Value); // equal to thrust * stretch factor in magnitude

                        // For cheaters...
                        float stretchFactor = 1.0f;
                        if (Base.worldBase.AllowsCheats) stretchFactor = direction.magnitude / (float)boosterModule.thrustVector.Value.magnitude;

                        // adjusting direction to what it should be now
                        direction = direction.normalized;

                        thrust += boosterModule.thrustVector.Value.magnitude * direction.normalized * stretchFactor;
                    }
                }

                float thrustVal = thrust.magnitude;

                ___thrustText.Text = thrustVal.ToThrustString().Split(':')[1];
                ___thrustToWeightText.Text = (thrustVal / mass * 9.8f / localGravity).ToTwrString().Split(':')[1];
            }
        }
    }
}