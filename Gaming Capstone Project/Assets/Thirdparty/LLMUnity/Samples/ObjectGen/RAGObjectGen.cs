using UnityEngine.UI;
using LLMUnity;
using System.Diagnostics;
using System.Collections.Generic;

namespace LLMUnitySamples
{
    public class RAGObjectGen : ObjectGame_Sample
    {
        public LLMCharacter llmCharacter;
        public Toggle ParaphraseWithLLM;

        public struct Room
        {
            public string roomName;
            public List<Object> objects;
        }

        public struct Object
        {
            public string objectName;
            public List<string> properties;
        }


        protected override void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            AIText.text = "...";
            _ = llmCharacter.Chat("Here is the object list and room list: " + RAGText.text + ". Please extract the relevant objects to put in the new json list.", SetAIText, AIReplyComplete, false);
        }

        public void CancelRequests()
        {
            llmCharacter.CancelRequests();
            AIReplyComplete();
        }

        protected override void CheckLLMs(bool debug)
        {
            base.CheckLLMs(debug);
            CheckLLM(llmCharacter, debug);
        }
    }
}
