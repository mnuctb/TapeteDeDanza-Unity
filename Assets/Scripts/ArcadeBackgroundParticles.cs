using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// Generates a continuous field of neon particles and floating cosmic dust in the game background.
/// </summary>
public class ArcadeBackgroundParticles : MonoBehaviour
{
    // [FormerlySerializedAs] keeps the values you already set in the Inspector.

    [Header("Particle Settings")]
    [FormerlySerializedAs("cantidadParticulas")]
    public int particleCount = 35;
    [FormerlySerializedAs("velocidadMin")]
    public float minSpeed = 25f;
    [FormerlySerializedAs("velocidadMax")]
    public float maxSpeed = 70f;
    [FormerlySerializedAs("tamanoMin")]
    public float minSize = 4f;
    [FormerlySerializedAs("tamanoMax")]
    public float maxSize = 12f;

    [Header("Cyber Colors")]
    [FormerlySerializedAs("paletaColores")]
    public Color[] colorPalette = new Color[]
    {
        new Color(0f, 0.95f, 1f, 0.5f),     // Cyan
        new Color(1f, 0.2f, 0.65f, 0.45f),  // Neon Pink
        new Color(0.65f, 0.2f, 1f, 0.4f),   // Purple
        new Color(1f, 0.85f, 0.2f, 0.4f)    // Gold
    };

    private class Particle
    {
        public RectTransform rt;
        public Image img;
        public float speed;
        public float frequency;
        public float amplitude;
        public float phaseOffset;
        public float baseX;
    }

    private List<Particle> particles = new List<Particle>();
    private float backgroundWidth = 1920f;
    private float backgroundHeight = 1080f;

    private void Start()
    {
        RectTransform backgroundRect = GetComponent<RectTransform>();
        if (backgroundRect != null && backgroundRect.rect.width > 200)
        {
            backgroundWidth = backgroundRect.rect.width;
            backgroundHeight = backgroundRect.rect.height;
        }

        // Generate the particles spread across the screen
        for (int i = 0; i < particleCount; i++)
        {
            GameObject obj = new GameObject("Background_Particle_" + i, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(this.transform, false);

            Image img = obj.GetComponent<Image>();
            img.color = colorPalette[Random.Range(0, colorPalette.Length)];
            img.raycastTarget = false;

            RectTransform rt = obj.GetComponent<RectTransform>();
            float size = Random.Range(minSize, maxSize);
            rt.sizeDelta = new Vector2(size, size);

            float x = Random.Range(-backgroundWidth * 0.5f, backgroundWidth * 0.5f);
            float y = Random.Range(-backgroundHeight * 0.5f, backgroundHeight * 0.5f);
            rt.anchoredPosition = new Vector2(x, y);

            particles.Add(new Particle
            {
                rt = rt,
                img = img,
                speed = Random.Range(minSpeed, maxSpeed),
                frequency = Random.Range(1f, 2.5f),
                amplitude = Random.Range(15f, 40f),
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                baseX = x
            });
        }
    }

    private void Update()
    {
        float halfHeight = backgroundHeight * 0.5f;

        for (int i = 0; i < particles.Count; i++)
        {
            Particle p = particles[i];
            Vector2 pos = p.rt.anchoredPosition;

            // Float gently upwards
            pos.y += p.speed * Time.deltaTime;

            // Natural sinusoidal horizontal sway
            pos.x = p.baseX + Mathf.Sin(Time.time * p.frequency + p.phaseOffset) * p.amplitude;

            // Infinite loop: leaving through the top re-enters from the bottom
            if (pos.y > halfHeight + 30f)
            {
                pos.y = -halfHeight - 30f;
                p.baseX = Random.Range(-backgroundWidth * 0.5f, backgroundWidth * 0.5f);
            }

            p.rt.anchoredPosition = pos;

            // Brightness flicker
            float alpha = 0.25f + Mathf.PingPong(Time.time * 0.7f + p.phaseOffset, 0.4f);
            Color c = p.img.color;
            c.a = alpha;
            p.img.color = c;
        }
    }
}