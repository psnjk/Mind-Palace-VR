using UnityEngine;
using UnityEngine.InputSystem;

public class CustomInputAction : MonoBehaviour
{
    public InputActionReference menuButton;
    [SerializeField] private UIManager uiManager;
    private string menuCanvasName = "Menu";

    void Start()
    {
        menuButton.action.started += toggleMenu;
        // customButton.action.canceled += SomeOtherFunction;
        // 
    }

    void toggleMenu(InputAction.CallbackContext context)
    {
        Debug.Log("Menu button pressed!");
        
        if (uiManager != null)
        {
            uiManager.ToggleCanvas(menuCanvasName);
        }
        else
        {
            Debug.LogWarning("UIManager reference is not set in CustomInputAction!");
        }
    }

    void Update()
    {
        
    }
}
