using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DialogueManager;

public class SceneManager : MonoBehaviour
{
    [Header("Characters")]
    public List<Character> characters;
    private Dictionary<string, Character> characterDictionary;

    [Header("Character Displays")]
    public CharacterDisplay willowDisplay;
    
    private Coroutine currentLoop;


    [System.Serializable]
    public class Character
    {
        public string characterName;

        [Header("Sprites")]
        // out of conversation (in background)
        public List<SpriteEntry> fullBodySpriteEntries;
        // in conversation (talking)
        public List<SpriteEntry> halfBodySpriteEntries;

        // runtime dictionaries
        private Dictionary<string, Sprite> fullBodySprites;
        private Dictionary<string, Sprite> halfBodySprites;

        // builds dictionaries from the lists
        public void Initialize()
        {
            // initialize runtime dictionaries
            fullBodySprites = new Dictionary<string, Sprite>();
            halfBodySprites = new Dictionary<string, Sprite>();

            // set up full body sprite dictionary 
            foreach (var entry in fullBodySpriteEntries)
            {
                // prevent duplicate entrys
                if (!fullBodySprites.ContainsKey(entry.key))
                    fullBodySprites.Add(entry.key, entry.sprite);
                else
                    Debug.LogWarning($"Duplicate full body key '{entry.key}' on {characterName}");
            }

            // set up half body sprite dictionary
            foreach (var entry in halfBodySpriteEntries)
            {
                // prevent duplicate entrys
                if (!halfBodySprites.ContainsKey(entry.key))
                    halfBodySprites.Add(entry.key, entry.sprite);
                else
                    Debug.LogWarning($"Duplicate half body key '{entry.key}' on {characterName}");
            }
        }

        // returns full body sprite based on key input
        public Sprite GetFullBody(string key)
        {
            fullBodySprites.TryGetValue(key, out Sprite sprite);
            return sprite;
        }

        // returns half body sprite based on key input
        public Sprite GetHalfBody(string key)
        {
            halfBodySprites.TryGetValue(key, out Sprite sprite);
            return sprite;
        }


    }

    // contains one entry in a sprite dictionary
    [System.Serializable]
    public class SpriteEntry
    {
        public string key;     
        public Sprite sprite;
    }


    void Start()
    {

    }

    void Awake()
    {
        // create dictionary of characters
        characterDictionary = new Dictionary<string, Character>();

        // set up sprite dictionaries
        foreach (var character in characters)
        {
            character.Initialize();
            characterDictionary[character.characterName] = character;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            willowDisplay.PlayFullBodyLoop(
                new List<string> {"walk1","walk2","walk3","walk4"},
                0.30f
                );
        }
    }

    // get a full body sprite of a character based on name and key input
    public Sprite GetCharacterFullBody(string characterName, string expressionKey)
    {
        if (characterDictionary.TryGetValue(characterName, out Character character))
        {
            return character.GetFullBody(expressionKey);
        }

        Debug.LogWarning($"Character '{characterName}' not found.");
        return null;
    }

    // get a half body sprite of a character based on name and key input
    public Sprite GetCharacterHalfBody(string characterName, string expressionKey)
    {
        if (characterDictionary.TryGetValue(characterName, out Character character))
        {
            return character.GetHalfBody(expressionKey);
        }

        Debug.LogWarning($"Character '{characterName}' not found.");
        return null;
    }



}
