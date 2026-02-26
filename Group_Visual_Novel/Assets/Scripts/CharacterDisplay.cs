using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDisplay : MonoBehaviour
{
    public string characterName;
    public SpriteRenderer targetRenderer;

    private SceneManager sceneManager;
    private Coroutine currentLoop;

    void Awake()
    {
        sceneManager = GameObject.FindWithTag("ManagerObject").GetComponent<SceneManager>();

        // Auto-assign if left empty
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
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
            Sprite sprite = sceneManager
                .GetCharacterFullBody(characterName, keys[index]);

            if (sprite != null)
                targetRenderer.sprite = sprite;

            index = (index + 1) % keys.Count;

            yield return new WaitForSeconds(delay);
        }
    }

    public void StopLoop()
    {
        if (currentLoop != null)
            StopCoroutine(currentLoop);
    }
}