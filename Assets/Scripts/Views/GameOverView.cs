using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView : MonoBehaviour
{
    [SerializeField] Button continueButton;
    [SerializeField] TMP_Text mainTitleText;
    [SerializeField] TMP_Text levelCompleteText;
    [SerializeField] TMP_Text tapToContinueText;
    [SerializeField] GameObject endGameParticles;
    [SerializeField] GameObject imageGroup;
    Action<bool> gameOverClosed;
    bool levelWon;

    public void ShowGameOver(bool win,int levelIndex, System.Action<bool> gameOverNextClicked)
    {
        if(win)
            endGameParticles.SetActive(true);

        levelWon = win;

        gameOverClosed = gameOverNextClicked;

        levelCompleteText.text = win ? "LEVEL "+(levelIndex+1) + " COMPLETE" : "NO MORE MOVES";

        levelCompleteText.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0, win ? 200 : -80, 0);

        mainTitleText.gameObject.SetActive(win);
        imageGroup.SetActive(win);

        continueButton.interactable = true;
            
        gameObject.SetActive(true);
    }

    public void ContinueClicked()
    {
        continueButton.interactable = false;

        gameOverClosed?.Invoke(levelWon);

        gameObject.SetActive(false);
        endGameParticles.SetActive(false);
    }
}
