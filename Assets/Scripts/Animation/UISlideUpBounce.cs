using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UISlideUpBounce : MonoBehaviour
{
    RectTransform rect;
    CanvasGroup canvasGroup;

    Vector2 shownPos;
    Vector2 hiddenPos;

    [Header("Animation")]
    [Tooltip("How far below the shown position the popup starts")]
    public float hiddenYOffset = -0.2f;

    [Tooltip("How far past the shown position the popup overshoots")]
    public float overshoot = 0.01f;

    public float duration = 0.35f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Save the designer-set visible position
        shownPos = rect.anchoredPosition;

        // Hidden position below
        hiddenPos = shownPos + Vector2.up * hiddenYOffset;

        // Start hidden
        rect.anchoredPosition = hiddenPos;
        //canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        rect.anchoredPosition = hiddenPos;
        //canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(ShowBounce());
    }

    public void Hide()
    {
        StopAllCoroutines();

        //canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(HideSlide());
    }

    IEnumerator ShowBounce()
    {
        float t = 0f;

        // Phase 1: slide past the target (ease out)
        Vector2 overshootPos = shownPos + Vector2.up * overshoot;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = EaseOutCubic(t / duration);
            rect.anchoredPosition = Vector2.Lerp(hiddenPos, overshootPos, p);
            yield return null;
        }

        // Phase 2: settle back to final position (quick)
        t = 0f;
        float settleTime = duration * 0.35f;

        while (t < settleTime)
        {
            t += Time.deltaTime;
            float p = EaseOutCubic(t / settleTime);
            rect.anchoredPosition = Vector2.Lerp(overshootPos, shownPos, p);
            yield return null;
        }

        rect.anchoredPosition = shownPos;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    IEnumerator HideSlide()
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = EaseInCubic(t / duration);
            rect.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, p);
            yield return null;
        }

        rect.anchoredPosition = hiddenPos;
        gameObject.SetActive(false);
    }

    float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    float EaseInCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * x;
    }
}
