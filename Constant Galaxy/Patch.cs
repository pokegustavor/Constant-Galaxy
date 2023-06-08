using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Reflection;

namespace Constant_Galaxy
{
    /*
    [HarmonyPatch(typeof(PLServer),"Update")]
    class Test 
    {
        static float LastTime = 0f;
        static void Postfix(PLServer __instance) 
        {
            if (Input.GetKey(KeyCode.F8) && Time.time - LastTime > 1f)
            {
                PulsarModLoader.Utilities.Messaging.Notification("Spreading");
                __instance.StartCoroutine(Patch.Spread(__instance));
                LastTime = Time.time;
            }
        }
    }
    */
    [HarmonyPatch(typeof(PLServer), "Internal_NetworkBeginWarp")]
    internal class Patch
    {
        static int Counter = 0;
        static void Postfix(PLServer __instance) 
        {
            if (PhotonNetwork.isMasterClient) 
            {
                Counter++;
                if (Counter >= 5) 
                {
                    Counter = 0 + Random.Range(0,3);
                    __instance.StartCoroutine(Spread(__instance));
                }
            }
        }
        internal static IEnumerator Spread(PLServer __instance) 
        {
            int m_NumberOfSectors = PLGlobal.Instance.Galaxy.AllSectorInfos.Count;
            List<PLFactionInfo> factionInfos = new List<PLFactionInfo>(PLGlobal.Instance.Galaxy.AllFactions);
            int iterCount = Mathf.RoundToInt(Mathf.Clamp(Mathf.Pow(PLGlobal.Instance.Galaxy.GenGalaxyScale, 2f) * Random.Range(1f,20f) * PLServer.Instance.ChaosLevel, 5f * PLGlobal.Instance.Galaxy.GenGalaxyScale, 25 * PLGlobal.Instance.Galaxy.GenGalaxyScale));
            for (int i = 0; i < iterCount; i++)
            {
                foreach (PLFactionInfo factionInfoLeft in factionInfos)
                {
                    int leftNumSecs = factionInfoLeft.GetNumSectors();
                    foreach (PLFactionInfo plfactionInfo in factionInfos)
                    {
                        if (factionInfoLeft != plfactionInfo && factionInfoLeft.Faction_ShouldAttemptToSpread() && (factionInfoLeft.FactionID != 4 || (factionInfoLeft.FactionAI_Continuous_GalaxySpreadLimit * (float)m_NumberOfSectors > (float)leftNumSecs) || plfactionInfo.FactionID == 1))
                        {
                            yield return __instance.StartCoroutine(factionInfoLeft.IterateGalaxy(plfactionInfo, false));
                        }
                    }
                }
            }
            //PulsarModLoader.Utilities.Messaging.Notification("Finished");
            yield break;
        }
    }

    [HarmonyPatch(typeof(PLFactionInfo), "ShouldTakeSector")]
    class ShouldTake 
    {
        static bool Prefix(PLFactionInfo __instance, PLSectorInfo mySector, PLSectorInfo otherSector, PLFactionInfo otherFaction, float spreadFactor, PLRand rand, ref bool __result)
        {
            float num = 0f;
            float num2 = 0f;
            float num3 = 0f;
            if (__instance.FactionID == 4 && otherSector.MySPI.Faction != otherFaction.FactionID && __instance.FactionAI_Continuous_GalaxySpreadLimit * PLGlobal.Instance.Galaxy.AllSectorInfos.Count < __instance.GetNumSectors())
            {
                __result = false;
                return false;
            }
            foreach (PLSectorInfo plsectorInfo in mySector.Cached_NearbyCells_FactionSpread)
            {
                if (mySector.MySPI.Faction != plsectorInfo.MySPI.Faction)
                {
                    plsectorInfo.HadAllFriendlyNeighborsLastCheck = false;
                }
                if (plsectorInfo.MySPI.Faction == otherSector.MySPI.Faction)
                {
                    Vector3 vector = plsectorInfo.Position - otherSector.Position;
                    vector.z = 0f;
                    float num4 = Vector3.SqrMagnitude(vector);
                    num += Mathf.Clamp01(1f - num4 * 50f) * plsectorInfo.FactionStrength;
                }
                else if (plsectorInfo.MySPI.Faction == mySector.MySPI.Faction)
                {
                    Vector3 vector2 = plsectorInfo.Position - mySector.Position;
                    vector2.z = 0f;
                    float num5 = Vector3.SqrMagnitude(vector2);
                    float num6 = Mathf.Clamp01(1f - num5 * 50f) * plsectorInfo.FactionStrength;
                    num2 += num6;
                    if (plsectorInfo != mySector)
                    {
                        num3 += num6;
                    }
                }
            }
            mySector.HadAllFriendlyNeighborsLastCheck = (num3 - num > 20f);
            if (otherSector.MissionSpecificID != -1)
            {
                num *= 25f;
            }
            if (mySector.MissionSpecificID != -1)
            {
                num2 *= 25f;
            }
            if (__instance.FactionID == 4)
            {
                if (PLServer.Instance != null)
                {
                    num2 *= 1.1f + PLServer.Instance.GetProcessedChaosLevel() * 0.02f;
                }
                if (otherFaction.FactionID == 1)
                {
                    num2 *= 1.25f;
                }
            }
            else if (otherFaction.FactionID == 1)
            {
                num2 *= 1.1f;
            }
            num2 *= 1f + (PLGlobal.Instance.Galaxy.GenGalaxyScale - 1f) * 0.06f;
            otherSector.LastCalculatedSectorStength = num;
            mySector.LastCalculatedSectorStength = num2;
            __result = num * rand.Next(1f, 1.75f) <= num2 * (1f + spreadFactor) || otherSector.MySPI.Faction == -1;
            return false;
        }
    }
}
