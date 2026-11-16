using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitFlash : MonoBehaviour
{
    public static HitFlash Instance;

    [Header("Setup")]
    [SerializeField] private RectTransform beamParent;   // Pista_Ritmo_Centro

    [Header("Beam Look")]
    [SerializeField] private float beamWidth = 18f;
    [SerializeField] private float beamHeight = 450f;
    [SerializeField] private float riseTime = 0.12f;     // time to reach full height
    [SerializeField] private float totalTime = 0.45f;    // total life (rise + fade)

    // Order: left, down, up, right
    [SerializeField]
    private Color[] laneColors =
    {
        new Color(0.0f, 0.9f, 1.0f, 1f),   // cyan
        new Color(1.0f, 0.5f, 0.1f, 1f),   // orange
        new Color(0.1f, 1.0f, 0.5f, 1f),   // green
        new Color(1.0f, 0.3f, 0.6f, 1f)    // pink
    };

    private Sprite beamSprite;

    private void Awake()
    {
        Instance = this;
        beamSprite = CreateBeamSprite();
    }

    // Same signature as before, so GameController does not change
    public void Flash(int lane, Vector3 worldPosition)
    {
        if (beamParent == null)
        {
            Debug.LogError("HitFlash: beamParent is NOT assigned!");
            return;
        }

        Color color = laneColors[Mathf.Clamp(lane, 0, laneColors.Length - 1)];
        color.a = 1f;
        StartCoroutine(BeamRoutine(color, worldPosition));
    }

    private IEnumerator BeamRoutine(Color color, Vector3 worldPosition)
    {
        GameObject obj = new GameObject("HitBeam", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(beamParent, false);
        obj.transform.SetAsLastSibling();

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);   // pivot at the bottom: it grows upward
        rt.localScale = Vector3.one;
        rt.position = worldPosition;

        Vector3 lp = rt.localPosition;
        lp.z = 0f;
        rt.localPosition = lp;

        Image img = obj.GetComponent<Image>();
        img.sprite = beamSprite;
        img.raycastTarget = false;

        float t = 0f;

        while (t < totalTime)
        {
            t += Time.deltaTime;

            // Rises quickly (ease out), then fades
            float rise = Mathf.Clamp01(t / riseTime);
            rise = 1f - Mathf.Pow(1f - rise, 3f);

            float fade = Mathf.Clamp01((t - riseTime) / (totalTime - riseTime));

            rt.sizeDelta = new Vector2(beamWidth * (1f - fade * 0.5f), beamHeight * rise);

            Color c = color;
            c.a = 1f - fade;
            img.color = c;

            yield return null;
        }

        Destroy(obj);
    }

    // Vertical gradient: bright at the bottom, fading toward the top
    private Sprite CreateBeamSprite()
    {
        const int w = 8;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float a = 1f - (y / (float)(h - 1));   // 1 at bottom, 0 at top

            for (int x = 0; x < w; x++)
            {
                // Brighter in the center, softer on the sides
                float side = 1f - Mathf.Abs((x / (float)(w - 1)) - 0.5f) * 2f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * Mathf.Clamp01(side + 0.4f)));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f));
    }
}