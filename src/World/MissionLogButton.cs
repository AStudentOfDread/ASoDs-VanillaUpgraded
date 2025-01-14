using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SFS.Input;
using SFS.Logs;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.World;
using SFS.World.Maps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = SFS.UI.Button;

namespace VanillaUpgrades
{
    public static class MissionLogButton
    {
        private static readonly IconButton MissionLog = new();
        public static void Create()
        {
            
            GameObject parent = GameSelector.main.focusButton.button.gameObject;
            MissionLog.button = Object.Instantiate(parent).GetComponent<Button>();
            GameObject gameObject = MissionLog.button.gameObject;
            gameObject.transform.SetParent(parent.transform.parent);
            gameObject.transform.localScale = Vector3.one;
            MissionLog.text = MissionLog.button.gameObject.GetComponentInChildren<TextAdapter>();
            MissionLog.text.Text = "Mission Log";
            MissionLog.Show = false;
            GameSelector.main.selected.OnChange += selected =>
            {
                GameObject endMissionButton = GameObject.Find("End Mission Button");
                var endMissionActive = endMissionButton != null && endMissionButton.activeSelf && endMissionButton.GetComponentInChildren<TextAdapter>().Text == "Destroy";
                if (selected is MapPlayer && !endMissionActive)
                {
                    MissionLog.Show = true;
                    return;
                }
                MissionLog.Show = false;
            };
            MissionLog.button.onClick += OpenMenu;
        }

        public static void OpenMenu()
        {
            var mapRocket = GameSelector.main.selected.Value as MapRocket;
            Rocket rocket = mapRocket != null ? mapRocket.rocket : PlayerController.main.player.Value as Rocket;
            if (rocket == null) return;
            MethodInfo logsMethod = EndMissionMenu.main.GetType()
                .GetMethod("ReplayMission", BindingFlags.NonPublic | BindingFlags.Static);

            if (logsMethod == null) return;
            
            OpenMissionLog((List<(string, double, LogId)>)logsMethod.Invoke(EndMissionMenu.main, new object[]  {rocket.stats.branch, rocket.location.Value, null, null, null}));
        }

        private static void OpenMissionLog(List<(string, double, LogId)> missions)
        {
            var menuElement = new MenuElement(delegate(GameObject root)
            {
                // Create the window.
                var containerObject = new GameObject("ModGUI Container");
                var rectTransform = containerObject.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(0, 0);

                Window scroll = Builder.CreateWindow(rectTransform, Builder.GetRandomID(), 1200, 1000, 0, 0, false, false,
                    1, "Mission Log");
                scroll.Position = new Vector2(0, scroll.Size.y / 2);

                // Populate the window with the mission entries.
                scroll.CreateLayoutGroup(Type.Vertical, TextAnchor.MiddleCenter, 15);
                scroll.EnableScrolling(Type.Vertical);
                containerObject.transform.SetParent(root.transform);
                foreach ((string, double, LogId) line in missions)
                {
                    Utility.UIExtensions.AlignedLabel(scroll, 1170, 35, "- " + line.Item1, TextAlignmentOptions.Left,
                        false, 30);
                }

                Builder.CreateSpace(scroll, 1, 100);
                
                scroll.gameObject.GetComponentInChildren<RectMask2D>().padding = new Vector4(0, 70, 0, 0);
                Container okayButton =
                    Builder.CreateContainer(scroll.gameObject.transform, 0, (int)-scroll.Size.y + 45);
                okayButton.CreateLayoutGroup(Type.Horizontal);

                // Create the "Okay" button to close the window.
                Builder.CreateButton(okayButton, 200, 50, onClick: ScreenManager.main.CloseCurrent, text: "Okay");
            });
            MenuGenerator.OpenMenu(CancelButton.Cancel, CloseMode.Current, menuElement);
        }
    }
    [HarmonyPatch(typeof(EndMissionMenu), "OpenEndMissionMenu")]
    internal class EndMissionMenuHook
    {
        private static SFS.UI.ModGUI.Button missionLogButton;

        static void Postfix(EndMissionMenu __instance)
        {
            if (missionLogButton == null)
            {
                GameObject completeButton = GameObject.Find("Complete Buttons").transform.Find("Complete Button").gameObject;
                if (completeButton != null)
                {
                    missionLogButton = Builder.CreateButton(completeButton.transform.parent, (int)completeButton.Rect().sizeDelta.x, (int)completeButton.Rect().sizeDelta.y, onClick: MissionLogButton.OpenMenu, text: "Mission Log");
                    missionLogButton.gameObject.name = "Mission Log Button";
                }
            }
            missionLogButton?.gameObject.SetActive(__instance.titleText.Text.ToLower().Contains("challenges"));
        }
    }
}