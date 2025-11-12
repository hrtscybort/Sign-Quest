using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class VideoController : MonoBehaviour
{
    // [SerializeField] private VideoClip[] wave1Videos;
    // [SerializeField] private VideoClip[] wave2Videos;
    // [SerializeField] private VideoClip[] wave3Videos;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private TMP_Text word;
    
    private int currVideoIndex = 0;
    private int currWave = 1;
    private VideoClip[] currWaveVideos;

    private void Start()
    {
        UpdateVideoForWave(currWave);
        
        nextButton.onClick.AddListener(NextVideo);
        prevButton.onClick.AddListener(PreviousVideo);
    }

    public void UpdateVideoForWave(int wave)
    {
        currWave = wave;
        string video_folder = $"level_{currWave}";
        currWaveVideos = Resources.LoadAll<VideoClip>(video_folder);
        
        if (currWaveVideos == null || currWaveVideos.Length == 0)
        {
            Debug.LogWarning("Counldn't find videos for Resources/" + video_folder);
            return;
        } 
        // switch (currWave)
        // {
        //     case 1:
        //         currWaveVideos = wave1Videos;
        //         break;
        //     case 2:
        //         currWaveVideos = wave2Videos;
        //         break;
        //     case 3:
        //         currWaveVideos = wave3Videos;
        //         break;
        //     default:
        //         Debug.LogWarning("No videos available for this wave.");
        //         return;
        // }
        
        currVideoIndex = 0;
        PlayCurrVideo();
    }

    private void PlayCurrVideo()
    {
        if (currWaveVideos == null || currWaveVideos.Length == 0)
        {
            Debug.LogWarning("No videos available for current wave.");
            return;
        }

        if (currVideoIndex >= 0 && currVideoIndex < currWaveVideos.Length)
        {
            videoPlayer.clip = currWaveVideos[currVideoIndex];
            videoPlayer.Play();
            word.text = videoPlayer.clip.name;
        }
    }

    private void NextVideo()
    {
        if (currWaveVideos == null || currWaveVideos.Length == 0) return;
        
        currVideoIndex++;
        if (currVideoIndex >= currWaveVideos.Length)
        {
            currVideoIndex = 0;
        }
        PlayCurrVideo();
    }

    private void PreviousVideo()
    {
        if (currWaveVideos == null || currWaveVideos.Length == 0) return;
        
        currVideoIndex--;
        if (currVideoIndex < 0)
        {
            currVideoIndex = currWaveVideos.Length - 1;
        }
        PlayCurrVideo();
    }
}