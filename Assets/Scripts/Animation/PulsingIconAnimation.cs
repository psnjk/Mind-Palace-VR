using UnityEngine;
using UnityEngine.UI;

public class PulsingIconAnimation : MonoBehaviour
{
    public Image icon;
    public float speed = 2f;
    public float minAlpha = 0.4f;
    public float maxAlpha = 1f;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // value 0 → 1
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = icon.color;
        c.a = alpha;
        icon.color = c;
    }
}
