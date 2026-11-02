using UnityEngine;

using UnityEngine;

public class loding : MonoBehaviour
{
    [Header("TMP-Objekt, das aktiviert werden soll")]
    public GameObject tmpObjekt;

    void Start()
    {
        if (tmpObjekt != null)
        {
            tmpObjekt.SetActive(false);
        }
    }

    void Update()
    {

    }

    public void AktiviereTMP()
    {
        if (tmpObjekt == null)
        {
            Debug.LogWarning("loding: Kein TMP-Objekt zugewiesen!");
            return;
        }

        tmpObjekt.SetActive(true);
    }
}