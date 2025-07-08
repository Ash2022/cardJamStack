using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class TutorialView : MonoBehaviour
{
    [SerializeField] RectTransform handImage;
    [SerializeField] TMP_Text tutorialText;

    [SerializeField]Canvas canvas;
    Sequence handSequence;
    Vector3 handPosition;
    Coroutine clickAnimationRoutine;


    public void ShowTutorialOnBoxID(Vector3 _handPosition)
    {
        //wait a second or so and than show the hand blinking

        gameObject.SetActive(true);

        handPosition = _handPosition;

        ResetRoutines();

        clickAnimationRoutine = StartCoroutine(StartClickAnimationRoutine());

    }

    private void ResetRoutines()
    {
        if (clickAnimationRoutine != null)
            StopCoroutine(clickAnimationRoutine);

        if (handSequence != null)
            handSequence.Kill();
    }

    internal void HideHand()
    {
        ResetRoutines();
        handImage.gameObject.SetActive(false);
    }

    IEnumerator StartClickAnimationRoutine()
    {
        yield return new WaitForSeconds(1);

        handImage.anchoredPosition = WorldToCanvasPosition(handPosition);
        handImage.gameObject.SetActive(true);

        float duration = 0.65f;
        float scaleDown = 0.8f;

        Vector3 originalScale = Vector3.one;
        Vector3 clickedScale = originalScale * scaleDown;

        handImage.localScale = originalScale;

        // Kill any existing tweens on this transform
        DOTween.Kill(handImage);

        handSequence = DOTween.Sequence();

        handSequence.Append(handImage.DOScale(clickedScale, duration).SetEase(Ease.OutQuad));
        handSequence.AppendInterval(0.3f);
        handSequence.Append(handImage.DOScale(originalScale, duration).SetEase(Ease.InQuad));

        handSequence.SetLoops(-1);
    }

    public Vector2 WorldToCanvasPosition(Vector3 worldPos)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.gameObject.GetComponent<RectTransform>(),
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out localPoint
        );

        return localPoint + new Vector2(20,0);
    }

    internal void CloseTutorial()
    {
        HideHand();
        gameObject.SetActive(false);
    }
}
