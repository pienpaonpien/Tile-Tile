using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class StageClearVisual : MonoBehaviour
{
    [SerializeField] private Sprite clearImage;
    [SerializeField] private AudioClip tilePlacementClip;
    [SerializeField, Min(0f)] private float displayDuration = 2f;

    private SpriteRenderer imageRenderer;
    private AudioSource tilePlacementSource;

    public float DisplayDuration => Mathf.Max(0f, displayDuration);

    private void Awake()
    {
        imageRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlayTilePlacementSound()
    {
        if (tilePlacementClip == null)
        {
            return;
        }

        if (tilePlacementSource == null)
        {
            tilePlacementSource = GetComponent<AudioSource>();
            if (tilePlacementSource == null)
            {
                tilePlacementSource = gameObject.AddComponent<AudioSource>();
            }

            tilePlacementSource.playOnAwake = false;
            tilePlacementSource.loop = false;
            tilePlacementSource.spatialBlend = 0f;
        }

        tilePlacementSource.PlayOneShot(tilePlacementClip);
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