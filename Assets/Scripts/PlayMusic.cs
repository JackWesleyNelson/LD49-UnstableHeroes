using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMusic : MonoBehaviour
{
    [SerializeField]
    private AudioClip intro = null;
    [SerializeField]
    private AudioClip loop = null;
    [SerializeField]
    private AudioSource audioSource = null;

    // Start is called before the first frame update
    void Start()
    {
        if(audioSource != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(intro);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(audioSource.isPlaying == false)
        {
            audioSource.clip = loop;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}
