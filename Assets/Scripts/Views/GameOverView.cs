using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView : MonoBehaviour
{
    [SerializeField] Button continueButton;
    [SerializeField] TMP_Text gameOverCaption;
    Action<bool> gameOverClosed;
    bool levelWon;

    public void ShowGameOver(bool win, System.Action<bool> gameOverNextClicked)
    {
        levelWon = win;

        gameOverClosed = gameOverNextClicked;

        gameOverCaption.text = win ? "LEVEL COMPLETE" : "NO MORE MOVES";

        continueButton.interactable = true;
            
        gameObject.SetActive(true);
    }

    public void ContinueClicked()
    {
        continueButton.interactable = false;

        gameOverClosed?.Invoke(levelWon);

        gameObject.SetActive(false);
    }
}
