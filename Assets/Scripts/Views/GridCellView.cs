using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridCellView : MonoBehaviour
{
    [Header("Prefabs & References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] SpriteRenderer cellSprite;

    /// <summary>
    /// Initialize this cell according to the slot data.
    /// </summary>
    public void Initialize(GridSlot slot, GameObject boxPrefab, GameObject pipePrefab, GameObject cardPrefab)
    {
        // Clear any existing content
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contentParent.GetChild(i).gameObject);
        }

        // Check for a pipe below that still has boxes, and show its first box here
        var level = GameManager.Instance.CurrentLevelData;
        bool abovePipe = level.gridSlots.Any(s => s.x == slot.x && s.y == slot.y + 1 && s.type == SlotType.Pipe && s.pipe != null && s.pipe.boxes.Count > 0);
        if (abovePipe)
        {
            var pipeSlot = level.gridSlots.First(
                s => s.x == slot.x
                  && s.y == slot.y + 1
                  && s.type == SlotType.Pipe);
            var pd = pipeSlot.pipe.boxes[0];
            var boxGO = Instantiate(boxPrefab, contentParent);
            var bv = boxGO.GetComponent<BoxView>();
            bv.Initialize(pd, cardPrefab);
            return;
        }

        // Otherwise, populate based on the slot's own type
        switch (slot.type)
        {
            case SlotType.Empty:
                // nothing to show
                cellSprite.color = Color.black;
                break;

            case SlotType.Box:
                if (slot.box != null)
                {
                    var boxGO = Instantiate(boxPrefab, contentParent);
                    var bv = boxGO.GetComponent<BoxView>();
                    bv.Initialize(slot.box, cardPrefab);
                }
                break;

            case SlotType.Pipe:
                if (slot.pipe != null)
                {
                    var pipeGO = Instantiate(pipePrefab, contentParent);
                    var pv = pipeGO.GetComponent<PipeView>();
                    pv.Initialize(slot.pipe);
                }
                break;
        }
    }

}
