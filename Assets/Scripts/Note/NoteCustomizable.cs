using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum NoteTextAlignHorizontal
{
    Left,
    Center,
    Right,
    Justified
}

public enum NoteTextAlignVertical
{
    Top,
    Middle,
    Bottom
}

public class NoteCustomizable : MonoBehaviour
{

    // Public variables (Should be saved to JSON later and used to construct the note)
    public float fontSize;
    public NoteTextAlignHorizontal textAlignHorizontal = NoteTextAlignHorizontal.Left;
    public NoteTextAlignVertical textAlignVertical = NoteTextAlignVertical.Middle;


    private Button _customizeButton;
    private Button _fontIncreaseButton;
    private Button _fontDecreaseButton;
    private GameObject _customizationCanvas;
    private Toggle _autoSizeToggle;
    private TMP_Text _inputFieldText;
    private TMP_InputField _inputField;
    private TMP_Text _fontSizeText;
    private GameObject _ONBackground;

    private NoteColorable _noteColorable;

    private Dictionary<NoteTextAlignHorizontal, Button> _textAlignHorizontalButtons;
    private Dictionary<NoteTextAlignVertical, Button> _textAlignVerticalButtons;


    // Continuous button press settings
    [SerializeField] private float initialDelay = 0.5f; // Initial delay before continuous action starts
    [SerializeField] private float repeatRate = 0.1f; // Rate of repetition while holding

    private Coroutine _fontIncreaseCoroutine;
    private Coroutine _fontDecreaseCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupNoteColorable();
        SetupInputFieldText();
        SetupFontSizeText();
        SetupCustomizationCanvas();
        SetupCustomizeButton();
        SetupFontIncreaseButton();
        SetupFontDecreaseButton();

        SetupTextAlignHorizontalButtons();
        SetupTextAlignVerticalButtons();

