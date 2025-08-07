using Newtonsoft.Json;
using System.Collections.Generic;

namespace GptPawnDialogue.Model
{
    public class PawnConversation
    {
        [JsonProperty("conversation_entries")]
        public IList<ConversationEntry> ConversationEntries { get; set; }
    }

    public class ConversationEntry
    {
        [JsonProperty("character_name")]
        public string CharacterName { get; set; }
        
        [JsonProperty("dialogue_line")]
        public string DialogueLine { get; set; }
    }
}
