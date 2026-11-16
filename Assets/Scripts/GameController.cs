using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    [Header("Score UI")]
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI multiplierText;
    public Slider songProgressSlider;

    [Header("Performance UI")]
    public TextMeshProUGUI perfectText;
    public TextMeshProUGUI brilliantText;
    public TextMeshProUGUI goodText;
    public TextMeshProUGUI failuresText;

    [Header("Life and Modals")]
    public Slider lifeSlider;
    public GameObject modalYouLost;
    public GameObject modalVictory;

    [Header("Character and Receptors")]
    public Animator dancerAnimator;
    public Image[] visualReceptors;   // Order: K, V, Up, X

    [Header("Falling Arrows Track")]
    public RectTransform rhythmTrack;
    public Sprite[] arrowSprites;     // Order: left, down, up, right
    public float fallSpeed = 380f;
    public float timeBetweenNotes = 0.9f;

    [Header("Song Settings")]
    public float songDuration = 65f;

    [Header("Audio")]
    public GameSongPlayer songPlayer;

    private float currentTime;
    private int score;
    private int combo;
    private int multiplier = 1;
    private float life = 100f;
    private int perfectCount;
    private int brilliantCount;
    private int goodCount;
    private int failureCount;
    private bool gameOver;

    private static readonly KeyCode[] keys =
    {
        KeyCode.K, KeyCode.V, KeyCode.UpArrow, KeyCode.X
    };

    private static readonly string[] poseNames = { "K", "V", "Up", "X" };

    private class FlyingNote
    {
        public int lane;
        public RectTransform rectTransform;
    }

    private readonly List<FlyingNote> activeNotes = new List<FlyingNote>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private GameSongPlayer GetSongPlayer()
    {
        if (songPlayer == null)
            songPlayer = FindFirstObjectByType<GameSongPlayer>();

        return songPlayer;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        gameOver = false;

        currentTime = 0f;
        score = 0;
        combo = 0;
        multiplier = 1;
        life = 100f;

        perfectCount = 0;
        brilliantCount = 0;
        goodCount = 0;
        failureCount = 0;

        StopAllCoroutines();

        foreach (var note in activeNotes)
        {
            if (note.rectTransform != null)
                Destroy(note.rectTransform.gameObject);
        }

        activeNotes.Clear();

        if (dancerAnimator == null)
        {
            GameObject dancerObject = GameObject.Find("dance");

            if (dancerObject != null)
                dancerAnimator = dancerObject.GetComponent<Animator>();
        }

        SetupReceptorButtons();

        if (modalYouLost != null)
            modalYouLost.SetActive(false);

        if (modalVictory != null)
            modalVictory.SetActive(false);

        if (songProgressSlider != null)
            songProgressSlider.value = songProgressSlider.minValue;

        UpdateHUD();

        StartCoroutine(NoteSpawner());
    }

    private void Update()
    {
        if (gameOver)
            return;

        // Song progress
        currentTime += Time.deltaTime;

        float progress = songDuration > 0f
            ? Mathf.Clamp01(currentTime / songDuration)
            : 1f;

        if (songProgressSlider != null)
        {
            songProgressSlider.value = Mathf.Lerp(
                songProgressSlider.minValue,
                songProgressSlider.maxValue,
                progress
            );
        }

        if (progress >= 1f)
        {
            WinGame();
            return;
        }

        MoveNotes();

        if (gameOver)
            return;

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
                PressLane(i);
        }
    }

    public void PressLane(int lane)
    {
        if (gameOver || lane < 0 || lane >= keys.Length)
            return;

        HighlightReceptor(lane, true);
        PlayDancePose(lane);

        if (ArcadeVisualEffects.Instance != null)
            ArcadeVisualEffects.Instance.DancerFlash(lane);

        CheckNoteHit(lane);

        StartCoroutine(TurnOffReceptorAfterTime(lane, 0.18f));
    }

    private void PlayDancePose(int lane)
    {
        if (dancerAnimator == null)
            return;

        foreach (string pose in poseNames)
            dancerAnimator.ResetTrigger("Pose_" + pose);

        dancerAnimator.SetTrigger("Pose_" + poseNames[lane]);
        dancerAnimator.Play(poseNames[lane], 0, 0f);
    }

    private void MoveNotes()
    {
        float hitHeight = GetHitHeight();

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            FlyingNote note = activeNotes[i];

            if (note.rectTransform == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            note.rectTransform.anchoredPosition +=
                Vector2.down * fallSpeed * Time.deltaTime;

            if (note.rectTransform.anchoredPosition.y < hitHeight - 65f)
            {
                Destroy(note.rectTransform.gameObject);
                activeNotes.RemoveAt(i);

                RegisterFailure();

                if (gameOver)
                    return;
            }
        }
    }

    private void CheckNoteHit(int lane)
    {
        FlyingNote bestNote = null;
        float smallestDistance = float.MaxValue;
        float hitHeight = GetHitHeight();

        foreach (var note in activeNotes)
        {
            if (note.lane != lane || note.rectTransform == null)
                continue;

            float distance = Mathf.Abs(
                note.rectTransform.anchoredPosition.y - hitHeight
            );

            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                bestNote = note;
            }
        }

        if (bestNote == null || smallestDistance > 95f)
            return;

        activeNotes.Remove(bestNote);
        Destroy(bestNote.rectTransform.gameObject);

        int basePoints;
        int quality;
        string rating;

        if (smallestDistance <= 30f)
        {
            basePoints = 300;
            quality = 1;
            rating = "PERFECT!";
        }
        else if (smallestDistance <= 65f)
        {
            basePoints = 180;
            quality = 2;
            rating = "BRILLIANT!";
        }
        else
        {
            basePoints = 100;
            quality = 3;
            rating = "GOOD!";
        }

        RegisterHit(basePoints, quality);

        if (HitFlash.Instance != null)
            HitFlash.Instance.Flash(lane, GetReceptorWorldPosition(lane));

        if (HitStreaks.Instance != null)
            HitStreaks.Instance.Spawn(lane, GetReceptorWorldPosition(lane));

        if (ArcadeVisualEffects.Instance != null)
            ArcadeVisualEffects.Instance.ExplodeReceptor(
                lane, GetReceptorWorldPosition(lane), rating
            );
    }

    private Vector3 GetReceptorWorldPosition(int lane)
    {
        if (lane < visualReceptors.Length && visualReceptors[lane] != null)
            return visualReceptors[lane].transform.position;

        return rhythmTrack != null
            ? rhythmTrack.transform.position
            : transform.position;
    }

    private void HighlightReceptor(int lane, bool active)
    {
        if (lane >= visualReceptors.Length || visualReceptors[lane] == null)
            return;

        Image receptor = visualReceptors[lane];

        Color color = receptor.color;
        color.a = active ? 1f : 0.6f;

        receptor.color = color;
        receptor.transform.localScale =
            active ? Vector3.one * 1.25f : Vector3.one;
    }

    private IEnumerator TurnOffReceptorAfterTime(int lane, float time)
    {
        yield return new WaitForSeconds(time);
        HighlightReceptor(lane, false);
    }

    public void RegisterHit(int basePoints, int quality)
    {
        combo++;
        multiplier = 1 + combo / 10;

        score += basePoints * multiplier;
        life = Mathf.Min(100f, life + 5f);

        switch (quality)
        {
            case 1: perfectCount++; break;
            case 2: brilliantCount++; break;
            case 3: goodCount++; break;
        }

        UpdateHUD();
    }

    public void RegisterFailure()
    {
        combo = 0;
        multiplier = 1;

        failureCount++;
        life -= 6f;

        UpdateHUD();

        if (life <= 0f)
            LoseGame();
    }

    private IEnumerator NoteSpawner()
    {
        yield return new WaitForSeconds(2f);

        int[] sequence = { 0, 1, 2, 3, 1, 0, 2, 3, 0, 2, 1, 3 };
        int index = 0;

        while (!gameOver)
        {
            SpawnNote(sequence[index % sequence.Length]);
            index++;

            yield return new WaitForSeconds(timeBetweenNotes);
        }
    }

    private void SpawnNote(int lane)
    {
        if (rhythmTrack == null ||
            lane >= arrowSprites.Length ||
            arrowSprites[lane] == null ||
            lane >= visualReceptors.Length ||
            visualReceptors[lane] == null)
            return;

        GameObject noteObject = new GameObject(
            "Note_" + lane,
            typeof(RectTransform),
            typeof(Image)
        );

        noteObject.transform.SetParent(rhythmTrack, false);

        RectTransform rt = noteObject.GetComponent<RectTransform>();

        rt.sizeDelta = new Vector2(68f, 68f);
        rt.anchoredPosition = new Vector2(
            visualReceptors[lane].rectTransform.anchoredPosition.x, 240f
        );

        noteObject.GetComponent<Image>().sprite = arrowSprites[lane];

        activeNotes.Add(new FlyingNote { lane = lane, rectTransform = rt });
    }

    private float GetHitHeight()
    {
        return visualReceptors.Length > 0 && visualReceptors[0] != null
            ? visualReceptors[0].rectTransform.anchoredPosition.y
            : -175f;
    }

    // Makes the on-screen receptors clickable too (mouse/touch).
    private void SetupReceptorButtons()
    {
        for (int i = 0; i < visualReceptors.Length; i++)
        {
            if (visualReceptors[i] == null)
                continue;

            Button button = visualReceptors[i].GetComponent<Button>();

            if (button == null)
                button = visualReceptors[i].gameObject.AddComponent<Button>();

            int lane = i;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => PressLane(lane));
        }
    }

    private void UpdateHUD()
    {
        if (pointsText != null) pointsText.text = score.ToString("D6");
        if (comboText != null) comboText.text = "COMBO\n" + combo;
        if (multiplierText != null) multiplierText.text = "x" + multiplier;
        if (lifeSlider != null) lifeSlider.value = life;

        // <pos=70%> deja la etiqueta a la izquierda y el número a la derecha
        if (perfectText != null) perfectText.text = "PERFECTO<pos=70%>" + perfectCount;
        if (brilliantText != null) brilliantText.text = "GENIAL<pos=70%>" + brilliantCount;
        if (goodText != null) goodText.text = "BIEN<pos=70%>" + goodCount;
        if (failuresText != null) failuresText.text = "FALLOS<pos=70%>" + failureCount;
    }

    private void LoseGame()
    {
        if (gameOver)
            return;

        gameOver = true;
        StopAllCoroutines();

        // Detiene la música al perder
        GameSongPlayer player = GetSongPlayer();
        if (player != null)
            player.Stop();

        if (modalYouLost != null)
        {
            modalYouLost.SetActive(true);
            modalYouLost.transform.SetAsLastSibling();
        }
    }

    public void WinGame()
    {
        if (gameOver)
            return;

        gameOver = true;
        StopAllCoroutines();

        if (modalVictory == null)
        {
            Debug.LogError("modalVictory is NOT assigned in the Inspector!");
            return;
        }

        modalVictory.SetActive(true);
        modalVictory.transform.SetAsLastSibling();
        modalVictory.transform.localScale = Vector3.one;

        RectTransform rt = modalVictory.GetComponent<RectTransform>();

        if (rt != null)
        {
            Vector3 position = rt.localPosition;
            position.z = 0f;
            rt.localPosition = position;
        }

        CanvasGroup group = modalVictory.GetComponent<CanvasGroup>();

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    public void RetryButton()
    {
        StartGame();

        // Reinicia la canción desde el inicio
        GameSongPlayer player = GetSongPlayer();
        if (player != null)
            player.Restart();
    }

    public void MenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu_scene");
    }
}