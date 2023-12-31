using UnityEngine;

public class SizeMinimap : MonoBehaviour
{
    private void Update()
    {
        var rect = (RectTransform)transform;
        var size = Screen.height * 0.4f;
        
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(-size/2, -size/2);
    }
}
