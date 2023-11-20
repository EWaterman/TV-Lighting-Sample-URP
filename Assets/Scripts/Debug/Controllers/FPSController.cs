using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Attach to a Game Object that has a TextMeshPro component to write the FPS to it.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class FPSController : MonoBehaviour
{
    TMP_Text _fpsText;

    void Start()
    {
        _fpsText = GetComponent<TMP_Text>();
        StartCoroutine(FramesPerSecond());
    }

    IEnumerator FramesPerSecond()
    {
        while (true)
        {
            int fps = (int)(1f / Time.deltaTime);
            DisplayFPS(fps);

            yield return new WaitForSeconds(0.2f);
        }
    }

    void DisplayFPS(float fps)
    {
        _fpsText.text = $"{fps} FPS";
    }
}