using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AirSketchDrawingSurface : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public AirSketchARController Controller { get; set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            Controller?.BeginStroke(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Controller?.ContinueStroke(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Controller?.ContinueStroke(eventData.position);
    }

}
