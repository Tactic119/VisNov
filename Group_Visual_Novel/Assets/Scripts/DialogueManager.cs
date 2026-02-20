using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("Text")]
    private TextAsset textFile; // variable to hold the contets of 'Dialogue.txt'
    [SerializeField] private TMP_Text textBox; // text in the main dialogue box
    [SerializeField] private TMP_Text nameBox; // text in the small box for a chracter's name

    [Header("Conversations")]
    public string currentCharacter; // what character is currently speaking
    private Dictionary<string, List<DialogueNode>> conversations
    = new Dictionary<string, List<DialogueNode>>();
    private string currentConversationID;
    private int lineIndex = 0; // what line of the conversation we are on
    [SerializeField] private float typingSpeed = 0.05f; // how fast he type writer effect goes
    private Coroutine typingCoroutine; // evil
    private bool isTyping = false; // weather the message is dont typing or not

    [Header("Choices")]
    private bool isChoosing = false;

    [Header("Flags")]
    private Dictionary<string, bool> flags = new Dictionary<string, bool>();

    [System.Serializable]
    public class DialogueNode
    {
        public string speaker;
        public string text;

        public List<Choice> choices;

        public string setFlag;        // flag to set when this node runs
        public string requiredFlag;   // flag required for this node to appear
        public string requiredFlagNot;
    }

    [System.Serializable]
    public class Choice
    {
        public string choiceText;
        public string targetConversationID;

        public string setFlag;
    }

    [SerializeField] private string startingConversationID;

    // runs on game start
    void Start()
    {
        // load dialogue file as 'textFile'
        textFile = Resources.Load<TextAsset>("dialogue");

        // make sure dialogue isn't empty, safety check
        if (textFile == null)
        {
            Debug.LogError("Dialogue file not found!");
            return;
        }

        ParseDialogue(textFile.text);

        currentConversationID = startingConversationID;
        lineIndex = 0;

        UpdateText();
    }

    // called every frame
    void Update()
    {
        PlayerInput(); 
    }

    // checks for player input
    void PlayerInput()
    {
        if (!isChoosing && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);

                var node = conversations[currentConversationID][lineIndex];
                textBox.text = node.text;

                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
        if (isChoosing)
        {
            var node = conversations[currentConversationID][lineIndex];

            for (int i = 0; i < node.choices.Count; i++)
            {
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    SelectChoice(i);
                    break;
                }
            }
        }
    }

    // moves to the next line in the conversation
    void NextLine()
    {
        if (!conversations.ContainsKey(currentConversationID))
            return;

        var currentConversation = conversations[currentConversationID];

        if (lineIndex >= currentConversation.Count)
            return;

        DialogueNode node = currentConversation[lineIndex];

        // If this node contains choices, show them instead of advancing
        if (node.choices != null && node.choices.Count > 0)
        {
            ShowChoices(node);
            return;
        }

        lineIndex++;

        if (lineIndex < currentConversation.Count)
            UpdateText();
    }

    void ShowChoices(DialogueNode node)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isTyping = false;

        isChoosing = true;
        textBox.text = "";

        for (int i = 0; i < node.choices.Count; i++)
        {
            textBox.text += $"{i + 1}. {node.choices[i].choiceText}\n";
        }
    }

    void SelectChoice(int index)
    {
        var node = conversations[currentConversationID][lineIndex];

        if (index >= node.choices.Count)
            return;

        var selectedChoice = node.choices[index];

        // 🔹 Set flag from choice
        if (!string.IsNullOrEmpty(selectedChoice.setFlag))
        {
            flags[selectedChoice.setFlag] = true;
        }

        currentConversationID = selectedChoice.targetConversationID;
        lineIndex = 0;
        isChoosing = false;

        UpdateText();
    }

    void UpdateText()
    {
        if (!conversations.ContainsKey(currentConversationID))
            return;

        var conversation = conversations[currentConversationID];

        while (lineIndex < conversation.Count)
        {
            DialogueNode checkNode = conversation[lineIndex];

            bool meetsRequired = true;

            // Requires TRUE
            if (!string.IsNullOrEmpty(checkNode.requiredFlag))
            {
                if (!flags.ContainsKey(checkNode.requiredFlag) ||
                    !flags[checkNode.requiredFlag])
                {
                    meetsRequired = false;
                }
            }

            // Requires FALSE
            if (!string.IsNullOrEmpty(checkNode.requiredFlagNot))
            {
                if (flags.ContainsKey(checkNode.requiredFlagNot) &&
                    flags[checkNode.requiredFlagNot])
                {
                    meetsRequired = false;
                }
            }

            if (meetsRequired)
                break;

            lineIndex++;
        }

        if (lineIndex >= conversation.Count)
            return;

        DialogueNode node = conversation[lineIndex];

        // 🔹 Set flag if this node sets one
        if (!string.IsNullOrEmpty(node.setFlag))
        {
            flags[node.setFlag] = true;
        }

        nameBox.text = node.speaker;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(node.text));
    }

    // coroutine that types messages character by character
    IEnumerator TypeText(string dialogue)
    {
        // currently typing message
        isTyping = true;
        textBox.text = "";

        foreach (char letter in dialogue)
        {
            // add a letter of the message
            textBox.text += letter;

            // wait for a fraction of a second (typingSpeed)
            yield return new WaitForSeconds(typingSpeed);
        }

        // no longer typing - message complete
        isTyping = false;
    }

    // Seprate lines of text in Dialogue.txt and decode them
    void ParseDialogue(string text)
    {
        string[] lines = text.Split('\n');

        List<DialogueNode> currentConversation = null;
        string currentSpeaker = "";
        string currentConversationID_Local = "";

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith("--"))
                continue;

            if (line.StartsWith("#Conversation"))
            {
                currentConversationID_Local = line.Replace("#Conversation ", "").Trim();
                currentConversation = new List<DialogueNode>();
                conversations[currentConversationID_Local] = currentConversation;
                continue;
            }

            if (line.StartsWith("Character_"))
            {
                currentSpeaker = line.Replace("Character_", "");
                continue;
            }

            if (line.StartsWith("\"") && line.EndsWith("\""))
            {
                string cleanedLine = line.Trim('"');

                currentConversation.Add(new DialogueNode
                {
                    speaker = currentSpeaker,
                    text = cleanedLine,
                    choices = null
                });

                continue;
            }

            if (line.StartsWith("#Choice"))
            {
                DialogueNode choiceNode = new DialogueNode
                {
                    speaker = "",
                    text = "",
                    choices = new List<Choice>()
                };

                currentConversation.Add(choiceNode);
                continue;
            }

            if (line.Contains("->"))
            {
                string[] parts = line.Split("->");

                string leftPart = parts[0].Trim().Trim('"');
                string rightPart = parts[1].Trim();

                string targetID;
                string flagToSet = null;

                if (rightPart.Contains("|"))
                {
                    string[] targetParts = rightPart.Split("|");
                    targetID = targetParts[0].Trim();
                    flagToSet = targetParts[1].Trim();
                }
                else
                {
                    targetID = rightPart;
                }

                var lastNode = currentConversation[currentConversation.Count - 1];

                lastNode.choices.Add(new Choice
                {
                    choiceText = leftPart,
                    targetConversationID = targetID,
                    setFlag = flagToSet
                });

                continue;
            }

            if (line.StartsWith("#SetFlag"))
            {
                string flagName = line.Replace("#SetFlag", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    lastNode.setFlag = flagName;
                }

                continue;
            }

            if (line.StartsWith("#RequiresNot"))
            {
                string flagName = line.Replace("#RequiresNot", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    lastNode.requiredFlagNot = flagName;
                }

                continue;
            }

            if (line.StartsWith("#Requires"))
            {
                string flagName = line.Replace("#Requires", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    lastNode.requiredFlag = flagName;
                }

                continue;
            }
        }
    }
}