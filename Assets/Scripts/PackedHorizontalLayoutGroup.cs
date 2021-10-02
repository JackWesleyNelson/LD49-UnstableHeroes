using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO: Handle if elements are wider than the layout group
public class PackedHorizontalLayoutGroup : HorizontalLayoutGroup
{
    new void Update()
    {
        RectTransform layoutRect = GetComponent<RectTransform>();
        if (layoutRect != null)
        {
            float widthOfChildren = 0;
            foreach (Transform child in transform)
            {
                RectTransform r = child.GetComponent<RectTransform>();
                if (r != null)
                {
                    widthOfChildren += r.rect.width;
                }
            }
            //spacing = -layoutRect.rect.width + widthOfChildren;
        }
    }
}
