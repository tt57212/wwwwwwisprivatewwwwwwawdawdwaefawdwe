using UnityEngine;
using System.Collections.Generic;

public class Ligt_maniger : MonoBehaviour
{
    public List<ligt> alleLichter = new List<ligt>();

    private bool alleAn = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Diese Methode an den Button (OnClick) hängen.
    // 1. Klick -> alle an, 2. Klick -> alle aus, usw.
    public void ButtonGedrueckt()
    {
        alleAn = !alleAn;

        if (alleAn)
            AlleAn();
        else
            AlleAus();
    }

    public void AlleAn()
    {
        foreach (ligt licht in alleLichter)
        {
            licht.istAn = true;
        }
    }

    public void AlleAus()
    {
        foreach (ligt licht in alleLichter)
        {
            licht.istAn = false;
        }
    }
}