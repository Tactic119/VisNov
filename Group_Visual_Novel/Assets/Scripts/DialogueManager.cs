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
    // stores the Dialoge.txt file
    private TextAsset textFile; 
    // reference to the text box text in Unity
    [SerializeField] private TMP_Text textBox; 
    // refernece to the character name text in Unity
    [SerializeField] private TMP_Text nameBox;
    #endregion

    [Header("Conversations")]
    #region Conversation State
    // character currently speaking
    public string currentCharacter;
    // dictionary of conversations containing lists of dialogue nodes
    private Dictionary<string, List<DialogueNode>> conversations = new Dictionary<string, List<DialogueNode>>();
    // dictionary key for a certain conversation
    private string currentConversationID;
    // dictionary key for first conversation
    [SerializeField] private string startingConversationID; 
    // what line of a conversation we are on
    private int lineIndex = 0; 
    #endregion

    #region Typewriter Effect
    // how fast the typewriter effect goes
    [SerializeField] private float typingSpeed = 0.05f;
    // responsible for running the typing effect overtime
    private Coroutine typingCoroutine; 
    // weather a message is still being typed or not
    private bool isTyping = false; 
    #endregion

    [Header("Choices")]
    // weather a choice is available - pervents skipping through a choice
    private bool isChoosing = false;
    // list of choices visible
    private List<Choice> currentVisibleChoices;
    // list of choices that can be picked - excludes choices with unmet requirements
    private List<bool> currentChoiceUnlockedState;

    [Header("Flags")]
    // flags that hold integer values like relation ship levels
    private Dictionary<string, int> intVariables = new Dictionary<string, int>(); 
    // flags that hold true/false values like weather the player has a key or not
    private Dictionary<string, bool> boolVariables = new Dictionary<string, bool>(); 

    #region DialogueNodeClass
    // represents one entry in a conversation (line of dialogue/condition line/choice node)
    [System.Serializable]
    public class DialogueNode 
    {
        // who is speaking, what they say
        public string speaker;
        public string text;

        // contains choices if DialogueNode is a choice node
        public List<Choice> choices;

        // stores #If condition of a node
        public string requiredCondition;

        // stores #Set commands of a node
        public List<string> variableChanges = new List<string>();

        // pervents variable changes from running multiple times if player revists or skips around
        [System.NonSerialized]
        public bool hasExecuted;
    }
    #endregion

    #region ChoiceClass
    // represents a single selectable option
    [System.Serializable]
    public class Choice
    {
        // choice text shown to player
        public string choiceText;

        // what conversation branch the choice takes the player to
        public string targetConversationID;

        // what variables change from selecting the choice
        public List<string> variableChanges = new List<string>();

        // optional condition required to unlock this choice
        public string requiredCondition; 
    }
    #endregion

    // runs on game start
    void Start()
    {
        // load dialogue file as 'textFile'
        textFile = Resources.Load<TextAsset>("Dialogue");

        // make sure dialogue isn't empty, safety check
        if (textFile == null)
        {
            Debug.LogError("Dialogue file not found!");
            return;
        }

        // parse textFile (seperate lines and translate them)
        ParseDialogue(textFile.text);

        // set starting conversation as conversation
        currentConversationID = startingConversationID;
        lineIndex = 0;

        // call to show first line
        UpdateText();
    }

    // called every frame
    void Update()
    {
        // checks for player input every frame
        PlayerInput(); 
    }

    // checks for player input
    void PlayerInput()
    {
        // player pressed ENTER
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            // type writer is going - skip and complete message instantly
            if (isTyping)
            {
                // stops typewriter
                StopCoroutine(typingCoroutine);

                // if currently displaying choices, reveal them instantly
                if (isChoosing)
                {
                    ShowChoicesInstantly();
                }
                // complete dialogue line instantly
                else
                {
                    var node = conversations[currentConversationID][lineIndex];
                    textBox.text = node.text;
                }

                isTyping = false;
            }
            // line complete - move to next line
            else if (!isChoosing)
            {
                NextLine();
            }
        }

        // lookes for number key input coreponing to a option during a choice node
        if (isChoosing)
        {
            if (isChoosing && currentVisibleChoices != null)
            {
                // loops through keys releavent to the choice node
                for (int i = 0; i < currentVisibleChoices.Count; i++)
                {
                    // checks if relavents key was pressed
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

    // starts coroutine to display chocies with typewriter effect
    void ShowChoices(DialogueNode node)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeChoices(node));
    }

    // instantly reveales choices, skip typewriter effect
    void ShowChoicesInstantly()
    {
        // empty text box
        textBox.text = "";

        // new list of unlocked states for new choice block
        currentChoiceUnlockedState = new List<bool>();

        // loop through visible choices
        for (int i = 0; i < currentVisibleChoices.Count; i++)
        {
            Choice choice = currentVisibleChoices[i];

            // if choice has no requirement allow it to be selected. if choice has requirement check if it can be selected
            bool unlocked = string.IsNullOrEmpty(choice.requiredCondition)
                ? true
                : EvaluateCondition(choice.requiredCondition);

            // save wether the choice is unlock or not
            currentChoiceUnlockedState.Add(unlocked);

            // choices with unmet requirements appear a different color
            string displayText = unlocked
              ? $"{i + 1}. {choice.choiceText}\n"
              : $"<color=#AA0000>{i + 1}. {choice.choiceText}</color>\n";

            // add choice text to text box
            textBox.text += displayText;
        }
    }

    // choose an option during a choice node
    void SelectChoice(int index)
    {
        // safety check for the validity of the choice
        if (currentVisibleChoices == null || index < 0 || index >= currentVisibleChoices.Count)
            return;

        // check that the choice can be picked based on its requirements
        if (!currentChoiceUnlockedState[index])
        {
            return;
        }

        // grap choice picked
        var selectedChoice = currentVisibleChoices[index];

        // apply variable changes
        if (selectedChoice.variableChanges != null)
        {
            foreach (var change in selectedChoice.variableChanges)
                ApplyVariableChange(change);
        }

        // move to target conversation
        if (!string.IsNullOrEmpty(selectedChoice.targetConversationID))
            currentConversationID = selectedChoice.targetConversationID;

        // start at line 0 of new conversation
        lineIndex = 0;
        isChoosing = false;

        UpdateText();
    }

    // core of the dialogue display
    void UpdateText()
    {
        // safety check
        if (!conversations.ContainsKey(currentConversationID))
            return;

        // get current conversation
        var conversation = conversations[currentConversationID];

        // skip dialogue nodes that fail their #If condition
        while (lineIndex < conversation.Count)
        {
            DialogueNode checkNode = conversation[lineIndex];

            if (EvaluateCondition(checkNode.requiredCondition))
                break;

            lineIndex++;
        }

        // stop if conversation ended
        if (lineIndex >= conversation.Count)
        {
            Debug.Log("Conversation Ended.");
            return;
        }

        // get the next valid node
        DialogueNode node = conversation[lineIndex];

        // Apply variable changes attached to this node (only once)
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
        // type and choosing are both true
        isTyping = true;
        isChoosing = true;

        // clear text box
        textBox.text = "";

        // initiate lists for choice data
        currentVisibleChoices = node.choices ?? new List<Choice>();
        currentChoiceUnlockedState = new List<bool>();

        // loop through choices
        for (int i = 0; i < currentVisibleChoices.Count; i++)
        {
            // get choice
            Choice choice = currentVisibleChoices[i];

            // choice is selectable is no conditions, if there are conditions check if they have been met
            bool unlocked = string.IsNullOrEmpty(choice.requiredCondition) 
                ? true 
                : EvaluateCondition(choice.requiredCondition);

            // track weather a choice's contion has been met or not
            currentChoiceUnlockedState.Add(unlocked);

            // choices with unmet conditions appear in a different text color
            string displayText = unlocked
               ? $"{i + 1}. {choice.choiceText}\n"
               : $"<color=#AA0000>{i + 1}. {choice.choiceText}</color>\n";

            // displau choices character by character
            foreach (char letter in displayText)
            {
                textBox.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        // type writer effect complete
        isTyping = false;
    }

    // set the value of integer flags
    public void SetInt(string key, int value)
    {
        intVariables[key] = value;
    }

    // get the state of integer flags
    public int GetInt(string key, int defaultValue = 0)
    {
        return intVariables.TryGetValue(key, out int value) ? value : defaultValue;
    }

    // set the value of bool flags
    public void SetBool(string key, bool value)
    {
        boolVariables[key] = value;
    }

    // get the value of bool flags
    public bool GetBool(string key, bool defaultValue = false)
    {
        return boolVariables.TryGetValue(key, out bool value) ? value : defaultValue;
    }

    // find if a condition of a choice has been met or not
    bool EvaluateCondition(string condition)
    {
        // condition is considered met if no condition exsists
        if (string.IsNullOrEmpty(condition))
            return true;

        // split multiple conditions if an or statement exsists
        string[] orParts = condition.Split(new string[] { "||" }, System.StringSplitOptions.None);

        // look through the or conditions individually
        foreach (string orPart in orParts)
        {
            // if either part of the or statement passes condition is true
            if (EvaluateAndCondition(orPart.Trim()))
                return true; 
        }

        // no condition is true
        return false;
    }

    // evaluate weather a single condition is true or not
    bool EvaluateSingleCondition(string condition)
    {
        // split condition into 3 parts
        string[] parts = condition.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        // safety check
        if (parts.Length != 3)
            return false;

        // variable
        string varName = parts[0];
        // operator
        string op = parts[1];
        // value
        string valueString = parts[2];

        // int comparison
        if (int.TryParse(valueString, out int intValue))
        {
            int current = GetInt(varName);

            // return comparison based on the operator
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

        // bool comparison
        if (bool.TryParse(valueString, out bool boolValue))
        {
            bool current = GetBool(varName);

            // return comparison based on the operator
            switch (op)
            {
                case "==": return current == boolValue;
                case "!=": return current != boolValue;
            }
        }
        
        // if value isn't a int or bool or if an operator wasn't recognized contion isn't met
        return false;
    }

    // evaluates if conditions linked by && are both true
    bool EvaluateAndCondition(string condition)
    {
        // seperated conditions linked by &&
        string[] andParts = condition.Split(new string[] { "&&" }, System.StringSplitOptions.None);

        // looks at each condition individually
        foreach (string andPart in andParts)
        {
            // if either condition fails - both fail
            if (!EvaluateSingleCondition(andPart.Trim()))
                return false; 
        }

        // neither condition was false
        return true;
    }

    // setting a flag variable to a value
    void ApplyVariableChange(string expression)
    {
        // safety checl
        if (string.IsNullOrEmpty(expression))
            return;

        // split variable chnage expression 3 ways
        string[] parts = expression.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return;

        // variable
        string varName = parts[0];
        // operator
        string op = parts[1];
        // value
        string valueString = parts[2];

        // iset an int flag value based on operator
        if (int.TryParse(valueString, out int intValue))
        {
            int current = GetInt(varName);

            // set based on operator
            switch (op)
            {
                case "=": SetInt(varName, intValue); break;
                case "+=": SetInt(varName, current + intValue); break;
                case "-=": SetInt(varName, current - intValue); break;
            }
        }
        // set boolean flag value
        else if (bool.TryParse(valueString, out bool boolValue))
        {
            if (op == "=")
                SetBool(varName, boolValue);
        }
    }

    // Seprate lines of text in Dialogue.txt and decode them
    void ParseDialogue(string text)
    {
        // split and clean lines
        text = text.Replace("\r", ""); 
        text = text.Trim();            
        string[] lines = text.Split('\n');

        // set empty values for conversation, speaker, and conversation ID
        List<DialogueNode> currentConversation = null;
        string currentSpeaker = "";
        string currentConversationID_Local = "";

        // look at each line individually
        foreach (string rawLine in lines)
        {
            // clean line again
            string line = rawLine.Trim();

            // comment line - ignore
            if (string.IsNullOrEmpty(line) || line.StartsWith("--"))
                continue;

            // Start of new conversation
            if (line.StartsWith("#Conversation"))
            {
                // set conversation
                currentConversationID_Local = line.Replace("#Conversation ", "").Trim();
                currentConversation = new List<DialogueNode>();
                conversations[currentConversationID_Local] = currentConversation;
                continue;
            }

            // Set speaker
            if (line.StartsWith("Character_"))
            {
                // set the current speaker and move on
                currentSpeaker = line.Replace("Character_", "").Trim();
                continue;
            }

            // Dialogue line
            if (line.StartsWith("\"") && line.EndsWith("\""))
            {
                // dialogue line does not belong to a conversation
                if (currentConversation == null)
                {
                    Debug.LogError("Dialogue line found before #Conversation block.");
                    continue;
                }

                // remove quotation marks
                string cleanedLine = line.Trim('"');

                // add dialogue node with speaker and text line
                currentConversation.Add(new DialogueNode
                {
                    speaker = currentSpeaker,
                    text = cleanedLine,

                    // create dialogue node without choices
                    choices = null
                });

                continue;
            }

            // Start of choice block
            if (line.StartsWith("#Choice"))
            {
                // create dialogue node with a list of choices
                DialogueNode choiceNode = new DialogueNode
                {
                    // speaker and text are not needed for choice nodes
                    speaker = "",
                    text = "",

                    // make dialogue ndoe a choice node
                    choices = new List<Choice>()
                };

                // add choice node to conversation
                currentConversation.Add(choiceNode);

                continue;
            }

            // An option(choice) for a choice node
            if (line.Contains("->"))
            {
                // split chuck before arrow and chunk after arrow
                string[] parts = line.Split("->");
                // choice text displayed in text window for player
                string leftPart = parts[0].Trim().Trim('"');
                // choice path, variable setting, and choice conditions
                string rightPart = parts[1].Trim();

                // start with empty values for branch path, variable setting, and choice conditions
                string targetID = null;
                string setExpression = null;
                string condition = null;

                // split by |, seperates branch path, variable setting, and choice conditions
                string[] segments = rightPart.Split('|');

                // branch path
                if (segments.Length > 0) targetID = segments[0].Trim();
                // variable setting
                if (segments.Length > 1) setExpression = segments[1].Trim();
                // choice conditions
                if (segments.Length > 2) condition = segments[2].Trim();

                // don't know the choice block this option(choice) belongs to yet
                DialogueNode lastChoiceNode = null;

                // find the choice node this option(choice) belongs to
                for (int i = currentConversation.Count - 1; i >= 0; i--)
                {
                    // if the current conversation possess a choice node that is the choice node this option(choice) belongs to
                    if (currentConversation[i].choices != null)
                    {
                        lastChoiceNode = currentConversation[i];
                        break;
                    }
                }

                // safety check for an option(choice) without a choice node to belong to
                if (lastChoiceNode == null)
                {
                    Debug.LogWarning("No choice node found for: " + leftPart);
                    continue;
                }

                // create an option(choice) in the choice node with choice text, branch path, and required choice conditions
                Choice choice = new Choice
                {
                    choiceText = leftPart,
                    targetConversationID = targetID,
                    requiredCondition = condition
                };

                // if the option(choice) has variable setters
                if (!string.IsNullOrEmpty(setExpression))
                {
                    // seperate variable setters
                    string[] changes = setExpression.Split(';');

                    // add variable chnages to this option in the choice node
                    foreach (var change in changes)
                        choice.variableChanges.Add(change.Trim());
                }

                // add the option(choice) to the choice node
                lastChoiceNode.choices.Add(choice);

                continue;
            }

            // #Set variables
            if (line.StartsWith("#Set"))
            {
                // trim away the #Set command
                string expression = line.Replace("#Set", "").Trim();

                // safety check to make sure a conversation exists
                if (currentConversation != null && currentConversation.Count > 0)
                {
                    // find the last node
                    var lastNode = currentConversation[currentConversation.Count - 1];
                    // seperate multiple set changes grouped on one line
                    string[] changes = expression.Split(';');

                    // look at each set change individually
                    foreach (var change in changes)
                    {
                        // add the variable changes to their dialogue node
                        lastNode.variableChanges.Add(change.Trim());
                    }
                }

                continue;
            }

            // #If condition
            if (line.StartsWith("#If"))
            {
                // trim away the #If command
                string condition = line.Replace("#If", "").Trim();

                // safety check to make sure a conversation exists
                if (currentConversation != null && currentConversation.Count > 0)
                {
                    // find the last node
                    var lastNode = currentConversation[currentConversation.Count - 1];

                    // add required condition to that node
                    lastNode.requiredCondition = condition;
                }

                continue;
            }
        }
    }


}