using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InGameUIView : MonoBehaviour
{
    [SerializeField] TMP_Text levelCaption;


    public void InitUI(LevelData levelData,int levelIndex)
    {
        levelCaption.text = "LEVEL " + (levelIndex + 1);
    }
}
