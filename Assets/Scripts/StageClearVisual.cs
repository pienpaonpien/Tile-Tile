using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class StageClearVisual : MonoBehaviour
{
    [SerializeField] private Sprite clearImage;
    [SerializeField, Min(0f)] private float displayDuration = 2f;

    private SpriteRenderer imageRenderer;

    public float DisplayDuration => Mathf.Max(0f, displayDuration);

    private void Awake()
    {
        imageRenderer = GetComponent<SpriteRenderer>();
    }

    public void ShowClearImage()
    {
        if (imageRenderer == null)
        {
            imageRenderer = GetComponent<SpriteRenderer>();
        }

        if (imageRenderer != null && clearImage != null)
        {
            imageRenderer.sprite = clearImage;
        }
    }
}