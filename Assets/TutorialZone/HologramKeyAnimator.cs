using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HologramKeyAnimator : MonoBehaviour
{
    [System.Serializable]
    public class KeySpriteSet
    {
        public Image keyImage;          // Image din UI
        public Sprite normalSprite;     // sprite normal
        public Sprite pressedSprite;    // sprite pressed
    }

    [Header("Keys")]
    [Tooltip("Adaugă aici 1 tastă (Space, Shift, E) sau mai multe (WASD).")]
    public List<KeySpriteSet> keys = new List<KeySpriteSet>();

    [Header("Animation Settings")]
    public float minDelayBetweenPresses = 0.3f;
    public float maxDelayBetweenPresses = 1.2f;

    public float pressedDuration = 0.15f;

    private void OnEnable()
    {
        // când holograma se activează, pornim coroutine
        StartCoroutine(RandomKeyPressRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ResetAllKeysToNormal();
    }

    private void ResetAllKeysToNormal()
    {
        if (keys == null)
            return;

        foreach (var key in keys)
        {
            if (key != null && key.keyImage != null && key.normalSprite != null)
                key.keyImage.sprite = key.normalSprite;
        }
    }

    private IEnumerator RandomKeyPressRoutine()
    {
        if (keys == null || keys.Count == 0)
            yield break;

        while (true)
        {
            // așteptăm un interval random
            float delay = Random.Range(minDelayBetweenPresses, maxDelayBetweenPresses);
            yield return new WaitForSeconds(delay);

            // alegem random una dintre taste, indiferent de câte sunt (1 sau mai multe)
            KeySpriteSet randomKey = keys[Random.Range(0, keys.Count)];

            if (randomKey == null || randomKey.keyImage == null ||
                randomKey.normalSprite == null || randomKey.pressedSprite == null)
                continue;

            // schimbăm la pressed
            randomKey.keyImage.sprite = randomKey.pressedSprite;

            // menținem puțin
            yield return new WaitForSeconds(pressedDuration);

            // revenim la normal
            randomKey.keyImage.sprite = randomKey.normalSprite;
        }
    }
}