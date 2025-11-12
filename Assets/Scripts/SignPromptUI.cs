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

    public TMP_Text WordText => wordText;
    public bool IsDone { get; private set; }
    public float Score { get; private set; }
    private bool TutorialPressed = false;
    public bool tutorialPressed => TutorialPressed;

    private void Start()
    {
        TutorialPressed = false;

        referenceButton.onClick.AddListener(Reference);

        foreach (var clip in tutorialVideos)
        {
            if (clip.name.Equals(wordText.text, System.StringComparison.OrdinalIgnoreCase))
            {
                video.clip = clip;
                video.Play();
                break;
            }
        }

    }

    public void Show(string word)
    {
        gameObject.SetActive(true);
        IsDone = false;
        wordText.text = word;
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
        referenceVideo.SetActive(true);
        TutorialPressed = true;
    }
}