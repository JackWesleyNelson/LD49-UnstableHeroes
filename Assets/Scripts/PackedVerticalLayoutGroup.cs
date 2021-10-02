using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO: Handle if the elements are taller than the layout group.
public class PackedVerticalLayoutGroup : VerticalLayoutGroup
{
    new void Update()
    {
        RectTransform layoutRect = GetComponent<RectTransform>();
        if (layoutRect != null)
        {
            float heightOfChildren = 0;
            foreach (Transform child in transform)
            {
                RectTransform r = child.GetComponent<RectTransform>();
                if (r != null)
                {
                    heightOfChildren += r.rect.height;
                }
            }
            spacing = -layoutRect.rect.height + heightOfChildren;
        }
    }
}
