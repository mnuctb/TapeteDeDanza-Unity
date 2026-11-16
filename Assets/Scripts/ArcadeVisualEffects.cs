using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
/// Handles lightning, per-key light sparks, shockwaves and flashes on the dancer.
/// </summary>
public class ArcadeVisualEffects : MonoBehaviour
{
    public static ArcadeVisualEffects Instance;

    // [FormerlySerializedAs] keeps the references you already assigned in the Inspector.

    [Header("Container and References")]
    [FormerlySerializedAs("contenedorEfectos")]
    public Transform effectsContainer;   // Drag Gameplay_Panel or the Canvas
    [FormerlySerializedAs("transformBailarin")]
    public Transform dancerTransform;    // Drag your "dance" GameObject

    [Header("Neon Colors per Arrow")]
    public Color colorA = new Color(0f, 0.95f, 1f, 1f);     // A: Cyan (#00F2FE)
    public Color colorS = new Color(1f, 0.55f, 0f, 1f);     // S: Orange (#FF8C00)
    public Color colorW = new Color(0.15f, 1f, 0.6f, 1f);   // W: Neon Green (#27FF99)
    public Color colorD = new Color(1f, 0.2f, 0.6f, 1f);    // D: Pink (#FF3399)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (effectsContainer == null) effectsContainer = transform;
    }

    public Color GetColor(int lane)
    {
        switch (lane)
        {
            case 0: return colorA;
            case 1: return colorS;
            case 2: return colorW;
            case 3: return colorD;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Launches the burst of lightning, neon sparks and floating text when an arrow is hit.
    /// </summary>
    public void ExplodeReceptor(int lane, Vector3 receptorPosition, string rating = "PERFECT!")
    {
        Color col = GetColor(lane);

        // 1. Expanding shockwave ring
        StartCoroutine(ShockwaveRoutine(receptorPosition, col));

        // 2. Burst of 10 rays/sparks fired in a fan
        for (int i = 0; i < 10; i++)
        {
            float angle = (360f / 10f) * i + Random.Range(-15f, 15f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            StartCoroutine(SparkRoutine(receptorPosition, dir, col));
        }

        // 3. Glowing floating text (PERFECT!, BRILLIANT!, etc.)
        StartCoroutine(FloatingTextRoutine(receptorPosition + new Vector3(0, 45f, 0), rating, col));
    }

    /// <summary>
    /// Light aura and flash under the dancer's feet and body when the pose is performed.
    /// </summary>
    public void DancerFlash(int lane)
    {
        Vector3 pos = dancerTransform != null ? dancerTransform.position : transform.position;
        Color col = GetColor(lane);

        StartCoroutine(DancerAuraRoutine(pos, col));
    }

    // --- PROCEDURAL ANIMATION COROUTINES ---

    private IEnumerator ShockwaveRoutine(Vector3 pos, Color col)
    {
        GameObject ring = new GameObject("FX_Shockwave", typeof(RectTransform), typeof(Image));
        ring.transform.SetParent(effectsContainer, false);
        ring.transform.position = pos;

        Image img = ring.GetComponent<Image>();
        img.color = new Color(col.r, col.g, col.b, 0.85f);
        img.raycastTarget = false;

        RectTransform rt = ring.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40f, 40f);

        float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;

            float size = Mathf.Lerp(40f, 140f, Mathf.Sin(p * Mathf.PI * 0.5f));
            rt.sizeDelta = new Vector2(size, size);
            img.color = new Color(col.r, col.g, col.b, (1f - p) * 0.85f);
            yield return null;
        }

        Destroy(ring);
    }

    private IEnumerator SparkRoutine(Vector3 origin, Vector2 dir, Color col)
    {
        GameObject spark = new GameObject("FX_Spark", typeof(RectTransform), typeof(Image));
        spark.transform.SetParent(effectsContainer, false);
        spark.transform.position = origin;

        Image img = spark.GetComponent<Image>();
        img.color = col;
        img.raycastTarget = false;

        RectTransform rt = spark.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(10f, 10f);

        float speed = Random.Range(200f, 350f);
        float dur = Random.Range(0.18f, 0.32f);
        float t = 0f;
        Vector3 currentPos = origin;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;

            currentPos += (Vector3)(dir * speed * Time.deltaTime);
            spark.transform.position = currentPos;

            float factor = 1f - p;
            rt.sizeDelta = new Vector2(10f * factor, 10f * factor);
            img.color = new Color(col.r, col.g, col.b, factor);
            yield return null;
        }

        Destroy(spark);
    }

    private IEnumerator DancerAuraRoutine(Vector3 pos, Color col)
    {
        GameObject aura = new GameObject("FX_DancerAura", typeof(RectTransform), typeof(Image));
        aura.transform.SetParent(effectsContainer, false);
        aura.transform.position = pos;
        aura.transform.SetAsFirstSibling(); // Drawn behind the dancer

        Image img = aura.GetComponent<Image>();
        img.color = new Color(col.r, col.g, col.b, 0.7f);
        img.raycastTarget = false;

        RectTransform rt = aura.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110f, 150f);

        // Sparks jumping from the dancer's floor
        for (int i = 0; i < 6; i++)
        {
            Vector2 dir = new Vector2(Random.Range(-0.9f, 0.9f), Random.Range(0.6f, 1.2f)).normalized;
            StartCoroutine(SparkRoutine(pos + new Vector3(0, -35f, 0), dir, col));
        }

        float dur = 0.22f;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;

            float scale = 1f + Mathf.Sin(p * Mathf.PI) * 0.35f;
            rt.sizeDelta = new Vector2(110f * scale, 150f * scale);
            img.color = new Color(col.r, col.g, col.b, (1f - p) * 0.7f);
            yield return null;
        }

        Destroy(aura);
    }

    private IEnumerator FloatingTextRoutine(Vector3 pos, string text, Color col)
    {
        GameObject textObject = new GameObject("FX_TextPopup", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(effectsContainer, false);
        textObject.transform.position = pos;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = col;
        tmp.raycastTarget = false;

        RectTransform rt = textObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260f, 60f);

        float dur = 0.45f;
        float t = 0f;
        Vector3 start = pos;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;

            textObject.transform.position = start + new Vector3(0, Mathf.Lerp(0, 55f, p), 0);
            float scale = (p < 0.25f) ? Mathf.Lerp(0.6f, 1.3f, p / 0.25f) : Mathf.Lerp(1.3f, 1f, (p - 0.25f) / 0.75f);
            rt.localScale = Vector3.one * scale;

            tmp.color = new Color(col.r, col.g, col.b, 1f - p);
            yield return null;
        }

        Destroy(textObject);
    }
}