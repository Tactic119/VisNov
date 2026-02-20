using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneManager : MonoBehaviour
{
    [Header("Text")]
    private TextAsset textFile; // variable to hold the contets of 'Dialogue.txt'
    [SerializeField] private TMP_Text textBox; // text in the main dialogue box
    [SerializeField] private TMP_Text nameBox; // text in the small box for a chracter's name

    [Header("Conversations")]
    public string currentCharacter; // what character is currently speaking
    private List<List<string>> dialogueGroup = new List<List<string>>(); // stores messages, one conversation per list
    private List<List<string>> characterSpeaking = new List<List<string>>(); // stores what character is saying what message
    private int conversationIndex = 0; // what conversation we are on
    private int lineIndex = 0; // what line of the conversation we are on
    [SerializeField] private float typingSpeed = 0.05f; // how fast he type writer effect goes
    private Coroutine typingCoroutine; // evil
    private bool isTyping = false; // weather the message is dont typing or not

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
                // if message is still typing - instantly complete the message
                StopCoroutine(typingCoroutine);
                textBox.text = dialogueGroup[conversationIndex][lineIndex];
                isTyping = false;
            }
            else
            {
                // message is complete - move to next line
                NextLine();
            }
        }
    }

    // moves to the next line in the conversation
    void NextLine()
    {
        // conersation is outside of the values help by dialogueGroup
        if (conversationIndex >= dialogueGroup.Count)
            return;

        // lineIndex is within bounds - move to next line and update text
        if (lineIndex < dialogueGroup[conversationIndex].Count - 1)
        {
            lineIndex++;
            UpdateText();
        }
        else
        {
            // line Index would be out of bounds - don't go further
            Debug.Log("End of conversation");
        }
    }

    void UpdateText()
    {
        // name of character speaking is instantly set
        nameBox.text = characterSpeaking[conversationIndex][lineIndex];

        // if a coroutine is currently going - fell it with a big sword
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // current message
        string dialogue = dialogueGroup[conversationIndex][lineIndex];

        // start coroutine (types message character by character until its complete)
        typingCoroutine = StartCoroutine(TypeText(dialogue));
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
        // seperate text by lines and shove theme into an array of strings
        string[] lines = text.Split('\n');

        List<string> currentDialogueList = null;
        List<string> currentCharacterList = null;

        string currentCharacter = "";

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith("--"))
                continue;

            // new conversation started with "#Conversation"
            if (line.StartsWith("#Conversation"))
            {
                currentDialogueList = new List<string>();
                currentCharacterList = new List<string>();

                dialogueGroup.Add(currentDialogueList);
                characterSpeaking.Add(currentCharacterList);

                continue;
            }

            // line with "Character_" tells us the character who is currently speaking
            if (line.StartsWith("Character_"))
            {
                currentCharacter = line.Replace("Character_", "");
                continue;
            }

            // dialogue line marked by quotations
            if (line.StartsWith("\"") && line.EndsWith("\""))
            {
                if (currentDialogueList == null)
                    continue; // safety check

                string cleanedLine = line.Trim('"');

                currentDialogueList.Add(cleanedLine);
                currentCharacterList.Add(currentCharacter);
            }
        }
    }
}