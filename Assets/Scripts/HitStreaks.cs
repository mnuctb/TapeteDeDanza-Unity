using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitStreaks : MonoBehaviour
{
    public static HitStreaks Instance;

    [Header("Setup")]
    [SerializeField] private RectTransform streakParent;   // Game_Background (or Gameplay_Panel)

    [Header("Amount")]
    [SerializeField] private int streaksPerHit = 16;

    [Header("Look")]
    [SerializeField] private float width = 4f;
    [SerializeField] private float minLength = 40f;
    [SerializeField] private float maxLength = 150f;
    [SerializeField] private float spawnRadius = 30f;

    [Header("Motion")]
    [SerializeField] private float minSpeed = 500f;
    [SerializeField] private float maxSpeed = 1500f;
    [SerializeField] private float minLife = 0.3f;
    [SerializeField] private float maxLife = 0.8f;
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.5f; // chance to go upward

    [Header("Flicker")]
    [SerializeField] private bool flicker = true;

    // Order: left, down, up, right
    [SerializeField]
    private Color[] laneColors =
    {
        new Color(0.0f, 0.9f, 1.0f, 1f),
        new Color(1.0f, 0.5f, 0.1f, 1f),
        new Color(0.1f, 1.0f, 0.5f, 1f),
        new Color(1.0f, 0.3f, 0.6f, 1f)
    };

    private Sprite streakSprite;

    private class Streak
    {
        public RectTransform rt;
        public Image img;
        public Vector2 dir;
        public float speed;
        public float life;
        public float age;
        public float length;
    }

    private void Awake()
    {
        Instance = this;
        streakSprite = CreateStreakSprite();
    }

    // Call this when the player hits a note
    public void Spawn(int lane, Vector3 worldPosition)
    {
        if (streakParent == null)
        {
            Debug.LogError("HitStreaks: streakParent is NOT assigned!");
            return;
        }

        Color baseColor = laneColors[Mathf.Clamp(lane, 0, laneColors.Length - 1)];
        StartCoroutine(BurstRoutine(baseColor, worldPosition));
    }

    private IEnumerator BurstRoutine(Color baseColor, Vector3 worldPosition)
    {
        List<Streak> streaks = new List<Streak>();

        for (int i = 0; i < streaksPerHit; i++)
        {
            float angle = Random.value < upwardBias
                ? Random.Range(30f, 150f)       // upward
                : Random.Range(0f, 360f);       // any direction

            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject obj = new GameObject("HitStreak", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(streakParent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.position = worldPosition;

            Vector3 lp = rt.localPosition;
            lp.z = 0f;
            lp += (Vector3)(Random.insideUnitCircle * spawnRadius);
            rt.localPosition = lp;

            // The sprite is vertical, so rotate it to face its travel direction
            rt.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            Image img = obj.GetComponent<Image>();
            img.sprite = streakSprite;
            img.raycastTarget = false;

            // Mix the lane color with white so the core looks brighter
            img.color = Color.Lerp(baseColor, Color.white, Random.Range(0f, 0.5f));

            Streak s = new Streak
            {
                rt = rt,
                img = img,
                dir = dir,
                speed = Random.Range(minSpeed, maxSpeed),
                life = Random.Range(minLife, maxLife),
                length = Random.Range(minLength, maxLength)
            };

            rt.sizeDelta = new Vector2(width, s.length);
            streaks.Add(s);
        }

        bool anyAlive = true;

        while (anyAlive)
        {
            anyAlive = false;

            for (int i = 0; i < streaks.Count; i++)
            {
                Streak s = streaks[i];
                if (s.rt == null) continue;

                s.age += Time.deltaTime;

                if (s.age >= s.life)
                {
                    Destroy(s.rt.gameObject);
                    s.rt = null;
                    continue;
                }

                anyAlive = true;

                float p = s.age / s.life;

                // Slows down over time
                float currentSpeed = s.speed * (1f - p);
                s.rt.localPosition += (Vector3)(s.dir * currentSpeed * Time.deltaTime);

                // Gets shorter and fades
                s.rt.sizeDelta = new Vector2(width, s.length * (1f - p * 0.6f));

                Color c = s.img.color;
                c.a = (1f - p) * (flicker ? Random.Range(0.5f, 1f) : 1f);
                s.img.color = c;
            }

            yield return null;
        }
    }

    // Thin line: bright in the center, soft at the edges and tips
    private Sprite CreateStreakSprite()
    {
        const int w = 8;
        const int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float ty = y / (float)(h - 1);
            float tipFade = 1f - Mathf.Abs(ty - 0.5f) * 2f;      // soft at both ends
            tipFade = Mathf.Clamp01(tipFade * 1.5f);

            for (int x = 0; x < w; x++)
            {
                float tx = x / (float)(w - 1);
                float side = 1f - Mathf.Abs(tx - 0.5f) * 2f;     // bright center
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, side * tipFade));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}