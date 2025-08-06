using HarmonyLib;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.Networking;
using Verse;

namespace GptPawnDialogue.Access
{
    [HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
    public static class Verse_PlayLog_Add
    {

        private static string GetPawnInfo(Pawn pawn)
        {
            Logger.Message($"PAWN INFO:");
            Logger.Message($"  Name: {pawn.Name.ToStringShort}");
            Logger.Message($"  Faction: {pawn.Faction?.Name ?? "None"}");
            Logger.Message($"  Gender: {pawn.gender}");
            Logger.Message($"  Age: {pawn.ageTracker.AgeNumberString}");
            Logger.Message($"  Backstory Childhood: {pawn.story.Childhood.description}");

            var pawnInfo = "";
            //TODO: add pawn's current needs and skills and traits
            //TODO: fix the pawns story to have the name of the pawn in it
            pawnInfo += $"initiator character:\r\n";
            pawnInfo += $"  Name: {pawn.Name.ToStringShort}\r\n";
            pawnInfo += $"  Gender: {pawn.gender}\r\n";
            pawnInfo += $"  Age: {pawn.ageTracker.AgeNumberString}\r\n";
            pawnInfo += $"  Faction: {pawn.Faction?.Name ?? "None"}\r\n";
            pawnInfo += $"  Backstory Childhood: {pawn.story.Childhood.description}\r\n";

            if (pawn.story.Adulthood != null)
            {
                Logger.Message($"  Backstory Adulthood: {pawn.story.Adulthood.description}");
                pawnInfo += $"  Backstory Adulthood: {pawn.story.Adulthood.description}\r\n";
            }
            
            return pawnInfo;
        }


        public static IEnumerator PostJson(string url, string json, string bearerToken)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Verse.Log.Message("Response: " + request.downloadHandler.text);
            else
                Verse.Log.Error($"Error: {request.responseCode} - {request.error}");
        }

        private static void Postfix(LogEntry entry)
        {
            Pawn initiator, recipient;

            switch (entry)
            {
                case PlayLogEntry_InteractionWithMany interaction:
                    Logger.Message("InteractionWithMany log entry detected, which is not supported.");
                    return;
                case PlayLogEntry_Interaction interaction:
                    initiator = (Pawn)Reflection.Verse_PlayLogEntry_Interaction_Initiator.GetValue(interaction);
                    recipient = (Pawn)Reflection.Verse_PlayLogEntry_Interaction_Recipient.GetValue(interaction);
                    break;
                case PlayLogEntry_InteractionSinglePawn interaction:
                    //initiator = (Pawn)Reflection.Verse_PlayLogEntry_InteractionSinglePawn_Initiator.GetValue(interaction);
                    //recipient = null;
                    return;
                default:
                    return;
            }

            if (initiator is null || initiator.Map != Find.CurrentMap || recipient is null || recipient.Map != Find.CurrentMap) { return; }

            var apiKey = "";


            //TODO use string builder

            string prompt = "You are a creative dialogue/script generator for the video game Rimworld. The conversation_entries array in your response should always be between 2-5 entires in length";

            prompt += $"Two characters in my world are interacting with this \"{entry.ToGameStringFromPOV(initiator)}\" <- interaction text\r\nHere is some background on both chacters which could potentially be relavant to their conversation: ";

            prompt += GetPawnInfo(initiator);
            prompt += GetPawnInfo(recipient);

            Logger.Message(prompt);

            var jsonSchemaObject = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "conversation_entries" },
                properties = new
                {
                    conversation_entries = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "character_name", "dialogue_line" },
                            properties = new
                            {
                                character_name = new { type = "string" },
                                dialogue_line = new { type = "string" }
                            }
                        }
                    }
                }
            };

            var request = new
            {
                model = "gpt-4o-mini",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 300,
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "dialogue_generation",
                        strict = true,
                        schema = jsonSchemaObject
                    }
                }
            };

            var json = JsonConvert.SerializeObject(request);

            CoroutineRunner.Instance.StartCoroutine(PostJson("https://api.openai.com/v1/chat/completions", json, apiKey));
        }
    }
}
