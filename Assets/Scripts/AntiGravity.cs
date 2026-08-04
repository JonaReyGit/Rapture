using UnityEngine;
using UnityEngine.Rendering;

public class AntiGravity : MonoBehaviour
{
    public float force;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other)
    {
        Rigidbody RigidBod = other.GetComponent<Rigidbody>();
        if (RigidBod != null)
        {
            RigidBod.AddForce(Vector3.up * force * Time.deltaTime, ForceMode.Force);
            Debug.Log("JUMP");
        }
    }
}
