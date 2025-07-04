// LevelEditorWindow.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using JsonUtility = UnityEngine.JsonUtility;
using System.Linq;
using Unity.VisualScripting;
using log4net.Core;
using UnityEngine.UIElements;

public class LevelEditorWindow : EditorWindow
{
    const int gridW = 8, gridH = 7;
    const float cellSize = 50f;
    const float boxScale = 0.9f;

    // Editor-only palette of length currentLevel.numColors
    private List<Color> editorPalette = new List<Color>();

    LevelData currentLevel;

    [MenuItem("Window/Level Builder")]
    public static void ShowWindow() => GetWindow<LevelEditorWindow>("Level Builder");

    void OnEnable()
    {


        // Initialize a blank level
        currentLevel = new LevelData
        {
            numColors = 4,

            // default to 5 middle slots
            middleSlots = new List<MiddleSlot>()
        };
        for (int i = 0; i < 5; i++)
            currentLevel.middleSlots.Add(new MiddleSlot { slotID = $"M{i + 1:000}", unlocksAtLevel = 1 });

        // default top area
        currentLevel.numberOfRows = 2;
        currentLevel.numberOfTopSlotsPerRow = 7;

        // grid slots
        currentLevel.gridSlots = new List<GridSlot>();
        for (int x = 0; x < gridW; x++)
            for (int y = 0; y < gridH; y++)
                currentLevel.gridSlots.Add(new GridSlot
                {
                    x = x,
                    y = y,
                    type = SlotType.Box,
                    box = new BoxData(),
                    pipe = null
                });

        // Initialize the palette to a default size (match initial numColors)
        editorPalette = Enumerable.Repeat(Color.white, currentLevel.numColors).ToList();
    }

    

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load JSON", GUILayout.Height(25))) LoadJson();
        if (GUILayout.Button("Save JSON", GUILayout.Height(25))) SaveJson();
        GUILayout.EndHorizontal();

        // 1) Draw your existing slider:
        int newNum = EditorGUILayout.IntSlider("Number of colors", currentLevel.numColors, 2, 8);
        if (newNum != currentLevel.numColors)
        {
            currentLevel.numColors = newNum;
            // Ensure the palette list matches the new length:
            while (editorPalette.Count < newNum)
                editorPalette.Add(Color.white);  // or any default
            while (editorPalette.Count > newNum)
                editorPalette.RemoveAt(editorPalette.Count - 1);
        }

        // 2) Draw the palette:
        EditorGUILayout.LabelField("Palette:", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < currentLevel.numColors; i++)
        {
            editorPalette[i] = EditorGUILayout.ColorField($"Color {i}", editorPalette[i]);
        }
        EditorGUI.indentLevel--;

        // ── Middle slot count only ──
        int targetCount = EditorGUILayout.IntSlider("Middle Slot Count", currentLevel.middleSlots.Count, 5, 7);
            while (currentLevel.middleSlots.Count < targetCount)
                currentLevel.middleSlots.Add(new MiddleSlot { slotID = "", unlocksAtLevel = 1 });
            while (currentLevel.middleSlots.Count > targetCount)
                currentLevel.middleSlots.RemoveAt(currentLevel.middleSlots.Count - 1);

        // ── Top area parameters ──
        currentLevel.numberOfRows = EditorGUILayout.IntField("Top Rows", currentLevel.numberOfRows);
        currentLevel.numberOfTopSlotsPerRow = EditorGUILayout.IntSlider("Top Slots/Row", currentLevel.numberOfTopSlotsPerRow, 1, 9);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize", GUILayout.Height(25)))
        {
            // 1) Apply the static Randomize to our real model
            LevelRandomizer.Randomize(currentLevel);
            Repaint();

        }
        if (GUILayout.Button("Clear", GUILayout.Height(25)))
        {
            ClearGrid();
        }
        GUILayout.EndHorizontal();

        // … Randomize, Clear, etc. …

