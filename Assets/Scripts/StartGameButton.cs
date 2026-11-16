using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game_scene";
    [SerializeField] private TMP_Text warningText; // Optional

    private void Start()
    {
        if (warningText != null) warningText.gameObject.SetActive(false);
        GetComponent<Button>().onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        if (!SongData.HasSong)
        {
            if (warningText != null)
            {
                warningText.text = "Please upload a song first!";
                warningText.gameObject.SetActive(true);
            }
            Debug.LogWarning("No song loaded.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}