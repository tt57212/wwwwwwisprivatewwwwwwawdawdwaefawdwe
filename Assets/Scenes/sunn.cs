using UnityEngine;

public class sunn : MonoBehaviour
{
    public float rotationSpeed = 0.1f; // Speed of rotation in degrees per second
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationSpeed, 0, 0);
    }
}
