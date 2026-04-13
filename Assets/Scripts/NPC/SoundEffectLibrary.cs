using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SoundEffectElement
{
    public AudioClip clip;
}

[System.Serializable]
public class SoundEffectGroup
{
    public string name;
    public List<SoundEffectElement> audioClips = new List<SoundEffectElement>();
}

public class SoundEffectLibrary : MonoBehaviour
{
    [SerializeField]
    private List<SoundEffectGroup> soundEffectGroups = new List<SoundEffectGroup>();

    public AudioClip GetSoundEffect(string groupName, int elementIndex)
    {
        var group = soundEffectGroups.Find(g => g.name == groupName);
        if (group != null && elementIndex >= 0 && elementIndex < group.audioClips.Count)
        {
            return group.audioClips[elementIndex].clip;
        }
        return null;
    }

    public void PlaySoundEffect(AudioSource audioSource, string groupName, int elementIndex)
    {
        AudioClip clip = GetSoundEffect(groupName, elementIndex);
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
    