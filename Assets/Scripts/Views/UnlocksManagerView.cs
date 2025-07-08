using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlocksManagerView : MonoBehaviour
{

    [SerializeField]List<GameObject> unlockScreens = new List<GameObject>();
    bool acceptButtonClicks = false;
    CanvasGroup currCanvasGroup;
    GameObject currUnlockObject;
    Action unlockScreenDone;

    public void ShowUnlockScreen(ModelManager.UnlockTypes unlockType,Action unlockScreenClosed)
    {
        unlockScreenDone = unlockScreenClosed;
        acceptButtonClicks = false;
        int unlockIndex = (int)unlockType;

        currUnlockObject = unlockScreens[unlockIndex];
        currUnlockObject.SetActive(true);

        currCanvasGroup = currUnlockObject.GetComponent<CanvasGroup>();
        currCanvasGroup.alpha = 0;

        currCanvasGroup.DOFade(1, 0.5f).OnComplete(()=>
        {
            //fade completed
            //make button work
            acceptButtonClicks = true;
        });

    }
    public void CloseUnlockClicked()
    {
        if(!acceptButtonClicks)
            return;

        acceptButtonClicks=false;

        CloseUnlockScreen();
    }

    private void CloseUnlockScreen()
    {
        currCanvasGroup.DOFade(0, 0.25f).OnComplete(() =>
        {
            //fade completed
            //make button work
            currUnlockObject.SetActive(false);

            unlockScreenDone?.Invoke();
        });
    }
}
