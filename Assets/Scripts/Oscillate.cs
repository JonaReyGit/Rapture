using UnityEngine;

public class Oscillate : MonoBehaviour
{
    public int oscilationSpeed;
    public int oscillationRange;
    public float delay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        oscilationSpeed = UnityEngine.Random.Range(-1, 1);
        oscillationRange = UnityEngine.Random.Range(3, 6);
        delay = UnityEngine.Random.Range(2, 4);
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