        DrawTopArea();
        DrawMiddleArea();
        DrawGrid();
        DrawPipesUI();
    }

    void DrawTopArea()
    {
        float margin = 5f;
        int rows = currentLevel.numberOfRows;
        int cols = currentLevel.numberOfTopSlotsPerRow;
        float slot = cellSize;
        float totalW = cols * slot + (cols - 1) * margin;
        float totalH = rows * slot + (rows - 1) * margin;
        GUILayout.Label("Top Area");
        Rect r = GUILayoutUtility.GetRect(totalW, totalH);
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                float x = r.x + col * (slot + margin);
                float y = r.y + row * (slot + margin);
                Rect cell = new Rect(x, y, slot, slot);
                EditorGUI.DrawRect(cell, Color.white);
                GUI.Box(cell, "");
            }
    }

    void DrawMiddleArea()
    {
        float margin = 5f;
        int count = currentLevel.middleSlots.Count;
        float slot = cellSize;
        float totalW = count * slot + (count - 1) * margin;
        GUILayout.Label("Middle Area");
        Rect r = GUILayoutUtility.GetRect(totalW, slot);
        for (int i = 0; i < count; i++)
        {
            float x = r.x + i * (slot + margin);
            Rect cell = new Rect(x, r.y, slot, slot);
            EditorGUI.DrawRect(cell, Color.white);
            GUI.Box(cell, "");
            
                    // editable unlock-level textbox inside the slot
            var ms = currentLevel.middleSlots[i];
            Rect field = new Rect(cell.x + 2, cell.y + 2, cell.width - 4, cell.height - 4);
            ms.unlocksAtLevel = EditorGUI.IntField(field, ms.unlocksAtLevel);
        }
    }
    void DrawGrid()
    {
        // Reserve space for the entire grid
        Rect gridRect = GUILayoutUtility.GetRect(gridW * cellSize, gridH * cellSize);

        // Draw each slot at (x,y) where (0,0) is top-left and y increases downward
        foreach (var slot in currentLevel.gridSlots)
        {
            int x = slot.x;
            int y = slot.y;

            // Calculate pixel position: y increases down from top of gridRect
            float px = gridRect.x + x * cellSize;
            float py = gridRect.y + y * cellSize;
            Rect cell = new Rect(px, py, cellSize, cellSize);

            // Background
            GUI.Box(cell, "");

            // detect if this cell is above a pipe with boxes
            bool abovePipe = currentLevel.gridSlots.Any(s => s.x == slot.x && s.y == slot.y + 1 && s.type == SlotType.Pipe && s.pipe != null && s.pipe.boxes.Count > 0);

            if (abovePipe)
            {
                // treat as a Box using the first pipe box
                var pipeSlot = currentLevel.gridSlots.First(s => s.x == slot.x && s.y == slot.y + 1 && s.type == SlotType.Pipe);
                BoxData pd = pipeSlot.pipe.boxes[0];

                float bs = cellSize * boxScale;
                Rect brect = new Rect(
                    cell.x + (cellSize - bs) / 2,
                    cell.y + (cellSize - bs) / 2,
                    bs, bs);
                Color col = GetEditorColor(pd.colorIndex);
                EditorGUI.DrawRect(brect, col);
                HandleClicks(slot, cell); // still allow toggling pipe parameters below
                continue;
            }

            // Content
            if (slot.type == SlotType.Empty)
            {
                EditorGUI.DrawRect(cell, Color.black);
            }
            else if (slot.type == SlotType.Pipe)
            {
                slot.pipe.spawnCount = EditorGUI.IntField(cell, slot.pipe.spawnCount);
            }
            else // Box
            {
                float bs = cellSize * boxScale;
                Rect brect = new Rect(
                    px + (cellSize - bs) / 2,
                    py + (cellSize - bs) / 2,
                    bs, bs);

                // Box background
                Color col = GetEditorColor(slot.box.colorIndex);
                EditorGUI.DrawRect(brect, col);

                // Ensure we have three CardData entries
                if (slot.box.initialCards == null)
                    slot.box.initialCards = new List<CardData>();
                while (slot.box.initialCards.Count < 3)
                    slot.box.initialCards.Add(new CardData { colorIndex = 0, cardID = "" });

                // Now you can safely do:
                float cw = bs / 3f;
                for (int i = 0; i < 3; i++)
                {
                    Rect crec = new Rect(
                        brect.x + i * cw,
                        brect.y,
                        cw, cw);
                    var card = slot.box.initialCards[i];
                    Color ccol = GetEditorColor(card.colorIndex); 
                    EditorGUI.DrawRect(crec, ccol);
                }
            }

            // Handle mouse interaction
            HandleClicks(slot, cell);
        }
    }

    // helper to draw a box + its 3 cards inside a 50×50 cell
    private void DrawBoxAndCards(Rect cellRect, BoxData box)
    {
        float bs = cellRect.width * 0.9f;
        var brect = new Rect(
            cellRect.x + (cellRect.width - bs) * 0.5f,
            cellRect.y + (cellRect.height - bs) * 0.5f,
            bs, bs);
        EditorGUI.DrawRect(brect, GetEditorColor(box.colorIndex));

        float cw = bs / 3f;
        for (int i = 0; i < 3 && box.initialCards != null && box.initialCards.Count > i; i++)
        {
            var cr = new Rect(brect.x + i * cw, brect.y, cw, cw);
            EditorGUI.DrawRect(cr, GetEditorColor(box.initialCards[i].colorIndex));
        }
    }
    void HandleClicks(GridSlot slot, Rect cell)
    {
        Event e = Event.current;
        if (e.type != EventType.MouseDown || !cell.Contains(e.mousePosition)) return;

        // Only proceed if not interacting with pipe field
        if (slot.type == SlotType.Pipe && cell.Contains(e.mousePosition) && e.button == 0)
            return;

        // Box click regions
        if (slot.type == SlotType.Box && e.button == 0)
        {
            float bs = cellSize * boxScale;
            Rect brect = new Rect(cell.x + (cellSize - bs) / 2, cell.y + (cellSize - bs) / 2, bs, bs);
            float cw = bs / 3f;
            // 1) small-card rects first
            for (int i = 0; i < 3; i++)
            {
                Rect crec = new Rect(brect.x + i * cw, brect.y, cw, cw);
                if (crec.Contains(e.mousePosition))
                {
                    slot.box.initialCards[i].colorIndex = (slot.box.initialCards[i].colorIndex + 1) % currentLevel.numColors;
                    e.Use(); Repaint();
                    return;
                }
            }
            // 2) then box background
            if (brect.Contains(e.mousePosition))
            {
                slot.box.colorIndex = (slot.box.colorIndex + 1) % currentLevel.numColors;
                e.Use(); Repaint();
                return;
            }
        }

        // Default left click: toggle exist/empty: toggle exist/empty
        if (e.button == 0)
        {
            slot.type = (slot.type == SlotType.Empty) ? SlotType.Box : SlotType.Empty;
            if (slot.type == SlotType.Box && slot.box == null)
                slot.box = new BoxData();
            e.Use(); Repaint();
        }

        // Right click still opens menu
        if (e.button == 1)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Empty"), slot.type == SlotType.Empty, () => { slot.type = SlotType.Empty; });
            menu.AddItem(new GUIContent("Box (Hidden)"), slot.type == SlotType.Box && slot.box.hidden,
                () => { slot.type = SlotType.Box; slot.box.hidden = true; });
            menu.AddItem(new GUIContent("Box (Visible)"), slot.type == SlotType.Box && !slot.box.hidden,
                () => { slot.type = SlotType.Box; slot.box.hidden = false; });
            menu.AddItem(new GUIContent("Pipe"), slot.type == SlotType.Pipe,
                () => { slot.type = SlotType.Pipe; slot.pipe = new PipeData { spawnCount = 1 }; });
            menu.ShowAsContext();
            e.Use();
        }
    }

    void LoadJson()
    {
        var path = EditorUtility.OpenFilePanel("Load Level JSON", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        currentLevel = JsonUtility.FromJson<LevelData>(json);

        // ─── Ensure every pipe has its boxes list populated ────────────────────
        foreach (var slot in currentLevel.gridSlots.Where(s => s.type == SlotType.Pipe && s.pipe != null))
        {
            if (slot.pipe.boxes == null)
                slot.pipe.boxes = new List<BoxData>();

            // We assume the JSON supplied exactly the right number of BoxData entries,
            // so we DON'T rebuild or overwrite the list here.
            // Just guard against null so DrawPipesUI will iterate properly.
        }
        // ────────────────────────────────────────────────────────────────────────

        // Force the editor to repaint and re-draw all UI (grid + pipes)
        Repaint();
    }

    private void PostRandomizer()
    {
        foreach (var slot in currentLevel.gridSlots.Where(s => s.type == SlotType.Pipe && s.pipe != null))
        {
            if (slot.pipe.boxes.Count > 0)
            {
                // exit is one row ABOVE the pipe
                int ex = slot.x, ey = slot.y - 1;
                var exitSlot = currentLevel.gridSlots.FirstOrDefault(s => s.x == ex && s.y == ey);
                if (exitSlot != null)
                {
                    exitSlot.type = SlotType.Box;
                    exitSlot.box = slot.pipe.boxes[0];
                }
            }
        }
    }

    private void SaveJson()
    {
        // 1) Assign each BoxData its originating grid position
        foreach (var slot in currentLevel.gridSlots)
        {
            if (slot.type == SlotType.Box && slot.box != null)
            {
                slot.box.gridPosition = new Vector2Int(slot.x, slot.y);
            }
            else if (slot.type == SlotType.Pipe && slot.pipe != null)
            {
                var spawnPos = new Vector2Int(slot.x, slot.y + 1);
                foreach (var b in slot.pipe.boxes)
                    b.gridPosition = spawnPos;
            }
        }

        // 2) Re-assign unique IDs to every grid box (B001, B002, …)
        int nextGridId = 1;
        foreach (var slot in currentLevel.gridSlots)
        {
            if (slot.type == SlotType.Box && slot.box != null)
            {
                slot.box.boxID = $"B{nextGridId++:000}";
            }
        }

        // 3) Re-assign unique IDs to every pipe-spawned box (P_x_y_1, P_x_y_2, …)
        foreach (var slot in currentLevel.gridSlots)
        {
            if (slot.type == SlotType.Pipe && slot.pipe != null)
            {
                for (int i = 0; i < slot.pipe.boxes.Count; i++)
                {
                    var b = slot.pipe.boxes[i];
                    b.boxID = $"P_{slot.x}_{slot.y}_{i + 1}";
                }
            }
        }

        // 4) Re-assign every card’s ID to match its box (boxID_C1, C2, C3)
        foreach (var slot in currentLevel.gridSlots)
        {
            if (slot.type == SlotType.Box && slot.box != null)
            {
                var b = slot.box;
                for (int i = 0; i < b.initialCards.Count; i++)
                    b.initialCards[i].cardID = $"{b.boxID}_C{i + 1}";
            }
            else if (slot.type == SlotType.Pipe && slot.pipe != null)
            {
                foreach (var b in slot.pipe.boxes)
                    for (int i = 0; i < b.initialCards.Count; i++)
                        b.initialCards[i].cardID = $"{b.boxID}_C{i + 1}";
            }
        }

        // 5) Serialize to JSON
        string json = JsonUtility.ToJson(currentLevel, prettyPrint: true);
        string path = EditorUtility.SaveFilePanel("Save Level JSON", "", "level.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, json);
        Debug.Log($"Level saved to {path}");
    }





    void ClearGrid()
    {
        // Set all grid slots to Empty (non-existent)
        foreach (var slot in currentLevel.gridSlots)
        {
            slot.type = SlotType.Empty;
            slot.box = null;
            slot.pipe = null;
        }
        Repaint();
    }

  
    /*
    private void DrawPipesUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Pipe Previews (click box to cycle color; click a card to cycle its color):", EditorStyles.boldLabel);

        // Capture the current Event so we can detect clicks
        var evt = Event.current;

        foreach (var slot in currentLevel.gridSlots.Where(s => s.type == SlotType.Pipe))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Pipe @ ({slot.x},{slot.y}):", GUILayout.Width(80));

            if (slot.pipe?.boxes != null)
            {
                for (int b = 0; b < slot.pipe.boxes.Count; b++)
                {
                    var box = slot.pipe.boxes[b];
                    // 1) Reserve a fixed 50×50 rect
                    Rect cellRect = GUILayoutUtility.GetRect(50, 50, GUILayout.ExpandWidth(false));

                    // 2) Draw box background at 90% size
                    float bs = cellRect.width * 0.9f;
                    var brect = new Rect(
                        cellRect.x + (cellRect.width - bs) * 0.5f,
                        cellRect.y + (cellRect.height - bs) * 0.5f,
                        bs, bs);
                    var boxCol = GetEditorColor(box.colorIndex);
                    EditorGUI.DrawRect(brect, boxCol);

                    // 3) Draw its 3 inner cards
                    float cw = bs / 3f;
                    for (int i = 0; i < 3; i++)
                    {
                        var cr = new Rect(
                            brect.x + i * cw,
                            brect.y + 0.66f * cw, // shift down slightly so cards appear below box top
                            cw * 0.9f,
                            cw * 0.9f);
                        if (box.initialCards != null && box.initialCards.Count > i)
                        {
                            var cidx = box.initialCards[i].colorIndex;
                            EditorGUI.DrawRect(cr, GetEditorColor(cidx));
                        }
                    }

                    // 4) Handle clicks: card first, then box
                    if (evt.type == EventType.MouseDown && cellRect.Contains(evt.mousePosition))
                    {
                        bool used = false;
                        for (int i = 0; i < 3; i++)
                        {
                            var cr = new Rect(
                                brect.x + i * cw,
                                brect.y + 0.66f * cw,
                                cw * 0.9f,
                                cw * 0.9f);
                            if (cr.Contains(evt.mousePosition) && box.initialCards != null && box.initialCards.Count > i)
                            {
                                // cycle this card's color
                                box.initialCards[i].colorIndex = (box.initialCards[i].colorIndex + 1) % currentLevel.numColors;
                                used = true;
                                evt.Use();
                                break;
                            }
                        }
                        if (!used)
                        {
                            // cycle the box's color
                            box.colorIndex = (box.colorIndex + 1) % currentLevel.numColors;
                            evt.Use();
                        }
                    }

                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ensure clicks repaint the window immediately
        if (evt.type == EventType.MouseDown)
            Repaint();
    }

    */

    private void DrawPipesUI()
    {
        GUILayout.Space(10);
        GUILayout.Label(
          "Pipe Previews (click box to cycle color; click a card to cycle its color):",
          EditorStyles.boldLabel);

        var evt = Event.current;

        foreach (var slot in currentLevel.gridSlots.Where(s =>
                 s.type == SlotType.Pipe && s.pipe != null))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Pipe @ ({slot.x},{slot.y}) count:", GUILayout.Width(120));

            // ‣ 1) Resize control
            int newCount = EditorGUILayout.IntField(
                slot.pipe.spawnCount, GUILayout.Width(40));
            newCount = Mathf.Max(1, newCount);
            if (newCount != slot.pipe.spawnCount)
            {
                slot.pipe.spawnCount = newCount;
                if (slot.pipe.boxes == null)
                    slot.pipe.boxes = new List<BoxData>();

                // expand
                while (slot.pipe.boxes.Count < newCount)
                {
                    var b = new BoxData();
                         // give it three default cards (color 0)
                    b.initialCards = new List<CardData>(3);
                         for (int k = 0; k < 3; k++)
                        b.initialCards.Add(new CardData { colorIndex = 0 });
                    b.colorIndex = 0;           // default box color
                    slot.pipe.boxes.Add(b);
                }
                // shrink
                while (slot.pipe.boxes.Count > newCount)
                    slot.pipe.boxes.RemoveAt(slot.pipe.boxes.Count - 1);
            }

            // ‣ 2) Draw each queued box + its cards
            if (slot.pipe.boxes != null)
            {
                foreach (var box in slot.pipe.boxes)
                {
                    // reserve a fixed 50×50 cell
                    Rect cellRect = GUILayoutUtility.GetRect(
                        50, 50,
                        GUILayout.ExpandWidth(false),
                        GUILayout.ExpandHeight(false)
                    );

                    // box background at 90% size
                    float bs = cellRect.width * 0.9f;
                    var brect = new Rect(
                        cellRect.x + (cellRect.width - bs) * 0.5f,
                        cellRect.y + (cellRect.height - bs) * 0.5f,
                        bs, bs);
                    EditorGUI.DrawRect(brect, GetEditorColor(box.colorIndex));

                    // inner 3 cards
                    float cw = bs / 3f;
                    for (int i = 0; i < 3; i++)
                    {
                        var cr = new Rect(
                            brect.x + i * cw,
                            brect.y,
                            cw, cw);
                        if (box.initialCards != null && box.initialCards.Count > i)
                            EditorGUI.DrawRect(
                                cr,
                                GetEditorColor(box.initialCards[i].colorIndex));
                    }

                    // ‣ 3) Click handling
                    if (evt.type == EventType.MouseDown
                     && cellRect.Contains(evt.mousePosition))
                    {
                        bool used = false;
                        // card clicks
                        if (box.initialCards != null)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                var cr = new Rect(
                                    brect.x + i * cw,
                                    brect.y,
                                    cw, cw);
                                if (cr.Contains(evt.mousePosition))
                                {
                                    box.initialCards[i].colorIndex =
                                      (box.initialCards[i].colorIndex + 1)
                                      % currentLevel.numColors;
                                    used = true;
                                    evt.Use();
                                    break;
                                }
                            }
                        }
                        // box click
                        if (!used)
                        {
                            box.colorIndex =
                              (box.colorIndex + 1) % currentLevel.numColors;
                            evt.Use();
                        }
                    }

                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Force immediate repaint on interaction
        if (evt.type == EventType.MouseDown || evt.type == EventType.KeyUp)
            Repaint();
    }


    private Color GetEditorColor(int colorIndex)
    {
        return editorPalette[colorIndex];
    }

}
