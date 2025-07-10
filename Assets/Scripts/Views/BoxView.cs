using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BoxView : MonoBehaviour
{
    const float FLY_TO_MIDDLE_TIME = 0.65f;
    const float DISAPPEAR_TIME = 0.5f;

    const float CARD_SHOW_DELAY = 0.1f;

    [Header("Visual References")]
    [SerializeField] private GameObject questionMarkGO;
    [SerializeField] private Transform cardSlotsParent;
    [SerializeField] private Collider Collider;

    [SerializeField] private Renderer boxRenderer;

    private BoxData _data;

    private int _cardsExpected;
    private int _cardsArrived;

    int resolvedSlotIndex = -1;
    bool boxArrivedToMiddleSlot;
    bool isLocked = false;
    

    public BoxData Data { get => _data; set => _data = value; }
    public bool BoxArrivedToMiddleSlot { get => boxArrivedToMiddleSlot; set => boxArrivedToMiddleSlot = value; }
    public bool IsLocked { get => isLocked; set => isLocked = value; }

    List<CardView> initialCardViews = new List<CardView>();



    /// <summary>
    /// Set up this box’s appearance and cards.
    /// </summary>
    /// <param name="data">The BoxData model.</param>
    /// <param name="level">The LevelData for unlock logic.</param>
    public void Initialize(BoxData data, GameObject cardPrefab)
    {
        _data = data;
        boxArrivedToMiddleSlot = false;
        // the number of cards we expect to arrive is the model’s assignedCards count
        _cardsExpected = 3;
        _cardsArrived = 0;

        

        var level = GameManager.Instance.CurrentLevelData;
        GameManager.Instance.RegisterBoxView(this);

        // 1) Determine unlock state
        bool unlocked = GameLogic.IsBoxUnlocked(level, data.boxID);

        isLocked = !unlocked;

        // 2) Hidden vs. visible coloring
        if (data.hidden)
        {
            boxRenderer.material = LevelVisualizer.Instance.GetHiddenMaterial();

            questionMarkGO.SetActive(true);
        }
        else
        {
            boxRenderer.material= LevelVisualizer.Instance.GetBoxMaterialByColorIndex(data.colorIndex);
            
            questionMarkGO.SetActive(false);
        }

        // Always instantiate all three cards, but only show them if unlocked & not hidden
        bool showCards = unlocked && !data.hidden;
        for (int i = 0; i < data.initialCards.Count; i++)
        {
            Transform slotT = cardSlotsParent.GetChild(i);
            var cData = data.initialCards[i];

            GameObject cardGO = Instantiate(cardPrefab, slotT);
            var cv = cardGO.GetComponent<CardView>();
            initialCardViews.Add(cv);
            cv.Initialize(cData, showCards);
        }

        //locked boxes are 50% on Z
        if(unlocked == false)
            transform.localScale = new Vector3(1, 1, 0.5f);
        else
        {
            //Debug.Log("Box View init scale up");

            //normal unlocked box - make it have an appear animation
            transform.localScale = new Vector3(0, 0, 0);
            transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutQuad);

        }
        
    }

    /// <summary>
    /// Call this to begin the box’s flight into its middle‐slot parent.
    /// </summary>
    /// <param name="middleSlotTransform">Transform of the middle‐slot this box should land in.</param>
    public void StartFlyToMiddle(Transform middleSlotTransform, int midIndex)
    {
        SoundsManager.Instance.PlayHaptics(SoundsManager.TapticsStrenght.Medium);
        SoundsManager.Instance.PlayBoxFlys();


        resolvedSlotIndex = midIndex;

        Collider.enabled = false;

        // Reparent so localPosition = zero lands at slot center
        transform.SetParent(middleSlotTransform);

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = Vector3.zero;
        float middleZ = -3.5f;
        float totalTime = FLY_TO_MIDDLE_TIME;

        float midX = startPos.x + (endPos.x - startPos.x) * (4f / 5f);
        float midY = (startPos.y + endPos.y) * 0.5f; // still center vertically
        Vector3 midPos = new Vector3(midX, midY, middleZ);

        Vector3 dir = endPos - startPos;
        float angleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Sequence
        var seq = DOTween.Sequence();

        // First half — move to midpoint & rotate
        seq.Append(transform.DOLocalMove(midPos, totalTime * 0.5f).SetEase(Ease.OutSine));
        seq.Join(transform.DOLocalRotate(new Vector3(120f, 0f, angleZ), totalTime * 0.5f, RotateMode.FastBeyond360).SetEase(Ease.Linear));


        // 🔔 Midpoint reached
        seq.AppendCallback(() =>
        {
            //Debug.Log("Reached midpoint!");
            // Do whatever you want here
            GameManager.Instance.SendBoxCardsToTop(_data);
        });

        // Second half — move to end & finish rotation
        seq.Append(transform.DOLocalMove(endPos, totalTime * 0.5f).SetEase(Ease.InSine));
        seq.Join(transform.DOLocalRotate(new Vector3(360f, 0f, 0f), totalTime * 0.5f, RotateMode.Fast).SetEase(Ease.Linear));

        // 🔔 Final position reached
        seq.OnComplete(() =>
        {
            //Debug.Log("Reached final position!");
            // Final callback logic

            // After box arrives, trigger each card’s flight to its top slot
            boxArrivedToMiddleSlot = true;

            //GameManager.Instance.SendBoxCardsToTop(_data);
            GameManager.Instance.OnBoxArrived(this);

        });
       
    }

    /// <summary>
    /// Returns the Transform of the slot (0,1,2) that holds the given card.
    /// </summary>
    public Transform GetCardSlotTransform(CardData cardData)
    {
        int childCount = cardSlotsParent.childCount;
        int startIdx = 0;// Mathf.Clamp(cardData.assignedMiddleSlotIndex, 0, childCount - 1);

        // Look for the first empty slot, starting at the assigned index
        for (int offset = 0; offset < childCount; offset++)
        {
            int idx = (startIdx + offset) % childCount;
            Transform slot = cardSlotsParent.GetChild(idx);
            if (slot.childCount == 0)
                return slot;
        }

        // All are occupied (shouldn’t happen)—fall back to the original slot
        return cardSlotsParent.GetChild(startIdx);
    }

    internal void BoxViewUnblocked()
    {
        if(_data.hidden)
        {
            SoundsManager.Instance.HiddenBoxUnlocked();
            boxRenderer.material = LevelVisualizer.Instance.GetBoxMaterialByColorIndex(_data.colorIndex);

            questionMarkGO.SetActive(false);
        }


        //Debug.Log("Box View UnBlocked");

        //if the box was hidden - show its color
        transform.DOScale(Vector3.one,0.15f).SetEase(Ease.OutElastic);
        //show the cards

        for (int i = 0; i < initialCardViews.Count; i++)
            initialCardViews[i].BoxWasUnlocked(CARD_SHOW_DELAY*i);
        
    }

    /// <summary>
    /// Called by CardView when its flight into this box completes.
    /// </summary>
    public void NotifyCardArrived(CardView cardView)
    {
        _cardsArrived++;

        //Debug.Log("Cards Arrived " + _cardsArrived);

        if (_cardsArrived == _cardsExpected)
            StartDisappear();
    }

    private void StartDisappear()
    {
        SoundsManager.Instance.PlayHaptics(SoundsManager.TapticsStrenght.Medium);
        SoundsManager.Instance.PlayBoxResolved();

        var seq = DOTween.Sequence();

        // 1. Scale down to 0
        seq.Join(transform.DOScale(new Vector3(1,0,0), DISAPPEAR_TIME).SetEase(Ease.InBack));

        // 2. Rotate 360° around Z
        seq.Join(transform.DOLocalRotate(new Vector3(0f, 0f, 360f),DISAPPEAR_TIME,RotateMode.FastBeyond360).SetEase(Ease.InOutSine));

        // 3. Small hop up on Z (relative by 1 unit)
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + new Vector3(0f, 0f, 1.75f);
        seq.Join(transform.DOLocalMove(endPos, -DISAPPEAR_TIME*1.5f).SetEase(Ease.InOutSine));

        seq.OnComplete(() =>
        {
            GameManager.Instance.OnBoxCompleted(this, resolvedSlotIndex);
        });
    }
}
