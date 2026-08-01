using UnityEngine;
using System.Collections;

public class PlaySoundAtRandomInterval : MonoBehaviour
{
    public float minInterval = 15f;
    public float maxInterval = 35f;
    public AudioClip clip;
    private AudioSource audioSource;

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("No attached audio source assigned to " + gameObject.name);
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("No attached audio clip assigned to " + gameObject.name);
            return;
        }

        StartCoroutine(PlayAtRandomIntervals());

    }

    private IEnumerator PlayAtRandomIntervals()
    {
        while (true)
        {
            //Play audio clip
            audioSource.Play();

            //Calculate wait
            float interval = Random.Range(minInterval, maxInterval);

            yield return new WaitForSeconds(interval);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}