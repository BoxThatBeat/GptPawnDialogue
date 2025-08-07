using GptPawnDialogue.Model;
using HarmonyLib;
using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Verse;

namespace GptPawnDialogue.Access
{
    [HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
    public static class Verse_PlayLog_Add
    {

        private static string GetPawnInfo(Pawn pawn)
        {
            var pawnInfo = "";
            //TODO: add pawn's current needs and skills and traits
            //TODO: fix the pawns story to have the name of the pawn in it
            pawnInfo += $"initiator character:\r\n";
            pawnInfo += $"  Name: {pawn.Name.ToStringShort}\r\n";
            pawnInfo += $"  Gender: {pawn.gender}\r\n";
            pawnInfo += $"  Age: {pawn.ageTracker.AgeNumberString}\r\n";
            pawnInfo += $"  Faction: {pawn.Faction?.Name ?? "None"}\r\n";
            pawnInfo += $"  Backstory Childhood: {pawn.story.Childhood.FullDescriptionFor(pawn)}\r\n";

            if (pawn.story.Adulthood != null)
            {
                pawnInfo += $"  Backstory Adulthood: {pawn.story.Adulthood.FullDescriptionFor(pawn)}\r\n";
            }
            
            return pawnInfo;
        }


        public static IEnumerator PostJson(string url, string json, string bearerToken)
        {
            Logger.Message("ChatGPT Coroutine Started");

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

            yield return request.SendWebRequest();

            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);


                    if (response.Choices?.Count > 0)
                    {
                        var message = (response.Choices[0].Message.Content ?? "");

                        PawnConversation pawnConversation = JsonConvert.DeserializeObject<PawnConversation>(message);

                        if (pawnConversation.ConversationEntries != null)
                        {
                            foreach (ConversationEntry entry in pawnConversation.ConversationEntries)
                            {
                                Logger.Message($"{entry.CharacterName}:  \"{entry.DialogueLine}\"");
                            }
                        }
                        else
                        {
                            Logger.Error("No conversation entries found in response.");
                        }
                    }
                    else
                    {
                        Logger.Error("No choices found in response.");
                    }
                }
                else
                {
                    Logger.Error($"Error: {request.responseCode} - {request.error}");
                    Logger.Error($"UnityWebRequest error: {request.error}");
                    Logger.Error($"Response code: {request.responseCode}");
                    Logger.Error($"Response text: {request.downloadHandler.text}");
                }
            } 
            catch (Exception e)
            {
                Logger.Error($"Aaron ERROR: {e.Message}");
            }
            
                
        }

        private static void Postfix(LogEntry entry)
        {
            Pawn initiator, recipient;
            InteractionDef interactionDef;

            switch (entry)
            {
                case PlayLogEntry_InteractionWithMany interaction:
                    Logger.Message("InteractionWithMany log entry detected, which is not supported.");
                    return;
                case PlayLogEntry_Interaction interaction:
                    initiator = (Pawn)Reflection.Verse_PlayLogEntry_Interaction_Initiator.GetValue(interaction);
                    recipient = (Pawn)Reflection.Verse_PlayLogEntry_Interaction_Recipient.GetValue(interaction);
                    interactionDef = (InteractionDef)Reflection.Verse_PlayLogEntry_Interaction_IntDef.GetValue(interaction);
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

            prompt += $"Two characters in my world are interacting with this interaction type: \"{interactionDef.defName}\" with this interaction prompt: \"{entry.ToGameStringFromPOV(initiator)}\" \r\nHere is some background on both chacters which could potentially be relavant to their conversation: ";
            prompt += GetPawnInfo(initiator);
            prompt += GetPawnInfo(recipient);
            prompt += $"{initiator.Name.ToStringShort}'s opinion and relationship with {recipient.Name.ToStringShort} is: {initiator.relations.OpinionExplanation(recipient)}";

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

            try
            {
                CoroutineRunner.Instance.StartCoroutine(PostJson("https://api.openai.com/v1/chat/completions", json, apiKey));
            } catch (Exception e)
            {
                Logger.Error("Coroutine failed: " + e.Message);
            }
        }
    }
}