        UpdateTextAlignment();
        UpdateButtonsVisuals();
    }

    // Update is called once per frame
    void Update()
    {
    }



    /// <summary>
    /// Setup reference to NoteColorable component
    /// </summary>
    private void SetupNoteColorable()
    {
        try
        {
            _noteColorable = GetComponent<NoteColorable>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteCustomizable] Failed to get NoteColorable component: {ex.Message}");
            return;
        }
    }

    /// <summary>
    /// Sets up the text align horizontal buttons
    /// </summary>
    private void SetupTextAlignHorizontalButtons()
    {
        _textAlignHorizontalButtons = new Dictionary<NoteTextAlignHorizontal, Button>();

        foreach (NoteTextAlignHorizontal align in Enum.GetValues(typeof(NoteTextAlignHorizontal)))
        {
            string buttonName = $"Align {align.ToString()} Button";
            Debug.Log($"[NoteCustomizable] Setting up button: {buttonName}");
            Button alignButton = null;
            try
            {
                alignButton = transform.Find("Customization Canvas").Find("Background").Find(buttonName).GetComponent<Button>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NoteCustomizable] Failed to find '{buttonName}': {ex.Message}");
                continue;
            }

            _textAlignHorizontalButtons[align] = alignButton;

            NoteTextAlignHorizontal capturedAlign = align; // Capture the current value of align
            alignButton.onClick.AddListener(() => OnTextAlignHorizontalButtonClicked(capturedAlign));
        }
    }

    /// <summary>
    /// Handles text align horizontal button clicks
    /// </summary>
    /// <param name="align"></param>
    private void OnTextAlignHorizontalButtonClicked(NoteTextAlignHorizontal align)
    {
        textAlignHorizontal = align;
        UpdateTextAlignment();
        UpdateButtonsVisuals();
    }

    /// <summary>
    /// Handles text align vertical button clicks
    /// </summary>
    /// <param name="align"></param>
    private void OnTextAlignVerticalButtonClicked(NoteTextAlignVertical align)
    {
        textAlignVertical = align;
        UpdateTextAlignment();
        UpdateButtonsVisuals();
    }

    /// <summary>
    /// Updates the text alignment based on both horizontal and vertical settings
    /// </summary>
    public void UpdateTextAlignment()
    {
        TextAlignmentOptions alignment = GetCombinedAlignment(textAlignHorizontal, textAlignVertical);
        Debug.Log($"[NoteCustomizable] Setting alignment to {alignment} (H: {textAlignHorizontal}, V: {textAlignVertical})");
        _inputFieldText.alignment = alignment;
    }

    /// <summary>
    /// Updates the button visuals (pressed down) to reflect current alignment settings
    /// </summary>
    private void UpdateButtonsVisuals()
    {
        // Update horizontal buttons
        foreach (var kvp in _textAlignHorizontalButtons)
        {
            NoteTextAlignHorizontal align = kvp.Key;
            Button button = kvp.Value;
            ButtonToggleVisual toggleVisual = button.GetComponent<ButtonToggleVisual>();
            if (toggleVisual != null)
            {
                toggleVisual.SetButtonState(align == textAlignHorizontal);
                Debug.Log($"[NoteCustomizable] Updated horizontal button {align} to state {(align == textAlignHorizontal)}");
            }
        }
        // Update vertical buttons
        foreach (var kvp in _textAlignVerticalButtons)
        {
            NoteTextAlignVertical align = kvp.Key;
            Button button = kvp.Value;
            ButtonToggleVisual toggleVisual = button.GetComponent<ButtonToggleVisual>();
            if (toggleVisual != null)
            {
                toggleVisual.SetButtonState(align == textAlignVertical);
                Debug.Log($"[NoteCustomizable] Updated vertical button {align} to state {(align == textAlignVertical)}");
            }
        }
    }

    /// <summary>
    /// Gets the combined TextAlignmentOptions based on horizontal and vertical alignment
    /// </summary>
    /// <param name="horizontal">Horizontal alignment</param>
    /// <param name="vertical">Vertical alignment</param>
    /// <returns>Combined TextAlignmentOptions</returns>
    private TextAlignmentOptions GetCombinedAlignment(NoteTextAlignHorizontal horizontal, NoteTextAlignVertical vertical)
    {
        Debug.Log($"[NoteCustomizable] GetCombinedAlignment called with H: {horizontal}, V: {vertical}");

        // Combine horizontal and vertical alignments
        switch (vertical)
        {
            case NoteTextAlignVertical.Top:
                switch (horizontal)
                {
                    case NoteTextAlignHorizontal.Left:
                        return TextAlignmentOptions.TopLeft;
                    case NoteTextAlignHorizontal.Center:
                        return TextAlignmentOptions.Top;
                    case NoteTextAlignHorizontal.Right:
                        return TextAlignmentOptions.TopRight;
                    case NoteTextAlignHorizontal.Justified:
                        return TextAlignmentOptions.TopJustified;
                }
                break;

            case NoteTextAlignVertical.Middle:
                switch (horizontal)
                {
                    case NoteTextAlignHorizontal.Left:
                        return TextAlignmentOptions.MidlineLeft;
                    case NoteTextAlignHorizontal.Center:
                        return TextAlignmentOptions.Midline;
                    case NoteTextAlignHorizontal.Right:
                        return TextAlignmentOptions.MidlineRight;
                    case NoteTextAlignHorizontal.Justified:
                        return TextAlignmentOptions.MidlineJustified;
                }
                break;

            case NoteTextAlignVertical.Bottom:
                switch (horizontal)
                {
                    case NoteTextAlignHorizontal.Left:
                        return TextAlignmentOptions.BottomLeft;
                    case NoteTextAlignHorizontal.Center:
                        return TextAlignmentOptions.Bottom;
                    case NoteTextAlignHorizontal.Right:
                        return TextAlignmentOptions.BottomRight;
                    case NoteTextAlignHorizontal.Justified:
                        return TextAlignmentOptions.BottomJustified;
                }
                break;
        }

        // Default should be MidlineLeft (Middle + Left) as per your specification
        Debug.LogWarning($"[NoteCustomizable] Unexpected alignment combination, using default MidlineLeft. H: {horizontal}, V: {vertical}");
        return TextAlignmentOptions.MidlineLeft;
    }


    /// <summary>
    /// Sets up the text align vertical buttons
    /// </summary>
    private void SetupTextAlignVerticalButtons()
    {
        _textAlignVerticalButtons = new Dictionary<NoteTextAlignVertical, Button>();

        foreach (NoteTextAlignVertical align in Enum.GetValues(typeof(NoteTextAlignVertical)))
        {
            string buttonName = $"Align {align.ToString()} Button";
            Debug.Log($"[NoteCustomizable] Setting up button: {buttonName}");
            Button alignButton = null;
            try
            {
                alignButton = transform.Find("Customization Canvas").Find("Background").Find(buttonName).GetComponent<Button>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NoteCustomizable] Failed to find '{buttonName}': {ex.Message}");
                continue;
            }

            _textAlignVerticalButtons[align] = alignButton;

            NoteTextAlignVertical capturedAlign = align; // Capture the current value of align
            alignButton.onClick.AddListener(() => OnTextAlignVerticalButtonClicked(capturedAlign));
        }
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
        if (_customizationCanvas.activeSelf)
        {
            _customizationCanvas.GetComponent<UISlideUpBounce>().Hide();
        }
        else
        {
            _customizationCanvas.GetComponent<UISlideUpBounce>().Show();
        }

        if (_noteColorable != null)
        {
            _noteColorable.CloseColorCanvas();
        }
    }

    /// <summary>
    /// Closes the customization canvas
    /// </summary>
    public void CloseCustomizationCanvas()
    {
        _customizationCanvas.GetComponent<UISlideUpBounce>().Hide();
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
