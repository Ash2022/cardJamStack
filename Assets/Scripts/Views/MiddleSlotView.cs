using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MiddleSlotView : MonoBehaviour
{
    [SerializeField] TMP_Text unlockText;
    [SerializeField] GameObject canvas;
    [SerializeField]SpriteRenderer spriteRenderer;
    [SerializeField] Sprite lockedSprite;

    internal void Initialize(MiddleSlot middleSlot)
    {
        if (middleSlot.unlocksAtLevel-1 > GameManager.Instance.CurrLevelIndex)
        {
            canvas.SetActive(true);
            unlockText.text = "LEVEL " + middleSlot.unlocksAtLevel;
            spriteRenderer.sprite = lockedSprite;
        }
        else
            canvas.SetActive(false);
    }

}
