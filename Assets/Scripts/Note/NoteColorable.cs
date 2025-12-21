using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;

public enum NoteColorTheme
{
    Yellow,
    Pink,
    Blue,
    Green,
    Orange
}

/// <summary>
/// Manages the color scheme of a note and all its child UI elements.
/// Applies primary and secondary colors to materials, images, and buttons throughout the note hierarchy.
/// </summary>
public class NoteColorable : MonoBehaviour
{
    [Header("Color Theme")]
    [SerializeField] private NoteColorTheme colorTheme = NoteColorTheme.Yellow;

    [Header("Debug")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyOnValidate = true;

    // Cached references
    private Transform _handleTransform;
    private Transform _noteCanvasTransform;
    private Transform _customizationCanvasTransform;
    private Transform _colorCanvasTransform;
    private Transform _attachPointsCanvasTransform;

    private Button _mainColorButton;

    private NoteCustomizable _noteCustomizable;

    // Color definitions for each theme
    private Color _primaryColor;
    private Color _secondaryColor;

    private Dictionary<NoteColorTheme, Button> _colorButtons;

    private void OnValidate()
    {
        if (applyOnValidate && Application.isPlaying)
        {
            ApplyColorTheme();
        }
    }

    void Start()
    {
        CacheTransforms();
        if (applyOnStart)
        {
            ApplyColorTheme();
        }

        SetupNoteCustomizable();
        SetupColorCanvas();
        SetupMainColorButton();

        SetupColorButtons();
        UpdateColorButtonsVisuals();
    }


    /// <summary>
    /// Setup reference to NoteCustomizable component
    /// </summary>
    private void SetupNoteCustomizable()
    {
        try
        {
            _noteCustomizable = GetComponent<NoteCustomizable>();
            if (_noteCustomizable == null)
            {
                Debug.LogWarning("[NoteColorable] NoteCustomizable component not found");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NoteColorable] Error setting up NoteCustomizable: " + ex.Message);
        }
    }


    /// <summary>
    /// Initially hide the color selection canvas
    /// </summary>
    private void SetupColorCanvas()
    {
        try
        {
            Transform colorCanvas = transform.Find("Color Canvas");
            if (colorCanvas != null)
            {
                colorCanvas.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[NoteColorable] Color Canvas not found");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NoteColorable] Error setting up Color Canvas: " + ex.Message);
        }
    }


    /// <summary>
    /// Close the color selection canvas
    /// </summary>
    public void CloseColorCanvas()
    {
        try
        {
            Transform colorCanvas = transform.Find("Color Canvas");
            if (colorCanvas != null)
            {
                colorCanvas.gameObject.GetComponent<UISlideUpBounce>().Hide();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NoteColorable] Error closing Color Canvas: " + ex.Message);
        }
    }


    /// <summary>
    /// Setup the main color button to toggle the color selection canvas
    /// </summary>
    private void SetupMainColorButton()
    {
        try
        {
            _mainColorButton = transform.Find("Note Canvas").Find("Background").Find("Color Button").GetComponent<Button>();
            if (_mainColorButton != null)
            {
                _mainColorButton.onClick.RemoveAllListeners();
                _mainColorButton.onClick.AddListener(() =>
                {
                    Transform colorCanvas = transform.Find("Color Canvas");
                    if (colorCanvas != null)
                    {
                        bool isActive = colorCanvas.gameObject.activeSelf;
                        if (!isActive)
                        {
                            colorCanvas.gameObject.GetComponent<UISlideUpBounce>().Show();
                        }
                        else
                        {
                            colorCanvas.gameObject.GetComponent<UISlideUpBounce>().Hide();
                        }
                    }
                    if (_noteCustomizable != null)
                    {
                        _noteCustomizable.CloseCustomizationCanvas();
                    }
                });
            }
            else
            {
                Debug.LogWarning("[NoteColorable] Main Color Button not found");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NoteColorable] Error setting up main color button: " + ex.Message);
        }
    }

    /// <summary>
    /// Cache all required transforms for better performance
    /// </summary>
    private void CacheTransforms()
    {
        _handleTransform = transform.Find("Handle");
        _noteCanvasTransform = transform.Find("Note Canvas");
        _customizationCanvasTransform = transform.Find("Customization Canvas");
        _colorCanvasTransform = transform.Find("Color Canvas");
        _attachPointsCanvasTransform = transform.Find("Attach Points Canvas");
    }

    /// <summary>
    /// Apply the selected color theme to all note elements
    /// </summary>
    public void ApplyColorTheme()
    {
        SetColorsForTheme();
        
        ApplyHandleColors();
        ApplyNoteCanvasColors();
        ApplyCustomizationCanvasColors();
        ApplyColorCanvasColors();
        ApplyAttachPointsCanvasColors();

        Debug.Log($"[NoteColorable] Applied {colorTheme} color theme");
    }

    /// <summary>
    /// Set primary and secondary colors based on the selected theme
    /// </summary>
    private void SetColorsForTheme()
    {
        switch (colorTheme)
        {
            case NoteColorTheme.Yellow:
                _primaryColor = HexToColor("DCDB6A");
                _secondaryColor = HexToColor("C0C06C");
                break;
            case NoteColorTheme.Pink:
                _primaryColor = HexToColor("FFB3D9");
                _secondaryColor = HexToColor("FF85C1");
                break;
            case NoteColorTheme.Blue:
                _primaryColor = HexToColor("A8D8EA");
                _secondaryColor = HexToColor("7FB3D5");
                break;
            case NoteColorTheme.Green:
                _primaryColor = HexToColor("A8E6A3");
                _secondaryColor = HexToColor("7FD77A");
                break;
            case NoteColorTheme.Orange:
                _primaryColor = HexToColor("FFD39B");
                _secondaryColor = HexToColor("FFB347");
                break;
        }
    }

    /// <summary>
    /// Apply colors to the Handle object
    /// </summary>
    private void ApplyHandleColors()
    {
        if (_handleTransform == null) return;

        Renderer renderer = _handleTransform.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = _primaryColor;
        }
    }

