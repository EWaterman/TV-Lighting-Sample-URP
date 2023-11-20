using UnityEngine.Video;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

/// <summary>
/// Controls all main functionality for TV objects.
/// </summary>
public class TVController : VideoPlayerController
{
    [Header("Dependencies")]
    [SerializeField, Tooltip("The light component that will emit the light of the screen.")]
    Light _screenLight;

    [SerializeField, Tooltip("The Game Object with a MeshRenderer containing the material of the screen.")]
    GameObject _screenObj;

    [SerializeField, Tooltip("The material index of the screen. Leave 0 unless your model is built with sub-meshes/materials.")]
    int _screenMatIndex = 0;

    [Header("Config - Sampling")]
    [SerializeField, Tooltip("The number of pixels in the current video frame to sample when calculating the average color of the emission. Making this higher makes the color more accurate but can reduce framerate.")]
    int _numPixelsToSample = 40;

    [Header("Config - Smoothing")]
    [SerializeField, Range(0, 1020), Tooltip("The threshold for the absolute color difference (to a max of 1020, 255 per RGBA channel) from the previous light color before we bypass smoothing and jump straight to that color. This lets us more accurately represent large color changes between frames. Making this too low can introduce color jitter between frames.")]
    float _thresholdForSmoothingBypass = 150;

    [Tooltip("How much to smooth out the light color changes from frame to frame; higher values reduce color jitter between frames but less accurately represents larger changes in colors and can cause the lighting to lag behind what's displayed on the screen.")]
    [SerializeField, Range(1, 50)]
    int _smoothing = 20;

    Queue<Color32> _smoothQueue;
    Colorish _colorTotalInWindow;
    Color32 _prevColor;
    Texture2D _screenTexture;
    Material _screenMaterial;

    protected override void Awake()
    {
        base.Awake();

        _smoothQueue = new Queue<Color32>(_smoothing);

        _screenTexture = new(1, 1);  // We'll resize later.

        _screenMaterial = _screenObj.GetComponent<MeshRenderer>().materials[_screenMatIndex];
    }

    void OnEnable()
    {
        _player.sendFrameReadyEvents = true;
        _player.frameReady += OnNewFrame;
    }

    void OnDisable()
    {
        _player.sendFrameReadyEvents = false;
        _player.frameReady -= OnNewFrame;
    }

    void OnNewFrame(VideoPlayer source, long frameIdx)
    {
        ExtractTextureFromScreen();
        ComputeAndApplyColorToLightSources();
    }

