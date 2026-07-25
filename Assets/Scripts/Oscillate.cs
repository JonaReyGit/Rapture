using UnityEngine;

public class Oscillate : MonoBehaviour
{
    public int oscilationSpeed = 1;
    public int oscillationRange;
    public float delay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        oscillationRange = UnityEngine.Random.Range(3, 6);
        delay = UnityEngine.Random.Range(3, 5);
        InvokeRepeating("Oscillation", 3, delay);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up * oscilationSpeed * Time.deltaTime);
    }

    void Oscillation()
    {
        for (int i = 0; i < oscillationRange; i++)
        {
            transform.Translate(Vector3.up * oscilationSpeed * Time.deltaTime);
        }
        oscilationSpeed = -1 * oscilationSpeed;
    }
}
