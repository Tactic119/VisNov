using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("Text")]
    #region DocumentsAndTMPs
    private TextAsset textFile; 
    [SerializeField] private TMP_Text textBox; 
    [SerializeField] private TMP_Text nameBox;
    #endregion

    [Header("Conversations")]
    #region Conversation State
    public string currentCharacter; 
    private Dictionary<string, List<DialogueNode>> conversations = new Dictionary<string, List<DialogueNode>>(); // dictionary of conversations containing lists of dialogue nodes
    private string currentConversationID; // dictionary key for a certain conversation
    [SerializeField] private string startingConversationID; // dictionary key for first conversation
    private int lineIndex = 0; 
    #endregion

    #region Typewriter Effect
    [SerializeField] private float typingSpeed = 0.05f; 
    private Coroutine typingCoroutine; // responsible for running the typing effect overtime
    private bool isTyping = false; 
    #endregion

    [Header("Choices")]
    private bool isChoosing = false;

    [Header("Flags")]
    private Dictionary<string, bool> flags = new Dictionary<string, bool>(); // dictonary of flags using Strings as keys

    #region DialogueNodeClass
    [System.Serializable]
    public class DialogueNode
    {
        public string speaker;
        public string text;

        public List<Choice> choices;

        public string setFlag;        // flag to set when this node runs
        public string requiredFlag;   // flag required for this node to appear
        public string requiredFlagNot; // only show node if flag is false
    }
    #endregion

    #region ChoiceClass
    [System.Serializable]
    public class Choice
    {
        public string choiceText;
        public string targetConversationID;

        public string setFlag;
    }
    #endregion

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
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);

                if (isChoosing)
                {
                    ShowChoicesInstantly();
                }
                else
                {
                    var node = conversations[currentConversationID][lineIndex];
                    textBox.text = node.text;
                }

                isTyping = false;
            }
            else if (!isChoosing)
            {
                NextLine();
            }
        }

        if (isChoosing)
        {
            // grab current line of dialogue in the current conversation
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
        // makes sure conversation exists before continueing
        if (!conversations.ContainsKey(currentConversationID))
            return;

        // grab current conversation
        var currentConversation = conversations[currentConversationID];

        // make sure conversation isn't complete already
        if (lineIndex >= currentConversation.Count)
            return;

        // grab current dialogue node
        DialogueNode node = currentConversation[lineIndex];

        // if this node contains choices, show them instead of advancing
        if (node.choices != null && node.choices.Count > 0)
        {
            ShowChoices(node);
            return;
        }

        lineIndex++;

        if (lineIndex >= currentConversation.Count)
            return;

        DialogueNode nextNode = currentConversation[lineIndex];

        // if next node is a choice show it
        if (nextNode.choices != null && nextNode.choices.Count > 0)
        {
            ShowChoices(nextNode);
        }
        else
        {
            UpdateText();
        }
    }

    // starts coroutine to display chocies
    void ShowChoices(DialogueNode node)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeChoices(node));
    }

    void ShowChoicesInstantly()
    {
        var node = conversations[currentConversationID][lineIndex];

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

    // 
    void UpdateText()
    {
        // safety check
        if (!conversations.ContainsKey(currentConversationID))
            return;

        // get current conversation
        var conversation = conversations[currentConversationID];

        // looks for next valid node (node that should be shown based on flags)
        while (lineIndex < conversation.Count)
        {
            // assume node is valid until proven false
            DialogueNode checkNode = conversation[lineIndex];
            bool meetsRequired = true;

            // requires flag to be true
            if (!string.IsNullOrEmpty(checkNode.requiredFlag))
            {
                if (!flags.ContainsKey(checkNode.requiredFlag) ||
                    !flags[checkNode.requiredFlag])
                {
                    meetsRequired = false;
                }
            }

            // requires flag to be false
            if (!string.IsNullOrEmpty(checkNode.requiredFlagNot))
            {
                if (flags.ContainsKey(checkNode.requiredFlagNot) &&
                    flags[checkNode.requiredFlagNot])
                {
                    meetsRequired = false;
                }
            }

            // node is valid, stop search
            if (meetsRequired)
                break;

            // skips to next node if this nod is invalid
            lineIndex++;
        }

        // stop if no more lines
        if (lineIndex >= conversation.Count)
            return;

        // get the next valid node
        DialogueNode node = conversation[lineIndex];

        // Set flag if this node sets one
        if (!string.IsNullOrEmpty(node.setFlag))
        {
            flags[node.setFlag] = true;
        }

        // display speacker name
        nameBox.text = node.speaker;

        // stop any previous typewriter effects
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // start typewriter effect
        typingCoroutine = StartCoroutine(TypeText(node.text));
    }

    // coroutine that types dialogue character by character
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

    // coroutine that types choices character by character
    IEnumerator TypeChoices(DialogueNode node)
    {
        isTyping = true;
        isChoosing = true;

        // remove previous text first
        textBox.text = "";

        // type choices character by character
        for (int i = 0; i < node.choices.Count; i++)
        {
            // set line in style we want
            string line = $"{i + 1}. {node.choices[i].choiceText}\n";

            // build letter by letter
            foreach (char letter in line)
            {
                textBox.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        // finished typing
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

            if (line.StartsWith("#Conversation")) // start of a new conversation
            {
                currentConversationID_Local = line.Replace("#Conversation ", "").Trim();
                currentConversation = new List<DialogueNode>();
                conversations[currentConversationID_Local] = currentConversation;
                continue;
            }

            if (line.StartsWith("Character_")) // line that sets what character is speaking
            {
                currentSpeaker = line.Replace("Character_", "");
                continue;
            }

            if (line.StartsWith("\"") && line.EndsWith("\"")) // dialogue line
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

            if (line.StartsWith("#Choice"))  // adds choice node to conversation
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

            if (line.Contains("->")) // options for a choice
            {
                // split between choice text and path|flag
                string[] parts = line.Split("->");

                // clean line
                string leftPart = parts[0].Trim().Trim('"');
                string rightPart = parts[1].Trim();

                string targetID;
                string flagToSet = null;

                if (rightPart.Contains("|")) // a flag exsists with the choice
                {
                    // split path from flag
                    string[] targetParts = rightPart.Split("|");
                    targetID = targetParts[0].Trim();
                    flagToSet = targetParts[1].Trim();
                }
                else
                {
                    targetID = rightPart;
                }

                var lastNode = currentConversation[currentConversation.Count - 1];

                // add choice to node
                lastNode.choices.Add(new Choice
                {
                    choiceText = leftPart,
                    targetConversationID = targetID,
                    setFlag = flagToSet
                });

                continue;
            }

            if (line.StartsWith("#SetFlag")) // set flag without the use of a choice
            {
                string flagName = line.Replace("#SetFlag", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    lastNode.setFlag = flagName;
                }

                continue;
            }

            if (line.StartsWith("#RequiresNot")) // option requires a flag to be false
            {
                string flagName = line.Replace("#RequiresNot", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    lastNode.requiredFlagNot = flagName;
                }

                continue;
            }

            if (line.StartsWith("#Requires")) // option requires a flag to be false
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