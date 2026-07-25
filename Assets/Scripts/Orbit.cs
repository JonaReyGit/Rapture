using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform target;     // The object to circle around
    public float speed = 50f;    // Speed of the circle rotation

    void Update()
    {
        if (target != null)
        {
            // Rotate around the target's position on the Y axis
            transform.RotateAround(target.position, Vector3.up, speed * Time.deltaTime);
        }
    }
}