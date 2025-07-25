using HarmonyLib;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Verse;

namespace GptPawnDialogue.Access
{
    [HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
    public static class Verse_PlayLog_Add
    {
        //private static readonly string JSON_SCHEMA = "{\n" +
        //    "  \"name\": \"character_conversation\",\n" +
        //    "  \"strict\": true,\n" +
        //    "  \"schema\": {\n" +
        //    "    \"type\": \"object\",\n" +
        //    "    \"additionalProperties\": false,\n" +
        //    "    \"required\": [\n" +
        //    "      \"conversation_entries\"\n" +
        //    "    ],\n" +
        //    "    \"properties\": {\n" +
        //    "      \"conversation_entries\": {\n" +
        //    "        \"type\": \"array\",\n" +
        //    "        \"items\": {\n" +
        //    "          \"type\": \"object\",\n" +
        //    "          \"additionalProperties\": false,\n" +
        //    "          \"required\": [\n" +
        //    "            \"character_name\",\n" +
        //    "            \"dialogue_line\"\n" +
        //    "          ],\n" +
        //    "          \"properties\": {\n" +
        //    "            \"character_name\": {\n" +
        //    "              \"type\": \"string\"\n" +
        //    "            },\n" +
        //    "            \"dialogue_line\": {\n" +
        //    "              \"type\": \"string\"\n" +
        //    "            }\n" +
        //    "          }\n" +
        //    "        }\n" +
        //    "      }\n" +
        //    "    }\n" +
        //    "  }\n" +
        //    "}";

        private static readonly byte[] JSON_SCHEMA = """
{
    "name": "character_conversation",
    "strict": true,
    "schema": {
        "type": "object",
        "additionalProperties": false,
        "required": [
            "conversation_entries"
        ],
        "properties": {
            "conversation_entries": {
                "type": "array",
                "items": {
					"type": "object",
					"additionalProperties": false,
					"required": [
						"character_name",
						"dialogue_line"
					],
					"properties": {
						"character_name": {
							"type": "string"
						},
						"dialogue_line": {
							"type": "string"
						}
                    }
                }
            }
        }
    }
}
"""u8.ToArray();

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
            pawnInfo += $"  Backstory Childhood: {pawn.story.Childhood.description}\r\n";
            pawnInfo += $"  Backstory Adulthood: {pawn.story.Adulthood.description}\r\n";
            return pawnInfo;
        }

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

            //TODO: Move
            var apiKey = "";
            ChatClient client = new ChatClient("gpt-4o-mini", apiKey);
            
            //TODO use string builder

            string prompt = "You are a creative dialogue/script generator for the video game Rimworld. ";

            prompt += $"Two characters in my world are interacting with this \"{entry.ToGameStringFromPOV(initiator)}\" <- interaction text\r\nHere is some background on both chacters which could potentially be relavant to their conversation: ";

            prompt += GetPawnInfo(initiator);
            prompt += GetPawnInfo(recipient);

            Mod.Log(prompt);

            List<ChatMessage> messages = new List<ChatMessage>() { 
                new UserChatMessage(prompt),
            };

            ChatCompletionOptions options = new ChatCompletionOptions()
            {
                Temperature = 0.7f,
                TopP = 0.8f,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "character_conversation",
                    jsonSchema: BinaryData.FromBytes(JSON_SCHEMA),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = client.CompleteChat(messages, options);

            using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);
            foreach (JsonElement conversationEntry in structuredJson.RootElement.GetProperty("conversation_entries").EnumerateArray())
            {
                Mod.Log($"Pawn: {conversationEntry.GetProperty("character_name")}");
                Mod.Log($"Dialogue: {conversationEntry.GetProperty("dialogue_line")}");
            }
        }
    }
}
