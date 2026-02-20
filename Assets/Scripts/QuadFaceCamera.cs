using Unity.Mathematics;
using UnityEngine;

public class QuadFaceCamera : MonoBehaviour
{
    [SerializeField] private Camera cam;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        
        transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }
}
