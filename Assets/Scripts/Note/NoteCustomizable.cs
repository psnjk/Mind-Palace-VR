using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
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
    private TMP_Text _fontSizeText;
    private GameObject _ONBackground;

    public float fontSize;


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

        _fontIncreaseButton.onClick.AddListener(IncrementFontSize); 
    }


    /// <summary>
    /// Increments the font size by 1
    /// </summary>
    private void IncrementFontSize()
    {

        fontSize += 1;
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

        _fontDecreaseButton.onClick.AddListener(DecrementFontSize); 
    }


    /// <summary>
    /// Decrements the font size by 1
    /// </summary>
    private void DecrementFontSize()
    {

        fontSize -= 1;
        _fontSizeText.text = fontSize.ToString(); 
        _inputFieldText.fontSizeMax = fontSize;
    }
}
