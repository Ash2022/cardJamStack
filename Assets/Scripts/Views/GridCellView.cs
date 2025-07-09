using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GridCellView : MonoBehaviour
{
    [Header("Prefabs & References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] SpriteRenderer cellSprite;

    [SerializeField]List<GameObject> outlines = new List<GameObject>();

    [SerializeField] List<GameObject> corners = new List<GameObject>();

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
                cellSprite.color = Color.white;
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, LevelVisualizer.EMPTY_ELEVATION);
                break;

            case SlotType.Box:
                if (slot.box != null && abovePipe==false)
                {
                    var boxGO = Instantiate(boxPrefab, contentParent);
                    var bv = boxGO.GetComponent<BoxView>();
                    bv.Initialize(slot.box, cardPrefab);
                    cellSprite.sprite = LevelVisualizer.Instance.CellFullSprite;
                }
                break;

            case SlotType.Pipe:
                if (slot.pipe != null)
                {
                    var pipeGO = Instantiate(pipePrefab, contentParent);
                    var pv = pipeGO.GetComponent<PipeView>();
                    pv.Initialize(slot.pipe);
                    cellSprite.sprite = LevelVisualizer.Instance.CellFullSprite;
                }
                break;
        }
    }

    public void SetOutlineVisibility(List<bool> visibility)
    {
        if (outlines == null || outlines.Count != 4)
        {
            Debug.LogWarning("GridCellView: Outline list is missing or incorrect size.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (outlines[i] != null)
                outlines[i].SetActive(visibility[i]);

            //corners order 
            //top left
            //top right
            //bottom left
            //bottom right

            //top and left are set - enable that corner
            if (visibility[0] && visibility[2])
                corners[0].SetActive(true);

            //top and right are set - enable that corner
            if (visibility[0] && visibility[3])
                corners[1].SetActive(true);

            //bottom left - 
            if (visibility[1] && visibility[2])
                corners[2].SetActive(true);

            //bottom right - 
            if (visibility[1] && visibility[3])
                corners[3].SetActive(true);

        }
    }
}
