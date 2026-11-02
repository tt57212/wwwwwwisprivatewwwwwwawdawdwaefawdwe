using UnityEngine;
using UnityEngine.UIElements;

public class rotate_nuggit : MonoBehaviour
{
    public float rotationSpeed = 1f;   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed);
    }
}
