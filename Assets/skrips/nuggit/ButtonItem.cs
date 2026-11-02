
using UnityEngine;

public class ButtonItem : MonoBehaviour
{
    public item1 item;

    public void GiveItem()
    {
        if (item != null)
        {
            item.GiveItem();
        }
    }
}

