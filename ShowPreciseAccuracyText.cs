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
    public class ShowPreciseAccuracyText : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "net.zachava.showpreciseaccuracytext";
        public const string PLUGIN_NAME = "Show Precise Accuracy Text";
        public const string PLUGIN_VERSION = "1.0.0";
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> ShowSpikeAccuracy;

        private void Awake()
        {
            Logger = base.Logger;
            ShowSpikeAccuracy = Config.Bind(
                "General",
                "ShowSpikeAccuracy",
                false,
                "Show precise accuracy text for spike dodges when possible.\n" +
                "If you dodge the spike outside the +-150ms window, it still won't show.\n" +
                "For example, if you jump over the spike with an input >150ms before the spike\n" +
                "or you dodge by standing under it."
            );
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(AttackInfo))]
    [HarmonyPatch("GetTierText")]
    class AttackInfoPatches
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
