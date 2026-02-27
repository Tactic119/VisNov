using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDisplay : MonoBehaviour
{
    [Header("Character")]
    // character name
    public string characterName;

    [Header("Rendering")]
    // reference to sprite renderer
    public SpriteRenderer targetRenderer;

    // reference to scene manager
    private SceneManager sceneManager;
    // current animation loop being played
    private Coroutine currentLoop;
    // default local position of the object
    private Vector3 baseLocalPosition;

    void Awake()
    {
        // assign scene manger reference with tag
        sceneManager = GameObject.FindWithTag("ManagerObject").GetComponent<SceneManager>();

        // if sprite renderer is not assigned in inpector - automatically get the sprite renderer of the object this script is attached to
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();

        baseLocalPosition = transform.localPosition;
    }

    // starts a looping animation using sprite keys
    public void PlayFullBodyLoop(List<string> keys, float delay)
    {
        // stop current loop if there is one
        if (currentLoop != null) StopCoroutine(currentLoop);

        // start coroutine loop of sprites
        currentLoop = StartCoroutine(LoopSprites(keys, delay));
    }

    // coroutine for looping through sprites to create animation effect
    private IEnumerator LoopSprites(List<string> keys, float delay)
    {
        // empty list of sprites
        if (keys == null || keys.Count == 0) yield break;

        // start at first sprite in the list
        int index = 0;

        // loop through sprites until stopped
        while (true)
        {
            // get sprite from scene manager using characer name and key
            var entry = sceneManager.GetCharacterFullBodyEntry(characterName, keys[index]);

            // set new sprite and new offset
            if (entry != null)
            {
                targetRenderer.sprite = entry.sprite;
                transform.localPosition = baseLocalPosition + (Vector3)entry.offset;
            }

            // increase index by one - if it reaches the end of sprite loop the remainder will reset the loop back to the first sprite
            index = (index + 1) % keys.Count;

            // pause for the length of delay in seconds
            yield return new WaitForSeconds(delay);
        }
    }

    // end current animation loop
    public void StopLoop()
    {
        // if there is a loop going - stop it
        if (currentLoop != null)
        {
            StopCoroutine(currentLoop);
            currentLoop = null;
        }
    }
}