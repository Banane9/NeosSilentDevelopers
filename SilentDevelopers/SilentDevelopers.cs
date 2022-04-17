using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeX;
using FrooxEngine;
using FrooxEngine.LogiX;
using HarmonyLib;
using NeosModLoader;

namespace SilentDevelopers
{
    public class SilentDevelopers : NeosMod
    {
        public static ModConfiguration Config;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> SilenceDevelopers = new ModConfigurationKey<bool>("SilenceDevelopers", "Disable any AudioOutputs under developer panels.", () => true);

        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosSilentDevelopers";
        public override string Name => "SilentDevelopers";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony($"{Author}.{Name}");
            Config = GetConfiguration();
            Config.Save(true);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(AudioOutput))]
        private static class AudioOutputPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("InitializeSyncMembers")]
            private static void InitializeSyncMembersPostfix(AudioOutput __instance)
            {
                __instance.RunInUpdates(0, () =>
                {
                    if (!Config.GetValue(SilenceDevelopers) || !__instance.Slot.IsDeveloperInterface())
                        return;

                    __instance.EnabledField.OverrideForUser(__instance.World.LocalUser, false);
                });
            }
        }

        [HarmonyPatch(typeof(LogixNodeSelector))]
        private static class LogixNodeSelectorPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("InitializeSyncMembers")]
            private static void InitializeSyncMembersPostfix(LogixNodeSelector __instance)
            {
                if (!Config.GetValue(SilenceDevelopers))
                    return;

                __instance.RunInUpdates(10, () =>
                {
                    var clipPlayers =
                        __instance.Slot.GetComponentsInChildren<ButtonHoverEventRelay>(relay => relay.Target.Target.GetComponent<ButtonAudioClipPlayer>() != null)
                        .Select(relay => relay.Target.Target)
                        .Concat(__instance.Slot.GetComponentsInChildren<ButtonPressEventRelay>(relay => relay.Target.Target.GetComponent<ButtonAudioClipPlayer>() != null)
                            .Select(relay => relay.Target.Target))
                        .Distinct()
                        .SelectMany(slot => slot.GetComponents<ButtonAudioClipPlayer>());

                    foreach (var clipPlayer in clipPlayers)
                        clipPlayer.ParentUnder.Target = __instance.Slot;
                });
            }
        }
    }
}