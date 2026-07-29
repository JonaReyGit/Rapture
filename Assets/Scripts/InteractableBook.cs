using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class InteractableButton : MonoBehaviour
{
    [Header("Narrator Lines")]
    public List<LineSet> pressSequences = new List<LineSet>
    {
        new LineSet { lines = new List<string>
        {
            "This is a school project made by a few of us.",
            "Thanks for checking it out."
        }},
        new LineSet { lines = new List<string>
        {
            "There wasn't a second page."
        }},
        new LineSet { lines = new List<string>
        {
            "Still here? The book hasn't changed since the last time you checked."
        }},
        new LineSet { lines = new List<string>
        {
            "Okay. This is just you clicking a book repeatedly now."
        }}
    };

    [Header("Prompt UI")]
    [Tooltip("e.g. 'Press E' text")]
    public GameObject promptUI;

    [Header("Interaction")]
    public string playerTag = "Player";
    public bool blockWhilePlaying = true;

    private bool playerInRange;
    private int pressCount;

    [System.Serializable]
    public class LineSet
    {
        [TextArea(2, 4)]
        public List<string> lines = new List<string>();
    }

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;
        if (blockWhilePlaying && NarratorController.Instance != null && NarratorController.Instance.IsPlaying) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Press();
        }
    }

    private void Press()
    {
        if (pressSequences.Count > 0)
        {
            int index = Mathf.Min(pressCount, pressSequences.Count - 1);
            List<string> linesToPlay = pressSequences[index].lines;

            if (NarratorController.Instance != null)
                NarratorController.Instance.Play(linesToPlay);
            else
                Debug.LogWarning("InteractableButton: no NarratorController found in scene.");
        }

        pressCount++;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;

        if (promptUI != null)
            promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;

        if (promptUI != null)
            promptUI.SetActive(false);
    }
}