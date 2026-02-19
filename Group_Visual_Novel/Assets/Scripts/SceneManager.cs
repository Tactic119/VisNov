using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneManager : MonoBehaviour
{
    [Header("Text")]
    private TextAsset textFile;
    [SerializeField] private TMP_Text textBox;
    [SerializeField] private TMP_Text nameBox;

    [Header("Conversations")]
    public string currentCharacter;
    private List<List<string>> dialogueGroup = new List<List<string>>();
    private List<List<string>> characterSpeaking = new List<List<string>>();
    private int conversationIndex = 0;
    private int lineIndex = 0;

    [SerializeField] private float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    void Start()
    {
        textFile = Resources.Load<TextAsset>("dialogue");

        if (textFile == null)
        {
            Debug.LogError("Dialogue file not found!");
            return;
        }

        ParseDialogue(textFile.text);

        UpdateText();
    }

    void Update()
    {
        PlayerInput(); 
    }

    void PlayerInput()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);
                textBox.text = dialogueGroup[conversationIndex][lineIndex];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    void NextLine()
    {
        if (conversationIndex >= dialogueGroup.Count)
            return;

        if (lineIndex < dialogueGroup[conversationIndex].Count - 1)
        {
            lineIndex++;
            UpdateText();
        }
        else
        {
            Debug.Log("End of conversation");
        }
    }

    void UpdateText()
    {
        nameBox.text = characterSpeaking[conversationIndex][lineIndex];

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        string dialogue =
            dialogueGroup[conversationIndex][lineIndex];

        typingCoroutine = StartCoroutine(TypeText(dialogue));
    }

    IEnumerator TypeText(string dialogue)
    {
        isTyping = true;
        textBox.text = "";

        foreach (char letter in dialogue)
        {
            textBox.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    // Seprate lines of text in Dialogue.txt and decode them
    void ParseDialogue(string text)
    {
        string[] lines = text.Split('\n');

        List<string> currentDialogueList = null;
        List<string> currentCharacterList = null;

        string currentCharacter = "";

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith("--"))
                continue;

            // NEW CONVERSATION MARKER
            if (line.StartsWith("#Conversation"))
            {
                currentDialogueList = new List<string>();
                currentCharacterList = new List<string>();

                dialogueGroup.Add(currentDialogueList);
                characterSpeaking.Add(currentCharacterList);

                continue;
            }

            // CHARACTER LINE
            if (line.StartsWith("Character_"))
            {
                currentCharacter = line.Replace("Character_", "");
                continue;
            }

            // DIALOGUE LINE
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