using DG.Tweening;
using System.Linq;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private float flyDuration = 0.75f;

    [Header("Visual References")]
    [SerializeField] private Renderer cardRenderer;

    private CardData _data;
    private LevelVisualizer _visualizer;
    private LevelData _level;
    bool flyingToTop = false;
    bool reachedTop = false;

    public CardData Data { get => _data; set => _data = value; }

    internal void Initialize(CardData cardData, bool showCards)
    {
        _data = cardData;
        _visualizer = GameManager.Instance.visualizer;
        _level = GameManager.Instance.CurrentLevelData;
        GameManager.Instance.RegisterCardView(this);

        // Set face color based on the card’s colorIndex
        cardRenderer.material.color = Helper.GetColor(cardData.colorIndex);

        // Show or hide the face/back overlay
        gameObject.SetActive(showCards);
    }

    public void BoxWasUnlocked()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Fly this card into its assigned top‐slot.
    /// </summary>
    public Tween FlyToTopSlot()
    {
        flyingToTop = true;

        int topIdx = _data.assignedTopSlot;
        int cols   = _level.numberOfTopSlotsPerRow;
        int row    = topIdx / cols;
        int col    = topIdx % cols;

        Transform topRoot    = _visualizer.TopHolder.GetChild(0);
        Transform rowTf      = topRoot.GetChild(row);
        Transform targetSlot = rowTf.GetChild(col);

        //Debug.Log("FlyToTopSlot");

        transform.SetParent(targetSlot);

        transform.DOLocalRotate(new Vector3(90, -90, 0), flyDuration);

        Tween tween= transform.DOLocalMove(Vector3.zero, flyDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                reachedTop = true;
                flyingToTop = false;
                // after landing in top slot, decide if we need to continue
                if (_data.resolvedBox != null)
                {
                    //Debug.Log("FlyToMidBox ---- CardReachedTop");

                    FlyToMiddleBox();
                }
            });

        return tween;
    }

    public void TargetBoxReachedItsMiddleSlot(BoxView boxView)
    {
        if(flyingToTop)
        {
            //do nothing - because its already assigned and it will resume going to the box when it reaches the top
        }
        else
        {
            //its already waiting there - so we need to make it move to box

            //Debug.Log("FlyToMidBox ---- TargetBoxReached");

            if(reachedTop)
                FlyToMiddleBox();
        }
    }

    /// <summary>
    /// Fly this card into its resolved middle‐box slot.
    /// </summary>
    public void FlyToMiddleBox()
    {
        if (flyingToTop)
            return;

        // find the box's BoxView via the resolvedBoxID
        var level = GameManager.Instance.CurrentLevelData;
        int midIdx = level.middleSlotBoxes.FindIndex(b => b != null && b.boxID == _data.resolvedBox.Data.boxID);
        if (midIdx < 0) return;

        // get the slot_transform inside that BoxView
        var viz = GameManager.Instance.visualizer;
        Transform middleRoot = viz.MiddleHolder.GetChild(0);
        Transform cellTf = middleRoot.GetChild(midIdx);
        var boxView = cellTf.GetComponentInChildren<BoxView>();
        if (boxView == null) return;

        // ask the BoxView for the exact card‐slot transform
        Transform slotTf = boxView.GetCardSlotTransform(_data);

        

        // reparent & animate
        transform.SetParent(slotTf);
        transform.DOLocalRotate(Vector3.zero, flyDuration);
        transform.DOLocalMove(Vector3.zero, flyDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            // let the box know this card has arrived
            Data.resolvedBox.NotifyCardArrived(this);
        });
    }

    /// <summary>
    /// Tell this card which BoxView it belongs to and which slot index within that box.
    /// </summary>
    public void AssignBox(BoxView boxView, int slotIndex)
    {
        Data.resolvedBox = boxView;
        Data.assignedMiddleSlotIndex = slotIndex;
    }
}