    /// <summary>
    /// Apply colors to all Note Canvas child elements
    /// </summary>
    private void ApplyNoteCanvasColors()
    {
        if (_noteCanvasTransform == null) return;

        Transform background = _noteCanvasTransform.Find("Background");
        if (background != null)
        {
            SetImageColor(background, _primaryColor);

            // Pin Button
            ApplyButtonColors(background, "Pin Button", "Pin Icon");
            
            // Delete Button
            Transform deleteButton = background.Find("Delete Button");
            if (deleteButton != null)
            {
                SetImageColor(deleteButton, _secondaryColor);
                SetImageColor(deleteButton.Find("Delete Icon"), _primaryColor);

                LongPressButton longPress = deleteButton.GetComponent<LongPressButton>();
                if (longPress != null)
                {
                    // Use reflection to set private serialized fields
                    var type = typeof(LongPressButton);
                    var progressColorField = type.GetField("progressColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var defaultColorField = type.GetField("defaultColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (progressColorField != null) progressColorField.SetValue(longPress, _secondaryColor);
                    if (defaultColorField != null) defaultColorField.SetValue(longPress, _secondaryColor);
                }
            }

            // Link Button
            ApplyButtonColors(background, "Link Button", "Link Icon");

            // Customize Button
            ApplyButtonColors(background, "Customize Button", "Customize Icon");

            // Color Button
            ApplyButtonColors(background, "Color Button", "Color Icon");
        }
    }

    /// <summary>
    /// Apply colors to all Customization Canvas child elements
    /// </summary>
    private void ApplyCustomizationCanvasColors()
    {
        if (_customizationCanvasTransform == null)
        {
            Debug.LogWarning("[NoteColorable] Customization Canvas transform not found");
            return;
        }

        Transform background = _customizationCanvasTransform.Find("Background");
        if (background == null)
        {
            Debug.LogWarning("[NoteColorable] Background child not found in Customization Canvas");
            return;
        }
        
        SetImageColor(background, _primaryColor);

            // Apply secondary color to all buttons
            string[] buttonNames = {
                "Align Left Button",
                "Align Top Button",
                "Align Center Button",
                "Align Middle Button",
                "Align Right Button",
                "Align Bottom Button",
                "Align Justified Button",
                "Font Size Text Area",
                "Font Decrease Button",
                "Font Increase Button"
            };

            foreach (string buttonName in buttonNames)
            {
                Transform button = background.Find(buttonName);
                if (button != null)
                {
                    SetImageColor(button, _secondaryColor);
                }
                else
                {
                    Debug.LogWarning($"[NoteColorable] Button not found: {buttonName}");
                }
            }
        }

    /// <summary>
    /// Apply colors to all Color Canvas child elements
    /// </summary>
    private void ApplyColorCanvasColors()
    {
        if (_colorCanvasTransform == null) return;

        Transform background = _colorCanvasTransform.Find("Background");
        if (background != null)
        {
            SetImageColor(background, _primaryColor);
        }
    }

    /// <summary>
    /// Apply colors to all Attach Points Canvas child elements
    /// </summary>
    private void ApplyAttachPointsCanvasColors()
    {
        if (_attachPointsCanvasTransform == null) return;

        string[] attachPoints = {
            "Top", "TopRight", "Right", "BottomRight",
            "Bottom", "BottomLeft", "Left", "TopLeft"
        };

        foreach (string pointName in attachPoints)
        {
            Transform point = _attachPointsCanvasTransform.Find(pointName);
            if (point != null)
            {
                SetImageColor(point, _primaryColor);
            }
        }
    }

    /// <summary>
    /// Helper method to apply button and icon colors
    /// </summary>
    private void ApplyButtonColors(Transform parent, string buttonName, string iconName)
    {
        Transform button = parent.Find(buttonName);
        if (button != null)
        {
            SetImageColor(button, _secondaryColor);
            Transform icon = button.Find(iconName);
            if (icon != null)
            {
                SetImageColor(icon, _primaryColor);
            }
        }
    }

    /// <summary>
    /// Set the color of an Image component on a transform
    /// </summary>
    private void SetImageColor(Transform target, Color color)
    {
        if (target == null) return;

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            
            // Update ButtonToggleVisual if present
            ButtonToggleVisual toggleVisual = target.GetComponent<ButtonToggleVisual>();
            if (toggleVisual != null)
            {
                toggleVisual.UpdateOriginalColor(color);
            }
        }
    }

