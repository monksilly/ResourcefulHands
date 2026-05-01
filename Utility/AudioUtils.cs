using UnityEngine;

namespace ResourcefulHands.Utility;

public static class AudioUtils
{
    public static AudioClip CreateAudioClip(float[] samples, int sampleRate, int channels, string name = "GeneratedClip")
    {
        int lengthSamples = samples.Length / channels;

        AudioClip clip = AudioClip.Create(name, lengthSamples, channels, sampleRate, true);
        clip.SetData(samples, 0);

        return clip;
    }
}