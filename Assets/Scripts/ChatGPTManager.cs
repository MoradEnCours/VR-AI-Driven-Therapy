using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using Oculus.Voice.Dictation;

public class ChatGPTManager : MonoBehaviour
{
    [TextArea(5, 20)]
    public string personality;
    [TextArea(5, 20)]
    public string scene;
    public int maxResponseWordLimit;

    public List<NPCAction> actions;

    public AppDictationExperience voiceToText;

    [System.Serializable]
    public struct NPCAction
    {
        public string actionKeyword;
        [TextArea(2, 5)]
        public string actionDescription;
        public UnityEvent actionEvent;
    }

    public OnResponseEvent OnResponse;

    [System.Serializable]
    public class OnResponseEvent : UnityEvent<string> { }

    private OpenAIApi openAI = new OpenAIApi();
    private List<ChatMessage> messages = new List<ChatMessage>();

    public string GetInstructions()
    {
        string instructions =
            $"You are roleplaying as a therapist for a player. You must reply to the player only using the information from your Personality and Scene that are provided afterwards. You must not break character or mention you're an AI. You must answer in less than {maxResponseWordLimit} words. Here is the information about your personality: {personality}; here is the information about the scene around you: {scene}. {BuildActionInstructions()} Here is the message from the player: \n";

        return instructions;
    }

    public string BuildActionInstructions()
    {
        string instructions = "";

        foreach (var item in actions)
        {
            instructions = $"If I imply that I want you to do the following: {item.actionDescription}, you must add to your answer the following keyword: {item.actionKeyword}.\n";
        }

        return instructions;
    }

    public async void AskChatGPT(string newText)
    {
        ChatMessage newMessage = new ChatMessage();
        newMessage.Content = GetInstructions() + newText;
        newMessage.Role = "user";

        messages.Add(newMessage);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo";

        var response = await openAI.CreateChatCompletion(request);

        if(response.Choices != null && response.Choices.Count > 0)
        {
            var chatResponse = response.Choices[0].Message;

            foreach (var item in actions)
            {
                if (chatResponse.Content.Contains(item.actionKeyword))
                {
                    string textNoKeyword = chatResponse.Content.Replace(item.actionKeyword, "");
                    chatResponse.Content = textNoKeyword;
                    item.actionEvent.Invoke();
                }
            }

            messages.Add(chatResponse);

            Debug.Log(chatResponse.Content);

            OnResponse.Invoke(chatResponse.Content);
        }
    }
    void Start()
    {
        voiceToText.DictationEvents.OnFullTranscription.AddListener(AskChatGPT);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            voiceToText.Activate();
        }
    }
}
