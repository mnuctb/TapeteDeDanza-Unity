using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
/// Handles custom song selection, audio preview and game start.
/// </summary>
public class MenuController : MonoBehaviour
{
    // [FormerlySerializedAs] keeps the references and values you already set in the Inspector.

    [Header("Scene Settings")]
    [FormerlySerializedAs("nombreEscenaJuego")]
    public string gameSceneName = "Game_scene";
    [FormerlySerializedAs("botonIniciar")]
    public Button startButton;

    [Header("Song Container (UI)")]
    [FormerlySerializedAs("textoNombreCancion")]
    public TextMeshProUGUI songNameText;
    [FormerlySerializedAs("textoDetallesCancion")]
    public TextMeshProUGUI songDetailsText;
    [FormerlySerializedAs("botonAnterior")]
    public Button previousButton;
    [FormerlySerializedAs("botonSiguiente")]
    public Button nextButton;
    [FormerlySerializedAs("botonPreescucha")]
    public Button previewButton;
    [FormerlySerializedAs("textoIconoPreescucha")]
    public TextMeshProUGUI previewIconText; // The text inside the preview button ("▶" or "⏸")

    [Header("Song Bank (Drag your MP3/WAV audio here!)")]
    [FormerlySerializedAs("canciones")]
    public AudioClip[] songs;
    [FormerlySerializedAs("nombresCanciones")]
    public string[] songNames;
    [FormerlySerializedAs("detallesCanciones")]
    public string[] songDetails; // E.g.: "130 BPM • Normal", "160 BPM • Hard"

    private AudioSource audioSource;
    private int currentIndex = 0;
    private bool isPlaying = false;

    private void Start()
    {
        Time.timeScale = 1f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;

        // Hook up buttons
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (previousButton != null) previousButton.onClick.AddListener(PreviousSong);
        if (nextButton != null) nextButton.onClick.AddListener(NextSong);
        if (previewButton != null) previewButton.onClick.AddListener(TogglePreview);

        UpdateDisplay();
    }

    public void PreviousSong()
    {
        if (songs == null || songs.Length == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = songs.Length - 1;
        UpdateDisplay();
        if (isPlaying) PlayCurrent();
    }

    public void NextSong()
    {
        if (songs == null || songs.Length == 0) return;
        currentIndex++;
        if (currentIndex >= songs.Length) currentIndex = 0;
        UpdateDisplay();
        if (isPlaying) PlayCurrent();
    }

    public void TogglePreview()
    {
        if (songs == null || songs.Length == 0) return;

        if (isPlaying)
        {
            audioSource.Stop();
            isPlaying = false;
            if (previewIconText != null) previewIconText.text = "▶";
        }
        else
        {
            PlayCurrent();
        }
    }

    private void PlayCurrent()
    {
        if (songs != null && songs.Length > currentIndex && songs[currentIndex] != null)
        {
            audioSource.clip = songs[currentIndex];
            audioSource.Play();
            isPlaying = true;
            if (previewIconText != null) previewIconText.text = "⏸";
        }
    }

    private void UpdateDisplay()
    {
        if (songs == null || songs.Length == 0)
        {
            if (songNameText != null) songNameText.text = "Drag your songs into the Inspector";
            if (songDetailsText != null) songDetailsText.text = "No track assigned";
            return;
        }

        if (songNameText != null)
        {
            string songName = (songNames != null && currentIndex < songNames.Length && !string.IsNullOrEmpty(songNames[currentIndex]))
                ? songNames[currentIndex]
                : (songs[currentIndex] != null ? songs[currentIndex].name : "Track " + (currentIndex + 1));
            songNameText.text = "🎵 " + songName;
        }

        if (songDetailsText != null)
        {
            if (songDetails != null && currentIndex < songDetails.Length && !string.IsNullOrEmpty(songDetails[currentIndex]))
                songDetailsText.text = songDetails[currentIndex];
            else
                songDetailsText.text = "4 Arrows Mode • Track " + (currentIndex + 1);
        }
    }

    public void StartGame()
    {
        // Saves the chosen song so Game_scene can play it
        PlayerPrefs.SetInt("SelectedSong", currentIndex);
        PlayerPrefs.Save();

        if (audioSource != null) audioSource.Stop();
        if (startButton != null) startButton.interactable = false;

        SceneManager.LoadScene(gameSceneName);
    }
}