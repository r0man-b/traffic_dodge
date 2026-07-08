using UnityEngine;

// Metadata for a single song. Lives on the same GameObject as the song's AudioSource.
[RequireComponent(typeof(AudioSource))]
public class Song : MonoBehaviour
{
    public string artist;
    public string songName;

    private AudioSource _source;

    // The AudioSource that plays this song (cached from the same GameObject).
    public AudioSource source
    {
        get
        {
            if (_source == null) _source = GetComponent<AudioSource>();
            return _source;
        }
    }
}
