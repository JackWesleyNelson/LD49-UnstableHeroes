using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowLabelOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    GameObject labelToShow = null;
    bool show = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        show = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        show = false;
    }

    private void Update()
    {
        labelToShow.SetActive(show);
    }
}
