using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDisplay : MonoBehaviour
{
    [Header("Character")]
    public string characterName;

    [Header("Rendering")]
    public SpriteRenderer targetRenderer;

    private SceneManager sceneManager;
    private Coroutine currentLoop;
    private Vector3 baseLocalPosition;

    void Awake()
    {
        sceneManager = GameObject.FindWithTag("ManagerObject")
            .GetComponent<SceneManager>();

        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        baseLocalPosition = transform.localPosition;
    }

    public void PlayFullBodyLoop(List<string> keys, float delay)
    {
        if (currentLoop != null)
            StopCoroutine(currentLoop);

        currentLoop = StartCoroutine(LoopSprites(keys, delay));
    }

    private IEnumerator LoopSprites(List<string> keys, float delay)
    {
        if (keys == null || keys.Count == 0)
            yield break;

        int index = 0;

        while (true)
        {
            var entry = sceneManager
                .GetCharacterFullBodyEntry(characterName, keys[index]);

            if (entry != null)
            {
                targetRenderer.sprite = entry.sprite;
                transform.localPosition = baseLocalPosition + (Vector3)entry.offset;
            }

            index = (index + 1) % keys.Count;
            yield return new WaitForSeconds(delay);
        }
    }

    // end current animation loop
    public void StopLoop()
    {
        if (currentLoop != null)
        {
            StopCoroutine(currentLoop);
            currentLoop = null;
        }
    }
}