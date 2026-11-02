using UnityEngine;

public class ligt : MonoBehaviour
{
    public Light meineLampe;
    public bool istAn = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (meineLampe == null)
            meineLampe = GetComponent<Light>();

        meineLampe.enabled = istAn;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            istAn = !istAn;
        }

        meineLampe.enabled = istAn;
    }
}