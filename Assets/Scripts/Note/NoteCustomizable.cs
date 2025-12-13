using System;
using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class NoteCustomizable : MonoBehaviour
{

    private Button _customizeButton;
    private Button _fontIncreaseButton;
    private Button _fontDecreaseButton;
    private GameObject _customizationCanvas;
    private Toggle _autoSizeToggle;
    private TMP_Text _inputFieldText;
    private TMP_InputField _inputField;
    private TMP_Text _fontSizeText;
    private GameObject _ONBackground;

    public float fontSize;

    // Continuous button press settings
    [SerializeField] private float initialDelay = 0.5f; // Initial delay before continuous action starts
    [SerializeField] private float repeatRate = 0.1f; // Rate of repetition while holding

    private Coroutine _fontIncreaseCoroutine;
    private Coroutine _fontDecreaseCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupInputFieldText();
        SetupFontSizeText();
        SetupCustomizationCanvas();
        SetupCustomizeButton();
        SetupFontIncreaseButton();
        SetupFontDecreaseButton();    
    }

    // Update is called once per frame
    void Update()
    {

    }


    private void OnAutoSizeToggleChanged(bool arg0)
    {
        _ONBackground.SetActive(arg0);
        _inputFieldText.autoSizeTextContainer = arg0;
    }


    /// <summary>
    /// Sets up the customization canvas
    /// </summary>
    private void SetupCustomizationCanvas()
    {
        try
        {
            _customizationCanvas = transform.Find("Customization Canvas").gameObject;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteCustomizable] Failed to find customization canvas: {ex.Message}");
            return;
        }
        _customizationCanvas.SetActive(false);
    }


    /// <summary>
    /// Sets up the customize button as "Customization Canvas"->"Background"->"Customize Button"
    /// </summary>
    private void SetupCustomizeButton()
    {

        try
        {
            _customizeButton = transform.Find("Note Canvas").Find("Background").Find("Customize Button").GetComponent<Button>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteCustomizable] Failed to find customize button: {ex.Message}");
            return;
        }

        _customizeButton.onClick.AddListener(OnCustomizeButtonClicked);
    }

    /// <summary>
    /// Open/close the customization canvas when the customize button is clicked 
    /// </summary>
    private void OnCustomizeButtonClicked()
    {
        _customizationCanvas.SetActive(!_customizationCanvas.activeSelf);
    }


    /// <summary>
    /// Sets up the input field text as "Note Canvas"->"Background"->"InputField"->"Text Area"->"Text"
    /// </summary>
    private void SetupInputFieldText()
    {
        try
        {
            _inputFieldText = transform.Find("Note Canvas").Find("Background").Find("InputField").Find("Text Area").Find("Text").GetComponent<TMP_Text>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteCustomizable] Failed to find input field text: {ex.Message}");
            return;
        }

        try
        {
            _inputField = transform.Find("Note Canvas").Find("Background").Find("InputField").GetComponent<TMP_InputField>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteCustomizable] Failed to find input field: {ex.Message}");
            return;
        }

        _inputFieldText.fontSizeMax = fontSize;
        _inputField.onValueChanged.AddListener(OnInputFieldValueChanged);

        
    }

    private void OnInputFieldValueChanged(string _)
    {
        float newFontSize = Mathf.FloorToInt(_inputFieldText.fontSize);
        fontSize = newFontSize;
        _fontSizeText.text = fontSize.ToString();
        _inputFieldText.fontSizeMax = fontSize;
    }


    /// <summary>
    /// Sets up the font size text as "Customization Canvas"->"Background"->"Font Size Text Area"->"Text"
    /// </summary>
    private void SetupFontSizeText()
    {
        try
        {
            _fontSizeText = transform.Find("Customization Canvas").Find("Background").Find("Font Size Text Area").Find("Text").GetComponent<TMP_Text>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteCustomizable] Failed to find font size text: {ex.Message}");
            return;
        }

        _fontSizeText.text = fontSize.ToString();
    }


    /// <summary>
    /// Sets up the font increase button as "Customization Canvas"->"Background"->"Font Increase Button"
    /// </summary>
    private void SetupFontIncreaseButton()
    {
        try
        {
            _fontIncreaseButton = transform.Find("Customization Canvas").Find("Background").Find("Font Increase Button").GetComponent<Button>();
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[NoteCustomizable] Failed to find 'Font Increase Button': {ex.Message}");
            return;
        }

        // Add EventTrigger component for continuous press detection
        EventTrigger eventTrigger = _fontIncreaseButton.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = _fontIncreaseButton.gameObject.AddComponent<EventTrigger>();
        }

        // Add pointer down event
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { StartFontIncrease(); });
        eventTrigger.triggers.Add(pointerDownEntry);

        // Add pointer up event
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { StopFontIncrease(); });
        eventTrigger.triggers.Add(pointerUpEntry);

        // Add pointer exit event (in case user drags off button)
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { StopFontIncrease(); });
        eventTrigger.triggers.Add(pointerExitEntry);
    }

    /// <summary>
    /// Starts continuous font size increase
    /// </summary>
    private void StartFontIncrease()
    {
        if (_fontIncreaseCoroutine != null)
        {
            StopCoroutine(_fontIncreaseCoroutine);
        }
        _fontIncreaseCoroutine = StartCoroutine(ContinuousFontIncrease());
    }

    /// <summary>
    /// Stops continuous font size increase
    /// </summary>
    private void StopFontIncrease()
    {
        if (_fontIncreaseCoroutine != null)
        {
            StopCoroutine(_fontIncreaseCoroutine);
            _fontIncreaseCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine for continuous font size increase
    /// </summary>
    private IEnumerator ContinuousFontIncrease()
    {
        // First increment happens immediately
        IncrementFontSize();

        // Wait for initial delay
        yield return new WaitForSeconds(initialDelay);

        // Continue incrementing at repeat rate
        while (true)
        {
            IncrementFontSize();
            yield return new WaitForSeconds(repeatRate);
        }
    }

    /// <summary>
    /// Increments the font size by 1
    /// </summary>
    private void IncrementFontSize()
    {
        fontSize += 1;

        // to limit font size due to auto size constraints
        float currentFontSize = Mathf.FloorToInt(_inputFieldText.fontSize);
        if (fontSize > currentFontSize + 1)
        {
            fontSize = currentFontSize;
        }

        _fontSizeText.text = fontSize.ToString();
        _inputFieldText.fontSizeMax = fontSize;
    }

    /// <summary>
    /// Sets up the font decrease button as "Customization Canvas"->"Background"->"Font Decrease Button"
    /// </summary>
    private void SetupFontDecreaseButton()
    {
        try
        {
            _fontDecreaseButton = transform.Find("Customization Canvas").Find("Background").Find("Font Decrease Button").GetComponent<Button>();
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[NoteCustomizable] Failed to find 'Font Decrease Button': {ex.Message}");
            return;
        }

        // Add EventTrigger component for continuous press detection
        EventTrigger eventTrigger = _fontDecreaseButton.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = _fontDecreaseButton.gameObject.AddComponent<EventTrigger>();
        }

        // Add pointer down event
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { StartFontDecrease(); });
        eventTrigger.triggers.Add(pointerDownEntry);

        // Add pointer up event
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { StopFontDecrease(); });
        eventTrigger.triggers.Add(pointerUpEntry);

        // Add pointer exit event (in case user drags off button)
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { StopFontDecrease(); });
        eventTrigger.triggers.Add(pointerExitEntry);
    }

    /// <summary>
    /// Starts continuous font size decrease
    /// </summary>
    private void StartFontDecrease()
    {
        if (_fontDecreaseCoroutine != null)
        {
            StopCoroutine(_fontDecreaseCoroutine);
        }
        _fontDecreaseCoroutine = StartCoroutine(ContinuousFontDecrease());
    }

    /// <summary>
    /// Stops continuous font size decrease
    /// </summary>
    private void StopFontDecrease()
    {
        if (_fontDecreaseCoroutine != null)
        {
            StopCoroutine(_fontDecreaseCoroutine);
            _fontDecreaseCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine for continuous font size decrease
    /// </summary>
    private IEnumerator ContinuousFontDecrease()
    {
        // First decrement happens immediately
        DecrementFontSize();

        // Wait for initial delay
        yield return new WaitForSeconds(initialDelay);

        // Continue decrementing at repeat rate
        while (true)
        {
            DecrementFontSize();
            yield return new WaitForSeconds(repeatRate);
        }
    }

    /// <summary>
    /// Decrements the font size by 1
    /// </summary>
    private void DecrementFontSize()
    {
        fontSize -= 1;
        
        if (fontSize <= _inputFieldText.fontSizeMin)
        {
            fontSize = Mathf.CeilToInt(_inputFieldText.fontSizeMin);
        }

        _fontSizeText.text = fontSize.ToString();
        _inputFieldText.fontSizeMax = fontSize;
    }
}
