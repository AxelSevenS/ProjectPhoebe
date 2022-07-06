using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SeleneGame.Core;

namespace SeleneGame {

    [System.Serializable]
    public class Dialogue{
        
        public enum Emotion {neutral, determined, hesitant, shocked, disgusted, sad, happy};
        public Entity entity;
        public Emotion emotion;
        [TextArea] public string text;
        public List<InvokableEvent> dialogueEvents;
    }

    [CreateAssetMenu(fileName = "new Conversation", menuName = "Conversation")]
    public class Conversation : ScriptableObject {

        public Dialogue[] dialogues;

        public static Conversation GetConversationByName(string fileName) => Resources.Load<Conversation>($"Conversations/{fileName}");
    }
}