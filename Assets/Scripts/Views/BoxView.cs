using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BoxView : MonoBehaviour
{
    const float FLY_TO_MIDDLE_TIME = 1f;

    [Header("Visual References")]
    [SerializeField] private SpriteRenderer boxRenderer;
    [SerializeField] private GameObject questionMarkGO;
    [SerializeField] private Transform cardSlotsParent;
    [SerializeField] private Collider Collider;

    private BoxData _data;

    private int _cardsExpected;
    private int _cardsArrived;

    int resolvedSlotIndex = -1;

    public BoxData Data { get => _data; set => _data = value; }
    List<CardView> initialCardViews = new List<CardView>();

    /// <summary>
    /// Set up this box’s appearance and cards.
    /// </summary>
    /// <param name="data">The BoxData model.</param>
    /// <param name="level">The LevelData for unlock logic.</param>
    public void Initialize(BoxData data, GameObject cardPrefab)
    {
        _data = data;

        // the number of cards we expect to arrive is the model’s assignedCards count
        _cardsExpected = 3;
        _cardsArrived = 0;

        var level = GameManager.Instance.CurrentLevelData;
        GameManager.Instance.RegisterBoxView(this);

        // 1) Determine unlock state
        bool unlocked = GameLogic.IsBoxUnlocked(level, data.boxID);

        // 2) Hidden vs. visible coloring
        if (data.hidden)
        {
            boxRenderer.color = Helper.GetHiddenColor();
            questionMarkGO.SetActive(true);
        }
        else
        {
            questionMarkGO.SetActive(false);
            boxRenderer.color = Helper.GetColor(data.colorIndex);
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
    }

    /// <summary>
    /// Call this to begin the box’s flight into its middle‐slot parent.
    /// </summary>
    /// <param name="middleSlotTransform">Transform of the middle‐slot this box should land in.</param>
    public void StartFlyToMiddle(Transform middleSlotTransform, int midIndex)
    {
        resolvedSlotIndex = midIndex;

        Collider.enabled = false;

        // Reparent so localPosition = zero lands at slot center
        transform.SetParent(middleSlotTransform);

        // Animate local position to (0,0,0)
        transform.DOLocalMove(Vector3.zero, FLY_TO_MIDDLE_TIME)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                
                // After box arrives, trigger each card’s flight to its top slot
                
                GameManager.Instance.SendBoxCardsToTop(_data);

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
        //if the box was hidden - show its color

        //show the cards

        for (int i = 0; i < initialCardViews.Count; i++)
            initialCardViews[i].BoxWasUnlocked();
        
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
        //Debug.Log(transform);

        // tween scale to zero
        transform.DOScale(Vector3.zero, 1)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                // notify GameManager to clean up model & view
                GameManager.Instance.OnBoxCompleted(this, resolvedSlotIndex);
            });
    }
}
