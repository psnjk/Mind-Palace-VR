using TMPro;
using UnityEngine;
using System;
using UnityEngine.UI;

public class HandCanvas : MonoBehaviour
{

    [Tooltip("Reference to the NodeManager to change the currently selected Node for spawning")]
    [SerializeField] private NodeManager nodeManager;
    [SerializeField] private UIManager uiManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find NodeManager if not assigned
        if (nodeManager == null)
        {
            nodeManager = FindObjectOfType<NodeManager>();
            if (nodeManager == null)
            {
                Debug.LogError("[HandCanvas] NodeManager not found!");
            }
            else
            {
                Debug.Log("[HandCanvas] NodeManager found automatically");
            }
        }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[HandCanvas] UIManager not found!");
            }
            else
            {
                Debug.Log("[HandCanvas] UIManager found automatically");

            }
        }

        SetupNodeSelectionDropdown();
        SetupConsoleToggle();
        SetupSettingsButton();

    }

    // Update is called once per frame
    void Update()
    {

    }
    
    /// <summary>
    /// Setup the node selection dropdown with all the spawnable prefabs from the node manager
    /// </summary>
    void SetupNodeSelectionDropdown()
    {

        Transform background = transform.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"No child named 'Background' found under Hand Canvas on {gameObject.name}");
            return;
        }

        Transform nodeSelectionDropdownTransform = background.Find("Node Selection Dropdown");
        if (nodeSelectionDropdownTransform == null)
        {
            Debug.LogWarning($"No child named 'Node Selection Dropdown' found under Background on {gameObject.name}");
            return;
        }

        TMP_Dropdown nodeSelectionDropdown = nodeSelectionDropdownTransform.GetComponent<TMP_Dropdown>();
        
        if (nodeSelectionDropdown != null && nodeManager != null)
        {
            nodeSelectionDropdown.options.Clear();

            int prefabCount = nodeManager.GetSpawnablePrefabCount();
            
            // Add options for each spawnable prefab
            for (int i = 0; i < prefabCount; i++)
            {
                string prefabName = nodeManager.GetPrefabNameAtIndex(i);
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(prefabName);
                nodeSelectionDropdown.options.Add(option);
            }

            // Set current value to the selected index from NodeManager
            nodeSelectionDropdown.value = nodeManager.selectedNodeIndex;
            nodeSelectionDropdown.RefreshShownValue();

            // Listen for dropdown value changes
            nodeSelectionDropdown.onValueChanged.AddListener((int value) => {
                if (nodeManager != null)
                {
                    nodeManager.SetSelectedNodeIndex(value);
                }
            });

            Debug.Log($"[HandCanvas] Setup dropdown with {prefabCount} prefab options");
        }
        else
        {
            if (nodeSelectionDropdown == null)
            {
                Debug.LogWarning($"'Node Selection Dropdown' found on {gameObject.name}, but it doesn't have a TMP_Dropdown component!");
            }
            if (nodeManager == null)
            {
                Debug.LogWarning($"NodeManager is null, cannot setup dropdown!");
            }
        }

    }

    void SetupConsoleToggle()
    {

        Transform background = transform.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"No child named 'Background' found under Hand Canvas on {gameObject.name}");
            return;
        }

        Transform consoleToggleTransform = background.Find("Console Toggle");
        if (consoleToggleTransform == null)
        {
            Debug.LogWarning($"No child named 'Console Toggle' found under Background on {gameObject.name}");
            return;
        }

        Toggle consoleToggle = consoleToggleTransform.GetComponent<Toggle>();
        if (consoleToggle != null)
        {
            consoleToggle.onValueChanged.AddListener((bool isOn) => {
                if (uiManager != null)
                {
                    uiManager.ToggleCanvas("Console");
                }
            });
        }
        else
        {
            Debug.LogWarning($"'Console Toggle' found on {gameObject.name}, but it doesn't have a Toggle component!");
        }


    }

    void SetupSettingsButton()
    {

        Transform background = transform.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"No child named 'Background' found under Hand Canvas on {gameObject.name}");
            return;
        }

        Transform settingsButtonTransform = background.Find("Settings Button");
        if (settingsButtonTransform == null)
        {
            Debug.LogWarning($"No child named 'Settings Button' found under Background on {gameObject.name}");
            return;
        }

        Button settingsButton = settingsButtonTransform.GetComponent<Button>();
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => {
                if (uiManager != null)
                {
                    uiManager.HideCanvas("Menu");
                    uiManager.ToggleCanvas("Settings");
                }
            });
        }
        else
        {
            Debug.LogWarning($"'Settings Button' found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }
}
