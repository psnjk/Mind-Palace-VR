using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// A reusable component that makes buttons expand when hovered for better VR interaction.
/// </summary>
[RequireComponent(typeof(Button))]
public class ExpandingButton : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleMultiplier = 1.2f;
    [SerializeField] private float scaleAnimationSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = false;

    private Vector3 _originalScale;
    private Transform _buttonTransform;
    private Coroutine _scalingCoroutine;
    private Button _button;
    private EventTrigger _eventTrigger;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _buttonTransform = transform;
        _originalScale = _buttonTransform.localScale;

        SetupEventTrigger();
    }

    private void OnDestroy()
    {
        // Clean up coroutine if the object is destroyed while scaling
        if (_scalingCoroutine != null)
        {
            StopCoroutine(_scalingCoroutine);
        }
    }

    /// <summary>
    /// Sets up the EventTrigger component for hover detection
    /// </summary>
    private void SetupEventTrigger()
    {
        _eventTrigger = GetComponent<EventTrigger>();
        if (_eventTrigger == null)
        {
            _eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing entries to avoid conflicts
        _eventTrigger.triggers.Clear();

        // Add pointer enter event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnHoverEnter(); });
        _eventTrigger.triggers.Add(pointerEnter);

        // Add pointer exit event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnHoverExit(); });
        _eventTrigger.triggers.Add(pointerExit);

        if (enableDebugLogging)
        {
            Debug.Log($"[ExpandingButton] Successfully set up hover effects for button: {gameObject.name}");
        }
    }

    /// <summary>
    /// Called when the button is hovered
    /// </summary>
    private void OnHoverEnter()
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[ExpandingButton] Button hover enter: {gameObject.name}");
        }

        // Stop any existing scaling coroutine
        if (_scalingCoroutine != null)
        {
            StopCoroutine(_scalingCoroutine);
        }

        // Start scaling to hover size
        Vector3 targetScale = Vector3.Scale(_originalScale, Vector3.one * hoverScaleMultiplier);
        _scalingCoroutine = StartCoroutine(AnimateButtonScale(targetScale));
    }

    /// <summary>
    /// Called when the button hover ends
    /// </summary>
    private void OnHoverExit()
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[ExpandingButton] Button hover exit: {gameObject.name}");
        }

        // Stop any existing scaling coroutine
        if (_scalingCoroutine != null)
        {
            StopCoroutine(_scalingCoroutine);
        }

        // Start scaling back to original size
        _scalingCoroutine = StartCoroutine(AnimateButtonScale(_originalScale));
    }

    /// <summary>
    /// Smoothly animates the button's scale to the target scale
    /// </summary>
    /// <param name="targetScale">The target scale to animate to</param>
    /// <returns></returns>
    private IEnumerator AnimateButtonScale(Vector3 targetScale)
    {
        if (_buttonTransform == null) yield break;

        Vector3 startScale = _buttonTransform.localScale;
        float elapsedTime = 0f;

        // Continue until we're close enough to the target
        while (Vector3.Distance(_buttonTransform.localScale, targetScale) > 0.001f)
        {
            elapsedTime += Time.deltaTime;
            _buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime * scaleAnimationSpeed);
            yield return null;
        }

        // Ensure we end up exactly at the target scale
        _buttonTransform.localScale = targetScale;

        // Clear the coroutine reference
        _scalingCoroutine = null;
    }
}