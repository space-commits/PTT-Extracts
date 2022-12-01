using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;

namespace PTTExtracts
{
    public class ScavExfiltrationPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Interactive.ScavExfiltrationPoint).GetMethod("InfiltrationMatch", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }


    public class InitAllExfiltrationPointsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1016).GetMethod("InitAllExfiltrationPoints", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref GClass1016 __instance, GClass1131[] settings, bool justLoadSettings = false, bool giveAuthority = true)
        {
            ExfiltrationPoint[] source = LocationScene.GetAllObjects<ExfiltrationPoint>(false).ToArray<ExfiltrationPoint>();


            ExfiltrationPoint[] scavExfilArr = source.Where(new Func<ExfiltrationPoint, bool>(IsScavExfil)).ToArray<ExfiltrationPoint>();
            ExfiltrationPoint[] pmcExfilArr = source.Where(new Func<ExfiltrationPoint, bool>(IsNotScavExfil)).ToArray<ExfiltrationPoint>();

            List<ExfiltrationPoint> pmcExfilList = pmcExfilArr.ToList<ExfiltrationPoint>();

            foreach (ExfiltrationPoint scavExfil in scavExfilArr)
            {
                if (!pmcExfilList.Any(k => k.Settings.Name == scavExfil.Settings.Name))
                {
                    pmcExfilList.Add(scavExfil);
                }
            }

            AccessTools.Property(typeof(GClass1016), "ExfiltrationPoints").SetValue(__instance, pmcExfilList.ToArray());

            AccessTools.Property(typeof(GClass1016), "ScavExfiltrationPoints").SetValue(__instance, source.Where(new Func<ExfiltrationPoint, bool>(IsScavExfil)).Cast<ScavExfiltrationPoint>().ToArray<ScavExfiltrationPoint>());

            AccessTools.Field(typeof(GClass1016), "list_0").SetValue(__instance, new List<ScavExfiltrationPoint>(__instance.ScavExfiltrationPoints.Length));
            AccessTools.Field(typeof(GClass1016), "list_1").SetValue(__instance, new List<ScavExfiltrationPoint>());


            List<ScavExfiltrationPoint> list_0 = (List<ScavExfiltrationPoint>)AccessTools.Field(typeof(GClass1016), "list_0").GetValue(__instance);
            List<ScavExfiltrationPoint> list_1 = (List<ScavExfiltrationPoint>)AccessTools.Field(typeof(GClass1016), "list_1").GetValue(__instance);


            foreach (ScavExfiltrationPoint scavExfiltrationPoint in __instance.ScavExfiltrationPoints)
            {
                Logger.LogWarning("Scav Exfil name = " + scavExfiltrationPoint.Settings.Name);
                SharedExfiltrationPoint sharedExfiltrationPoint;
                if ((sharedExfiltrationPoint = (scavExfiltrationPoint as SharedExfiltrationPoint)) != null && sharedExfiltrationPoint.IsMandatoryForScavs)
                {
                    list_1.Add(scavExfiltrationPoint);
                }
                else
                {
                    list_0.Add(scavExfiltrationPoint);
                }
            }
            AccessTools.Field(typeof(GClass1016), "list_0").SetValue(__instance, list_0);
            AccessTools.Field(typeof(GClass1016), "list_1").SetValue(__instance, list_1);


            UnityEngine.Random.InitState(GClass1160.Now.Millisecond);
            foreach (ExfiltrationPoint exfiltrationPoint in __instance.ExfiltrationPoints)
            {
                Logger.LogWarning("PMC Exfil name = " + exfiltrationPoint.Settings.Name);
                exitName = exfiltrationPoint.Settings.Name;
                GClass1131 gclass = settings.FirstOrDefault(new Func<GClass1131, bool>(NameMatches));
                if (gclass != null)
                {
                    exfiltrationPoint.LoadSettings(gclass, giveAuthority);
                    if (!justLoadSettings && !RandomRange(exfiltrationPoint))
                    {
                        exfiltrationPoint.SetStatusLogged(EExfiltrationStatus.NotPresent, "ExfiltrationController.InitAllExfiltrationPoints-2");
                    }
                }
                else
                {
                    exfiltrationPoint.SetStatusLogged(EExfiltrationStatus.NotPresent, "ExfiltrationController.InitAllExfiltrationPoints-3");
                }
            }

            return false;
        }

        public static bool NameMatches(GClass1131 x)
        {
            return exitName == x.Name;
        }

        public static string exitName;

        public static bool RandomRange(ExfiltrationPoint trigger)
        {
            return UnityEngine.Random.Range(0f, 100f) <= trigger.Settings.Chance;
        }

        public static bool IsNotScavExfil(ExfiltrationPoint x)
        {
            return !(x is ScavExfiltrationPoint) || x is SharedExfiltrationPoint;
        }

        public static bool IsScavExfil(ExfiltrationPoint x)
        {
            return x is ScavExfiltrationPoint;
        }
    }
}