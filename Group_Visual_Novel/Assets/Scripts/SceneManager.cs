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

            foreach (var entry in fullBodySpriteEntries)
            {
                if (!fullBodyEntries.ContainsKey(entry.key))
                    fullBodyEntries.Add(entry.key, entry);
                else
                    Debug.LogWarning($"Duplicate full body key '{entry.key}' on {characterName}");
            }

            foreach (var entry in halfBodySpriteEntries)
            {
                if (!halfBodyEntries.ContainsKey(entry.key))
                    halfBodyEntries.Add(entry.key, entry);
                else
                    Debug.LogWarning($"Duplicate half body key '{entry.key}' on {characterName}");
            }
        }

        public SpriteEntry GetFullBodyEntry(string key)
        {
            fullBodyEntries.TryGetValue(key, out SpriteEntry entry);
            return entry;
        }

        public SpriteEntry GetHalfBodyEntry(string key)
        {
            halfBodyEntries.TryGetValue(key, out SpriteEntry entry);
            return entry;
        }
    }

    [System.Serializable]
    public class SpriteEntry
    {
        public string key;
        public Sprite sprite;
        public Vector2 offset;
    }

    void Awake()
    {
        characterDictionary = new Dictionary<string, Character>();

        foreach (var character in characters)
        {
            character.Initialize();
            characterDictionary[character.characterName] = character;
        }

        willowDisplay = GameObject.Find("WillowSprite").GetComponent<CharacterDisplay>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            willowDisplay.PlayFullBodyLoop(
                new List<string> { "walk1", "walk2", "walk3", "walk4" },
                0.3f
                );
        }
    }


    public SpriteEntry GetCharacterFullBodyEntry(string characterName, string key)
    {
        if (characterDictionary.TryGetValue(characterName, out Character character))
        {
            return character.GetFullBodyEntry(key);
        }

        Debug.LogWarning($"Character '{characterName}' not found.");
        return null;
    }

    public SpriteEntry GetCharacterHalfBodyEntry(string characterName, string key)
    {
        if (characterDictionary.TryGetValue(characterName, out Character character))
        {
            return character.GetHalfBodyEntry(key);
        }

        Debug.LogWarning($"Character '{characterName}' not found.");
        return null;
    }
}