using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Wraps objects that can play videos.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerController : MonoBehaviour   
{
    protected VideoPlayer _player;

    protected virtual void Awake()
    {
        _player = GetComponent<VideoPlayer>();
    }

    /// <summary>
    /// Changes the video being played.
    /// </summary>
    public void SwitchVideo(VideoClip clip)
    {
        _player.clip = clip;
    }
}
