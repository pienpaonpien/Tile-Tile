using UnityEngine;

public class StoryGuideHighlighter : MonoBehaviour
{
    public void HideAll()
    {
        SetGuideVisibility(false);
    }

    public void ShowAll()
    {
        SetGuideVisibility(true);
    }

    private static void SetGuideVisibility(bool visible)
    {
        BrockGuide[] guides = FindObjectsByType<BrockGuide>(FindObjectsSortMode.None);

        foreach (BrockGuide guide in guides)
        {
            if (guide == null)
            {
                continue;
            }

            SpriteRenderer indicator = guide.GetComponent<SpriteRenderer>();
            if (indicator != null)
            {
                indicator.enabled = visible;
            }
        }
    }
}
