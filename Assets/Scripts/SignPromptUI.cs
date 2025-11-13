using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using Assets.Scripts.Combat;

public class SignPromptUI : MonoBehaviour
{
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private GameObject referenceVideo;
    [SerializeField] private Button referenceButton;
    [SerializeField] private VideoClip[] tutorialVideos;
    [SerializeField] private VideoPlayer video;

    [SerializeField] private BattleSystem battleSystem;

    public TMP_Text WordText => wordText;
    public bool IsDone { get; private set; }
    public float Score { get; private set; }
    private bool TutorialPressed = false;
    public bool tutorialPressed => TutorialPressed;


    private void Start()
    {
        TutorialPressed = false;

        referenceButton.onClick.AddListener(Reference);
        referenceVideo.SetActive(false);

        int currWave = battleSystem.CurrentWave;

        string video_folder = $"level_{currWave}";
        tutorialVideos = Resources.LoadAll<VideoClip>(video_folder);
    }

    public void Show(string word)
    {
        wordText.text = word;
        gameObject.SetActive(true);
        IsDone = false;

        referenceVideo.SetActive(false);
    }

    public void Finish(float score)
    {
        Score = score;
        IsDone = true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Reference()
    {
        foreach (var clip in tutorialVideos)
        {
            if (clip.name.Equals(wordText.text, System.StringComparison.OrdinalIgnoreCase))
            {
                video.clip = clip;
                video.Play();

                Debug.Log("Current video" + clip.name);
                break;
            }
        }

        referenceVideo.SetActive(true);
        TutorialPressed = true;
    }
}