    /// <summary>
    /// Convert hex string to Unity Color
    /// </summary>
    private Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        
        if (hex.Length == 6)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            
            return new Color(r / 255f, g / 255f, b / 255f);
        }
        
        Debug.LogWarning($"[NoteColorable] Invalid hex color: {hex}");
        return Color.white;
    }

    /// <summary>
    /// Public method to change the color theme programmatically
    /// </summary>
    public void SetColorTheme(NoteColorTheme theme)
    {
        colorTheme = theme;
        ApplyColorTheme();
    }

    /// <summary>
    /// Get the current color theme
    /// </summary>
    public NoteColorTheme GetColorTheme()
    {
        return colorTheme;
    }

    /// <summary>
    /// Setup color buttons dictionary
    /// </summary>
    private void SetupColorButtons()
    {
        _colorButtons = new Dictionary<NoteColorTheme, Button>();

        foreach (NoteColorTheme theme in System.Enum.GetValues(typeof(NoteColorTheme)))
        {
            string buttonName = $"{theme.ToString()} Button";
            Debug.Log($"[NoteColorable] Setting up button: {buttonName}");
            Button colorButton = null;
            try 
            {
                colorButton = transform.Find("Color Canvas").Find("Background").Find(buttonName).GetComponent<Button>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NoteColorable] Failed to find button {buttonName}: {ex.Message}");
                continue;
            }

            _colorButtons[theme] = colorButton;

            colorButton.onClick.RemoveAllListeners();
            colorButton.onClick.AddListener(() => OnColorButtonClicked(theme));
        }
    }

    /// <summary>
    /// Update the visual states of color buttons
    /// </summary>
    private void OnColorButtonClicked(NoteColorTheme theme)
    {
        colorTheme = theme;
        SetColorTheme(theme);
        UpdateColorButtonsVisuals();
    }


    private void UpdateColorButtonsVisuals()
    {
        foreach (var kvp in _colorButtons)
        {
            try {
                NoteColorTheme color = kvp.Key;
                Button button = kvp.Value;
                if (button)
                {
                    // Show the "Icon" when selected, hide otherwise 
                    button.transform.Find("Icon").gameObject.SetActive(color == colorTheme);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NoteColorable] Error updating button visual for {kvp.Key}: {ex.Message}");
            }
        }
        
    }
}
