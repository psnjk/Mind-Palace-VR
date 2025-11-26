using TMPro;
using UnityEngine;

public class InputFieldFocusManager : MonoBehaviour
{
    public static InputFieldFocusManager Instance;

    public TMP_InputField currentFocusedField;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Set currently focused input field, called by input field onSelect events
    /// </summary>
    /// <param name="field"></param>
    public void SetFocusedField(TMP_InputField field)
    {
        currentFocusedField = field;
        Debug.Log("[InputFieldFocusManager] Focused field set to: " + field.name);
    }

    /// <summary>
    /// Returns the currently focused TMP_InputField, or null if none
    /// </summary>
    public TMP_InputField GetFocusedField()
    {
        return currentFocusedField;
    }


    [ContextMenu("Test Insert Text")]
    public void TestInsertText()
    {
        string text = "Hello from InputFieldFocusManager!";
        InsertText(text);
    }

    /// <summary>
    /// Inserts text into the currently focused input field by appending it to the current cursor position.
    /// </summary>
    /// <param name="text"></param>
    public void InsertText(string text)
    {
        if (CurrentFocusedFieldExists())
        {
            InsertTextInField(text, currentFocusedField);
        }

        else
        {
            Debug.LogWarning("[InputFieldFocusManager] No field is currently focused");
        }
    }

    /// <summary>
    /// Inserts text into a specified input field
    /// </summary>
    /// <param name="text"></param>
    /// <param name="field"></param>
    public void InsertTextInField(string text, TMP_InputField field)
    {
        int caretPos = field.stringPosition; // current caret position
        string originalText = currentFocusedField.text;
        if (originalText != "")
        {
            text = " " + text; // add space before text if field is not empty
        }

        // Insert text at caret position
        field.text = originalText.Insert(caretPos, text);

        // Move caret after the inserted text
        field.stringPosition = caretPos + text.Length;
        field.caretPosition = caretPos + text.Length;
        Debug.Log($"[InputFieldFocusManager] inserted text into input field: {text}");
    }

    /// <summary>
    /// Returns true if there is a currently focused input field
    /// </summary>
    /// <returns></returns>
    public bool CurrentFocusedFieldExists()
    {
        return currentFocusedField != null;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
