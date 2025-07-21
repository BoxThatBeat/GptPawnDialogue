using HarmonyLib;
using Verse;

namespace GptPawnDialogue.Access
{
    [HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
    public static class Verse_PlayLog_Add
    {
        private static void Postfix(LogEntry entry)
        {
            Pawn initiator, recipient;

            switch (entry)
            {
                case PlayLogEntry_InteractionWithMany interaction:
                    Mod.Log("InteractionWithMany log entry detected, which is not supported.");
                    return;
                case PlayLogEntry_Interaction interaction:
                    initiator = (Pawn)Reflection.Verse_PlayLogEntry_Interaction_Initiator.GetValue(interaction);
                    recipient = (Pawn)Reflection.Verse_PlayLogEntry_Interaction_Recipient.GetValue(interaction);
                    break;
                case PlayLogEntry_InteractionSinglePawn interaction:
                    initiator = (Pawn)Reflection.Verse_PlayLogEntry_InteractionSinglePawn_Initiator.GetValue(interaction);
                    recipient = null;
                    break;
                default:
                    return;
            }

            if (initiator is null || initiator.Map != Find.CurrentMap) { return; }

            // Log information about the initiator pawn
            Mod.Log($"INITIATOR INFO:");
            Mod.Log($"  Name: {initiator.Name.ToStringShort}");
            Mod.Log($"  Faction: {initiator.Faction?.Name ?? "None"}");
            Mod.Log($"  Gender: {initiator.gender}");
            Mod.Log($"  Age: {initiator.ageTracker.AgeNumberString}");
            Mod.Log($"  Backstory Childhood: {initiator.story.Childhood.description}");
            Mod.Log($"  Backstory Adulthood: {initiator.story.Adulthood.description}");

            //TODO: add pawn's current needs and skills




        }
    }
}
