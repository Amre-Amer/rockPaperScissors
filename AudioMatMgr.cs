using UnityEngine;

public class AudioMatMgr : MonoBehaviour
{
    [HideInInspector]
    public AudioSource audioSource;
    public AudioClip clipPing;
    public AudioClip clipBoing;
    public AudioClip clipYeah;
    public AudioClip clipBoo;
    public Material matRed;
    public Material matGreen;
    public Material matBlue;
    public Material matWhite;
    public Material matOrig;
    [HideInInspector]
    public Material matInfer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayYeah()
    {
        audioSource.PlayOneShot(clipYeah);
    }

    public void PlayPing()
    {
        audioSource.PlayOneShot(clipPing);
    }

    public void PlayBoing()
    {
        audioSource.PlayOneShot(clipBoing);
    }

    public void PlayBoo()
    {
        audioSource.PlayOneShot(clipBoo);
    }
}
