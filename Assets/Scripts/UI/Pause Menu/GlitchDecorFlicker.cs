using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlitchDecorFlicker : MonoBehaviour
{
    [System.Serializable]
    public class DecorElement
    {
        public Graphic graphic;
        [Range(0f, 1f)] public float minOpacity = 0.3f;
        [Range(0f, 1f)] public float maxOpacity = 1f;
    }

    [SerializeField] private List<DecorElement> decorElements = new List<DecorElement>();
    [SerializeField] private float minInterval = 0.5f;
    [SerializeField] private float maxInterval = 3f;
    [SerializeField] private float flickerDuration = 0.08f;

    private void OnEnable()
    {
        foreach (var el in decorElements)
        {
            if (el.graphic != null)
                StartCoroutine(FlickerLoop(el));
        }
    }

    private IEnumerator FlickerLoop(DecorElement el)
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSecondsRealtime(wait);

            if (!gameObject.activeInHierarchy) yield break;

            float targetOpacity = Random.Range(el.minOpacity, el.maxOpacity);
            yield return StartCoroutine(FlickerBurst(el.graphic, targetOpacity));
        }
    }

    private IEnumerator FlickerBurst(Graphic graphic, float targetOpacity)
    {
        Color original = graphic.color;
        int flickers = Random.Range(1, 3);

        for (int i = 0; i < flickers; i++)
        {
            float t = 0f;
            while (t < flickerDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(original.a, targetOpacity, t / flickerDuration);
                graphic.color = new Color(original.r, original.g, original.b, a);
                yield return null;
            }

            t = 0f;
            while (t < flickerDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(targetOpacity, original.a, t / flickerDuration);
                graphic.color = new Color(original.r, original.g, original.b, a);
                yield return null;
            }
        }

        graphic.color = original;
    }
}