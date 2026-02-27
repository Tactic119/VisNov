using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [Header("Characters")]
    public List<Character> characters;
    private Dictionary<string, Character> characterDictionary;

    [Header("CharacterDisplays")]
    public CharacterDisplay willowDisplay;

    [System.Serializable]
    public class Character
    {
        public string characterName;

        [Header("Sprites")]
        public List<SpriteEntry> fullBodySpriteEntries;
        public List<SpriteEntry> halfBodySpriteEntries;

        // runtime dictionaries
        private Dictionary<string, SpriteEntry> fullBodyEntries;
        private Dictionary<string, SpriteEntry> halfBodyEntries;

        public void Initialize()
        {
            fullBodyEntries = new Dictionary<string, SpriteEntry>();
            halfBodyEntries = new Dictionary<string, SpriteEntry>();

            // loop through all full body sprites to asign them to the fullBodyEntries dictionary
            foreach (var entry in fullBodySpriteEntries)
            {
                // add sprite to the runtime dictionary
                if (!fullBodyEntries.ContainsKey(entry.key))
                    fullBodyEntries.Add(entry.key, entry);
                else
                    Debug.LogWarning($"Duplicate full body key '{entry.key}' on {characterName}");
            }

            // loop through all full half sprites to asign them to the halfBodyEntries dictionary
            foreach (var entry in halfBodySpriteEntries)
            {
                // add sprite to the runtime dictionary
                if (!halfBodyEntries.ContainsKey(entry.key))
                    halfBodyEntries.Add(entry.key, entry);
                else
                    Debug.LogWarning($"Duplicate half body key '{entry.key}' on {characterName}");
            }
        }

        // use key to get full body sprite entry
        public SpriteEntry GetFullBodyEntry(string key)
        {
            fullBodyEntries.TryGetValue(key, out SpriteEntry entry);
            return entry;
        }

        // use key to get half body sprite entry
        public SpriteEntry GetHalfBodyEntry(string key)
        {
            halfBodyEntries.TryGetValue(key, out SpriteEntry entry);
            return entry;
        }
    }

    // each sprite has this class attached to it, containing its key, sprite, and offset
    [System.Serializable]
    public class SpriteEntry
    {
        public string key;
        public Sprite sprite;
        public Vector2 offset;
    }

    void Awake()
    {
        // runtime dictionary that holds every character
        characterDictionary = new Dictionary<string, Character>();

        // runs throught every character
        foreach (var character in characters)
        {
            // runs the initialize method of each character and places them in the character dictionary
            character.Initialize();
            characterDictionary[character.characterName] = character;
        }

        // assign willow display using object name
        willowDisplay = GameObject.Find("WillowSprite").GetComponent<CharacterDisplay>();
    }

    private void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.E))
        {
            willowDisplay.PlayFullBodyLoop(
                new List<string> { "walk1", "walk2", "walk3", "walk4" },
                0.3f
                );
        }
    }

    // return full body character sprite using character name and dictionary key 
    public SpriteEntry GetCharacterFullBodyEntry(string characterName, string key)
    {
        if (characterDictionary.TryGetValue(characterName, out Character character))
        {
            return character.GetFullBodyEntry(key);
        }
        // safety check if sprite can't be found
        Debug.LogWarning($"Character '{characterName}' not found.");
        return null;
    }

    // return half body character sprite using character name and dictionary key 
    public SpriteEntry GetCharacterHalfBodyEntry(string characterName, string key)
    {
        if (characterDictionary.TryGetValue(characterName, out Character character))
        {
            return character.GetHalfBodyEntry(key);
        }
        // safety check if sprite can't be found
        Debug.LogWarning($"Character '{characterName}' not found.");
        return null;
    }
}