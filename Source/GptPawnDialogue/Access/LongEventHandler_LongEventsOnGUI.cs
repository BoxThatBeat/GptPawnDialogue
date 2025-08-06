using HarmonyLib;
using Verse;

namespace GptPawnDialogue.Access
{
    //[HarmonyPatch(typeof(LongEventHandler), nameof(LongEventHandler.LongEventsOnGUI))]
    //public static class LongEventHandler_LongEventsOnGUI_Patch
    //{
    //    public static void Postfix()
    //    {
    //        Logger.Log();
    //    }
    //}
    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    public static class TickManager_DoSingleTick_Patch
    {
        public static void Postfix()
        {
            Logger.Log();
        }
    }
}
