using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayPiano : MonoBehaviour
{
    Transform player;
    AudioSource audioSource;
    private float currentPosition = 0f;
    private Coroutine playRoutine;
    private float interactableDistance = 300;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        player = GameObject.Find("Player").GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactableDistance && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (playRoutine != null) StopCoroutine(playRoutine);
                playRoutine = StartCoroutine(PlayOneSecond());
        }
    }

    IEnumerator PlayOneSecond()
    {
        audioSource.time = currentPosition;
        audioSource.Play();

        yield return new WaitForSeconds(0.5f);

        audioSource.Stop();
        currentPosition += 1;

        if (currentPosition >= audioSource.clip.length)
            currentPosition = 0f;
    }
}
