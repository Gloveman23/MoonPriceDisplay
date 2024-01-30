using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;


namespace MoonPriceDisplay
{
    [BepInPlugin(GUID,"MoonPriceDisplay", "1.0.0.0")]
    [BepInProcess("Lethal Company.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "gloveman.MoonPriceDisplay";
        private readonly Harmony harmony = new Harmony(GUID);
        new static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(GUID);
        public Plugin Instance;
        public static List<TerminalNode> routeNodes = new List<TerminalNode>();
        private void Awake()
        {
            Instance = new Plugin();
            var mAddPrice = typeof(Plugin).GetMethod("AddPrice");
            var target = AccessTools.Method(typeof(Terminal), "TextPostProcess");

            var mTerminalAwake = typeof(Plugin).GetMethod("TerminalAwake");
            var target2 = AccessTools.Method(typeof(Terminal), "Awake");

            harmony.Patch(target, transpiler: new HarmonyMethod(mAddPrice));
            harmony.Patch(target2, postfix: new HarmonyMethod(mTerminalAwake));
            
            Logger.LogInfo($"{GUID} is loaded!");
        }


        public static IEnumerable<CodeInstruction> AddPrice(IEnumerable<CodeInstruction> instructions){
            var foundLoopEnd = false;
            var codes = new List<CodeInstruction>(instructions);
            yield return codes[0];
            yield return codes[1];
            for(var i = 2; i < codes.Count; i++){
                if(codes[i-1].opcode == OpCodes.Ldloc_2 && codes[i-2].opcode == OpCodes.Stloc_1 && codes[i].opcode == OpCodes.Ldarg_1){
                    yield return new CodeInstruction(OpCodes.Stloc_2);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Callvirt, concat);
                    yield return new CodeInstruction(OpCodes.Stloc_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                }
                if(!foundLoopEnd && codes[i].opcode == OpCodes.Nop && codes[i-1].opcode == OpCodes.Blt){

                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld,moonCatalogue);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Callvirt, appendPrice);
                        yield return new CodeInstruction(OpCodes.Starg_S, 1);

                        foundLoopEnd = true;

                        Logger.LogInfo("Finished patching!");

                }
                yield return codes[i];
            }
        }
        static MethodInfo appendPrice = typeof(Plugin).GetMethod("AppendPrice");
        static MethodInfo concat = typeof(Plugin).GetMethod("Concat");
        static FieldInfo moonCatalogue = AccessTools.Field(typeof(Terminal),"moonsCatalogueList");

        public static string AppendPrice(Terminal terminal,SelectableLevel[] levels,  string modifiedDisplayText){
            int num = modifiedDisplayText.Split("[moonPrice]", StringSplitOptions.None).Length - 1;
            Regex regex = new Regex(Regex.Escape("[moonPrice]"));
            for(int i = 0; i < num; i++){
                int price = 0;
                SelectableLevel level = levels[i];
                foreach(TerminalNode node in routeNodes){
                    if(node.displayPlanetInfo == level.levelID){
                        price = node.itemCost;
                        break;
                    }
                }
                var text = "";
                if(price > 0){
                    text += "$[cost]"+price.ToString();
                }
                modifiedDisplayText = regex.Replace(modifiedDisplayText, text, 1);
            }
            StringReader reader = new StringReader(modifiedDisplayText);
            string line;
            string returnText = "";
            do{
                line = reader.ReadLine();
                if(line == null){ continue; }
                if(line.Contains("$[cost]")){
                    int l = line.IndexOf("$[cost]");
                    var split = line.Split("$[cost]");
                    string insert = "";
                    for(int k = 0; k < (30-l); k++){
                        insert += " ";
                    }
                    returnText += split[0] + insert + "$" + split[1] + "\n";
                } else {
                    returnText += line + "\n";
                }
            } while(line != null);
            return returnText;
        }


        public static void TerminalAwake(Terminal __instance){
            foreach(TerminalKeyword keyword in __instance.terminalNodes.allKeywords){
                if(keyword.name == "Route"){
                    foreach(CompatibleNoun noun in keyword.compatibleNouns){
                        routeNodes.Add(noun.result);
                    }
                    break;
                }
            }
        }
        public static string Concat(string input){
            var ret = input + "[moonPrice] ";
            return ret;
        }
    }
    
}