using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Used to make buttons visually toggle between pressed and unpressed states.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonToggleVisual : MonoBehaviour
{
    private Button button;
    private bool isCurrentlyPressed = false;
    private Color originalColor;

    void Awake()
    {
        SetupButton();
    }
    private void SetupButton()
    {
        try
        {
            button = this.transform.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("[ButtonToggleVisual] Button component not found on " + gameObject.name);
            }
            // Store the original color
            if (button != null && button.image != null)
            {
                originalColor = button.image.color;
            }
            else
            {
                Debug.LogError("[ButtonToggleVisual] Button image component not found on " + gameObject.name);
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ButtonToggleVisual] Error setting up button: " + ex.Message);
        }
    }

    public void SetButtonState(bool isPressed)
    {
        // Ensure button is initialized before using it
        if (button == null)
        {
            SetupButton();
        }

        if (button == null)
        {
            Debug.LogWarning("[ButtonToggleVisual] Button is null, cannot set state for " + gameObject.name);
            return;
        }

        if (isPressed != isCurrentlyPressed)
        {
            isCurrentlyPressed = isPressed;

            if (isPressed)
            {
                // Apply the pressed color using the button's color tint system
                ColorBlock colors = button.colors;
                button.image.color = originalColor * colors.pressedColor;
            }
            else
            {
                // Reset to normal color using the button's color tint system
                ColorBlock colors = button.colors;
                button.image.color = originalColor * colors.normalColor;
            }
        }
    }

    /// <summary>
    /// Update the original color when the button's color scheme changes.
    /// Call this after changing the button's image color externally.
    /// </summary>
    public void UpdateOriginalColor(Color newColor)
    {
        originalColor = newColor;
        
        // Re-apply the current state with the new color
        if (button != null && button.image != null)
        {
            ColorBlock colors = button.colors;
            if (isCurrentlyPressed)
            {
                button.image.color = originalColor * colors.pressedColor;
            }
            else
            {
                button.image.color = originalColor * colors.normalColor;
            }
        }
    }
}
