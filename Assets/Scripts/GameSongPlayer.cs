using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameSongPlayer : MonoBehaviour
{
    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    private void Start()
    {
        if (SongData.HasSong)
        {
            source.clip = SongData.Clip;
            source.time = 0f;
            source.Play();
        }
        else
        {
            Debug.LogWarning("No song found in SongData. Did you start from the menu?");
        }
    }

    // Reinicia la canción desde el segundo 0 (para el botón Reintentar)
    public void Restart()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        if (source.clip == null)
            return;

        source.Stop();
        source.time = 0f;
        source.Play();
    }

    // Detiene la canción (por ejemplo al perder)
    public void Stop()
    {
        if (source != null)
            source.Stop();
    }

    public float SongTime => source != null ? source.time : 0f;
    public bool IsPlaying => source != null && source.isPlaying;
}