﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static PulsarModLoader.Patches.HarmonyHelpers;

namespace InsaneFire
{
    [HarmonyPatch(typeof(PLFire), "Update")]
    class MainUpdatePatch
    {
        public static Dictionary<PLFire, bool> RegisteredFires = new Dictionary<PLFire, bool>();
        static bool PatchMethod(PLRoomArea roomArea, PLFire inFire)
        {
            bool Succession = roomArea.IsVisible() && !Physics.Linecast(PLCameraSystem.GetCenterEyeAnchor().position, inFire.transform.position);
            RegisteredFires.Add(inFire, Succession);
            return Succession;
        }

        static bool PatchMethodSuccession(PLRoomArea roomArea, PLFire inFire)
        {
            return RegisteredFires[inFire];
        }

        static bool PatchMethodEnd(PLRoomArea roomArea, PLFire inFire)
        {
            bool Succession = RegisteredFires[inFire];
            RegisteredFires.Remove(inFire);
            return Succession;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            //Fire limit
            List<CodeInstruction> targetSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)20),
            };

            List<CodeInstruction> injectedSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Global), "FireCap")),
            };

            instructions = PatchBySequence(instructions, targetSequence, injectedSequence, patchMode: PatchMode.REPLACE);


            //Oxy Consumption
            targetSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_R4, 0.0005f),
            };

            injectedSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Global), "O2Consumption")),
            };

            instructions = PatchBySequence(instructions, targetSequence, injectedSequence, patchMode: PatchMode.REPLACE);


            MethodInfo visibleMethod = AccessTools.Method(typeof(PLRoomArea), "IsVisible");
            MethodInfo PatchMethodStart = AccessTools.Method(typeof(MainUpdatePatch), "PatchMethod");
            MethodInfo PatchMethodSuccession = AccessTools.Method(typeof(MainUpdatePatch), "PatchMethodSuccession");
            MethodInfo PatchMethodEnd = AccessTools.Method(typeof(MainUpdatePatch), "PatchMethodEnd");
            targetSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Callvirt, visibleMethod),
            };

            injectedSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PatchMethodStart),
            };

            instructions = PatchBySequence(instructions, targetSequence, injectedSequence, patchMode: PatchMode.REPLACE);

            injectedSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PatchMethodSuccession),
            };

            instructions = PatchBySequence(instructions, targetSequence, injectedSequence, patchMode: PatchMode.REPLACE);
            instructions = PatchBySequence(instructions, targetSequence, injectedSequence, patchMode: PatchMode.REPLACE);

            injectedSequence = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PatchMethodEnd),
            };

            return PatchBySequence(instructions, targetSequence, injectedSequence, patchMode: PatchMode.REPLACE);
        }

        static void Postfix(PLFire __instance)
        {
            if (Global.ModEnabled)
            {
                __instance.HasSpread = false;
            }
        }
    }

    [HarmonyPatch(typeof(PLFire), "Spread")]
    class Spreadlocationfix
    {
        static bool Prefix(PLFire __instance)
        {
            if(!Global.ModEnabled)
            {
                return true;
            }
            bool tryspread = true;
            Vector3 inOffset = new Vector3();
            while (tryspread)
            {
                inOffset = UnityEngine.Random.onUnitSphere * 2f;
                inOffset.y = 0f;
                tryspread = false;
                foreach (PLFire fire in __instance.MyShip.AllFires.Values)
                {
                    float distance = Vector3.Distance(fire.transform.position, inOffset);
                    if (distance <= 1.5f)
                    {
                        tryspread = true;
                        break;
                    }
                }
            }


            if (PLServer.Instance != null)
            {
                PLServer.Instance.CreateFireAtOffset(__instance, inOffset);
            }
            return false;
        }
    }
}
