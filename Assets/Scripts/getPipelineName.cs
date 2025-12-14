using UnityEngine;

public class getPipelineName : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType().Name);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
