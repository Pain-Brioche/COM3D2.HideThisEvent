using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace COM3D2.HideThisEvent
{
    [BepInPlugin("COM3D2.HideThisEvent", "Hide This Event", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
        //config
        private static KeyboardShortcut keyboardShortcut = new(KeyCode.LeftShift);

        private static List<int> disabledScenarioIDList = new();

        private static ConfigEntry<bool> hideEvents;

        private static string jsonPath = BepInEx.Paths.ConfigPath + "\\COM3D2.HideThisEvent.json";


        // Logger
        private static ManualLogSource logger;

        private void Awake()
        {
            // BepinEx config
            hideEvents = Config.Bind("General", "Hide Events", true, "If set to false the disabled events will be temporarily displayed in your event list");

            // Logger
            logger = base.Logger;

            // Loading from JSon
            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                disabledScenarioIDList = JsonConvert.DeserializeObject<List<int>>(json);
            }
            // Harmony
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScenarioSelectMgr), "GetAllScenarioData")]
        private static ScenarioData[] EditScenarioList(ScenarioData[] originalScenarioArray)
        {
            if (hideEvents.Value)
            {
                List<ScenarioData> editedScenarioList = new List<ScenarioData>();
                foreach (ScenarioData scenarioData in originalScenarioArray)
                {
                    if (!disabledScenarioIDList.Contains(scenarioData.ID))
                    {
                        editedScenarioList.Add(scenarioData);
                    }
                }
                return editedScenarioList.ToArray();
            }
            return originalScenarioArray;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneScenarioSelect), "OnSelectScenario")]
        private static void ShiftClick()
        {
            if (keyboardShortcut.IsDown() || keyboardShortcut.IsPressed())
            {
                UIWFTabButton currentSelectedTab = (UIWFTabButton)UIWFSelectButton.current;

                Vector3 enabledEventVector = new Vector3(0, 0, 0);
                Vector3 disabledEventVector = new Vector3(0, 0, 180);

                // prevents spamming because of Kiss' code.
                if (!UIWFSelectButton.current.isSelected) { return; }

                // retrieve the SceneScenarioSelect object.
                SceneScenarioSelect sceneScenarioSelect = (SceneScenarioSelect)FindObjectOfType(typeof(SceneScenarioSelect));

                // get the clicked Scenario infos.
                ScenarioData selectedScenario = sceneScenarioSelect.m_ScenarioButtonpair[currentSelectedTab];

                // Add or Remove the ID from the hidden list and change the event tab to reflect that.
                if (disabledScenarioIDList.Contains(selectedScenario.ID))
                {
                    disabledScenarioIDList.Remove(selectedScenario.ID);
                    logger.LogMessage($"Event:{selectedScenario.Title} ID:{selectedScenario.ID} is now Enabled.");

                    currentSelectedTab.transform.gameObject.transform.parent.eulerAngles = enabledEventVector;
                }
                else
                {
                    disabledScenarioIDList.Add(selectedScenario.ID);
                    logger.LogMessage($"Event:{selectedScenario.Title} ID:{selectedScenario.ID} is now Disabled");

                    currentSelectedTab.transform.gameObject.transform.parent.eulerAngles = disabledEventVector;
                }
                // Save disabled ID as Json.
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(disabledScenarioIDList));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SceneScenarioSelect), "Start")]
        private static void ShowDisabledEvents()
        {
            if (!hideEvents.Value)
            {
                // retrieve the SceneScenarioSelect object.
                SceneScenarioSelect sceneScenarioSelect = (SceneScenarioSelect)FindObjectOfType(typeof(SceneScenarioSelect));

                Vector3 disabledEventVector = new Vector3(0, 0, 180);

                foreach (KeyValuePair<UIWFTabButton, ScenarioData> keyValuePair in sceneScenarioSelect.m_ScenarioButtonpair)
                {
                    if (disabledScenarioIDList.Contains(keyValuePair.Value.ID))
                    {
                        keyValuePair.Key.transform.gameObject.transform.parent.eulerAngles = disabledEventVector;
                    }
                }
            }

            // set back hidden state to true
            hideEvents.Value = true;
        }
    }
}
