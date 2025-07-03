using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelVisualizer : MonoBehaviour
{
    public static LevelVisualizer Instance;

    [Header("Global Settings")]
    [SerializeField] private float cellSpacing = 1f;
    [SerializeField] private int gridWidth = 8;
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

    [Header("Test Level")]
    [SerializeField] private TextAsset testLevelJson;  // ← add this


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

        // 0) Ensure runtime lists are initialized to the right length
        int midCount = level.middleSlots.Count;
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

                    // consume it so next Visualize / respawn won’t reuse it
                    //pipeSlot.pipe.boxes.RemoveAt(0);
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

        // 3 Top area positioning under Row1 and Row2
        Transform row1 = topRoot.transform.Find("Row1");
        Transform row2 = topRoot.transform.Find("Row2");

        int cols = level.numberOfTopSlotsPerRow;


        // Row 1 spacing
        Vector2 row1SlotSize = new Vector2(0.7f, 0.7f);
        Vector3 row1Offset = Vector3.zero;
        SpriteRenderer sr1 = row1.GetComponent<SpriteRenderer>();
        if (sr1 != null)
        {
            Vector3 size1 = sr1.bounds.size;
            if (cols > 1)
                row1SlotSize.x = size1.x / (cols - 1);
            row1SlotSize.y = size1.y;
            row1Offset = new Vector3(-size1.x * 0.5f, 0f, 0f);
        }

        // Row 2 spacing
        Vector2 row2SlotSize = new Vector2(0.7f, 0.7f);
        Vector3 row2Offset = Vector3.zero;
        SpriteRenderer sr2 = row2.GetComponent<SpriteRenderer>();
        if (sr2 != null)
        {
            Vector3 size2 = sr2.bounds.size;
            if (cols > 1)
                row2SlotSize.x = size2.x / (cols - 1);
            row2SlotSize.y = size2.y;
            row2Offset = new Vector3(-size2.x * 0.5f, 0f, 0f);
        }

        // Instantiate first row slots
        for (int col = 0; col < cols; col++)
        {
            GameObject slotGO = Instantiate(TopSlotPrefab, row1);
            slotGO.transform.localPosition = row1Offset + new Vector3(col * row1SlotSize.x, 0f, 0f);
            //slotGO.GetComponent<TopSlotView>().Initialize(0, col);
        }

        // Instantiate second row slots
        for (int col = 0; col < cols; col++)
        {
            GameObject slotGO = Instantiate(TopSlotPrefab, row2);
            slotGO.transform.localPosition = row2Offset + new Vector3(col * row2SlotSize.x, 0f, 0f);
            // slotGO.GetComponent<TopSlotView>().Initialize(1, col);
        }

        // Middle area
        // compute per-slot size from middle frame sprite (or fallback)
        Vector2 middleSlotSize = new Vector2(0.7f, 0.7f);
        Vector3 middleOriginOffset = Vector3.zero;
        SpriteRenderer middleFrame = middleRoot.GetComponentInChildren<SpriteRenderer>();
        if (middleFrame != null)
        {
            Vector3 frameSize = middleFrame.bounds.size;
            int count = level.middleSlots.Count;
            if (count > 1)
                middleSlotSize.x = frameSize.x / (count - 1);
            middleSlotSize.y = frameSize.y;
            // left-most corner of the frame in local space
            middleOriginOffset = new Vector3(-frameSize.x * 0.5f, 0f, 0f);
        }

        for (int i = 0; i < level.middleSlots.Count; i++)
        {
            GameObject slotGO = Instantiate(MiddleSlotPrefab, middleRoot.transform);
            slotGO.transform.localPosition = middleOriginOffset
                + new Vector3(i * middleSlotSize.x, 0f, 0f);

            // initialize view if needed, e.g.:
             slotGO.GetComponent<MiddleSlotView>().Initialize(level.middleSlots[i]);
        }

        // 5) Grid area
        // compute per-cell size from grid frame sprite (or fallback)
        Vector2 gridCellSize = new Vector2(0.7f, 0.7f);
        Vector3 originOffset = Vector3.zero;
        SpriteRenderer frameSprite = gridRoot.GetComponentInChildren<SpriteRenderer>();
        if (frameSprite != null)
        {
            Vector3 boundsSize = frameSprite.bounds.size;
            gridCellSize.x = boundsSize.x / (gridWidth - 1);
            gridCellSize.y = boundsSize.y / (gridHeight - 1);
            // top-left corner of the frame in local space:
            originOffset = new Vector3(-boundsSize.x * 0.5f, boundsSize.y * 0.5f, 0f);
        }

        foreach (var slot in level.gridSlots)
        {
            GameObject slotGO = Instantiate(GridSlotPrefab, gridRoot.transform);
            // position so (0,0) is at top-left of frame, y increases downward
            slotGO.transform.localPosition = originOffset +
                new Vector3(slot.x * gridCellSize.x, -slot.y * gridCellSize.y, 0f);

            // (rest of your slot initialization here)
            var cellView = slotGO.GetComponent<GridCellView>();
            cellView.Initialize(slot, BoxPrefab, PipePrefab,CardPrefab);
            _cellViews[new Vector2Int(slot.x, slot.y)] = cellView;
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
}
