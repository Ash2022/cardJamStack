﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class LevelVisualizer : MonoBehaviour
{
    public const float EMPTY_ELEVATION = -0.1f;
    const float MIDDLE_SLOT_SPACING = 0.075f;
    //const float TOP_SLOT_SPACING = 0.05f;

    public static LevelVisualizer Instance;

    const float CELL_SIZE = 0.7f;
    public const float SCALE_MULTI = 1.15f;

    const float TOP_TOTAL_WIDTH = 5.5f;
    const float TOP_CELL_WIDTH = 0.464f;

    [Header("Global Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 7;

    [Header("Top Area")]
    [SerializeField] public Transform TopHolder;
    [SerializeField] private GameObject TopHolderPrefab;
    [SerializeField] private GameObject TopSlotPrefab;

    [Header("Middle Area")]
    [SerializeField] public Transform MiddleHolder;
    [SerializeField] private GameObject MiddleHolderPrefab;
    [SerializeField] private GameObject MiddleSlotPrefab;

    [Header("Grid Area")]
    [SerializeField] private Transform GridHolder;
    [SerializeField] private GameObject GridHolderPrefab;
    [SerializeField] private GameObject GridSlotPrefab;
    [SerializeField] public GameObject BoxPrefab;
    [SerializeField] public GameObject PipePrefab;

    [Header("Card")]
    [SerializeField] public GameObject CardPrefab;

    private Dictionary<Vector2Int, GridCellView> _cellViews;

    [Header("Effects")]
    [SerializeField] private GameObject disappearEffect;  // ← add this

    [SerializeField] Sprite cellFullSprite;

    [Header("Materials")]
    [SerializeField] List<Material> cardMaterials;
    [SerializeField] List<Material> boxMaterials;



    public Sprite CellFullSprite { get => cellFullSprite; set => cellFullSprite = value; }
    public GameObject DisappearEffect { get => disappearEffect; set => disappearEffect = value; }

    void Awake()
    {
        Instance = this;
    }


    /// <summary>
    /// Clears existing children and rebuilds the entire level visualization.
    /// </summary>
    public void VisualizeLevel(LevelData level)
    {
        // initialize the lookup
        _cellViews = new Dictionary<Vector2Int, GridCellView>();

        //apply the scale multi to cell size

        float adjustedCellSize = CELL_SIZE * SCALE_MULTI;

        // 0) Ensure runtime lists are initialized to the right length

        //in middle slots - some slots might appear visually but are still locked - so they cannot go to the model

        int numLockedMidSlots = level.middleSlots.FindAll(x => x.unlocksAtLevel > GameManager.Instance.CurrLevelIndex+1).Count;

        int midCount = level.middleSlots.Count- numLockedMidSlots;
        if (level.middleSlotBoxes == null || level.middleSlotBoxes.Count != midCount)
            level.middleSlotBoxes = Enumerable.Repeat<BoxData>(null, midCount).ToList();

        int topCount = level.numberOfRows * level.numberOfTopSlotsPerRow;
        if (level.topSlotsCards == null || level.topSlotsCards.Count != topCount)
            level.topSlotsCards = Enumerable.Repeat<CardData>(null, topCount).ToList();


        // ─── SHOW THE PIPE’S FIRST BOX ABOVE EACH PIPE ─────────────────────────
        foreach (var pipeSlot in level.gridSlots.Where(s => s.type == SlotType.Pipe && s.pipe != null))
        {
            // if the pipe still has at least one BoxData in its queue…
            if (pipeSlot.pipe.boxes != null && pipeSlot.pipe.boxes.Count > 0)
            {
                // find the model slot above the pipe
                int x = pipeSlot.x, y = pipeSlot.y - 1;
                var above = level.gridSlots.FirstOrDefault(s => s.x == x && s.y == y);
                if (above != null)
                {
                    // mark it as a Box cell and give it that very first BoxData
                    above.type = SlotType.Box;
                    above.box = pipeSlot.pipe.boxes[0];

                }
            }
        }

        // 1) Clear existing
        ClearChildren(TopHolder);
        ClearChildren(MiddleHolder);
        ClearChildren(GridHolder);

        // 2) Instantiate holders
        var topRoot = Instantiate(TopHolderPrefab, TopHolder);
        var middleRoot = Instantiate(MiddleHolderPrefab, MiddleHolder);
        var gridRoot = Instantiate(GridHolderPrefab, GridHolder);

        //center grid based on the top row data

        var boxSlots = level.gridSlots.Where(s => s.box.boxID != "").ToList();
        int delta = 0;
        float centerX = 0;
        int minX = 0;

        if (boxSlots.Count > 0)
        {
            minX = boxSlots.Min(s => s.x);
            int maxX = boxSlots.Max(s => s.x);
            delta = maxX - minX;
            centerX = (minX + maxX) / 2f;
            //Debug.Log($"Boxes at y=0: minX = {minX}, maxX = {maxX}, delta = {delta}");
        }

        

        // 3 Top area positioning under Row1 and Row2
        Transform row1 = topRoot.transform.Find("Row1");
        Transform row2 = topRoot.transform.Find("Row2");

        int cols = level.numberOfTopSlotsPerRow;

        float space = (TOP_TOTAL_WIDTH - TOP_CELL_WIDTH * cols) / (cols + 1);
        
        float totalUsedWidth = TOP_CELL_WIDTH * cols + space * (cols + 1);
        Vector3 topOriginOffset = new Vector3(-totalUsedWidth * 0.5f + TOP_CELL_WIDTH * 0.5f, 0f, 0f);


        // Instantiate first row slots
        for (int col = 0; col < cols; col++)
        {
            GameObject slotGO = Instantiate(TopSlotPrefab, row1);
            //slotGO.transform.localPosition = row1Offset + new Vector3(col * row1SlotSize.x, 0f, 0f);
            slotGO.transform.localPosition = topOriginOffset+new Vector3(col * (TOP_CELL_WIDTH + space)+space, 0.25f, 0f);
        }

        // Instantiate second row slots
        for (int col = 0; col < cols; col++)
        {
            GameObject slotGO = Instantiate(TopSlotPrefab, row2);
            //slotGO.transform.localPosition = row2Offset + new Vector3(col * row2SlotSize.x, 0f, 0f);

            slotGO.transform.localPosition = topOriginOffset+new Vector3(col * (TOP_CELL_WIDTH   + space)+space, 0.25f, 0f);
        }

        // Middle area
        // compute per-slot size from middle frame sprite (or fallback)
        Vector2 middleSlotSize = new Vector2(adjustedCellSize, adjustedCellSize);
        Vector3 middleOriginOffset = Vector3.zero;

        middleSlotSize = new Vector2(adjustedCellSize, adjustedCellSize);
        float totalWidth = (adjustedCellSize + MIDDLE_SLOT_SPACING) * (level.middleSlots.Count - 1);
        middleOriginOffset = new Vector3(-totalWidth * 0.5f, 0f, 0f);

        for (int i = 0; i < level.middleSlots.Count; i++)
        {
            //check if this is an active middle slot or a locked one
            //locked ones go to different holder so they are not mixed with active ones.

            bool isSlotLocked = level.middleSlots[i].unlocksAtLevel > GameManager.Instance.CurrLevelIndex + 1;

            GameObject slotGO = Instantiate(MiddleSlotPrefab, isSlotLocked?middleRoot.transform.GetChild(0): middleRoot.transform.GetChild(1));


            slotGO.transform.localPosition = middleOriginOffset
                + new Vector3(i * (adjustedCellSize + MIDDLE_SLOT_SPACING), 0.3f, 0f);

            // initialize view if needed, e.g.:
            slotGO.GetComponent<MiddleSlotView>().Initialize(level.middleSlots[i],SCALE_MULTI);

        }

        // 5) Grid area
        // compute per-cell size from grid frame sprite (or fallback)
        Vector2 gridCellSize = new Vector2(adjustedCellSize, adjustedCellSize);
        Vector3 originOffset = Vector3.zero;
        
        gridCellSize = new Vector2(adjustedCellSize, adjustedCellSize);
        originOffset = new Vector3(-(gridWidth - 1) * gridCellSize.x * 0.5f,(gridHeight - 1) * gridCellSize.y * 0.5f,0f);


        Dictionary<Vector2Int, GridSlot> slotLookup = level.gridSlots.ToDictionary(s => new Vector2Int(s.x, s.y));


        foreach (var slot in level.gridSlots)
        {
            GameObject slotGO = Instantiate(GridSlotPrefab, gridRoot.transform);
            // position so (0,0) is at top-left of frame, y increases downward
            slotGO.transform.localPosition = originOffset + new Vector3(slot.x * gridCellSize.x, -slot.y * gridCellSize.y, 0f);

            slotGO.name = "Cell: " + slot.x+"_"+slot.y;
            // (rest of your slot initialization here)
            var cellView = slotGO.GetComponent<GridCellView>();
            cellView.Initialize(slot, BoxPrefab, PipePrefab,CardPrefab,SCALE_MULTI);
            _cellViews[new Vector2Int(slot.x, slot.y)] = cellView;

            var outline = ComputeOutline(new Vector2Int(slot.x, slot.y), slotLookup, gridWidth, gridHeight);
            cellView.SetOutlineVisibility(outline);

            
        }

        centerX = originOffset.x + centerX * adjustedCellSize;

        GridHolder.transform.localPosition = new Vector3(-centerX, GridHolder.transform.localPosition.y, GridHolder.transform.localPosition.z);


        // ─── Add dummy visual border cells ──────────────────────────────────────────
        gridCellSize = new Vector2(adjustedCellSize, adjustedCellSize); // ensure fixed size
        int realGridWidth = 8;
        int realGridHeight = 7;

        var usedPositions = new HashSet<Vector2Int>(level.gridSlots.Select(s => new Vector2Int(s.x, s.y)));

        // Calculate originOffset based on expanded dimensions
        int expandedWidth = realGridWidth + 4;  // 2 left + 8 real + 2 right = 12
        int expandedHeight = realGridHeight + 3; // 7 real + 3 bottom = 10
        originOffset = new Vector3(-(expandedWidth - 1) * gridCellSize.x * 0.5f,
                                   (expandedHeight - 1) * gridCellSize.y * 0.5f,
                                   0f);

        // Range of x: -2 to 9 (inclusive)
        // Range of y: 0 to 9 (inclusive)
        for (int y = 0; y <= 9; y++) // 7 real + 3 dummy bottom
        {
            for (int x = -2; x <= 9; x++) // 2 left, 8 real, 2 right
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Skip real cells
                if (x >= 0 && x < gridWidth && y < gridHeight)
                    continue;

                GameObject dummyGO = Instantiate(GridSlotPrefab, gridRoot.transform);
                dummyGO.transform.localPosition = originOffset +
                    new Vector3((x + 2) * gridCellSize.x, -(y) * gridCellSize.y-1.05f*SCALE_MULTI, EMPTY_ELEVATION);

                dummyGO.name = "Dummy_" + pos;

                var dummyView = dummyGO.GetComponent<GridCellView>();
                
                // Add dummy slot to lookup before computing outline
                if (!slotLookup.ContainsKey(pos))
                {
                    slotLookup[pos] = new GridSlot
                    {
                        x = x,
                        y = y,
                        box = null,
                        pipe = null,
                        type = SlotType.Empty
                    };
                }

                dummyView.DummyCellInit(SCALE_MULTI);
                var outline = ComputeOutlineDummy(pos, slotLookup, gridWidth, gridHeight);
                dummyView.SetOutlineVisibility(outline);
            }
        }

    }


    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            DestroyImmediate(parent.GetChild(i).gameObject);
    }

    /// <summary>
    /// Returns the GridCellView for the given coordinates.
    /// </summary>
    public GridCellView GetGridCellView(int x, int y)
    {
        _cellViews.TryGetValue(new Vector2Int(x, y), out var view);
        return view;
    }


    private List<bool> ComputeOutline(Vector2Int pos, Dictionary<Vector2Int, GridSlot> slotLookup, int width, int height)
    {
        if(slotLookup.TryGetValue(pos, out var slot))
        {
            if(slot.box.boxID!="" || slot.pipe.spawnCount>0)
            {
                List<bool> emptyOutline = new List<bool>();

                for (int i = 0; i < 4; i++)
                    emptyOutline.Add(false);

                return emptyOutline;
            }
                
            
        }

        bool IsEmpty(Vector2Int p)
        {
            if (!slotLookup.TryGetValue(p, out var slot))
                return true;

            if (slot.type == SlotType.Empty)
                return true;

            if (slot.type == SlotType.Box)
                return false;

            if (slot.box == null && slot.pipe == null)
                return true;

            return (slot.box.boxID == "" && slot.pipe.spawnCount == 0);
        }

        List<bool> outline = new List<bool>(4);

        // Top (Y-1) — show if Y == 0 or cell above is FULL
        bool topVisible = pos.y != 0 && IsEmpty(new Vector2Int(pos.x, pos.y - 1));
        outline.Add(!topVisible);

        // Bottom (Y+1) — show if cell below is FULL
        bool bottomVisible = IsEmpty(new Vector2Int(pos.x, pos.y + 1));
        outline.Add(!bottomVisible);

        // Left (X-1)
        bool leftVisible = IsEmpty(new Vector2Int(pos.x - 1, pos.y));
        outline.Add(!leftVisible);

        // Right (X+1)
        bool rightVisible = IsEmpty(new Vector2Int(pos.x + 1, pos.y));
        outline.Add(!rightVisible);

        return outline;
    }

    private List<bool> ComputeOutlineDummy(Vector2Int pos, Dictionary<Vector2Int, GridSlot> slotLookup, int width, int height)
    {
        bool IsEmpty(Vector2Int p)
        {
            if (!slotLookup.TryGetValue(p, out var slot))
                return true;

            if (slot.box == null && slot.pipe == null)
                return true;

            return(slot.box.boxID == "" && slot.pipe.spawnCount==0);
        }

        List<bool> outline = new List<bool>(4);

        // Top (Y-1) — show if Y == 0 or cell above is FULL
        bool topVisible = pos.y != 0 && IsEmpty(new Vector2Int(pos.x, pos.y - 1));
        outline.Add(!topVisible);

        // Bottom (Y+1) — show if cell below is FULL
        bool bottomVisible = IsEmpty(new Vector2Int(pos.x, pos.y + 1));
        outline.Add(!bottomVisible);

        // Left (X-1)
        bool leftVisible = IsEmpty(new Vector2Int(pos.x - 1, pos.y));
        outline.Add(!leftVisible);

        // Right (X+1)
        bool rightVisible = IsEmpty(new Vector2Int(pos.x + 1, pos.y));
        outline.Add(!rightVisible);

        return outline;
    }

    public Material GetCardMaterialByColorIndex(int colorIndex)
    {
        string colorName = GameManager.Instance.CurrentLevelData.colorNames[colorIndex];

        return colorName switch
        {
            "Red" => cardMaterials[0],
            "Green" => cardMaterials[1],
            "Blue" => cardMaterials[2],
            "Orange" => cardMaterials[3],
            "Yellow" => cardMaterials[4],
            "Pink" => cardMaterials[5],
            "Purple" => cardMaterials[6],
            "White" => cardMaterials[7],
            "LightBlue" => cardMaterials[8],
            "Turquoise" => cardMaterials[9],
            _ => cardMaterials[10],
        };
    }

    public Material GetBoxMaterialByColorIndex(int colorIndex)
    {
        string colorName = GameManager.Instance.CurrentLevelData.colorNames[colorIndex];

        return colorName switch
        {
            "Red" => boxMaterials[0],
            "Green" => boxMaterials[1],
            "Blue" => boxMaterials[2],
            "Orange" => boxMaterials[3],
            "Yellow" => boxMaterials[4],
            "Pink" => boxMaterials[5],
            "Purple" => boxMaterials[6],
            "White" => boxMaterials[7],
            "LightBlue" => boxMaterials[8],
            "Turquoise" => boxMaterials[9],
            _ => boxMaterials[10],
        };
    }

    public Material GetHiddenMaterial()
    {
        return boxMaterials[10];
    }

    public static Color GetEditorColor(string colorName)
    {
        return colorName switch
        {
            "Red" => Color.red,
            "Green" => Color.green,
            "Blue" => Color.blue,
            "Orange" => new Color(1f, 0.5f, 0f),
            "Yellow" => Color.yellow,
            "Pink" => new Color(1f, 0.4f, 0.7f),
            "Purple" => new Color(0.6f, 0.2f, 0.7f),
            "White" => Color.white,
            "LightBlue" => new Color(0.5f, 0.8f, 1f),
            "Turquoise" => new Color(0.3f, 1f, 0.9f),
            _ => Color.gray,
        };
    }


}
