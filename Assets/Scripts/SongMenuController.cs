using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#else
using SFB; // StandaloneFileBrowser (see notes below)
#endif

// Stores the selected song so the game scene can use it later
public static class SongData
{
    public static AudioClip Clip;
    public static string Name;
    public static bool HasSong => Clip != null;
}

[RequireComponent(typeof(AudioSource))]
public class SongMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private Button uploadButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Slider progressBar; // Optional

    [Header("Options")]
    [SerializeField] private string defaultText = "SongName";

    private AudioSource source;
    private bool isPaused;
    private bool hasStarted;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    private void Start()
    {
        uploadButton.onClick.AddListener(OnUploadClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        pauseButton.onClick.AddListener(OnPauseClicked);

        if (progressBar != null)
        {
            progressBar.interactable = false;
            progressBar.value = 0f;
        }

        // Restore the song if we are coming back to the menu
        if (SongData.HasSong)
        {
            source.clip = SongData.Clip;
            songNameText.text = SongData.Name;
        }
        else
        {
            songNameText.text = defaultText;
        }

        RefreshButtons();
    }

    private void Update()
    {
        if (source.clip == null) return;

        // Update the progress bar
        if (progressBar != null && source.clip.length > 0f)
            progressBar.value = source.time / source.clip.length;

        // Detect when the song has finished
        if (hasStarted && !source.isPlaying && !isPaused)
        {
            hasStarted = false;
            source.time = 0f;
            if (progressBar != null) progressBar.value = 0f;
            RefreshButtons();
        }
    }

    // ---------- UPLOAD ----------
    private void OnUploadClicked()
    {
        string path = OpenFileDialog();
        if (string.IsNullOrEmpty(path)) return;
        StartCoroutine(LoadAudio(path));
    }

    private string OpenFileDialog()
    {
#if UNITY_EDITOR
        return EditorUtility.OpenFilePanel("Select a song", "", "mp3,wav,ogg");
#else
        var extensions = new[] { new ExtensionFilter("Audio", "mp3", "wav", "ogg") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select a song", "", extensions, false);
        return paths.Length > 0 ? paths[0] : null;
#endif
    }

    private IEnumerator LoadAudio(string path)
    {
        string uri = new System.Uri(path).AbsoluteUri;
        AudioType type = GetAudioType(path);

        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, type))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading audio: " + req.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            string fileName = Path.GetFileNameWithoutExtension(path);
            clip.name = fileName;

            source.Stop();
            isPaused = false;
            hasStarted = false;
            source.clip = clip;

            songNameText.text = fileName;

            SongData.Clip = clip;
            SongData.Name = fileName;

            if (progressBar != null) progressBar.value = 0f;
            RefreshButtons();
        }
    }

    private AudioType GetAudioType(string path)
    {
        switch (Path.GetExtension(path).ToLower())
        {
            case ".wav": return AudioType.WAV;
            case ".ogg": return AudioType.OGGVORBIS;
            default: return AudioType.MPEG; // mp3
        }
    }

    // ---------- PLAY / PAUSE ----------
    private void OnPlayClicked()
    {
        if (source.clip == null) return;

        if (isPaused) source.UnPause();
        else source.Play();

        isPaused = false;
        hasStarted = true;
        RefreshButtons();
    }

    private void OnPauseClicked()
    {
        if (!source.isPlaying) return;

        source.Pause();
        isPaused = true;
        RefreshButtons();
    }

    // Shows Play when stopped/paused, and Pause while playing
    private void RefreshButtons()
    {
        bool playing = source.isPlaying;
        playButton.gameObject.SetActive(!playing);
        pauseButton.gameObject.SetActive(playing);
        playButton.interactable = source.clip != null;
    }
}