using System;
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
    private List<Choice> currentVisibleChoices;
    private List<bool> currentChoiceUnlockedState;

    [Header("Flags")]
    [Header("Variables")]
    private Dictionary<string, int> intVariables = new Dictionary<string, int>();
    private Dictionary<string, bool> boolVariables = new Dictionary<string, bool>();

    #region DialogueNodeClass
    [System.Serializable]
    public class DialogueNode
    {
        public string speaker;
        public string text;

        public List<Choice> choices;

        public string requiredCondition;
        public List<string> variableChanges = new List<string>();

        [System.NonSerialized]
        public bool hasExecuted;
    }
    #endregion

    #region ChoiceClass
    [System.Serializable]
    public class Choice
    {
        public string choiceText;
        public string targetConversationID;
        public List<string> variableChanges = new List<string>();
        public string requiredCondition; 
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
            if (isChoosing && currentVisibleChoices != null)
            {
                for (int i = 0; i < currentVisibleChoices.Count; i++)
                {
                    if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                    {
                        SelectChoice(i);
                        break;
                    }
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
        textBox.text = "";

        for (int i = 0; i < currentVisibleChoices.Count; i++)
        {
            textBox.text += $"{i + 1}. {currentVisibleChoices[i].choiceText}\n";
        }
    }

    void SelectChoice(int index)
    {
        if (currentVisibleChoices == null || index < 0 || index >= currentVisibleChoices.Count)
            return;

        if (!currentChoiceUnlockedState[index])
        {
            return;
        }

        var selectedChoice = currentVisibleChoices[index];

        // Apply variable changes
        if (selectedChoice.variableChanges != null)
        {
            foreach (var change in selectedChoice.variableChanges)
                ApplyVariableChange(change);
        }

        // Move to target conversation
        if (!string.IsNullOrEmpty(selectedChoice.targetConversationID))
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

        while (lineIndex < conversation.Count)
        {
            DialogueNode checkNode = conversation[lineIndex];

            if (EvaluateCondition(checkNode.requiredCondition))
                break;

            lineIndex++;
        }

        if (lineIndex >= conversation.Count)
        {
            Debug.Log("Conversation Ended.");
            return;
        }

        // get the next valid node
        DialogueNode node = conversation[lineIndex];

        // Set flag if this node sets one
        if (!node.hasExecuted)
        {
            foreach (var change in node.variableChanges)
            {
                ApplyVariableChange(change);
            }

            node.hasExecuted = true;
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

        textBox.text = "";
        currentVisibleChoices = node.choices ?? new List<Choice>();
        currentChoiceUnlockedState = new List<bool>();

        for (int i = 0; i < currentVisibleChoices.Count; i++)
        {
            Choice choice = currentVisibleChoices[i];
            bool unlocked = string.IsNullOrEmpty(choice.requiredCondition) ? true : EvaluateCondition(choice.requiredCondition);
            currentChoiceUnlockedState.Add(unlocked);

            string displayText = unlocked
                ? $"{i + 1}. {choice.choiceText}\n"
                : $"<color=#888888>{i + 1}. {choice.choiceText}</color>\n";

            foreach (char letter in displayText)
            {
                textBox.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
    }

    // INT
    public void SetInt(string key, int value)
    {
        intVariables[key] = value;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return intVariables.TryGetValue(key, out int value) ? value : defaultValue;
    }

    // BOOL
    public void SetBool(string key, bool value)
    {
        boolVariables[key] = value;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        return boolVariables.TryGetValue(key, out bool value) ? value : defaultValue;
    }

    bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
            return true;

        // Split OR first
        string[] orParts = condition.Split(new string[] { "||" }, System.StringSplitOptions.None);

        foreach (string orPart in orParts)
        {
            if (EvaluateAndCondition(orPart.Trim()))
                return true; // if any OR group passes, condition is true
        }

        return false;
    }

    bool EvaluateSingleCondition(string condition)
    {
        string[] parts = condition.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3)
            return false;

        string varName = parts[0];
        string op = parts[1];
        string valueString = parts[2];

        // INT comparison
        if (int.TryParse(valueString, out int intValue))
        {
            int current = GetInt(varName);

            switch (op)
            {
                case "==": return current == intValue;
                case "!=": return current != intValue;
                case ">": return current > intValue;
                case "<": return current < intValue;
                case ">=": return current >= intValue;
                case "<=": return current <= intValue;
            }
        }

        // BOOL comparison
        if (bool.TryParse(valueString, out bool boolValue))
        {
            bool current = GetBool(varName);

            switch (op)
            {
                case "==": return current == boolValue;
                case "!=": return current != boolValue;
            }
        }

        return false;
    }

    bool EvaluateAndCondition(string condition)
    {
        string[] andParts = condition.Split(new string[] { "&&" }, System.StringSplitOptions.None);

        foreach (string andPart in andParts)
        {
            if (!EvaluateSingleCondition(andPart.Trim()))
                return false; // If any AND fails, whole AND fails
        }

        return true;
    }

    void ApplyVariableChange(string expression)
    {
        if (string.IsNullOrEmpty(expression))
            return;

        string[] parts = expression.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return;

        string varName = parts[0];
        string op = parts[1];
        string valueString = parts[2];

        // INT
        if (int.TryParse(valueString, out int intValue))
        {
            int current = GetInt(varName);

            switch (op)
            {
                case "=": SetInt(varName, intValue); break;
                case "+=": SetInt(varName, current + intValue); break;
                case "-=": SetInt(varName, current - intValue); break;
            }
        }
        // BOOL
        else if (bool.TryParse(valueString, out bool boolValue))
        {
            if (op == "=")
                SetBool(varName, boolValue);
        }
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

            // Start of new conversation
            if (line.StartsWith("#Conversation"))
            {
                currentConversationID_Local = line.Replace("#Conversation ", "").Trim();
                currentConversation = new List<DialogueNode>();
                conversations[currentConversationID_Local] = currentConversation;
                continue;
            }

            // Set speaker
            if (line.StartsWith("Character_"))
            {
                currentSpeaker = line.Replace("Character_", "").Trim();
                continue;
            }

            // Dialogue line
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

            // Start of choice block
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

            // Choice line with -> syntax
            if (line.Contains("->"))
            {
                string[] parts = line.Split("->");
                string leftPart = parts[0].Trim().Trim('"');
                string rightPart = parts[1].Trim();

                string targetID = null;
                string setExpression = null;
                string condition = null;

                string[] segments = rightPart.Split('|');

                if (segments.Length > 0) targetID = segments[0].Trim();
                if (segments.Length > 1) setExpression = segments[1].Trim();
                if (segments.Length > 2) condition = segments[2].Trim();

                // Find the last node in the conversation that has a choices list
                DialogueNode lastChoiceNode = null;
                for (int i = currentConversation.Count - 1; i >= 0; i--)
                {
                    if (currentConversation[i].choices != null)
                    {
                        lastChoiceNode = currentConversation[i];
                        break;
                    }
                }

                if (lastChoiceNode == null)
                {
                    Debug.LogWarning("No choice node found for: " + leftPart);
                    continue;
                }

                Choice choice = new Choice
                {
                    choiceText = leftPart,
                    targetConversationID = targetID,
                    requiredCondition = condition
                };

                if (!string.IsNullOrEmpty(setExpression))
                {
                    string[] changes = setExpression.Split(';');
                    foreach (var change in changes)
                        choice.variableChanges.Add(change.Trim());
                }

                lastChoiceNode.choices.Add(choice);
                continue;
            }

            // #Set variables
            if (line.StartsWith("#Set"))
            {
                string expression = line.Replace("#Set", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    string[] changes = expression.Split(';');

                    foreach (var change in changes)
                    {
                        lastNode.variableChanges.Add(change.Trim());
                    }
                }

                continue;
            }

            // #If condition
            if (line.StartsWith("#If"))
            {
                string condition = line.Replace("#If", "").Trim();

                if (currentConversation != null && currentConversation.Count > 0)
                {
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    lastNode.requiredCondition = condition;
                }

                continue;
            }
        }
    }


}