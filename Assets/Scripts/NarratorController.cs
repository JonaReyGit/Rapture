using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NarratorController : MonoBehaviour
{
    public static NarratorController Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI narratorText;
    public GameObject narratorPanel;

    [Header("Timing")]
    public float typeSpeed = 0.03f;
    public float pauseBetweenLines = 1.2f;
    public float holdAfterLastLine = 1.5f;

    private Coroutine playRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (narratorPanel != null)
            narratorPanel.SetActive(false);
    }

    public void Play(List<string> lines)
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayLines(lines));
    }

    public void Play(string[] lines)
    {
        Play(new List<string>(lines));
    }

    private IEnumerator PlayLines(List<string> lines)
    {
        if (narratorPanel != null)
            narratorPanel.SetActive(true);

        foreach (string line in lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(pauseBetweenLines);
        }

        yield return new WaitForSeconds(holdAfterLastLine);

        if (narratorPanel != null)
            narratorPanel.SetActive(false);
        else if (narratorText != null)
            narratorText.text = "";

        playRoutine = null;
    }

    private IEnumerator TypeLine(string line)
    {
        if (narratorText == null) yield break;

        narratorText.text = "";
        foreach (char c in line)
        {
            narratorText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public bool IsPlaying => playRoutine != null;
}