using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoonPriceDisplay
{
    [BepInPlugin(GUID, "MoonPriceDisplay", "1.0.0.0")]
    [BepInProcess("Lethal Company.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "gloveman.MoonPriceDisplay";
        const string LEVELLOADER_GUID = "imabatby.lethallevelloader";
        private readonly Harmony harmony = new Harmony(GUID);
        new static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(GUID);
        public Plugin Instance;
        public static TerminalKeyword route;
        private void Awake()
        {
            Instance = new Plugin();
            var mAddPrice = typeof(Plugin).GetMethod("AddPrice");
            var target = AccessTools.Method(typeof(Terminal), "TextPostProcess");

            var mTerminalAwake = typeof(Plugin).GetMethod("TerminalAwake");
            var target2 = AccessTools.Method(typeof(Terminal), "Awake");

            harmony.Patch(target, prefix: new HarmonyMethod(mAddPrice));
            harmony.Patch(target2, postfix: new HarmonyMethod(mTerminalAwake));
            

            Logger.LogInfo($"{GUID} is loaded!");
        }


        public static bool AddPrice(Terminal __instance, ref string modifiedDisplayText, TerminalNode node, ref string __result){
        
            
            
        int num = modifiedDisplayText.Split("[planetTime]", StringSplitOptions.None).Length - 1;
		if (num > 0)
		{
			Regex regex = new Regex(Regex.Escape("[planetTime]"));
			int num2 = 0;
			while (num2 < num && __instance.moonsCatalogueList.Length > num2)
			{
				Debug.Log(string.Format("isDemo:{0} ; {1}", GameNetworkManager.Instance.isDemo, __instance.moonsCatalogueList[num2].lockedForDemo));
				string text;
				if (GameNetworkManager.Instance.isDemo && __instance.moonsCatalogueList[num2].lockedForDemo)
				{
					text = "(Locked)";
				}
				else if (__instance.moonsCatalogueList[num2].currentWeather == LevelWeatherType.None)
				{
					text = "";
				}
				else
				{
					text = "(" + __instance.moonsCatalogueList[num2].currentWeather.ToString() + ")";
				}
                foreach (CompatibleNoun n3 in route.compatibleNouns)
                    {
                        var n2 = n3.result;
                        if (n2.displayPlanetInfo == __instance.moonsCatalogueList[num2].levelID){
                        if(n2.itemCost > 0){
                            text += " $" + n2.itemCost.ToString();
                            break;
                        }
                    }
                }
				modifiedDisplayText = regex.Replace(modifiedDisplayText, text, 1);
				num2++;
			}
            __result = modifiedDisplayText;
		}
            return true;
        }
        

        public static void TerminalAwake(Terminal __instance){
            foreach(TerminalKeyword keyword in __instance.terminalNodes.allKeywords){
                if(keyword.name == "Route"){
                    route = keyword;
                }
            }
        }

    }
    
}