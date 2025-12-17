using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rhythm;

namespace ShowPreciseAccuracyText
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("UNBEATABLE [DEMO].exe")]
    [BepInProcess("UNBEATABLE [white label].exe")]
    [BepInProcess("UNBEATABLE.exe")]
    public class ShowPreciseAccuracyText : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "net.zachava.showpreciseaccuracytext";
        public const string PLUGIN_NAME = "Show Precise Accuracy Text";
        public const string PLUGIN_VERSION = "1.1.0";
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> ShowSpikeAccuracy;
        // Ugly way to see if this is the full game or [white label]/demo
        public static readonly bool isFullGame = String.Equals(Process.GetCurrentProcess().ProcessName, "UNBEATABLE", StringComparison.OrdinalIgnoreCase);

        private void Awake()
        {
            Logger = base.Logger;
            ShowSpikeAccuracy = Config.Bind(
                "General",
                "ShowSpikeAccuracy",
                false,
                "Show precise accuracy text for spike dodges when possible.\n" +
                "If you dodge the spike outside the leniency window, it still won't show.\n" +
                "For example, if you jump over the spike with an input long before the spike\n" +
                "or you dodge by standing under it.\n" +
                "This doesn't work in the full game, sorry."
            );
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PLUGIN_GUID);
            if (isFullGame)
            {
                Logger.LogInfo($"Patching for full game...");
                harmony.PatchAll(typeof(AttackInfoFullPatches));
            }
            else
            {
                Logger.LogInfo($"Patching for [white label]/demo...");
                harmony.PatchAll(typeof(AttackInfoDemoPatches));
            }
        }
    }

    [HarmonyPatch(typeof(AttackInfo))]
    [HarmonyPatch("GetTierText")]
    class AttackInfoFullPatches
    {
        static void Postfix(AttackInfo __instance, Score score, ref string __result)
        {
            // Full game has judgment display style options, so we only want to do this if it's set to "Detailed"
            if (FileStorage.beatmapOptions.judgmentDisplayStyle == StorableBeatmapOptions.JudgmentDisplayStyle.Detailed)
            {
                // The full game directly checks if it's a dodge note and forces a display of "NICE"
                // I'm not patching that method right now, so spike accuracy text doesn't work in the full game

                // If the score is not "CRITICAL" or "PERFECT", add the precise accuracy text
                // (so long as it's not a miss from completely missing the note)
                // (if it's a miss from releasing a hold note too early, show the precise accuracy text)
                if (!__result.StartsWith("CRITICAL") && !__result.StartsWith("PERFECT") && (__instance.GetPreciseAccuracy(score) < score.leniency))
                {
                    __result += " " + __instance.GetPreciseAccuracyText(score);
                }
            }
        }
    }

    [HarmonyPatch(typeof(AttackInfo))]
    [HarmonyPatch("GetTierText")]
    class AttackInfoDemoPatches
    {
        static void Postfix(AttackInfo __instance, Score score, ref string __result)
        {
            // "PERFECT" with no precise accuracy text is a spike dodge
            // This is normally replaced by "NICE" at a later point
            // But we'll do it here (if we're showing spike dodges) so we can add the precise accuracy text in the next block
            if (ShowPreciseAccuracyText.ShowSpikeAccuracy.Value && (__result == "PERFECT"))
            {
                __result = "NICE";
            }
            // If the score is not "PERFECT", add the precise accuracy text
            // (so long as it's not a miss from completely missing the note)
            // (if it's a miss from releasing a hold note too early, show the precise accuracy text)
            if (!__result.StartsWith("PERFECT") && (__instance.GetPreciseAccuracy(score) < score.leniency))
            {
                __result += " " + __instance.GetPreciseAccuracyText(score);
            }
        }
    }
}
