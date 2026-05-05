using UnityEngine;

namespace ResourcefulHands.Utility;

public static class AudioUtils
{
    /// <summary>
    /// Creates an AudioClip from samples
    /// </summary>
    /// <param name="samples">Array of samples to create the audio clip from</param>
    /// <param name="sampleRate">The sample rate of the AudioClip</param>
    /// <param name="channels">Number of channels for the audio clip (usually 1 or 2)</param>
    /// <param name="name">The name to give the audio clip</param>
    /// <returns>The AudioClip</returns>
    public static AudioClip CreateAudioClip(float[] samples, int sampleRate, int channels, string name = "GeneratedClip")
    {
        int lengthSamples = samples.Length / channels;

        AudioClip clip = AudioClip.Create(name, lengthSamples, channels, sampleRate, true);
        clip.SetData(samples, 0);

        return clip;
    }
}