using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MiddleSlotView : MonoBehaviour
{
    [SerializeField] TMP_Text unlockText;
    [SerializeField] GameObject canvas;

    internal void Initialize(MiddleSlot middleSlot)
    {
        if (middleSlot.unlocksAtLevel > 1)
        {
            canvas.SetActive(true);
            unlockText.text = "LEVEL " + middleSlot.unlocksAtLevel;
        }
        else
            canvas.SetActive(false);
    }

}