    /// <summary>
    /// Get the pixels of the current frame from the screen shader output as a Texture2D.
    /// </summary>
    void ExtractTextureFromScreen()
    {
        Texture screenTexture = _screenMaterial.mainTexture;
        int width = screenTexture.width;
        int height = screenTexture.height;

        if (_screenTexture.width != width || _screenTexture.height != height)
        {
            _screenTexture.Reinitialize(width, height);
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = renderTexture;
        Graphics.Blit(null, renderTexture, _screenMaterial);
        _screenTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        _screenTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
    }

    /// <summary>
    /// Determines the color that the TV light source should emit, then applies it.
    /// </summary>
    void ComputeAndApplyColorToLightSources()
    {
        Color32 newColor = AverageColorsFromFrame();

        Color32 newColorSmoothed = HasColorChangeSurpassedThreshold(newColor) ?
            ComputeNonSmoothedColor(newColor) :
            ComputeSmoothedColor(newColor);

        _prevColor = newColorSmoothed;
        _screenLight.color = newColorSmoothed;
    }

    /// <summary>
    /// Returns true if the color value of a given color differs by a certain absolute magnitude from
    /// the previous color.
    /// </summary>
    bool HasColorChangeSurpassedThreshold(Color32 newColor)
    {
        float colorDiff = Mathf.Abs(_prevColor.r - newColor.r)
            + Mathf.Abs(_prevColor.g - newColor.g)
            + Mathf.Abs(_prevColor.b - newColor.b)
            + Mathf.Abs(_prevColor.a - newColor.a);

        return colorDiff > _thresholdForSmoothingBypass;
    }

    /// <summary>
    /// Randomly sample some pixels from the current frame to get the average color of the frame.
    /// </summary>
    Color32 AverageColorsFromFrame()
    {
        Color32[] texColors = _screenTexture.GetPixels32();
        int numPixels = texColors.Length;
        int actualNumSamplesToSample = Mathf.Min(_numPixelsToSample, numPixels);

        float r = 0;
        float g = 0;
        float b = 0;
        float a = 0;

        for (int i = 0; i < actualNumSamplesToSample; i++)
        {
            int pixelToSample = UnityEngine.Random.Range(0, numPixels);

            r += texColors[pixelToSample].r;
            g += texColors[pixelToSample].g;
            b += texColors[pixelToSample].b;
            a += texColors[pixelToSample].a;
        }
        r /= actualNumSamplesToSample;
        g /= actualNumSamplesToSample;
        b /= actualNumSamplesToSample;
        a /= actualNumSamplesToSample;

        return new((byte)r, (byte)g, (byte)b, (byte)a);
    }

    /// <summary>
    /// Compute a more smoothed light color to emit via a rolling average using a FIFO queue.
    /// </summary>
    Color32 ComputeSmoothedColor(Color32 newColor)
    {
        // Slide the rolling average window by removing the oldest values.
        // We do this via loop (instead of just removing one entry) in case the smoothing value is reduced via Inspector at runtime.
        while (_smoothQueue.Count >= _smoothing)
        {
            _colorTotalInWindow = _colorTotalInWindow.Subtract(_smoothQueue.Dequeue());
        }

        // Then add the new color to the end of the window and compute a new average with it.
        _smoothQueue.Enqueue(newColor);
        _colorTotalInWindow = _colorTotalInWindow.Add(newColor);

        return _colorTotalInWindow.Divide(_smoothQueue.Count).ToColor32();
    }

    /// <summary>
    /// Bypass smoothing for the given color by clearing the queue of all other colors.
    /// This allows the given color to completely take over (and then we can start smoothing again based on it).
    /// </summary>
    Color32 ComputeNonSmoothedColor(Color32 newColor)
    {
        _smoothQueue.Clear();
        _smoothQueue.Enqueue(newColor);
        _colorTotalInWindow = new(newColor);
        return newColor;
    }
}

/// <summary>
/// An intermediary object that functions similar to the color struct. Needed because this holds a sum of all
/// the colors across the window which will likely have a value over 255 (which is the max for Color32).
/// </summary>
public struct Colorish
{
    public float r;
    public float g;
    public float b;
    public float a;

    public Colorish(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Colorish(Color32 color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public static implicit operator Colorish(Color32 color)
    {
        return new(color);
    }

    public override string ToString()
    {
        return new StringBuilder("RGBA(")
            .Append(r).Append(", ")
            .Append(g).Append(", ")
            .Append(b).Append(", ")
            .Append(a).Append(")")
            .ToString();
    }
}

public static class ColorishUtils
{
    public static Colorish Add(this Colorish color, Color32 colorToAdd)
    {
        return new(
            color.r + colorToAdd.r,
            color.g + colorToAdd.g,
            color.b + colorToAdd.b,
            color.a + colorToAdd.a);
    }

    public static Colorish Subtract(this Colorish color, Color32 colorToAdd)
    {
        return new(
            color.r - colorToAdd.r,
            color.g - colorToAdd.g,
            color.b - colorToAdd.b,
            color.a - colorToAdd.a);
    }

    public static Colorish Divide(this Colorish color, float divisor)
    {
        return new(
            color.r / divisor,
            color.g / divisor,
            color.b / divisor,
            color.a / divisor);
    }

    public static Color32 ToColor32(this Colorish color)
    {
        return new((byte)color.r, (byte)color.g, (byte)color.b, (byte)color.a);
    }
}