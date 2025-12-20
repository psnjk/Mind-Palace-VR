using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// A reusable component for circular delete buttons that require a long press (2 seconds) 
/// to prevent accidental deletions, with radial progress visual feedback.
/// </summary>
[RequireComponent(typeof(Button))]
public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Long Press Settings")]
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float hoverScaleMultiplier = 1.1f;
    [SerializeField] private float scaleAnimationSpeed = 8f;

    [Header("Visual Feedback")]
    [SerializeField] private Image progressImage;
    [SerializeField] private Color progressColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    // Events that can be subscribed to
    public System.Action OnDeletePressed;
    public System.Action OnDeleteCancelled;
    public System.Action<float> OnProgressChanged; // 0-1 progress value

    private Vector3 _originalScale;
    private Transform _buttonTransform;
    private Button _button;
    private AudioSource _audioSource;

    private bool _isPressed = false;
    private bool _isHovered = false;
    private float _currentProgress = 0f;
    private Coroutine _holdCoroutine;
    private Coroutine _scaleCoroutine;
    private Coroutine _audioTickCoroutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _buttonTransform = transform;
        _originalScale = _buttonTransform.localScale;


        // Setup progress image if not assigned
        if (!progressImage)
        {
            Debug.LogWarning($"[LongPressButton] No progress image assigned on {gameObject.name}");
        }
        else
        {
            // Configure the assigned progress image
            ConfigureProgressImage();
        }

        // Initialize visual state
        ResetVisualState();

            Debug.Log($"[LongPressButton] Initialized for: {gameObject.name}");
            Debug.Log($"[LongPressButton] Progress image assigned: {(progressImage != null ? progressImage.name : "None")}");
            if (progressImage)
            {
                Debug.Log($"[LongPressButton] Progress image type: {progressImage.type}");
                Debug.Log($"[LongPressButton] Progress image fill method: {progressImage.fillMethod}");
                Debug.Log($"[LongPressButton] Progress image fill amount: {progressImage.fillAmount}");
            }
    }

    private void Start()
    {
        // Additional validation in Start to ensure everything is properly set up
        if (progressImage)
        {
            Debug.Log($"[LongPressButton] Start() - Progress image validation:");
            Debug.Log($"  - Active: {progressImage.gameObject.activeInHierarchy}");
            Debug.Log($"  - Enabled: {progressImage.enabled}");
            Debug.Log($"  - Color: {progressImage.color}");
            Debug.Log($"  - Fill Amount: {progressImage.fillAmount}");
        }
    }

    private void OnDestroy()
    {
        // Clean up coroutines
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
        }
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }
        if (_audioTickCoroutine != null)
        {
            StopCoroutine(_audioTickCoroutine);
        }
    }

    /// <summary>
    /// Configures an assigned progress image to work with radial filling
    /// </summary>
    private void ConfigureProgressImage()
    {
        if (!progressImage) return;

        // Ensure the image is set up for radial filling
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.fillOrigin = (int)Image.Origin360.Top;
        progressImage.fillClockwise = true;
        progressImage.fillAmount = 0f;

            Debug.Log($"[LongPressButton] Configured assigned progress image: {progressImage.name}");
    }

    /// <summary>
    /// Resets all visual elements to their default state
    /// </summary>
    private void ResetVisualState()
    {
        _currentProgress = 0f;

        if (progressImage)
        {
            progressImage.fillAmount = 0f;
            progressImage.color = defaultColor;

            Debug.Log($"[LongPressButton] Reset visual state - Fill amount: {progressImage.fillAmount}, Color: {progressImage.color}");
        }

        OnProgressChanged?.Invoke(0f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        _isPressed = true;
        StartHoldProcess();

        Debug.Log($"[LongPressButton] Started hold process for: {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isPressed)
        {
            _isPressed = false;
            CancelHoldProcess();

            Debug.Log($"[LongPressButton] Cancelled hold process for: {gameObject.name}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        AnimateScale(_originalScale * hoverScaleMultiplier);

        Debug.Log($"[LongPressButton] Pointer entered: {gameObject.name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        AnimateScale(_originalScale);

        // Cancel hold if pointer exits while pressed
        if (_isPressed)
        {
            _isPressed = false;
            CancelHoldProcess();

            Debug.Log($"[LongPressButton] Cancelled due to pointer exit: {gameObject.name}");
        }
    }

    /// <summary>
    /// Starts the hold process with visual and audio feedback
    /// </summary>
    private void StartHoldProcess()
    {
        // Stop any existing hold process
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
        }
        if (_audioTickCoroutine != null)
        {
            StopCoroutine(_audioTickCoroutine);
        }

        // Start the hold coroutine
        _holdCoroutine = StartCoroutine(HoldProgressCoroutine());


        Debug.Log($"[LongPressButton] Hold process started, progress image null: {progressImage == null}");
    }

    /// <summary>
    /// Cancels the hold process and resets visual state
    /// </summary>
    private void CancelHoldProcess()
    {
        // Stop coroutines
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }
        if (_audioTickCoroutine != null)
        {
            StopCoroutine(_audioTickCoroutine);
            _audioTickCoroutine = null;
        }

        if (_currentProgress > 0.1f)
        {
            OnDeleteCancelled?.Invoke();
        }

        // Reset visual state
        ResetVisualState();
    }

    /// <summary>
    /// Coroutine that handles the hold progress and visual feedback
    /// </summary>
    private IEnumerator HoldProgressCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < holdDuration && _isPressed)
        {
            elapsedTime += Time.deltaTime;
            _currentProgress = Mathf.Clamp01(elapsedTime / holdDuration);

            // Apply animation curve to progress
            float curvedProgress = progressCurve.Evaluate(_currentProgress);

            // Update visual feedback
            if (progressImage != null)
            {
                progressImage.fillAmount = curvedProgress;
                progressImage.color = Color.Lerp(defaultColor, progressColor, curvedProgress);

            }
            else
            {
                Debug.LogWarning($"[LongPressButton] Progress image is null during update!");
            }

            // Notify progress change
            OnProgressChanged?.Invoke(_currentProgress);

            yield return null;
        }

        // Check if we completed the hold duration
        if (_currentProgress >= 1f && _isPressed)
        {
            CompleteDelete();
        }

        _holdCoroutine = null;
    }

    /// <summary>
    /// Completes the delete action
    /// </summary>
    private void CompleteDelete()
    {

        // Trigger delete event
        OnDeletePressed?.Invoke();

        // Reset visual state
        ResetVisualState();

            Debug.Log($"[LongPressButton] Delete completed for: {gameObject.name}");
    }

    /// <summary>
    /// Smoothly animates the button's scale
    /// </summary>
    /// <param name="targetScale">The target scale to animate to</param>
    private void AnimateScale(Vector3 targetScale)
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(AnimateScaleCoroutine(targetScale));
    }

    /// <summary>
    /// Coroutine for scale animation
    /// </summary>
    private IEnumerator AnimateScaleCoroutine(Vector3 targetScale)
    {
        if (_buttonTransform == null) yield break;

        Vector3 startScale = _buttonTransform.localScale;
        float elapsedTime = 0f;

        while (Vector3.Distance(_buttonTransform.localScale, targetScale) > 0.001f)
        {
            elapsedTime += Time.deltaTime;
            _buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime * scaleAnimationSpeed);
            yield return null;
        }

        _buttonTransform.localScale = targetScale;
        _scaleCoroutine = null;
    }

    /// <summary>
    /// Plays an audio clip if available
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Public method to manually trigger the delete (for testing or external calls)
    /// </summary>
    public void TriggerDelete()
    {
        OnDeletePressed?.Invoke();
        ResetVisualState();
    }

    /// <summary>
    /// Public method to manually cancel the current hold process
    /// </summary>
    public void CancelCurrentHold()
    {
        if (_isPressed)
        {
            _isPressed = false;
            CancelHoldProcess();
        }
    }

    /// <summary>
    /// Gets the current progress value (0-1)
    /// </summary>
    public float GetCurrentProgress()
    {
        return _currentProgress;
    }

    /// <summary>
    /// Checks if the button is currently being held
    /// </summary>
    public bool IsCurrentlyPressed()
    {
        return _isPressed;
    }

    /// <summary>
    /// Test method to manually animate the progress (for debugging)
    /// </summary>
    [ContextMenu("Test Progress Animation")]
    public void TestProgressAnimation()
    {
        StartCoroutine(TestProgressCoroutine());
    }

    private IEnumerator TestProgressCoroutine()
    {
        Debug.Log("[LongPressButton] Starting test animation");

        for (float t = 0f; t <= 1f; t += Time.deltaTime / 2f) // 2-second test
        {
            if (progressImage != null)
            {
                progressImage.fillAmount = t;
                progressImage.color = Color.Lerp(defaultColor, progressColor, t);
                Debug.Log($"[LongPressButton] Test progress: {t:F2}, Fill: {progressImage.fillAmount:F2}");
            }
            yield return null;
        }

        // Reset
        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
            progressImage.color = defaultColor;
        }

        Debug.Log("[LongPressButton] Test animation complete");
    }
}