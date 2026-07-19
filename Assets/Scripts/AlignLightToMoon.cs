using UnityEngine;

[RequireComponent(typeof(Light))]
public class AlignLightToMoon : MonoBehaviour
{
    public Transform moon;
    public Transform sceneCenter; 

    [ContextMenu("Align To Moon")]
    void Align()
    {
        if (moon == null) return;
        Vector3 target = sceneCenter != null ? sceneCenter.position : Vector3.zero;
        Vector3 dir = (target - moon.position).normalized; 
        transform.rotation = Quaternion.LookRotation(dir);
    }
}