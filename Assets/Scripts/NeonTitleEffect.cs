using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Arcade-style title effect for a TextMeshPro (UI) text:
/// moving rainbow gradient, per-letter wave, pulse, neon glow and floating sparkles.
/// Add it to the "Title" object in Menu_scene. Every effect can be toggled in the Inspector.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class NeonTitleEffect : MonoBehaviour
{
    [Header("Rainbow Gradient")]
    public bool rainbow = true;
    public float colorSpeed = 0.25f;     // How fast the colors travel
    public float rainbowWidth = 350f;    // Distance (in pixels) of one full color cycle
    [Range(0f, 1f)] public float saturation = 0.75f;

    [Header("Letter Wave")]
    public bool wave = true;
    public float waveHeight = 8f;
    public float waveSpeed = 3f;
    public float waveSpacing = 0.6f;     // Delay between letters

    [Header("Pulse")]
    public bool pulse = true;
    public float pulseSpeed = 2.2f;
    public float pulseAmount = 0.04f;

    [Header("Neon Glow")]
    public bool glow = true;
    [Range(0f, 1f)] public float glowSoftness = 0.6f;
    [Range(-1f, 1f)] public float glowDilate = 0.3f;

    [Header("Sparkles")]
    public bool sparkles = true;
    public float sparklesPerSecond = 18f;
    public int maxSparkles = 40;
    public Vector2 sparkleSize = new Vector2(5f, 12f);
    public Vector2 sparkleLifetime = new Vector2(0.6f, 1.3f);
    public float sparkleRiseSpeed = 40f;

    private class Sparkle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public Color color;
        public float size;
        public float spin;
        public float life;
        public float age;
    }

    private TextMeshProUGUI titleText;
    private Material glowMaterial;
    private RectTransform sparkleRoot;
    private readonly List<Sparkle> activeSparkles = new List<Sparkle>();
    private Vector3 baseScale;
    private float spawnTimer;

    private void Awake()
    {
        titleText = GetComponent<TextMeshProUGUI>();
        baseScale = transform.localScale;

        // Vertex colors are multiplied by the text color, so it must be white
        titleText.color = Color.white;

        GameObject root = new GameObject("TitleSparkles", typeof(RectTransform));
        sparkleRoot = root.GetComponent<RectTransform>();
        sparkleRoot.SetParent(transform, false);
        sparkleRoot.anchorMin = sparkleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        sparkleRoot.anchoredPosition = Vector2.zero;
    }

    private void Start()
    {
        // fontMaterial creates a private copy so other texts are not affected
        glowMaterial = titleText.fontMaterial;
    }

    private void LateUpdate()
    {
        ApplyTextEffects();
        ApplyGlow();
        ApplyPulse();
        UpdateSparkles();
    }

    // ---------------- Letters: rainbow + wave ----------------

    private void ApplyTextEffects()
    {
        if (!rainbow && !wave)
            return;

        titleText.ForceMeshUpdate();
        TMP_TextInfo info = titleText.textInfo;
        float time = Time.time;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible)
                continue;

            Vector3[] vertices = info.meshInfo[c.materialReferenceIndex].vertices;
            Color32[] colors = info.meshInfo[c.materialReferenceIndex].colors32;
            int v = c.vertexIndex;

            float offsetY = wave
                ? Mathf.Sin(time * waveSpeed + i * waveSpacing) * waveHeight
                : 0f;

            for (int j = 0; j < 4; j++)
            {
                if (rainbow)
                {
                    float hue = Mathf.Repeat(
                        time * colorSpeed + vertices[v + j].x / rainbowWidth, 1f);
                    colors[v + j] = Color.HSVToRGB(hue, saturation, 1f);
                }

                vertices[v + j] += new Vector3(0f, offsetY, 0f);
            }
        }

        titleText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    // ---------------- Glow (TMP underlay) ----------------

    private void ApplyGlow()
    {
        if (glowMaterial == null)
            return;

        if (!glow)
        {
            glowMaterial.DisableKeyword("UNDERLAY_ON");
            return;
        }

        float hue = Mathf.Repeat(Time.time * colorSpeed + 0.5f, 1f);
        Color glowColor = Color.HSVToRGB(hue, 1f, 1f);
        glowColor.a = 0.6f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f;

        glowMaterial.EnableKeyword("UNDERLAY_ON");
        glowMaterial.SetColor("_UnderlayColor", glowColor);
        glowMaterial.SetFloat("_UnderlayOffsetX", 0f);
        glowMaterial.SetFloat("_UnderlayOffsetY", 0f);
        glowMaterial.SetFloat("_UnderlayDilate", glowDilate);
        glowMaterial.SetFloat("_UnderlaySoftness", glowSoftness);
    }

    // ---------------- Pulse ----------------

    private void ApplyPulse()
    {
        float factor = pulse
            ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount
            : 1f;

        transform.localScale = new Vector3(
            baseScale.x * factor, baseScale.y * factor, baseScale.z);
    }

    // ---------------- Sparkles ----------------

    private void UpdateSparkles()
    {
        if (sparkles)
        {
            spawnTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.01f, sparklesPerSecond);

            while (spawnTimer >= interval)
            {
                spawnTimer -= interval;

                if (activeSparkles.Count < maxSparkles)
                    SpawnSparkle();
            }
        }

        for (int i = activeSparkles.Count - 1; i >= 0; i--)
        {
            Sparkle s = activeSparkles[i];
            s.age += Time.deltaTime;

            if (s.age >= s.life)
            {
                Destroy(s.rt.gameObject);
                activeSparkles.RemoveAt(i);
                continue;
            }

            float p = s.age / s.life;
            float fade = Mathf.Sin(p * Mathf.PI);   // grows then shrinks

            s.rt.anchoredPosition += s.velocity * Time.deltaTime;
            s.rt.sizeDelta = Vector2.one * s.size * fade;
            s.rt.localRotation = Quaternion.Euler(0f, 0f, 45f + s.spin * s.age);
            s.img.color = new Color(s.color.r, s.color.g, s.color.b, fade);
        }
    }

    private void SpawnSparkle()
    {
        Bounds b = titleText.textBounds;
        if (b.size.x <= 0f)
            return;

        GameObject obj = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(sparkleRoot, false);

        Image img = obj.GetComponent<Image>();
        img.raycastTarget = false;

        Sparkle s = new Sparkle
        {
            rt = rt,
            img = img,
            color = Color.HSVToRGB(Random.value, 0.45f, 1f),
            size = Random.Range(sparkleSize.x, sparkleSize.y),
            spin = Random.Range(-180f, 180f),
            life = Random.Range(sparkleLifetime.x, sparkleLifetime.y),
            velocity = new Vector2(
                Random.Range(-15f, 15f),
                Random.Range(sparkleRiseSpeed * 0.4f, sparkleRiseSpeed))
        };

        rt.anchoredPosition = new Vector2(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y));

        activeSparkles.Add(s);
    }

    private void OnDisable()
    {
        foreach (Sparkle s in activeSparkles)
        {
            if (s.rt != null)
                Destroy(s.rt.gameObject);
        }

        activeSparkles.Clear();
        transform.localScale = baseScale;
    }
}