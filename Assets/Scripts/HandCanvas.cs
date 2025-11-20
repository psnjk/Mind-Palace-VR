using TMPro;
using UnityEngine;
using System;

public class HandCanvas : MonoBehaviour
{

    [Tooltip("Reference to the NodeManager to change the currently selected Node for spawning")]
    [SerializeField] private NodeManager nodeManager;
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

        SetupNodeSelectionDropdown();

    }

    // Update is called once per frame
    void Update()
    {

    }
    
    /// <summary>
    /// Setup the node selection dropdown with all the nodes that can be spawned with the node manager
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
            Debug.LogWarning($"No child named 'Note Selection Dropdown' found under Background on {gameObject.name}");
            return;
        }

        TMP_Dropdown nodeSelectionDropdown = nodeSelectionDropdownTransform.GetComponent < TMP_Dropdown>();
        
        if (nodeSelectionDropdown != null)
        {
            nodeSelectionDropdown.options.Clear();

            NodeType[] nodeTypes = (NodeType[])Enum.GetValues(typeof(NodeType));
            
            foreach (NodeType nodeType in nodeTypes)
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(nodeType.ToString());
                nodeSelectionDropdown.options.Add(option);
            }

            if (nodeManager != null)
            {
                nodeSelectionDropdown.value = (int)nodeManager.selectedNode;
            }

            nodeSelectionDropdown.RefreshShownValue();

            nodeSelectionDropdown.onValueChanged.AddListener((int value) => {
                if (nodeManager != null && value >= 0 && value < nodeTypes.Length)
                {
                    NodeType selectedNodeType = nodeTypes[value];
                    nodeManager.SetSelectedNode(selectedNodeType);
                }
            });
        }
        else
        {
            Debug.LogWarning($"'Node Selection Dropdown' found on {gameObject.name}, but it doesn't have a Dropdown component!");
        }

    }
}
