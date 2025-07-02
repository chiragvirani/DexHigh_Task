using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Fades a CanvasGroup's alpha from start to end over duration.
    /// </summary>
    public IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float start, float end, float duration, Action onComplete = null)
    {
        float currentTime = 0f;
        canvasGroup.alpha = start;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, currentTime / duration);
            yield return null;
        }
        canvasGroup.alpha = end;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Moves a RectTransform's anchoredPosition from start to end over duration.
    /// </summary>
    public IEnumerator MoveAnchoredPosition(RectTransform rect, Vector2 start, Vector2 end, float duration, Action onComplete = null)
    {
        float currentTime = 0f;
        rect.anchoredPosition = start;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, end, currentTime / duration);
            yield return null;
        }
        rect.anchoredPosition = end;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Changes a RectTransform's sizeDelta (width and height) from start to end over duration.
    /// </summary>
    public IEnumerator ChangeSize(RectTransform rect, Vector2 start, Vector2 end, float duration, Action onComplete = null)
    {
        float currentTime = 0f;
        rect.sizeDelta = start;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            rect.sizeDelta = Vector2.Lerp(start, end, currentTime / duration);
            yield return null;
        }
        rect.sizeDelta = end;
        onComplete?.Invoke();
    }
}