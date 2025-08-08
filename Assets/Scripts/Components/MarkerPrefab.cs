using UnityEngine;

public class MarkerPrefab : MonoBehaviour
{
    void Start()
    {
        // 可自定义外观，如颜色、大小、闪烁等
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.red;
    }
} 