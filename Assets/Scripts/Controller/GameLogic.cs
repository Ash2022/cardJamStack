// GameLogic.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameLogic
{
    /// <summary>
    /// Determines if the box with the given ID is unlocked (i.e., it has a clear orthogonal path to the top row (y=0)).
    /// </summary>
    public static bool IsBoxUnlocked(LevelData level, string boxID)
    {
        int gridWidth = level.gridSlots.Max(s => s.x) + 1;
        int gridHeight = level.gridSlots.Max(s => s.y) + 1;

        // 1) Find the starting slot for this box
        GridSlot startSlot = null;
        foreach (var s in level.gridSlots)
        {
            if (s.type == SlotType.Box && s.box != null && s.box.boxID == boxID)
            {
                startSlot = s;
                break;
            }
        }
        if (startSlot == null)
            return false;

        var start = new Vector2Int(startSlot.x, startSlot.y);

        // Helper to get a slot by coords
        GridSlot GetSlot(int x, int y)
        {
            return level.gridSlots.FirstOrDefault(s => s.x == x && s.y == y);
        }

        // Helper to test if a neighbor cell is blocked
        bool IsBlocked(Vector2Int pos)
        {
            // Off-grid
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
                return true;

            var slot = GetSlot(pos.x, pos.y);
            if (slot == null)
                return true;                  // non-existing cell

            // Block if standing on top of a pipe that still has boxes
            var below = GetSlot(pos.x, pos.y + 1);
            if (below != null
             && below.type == SlotType.Pipe
             && below.pipe != null
             && below.pipe.boxes.Count > 0)
            {
                return true;
            }

            // ✅ NEW: block pipe cells
            if (slot.type == SlotType.Pipe)
                return true;


            if (slot.type == SlotType.Empty)
                return true;                  // removed cell
            if (slot.type == SlotType.Box && slot.box != null)
                return true;                  // occupied by another box

            // slot.type == Pipe (or Box with null boxData) is passable
            return false;
        }

        // 2) BFS from start
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        visited.Add(start);
        queue.Enqueue(start);

        var dirs = new[]
        {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
    };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();

            // Reached top row?
            if (cur.y == 0)
                return true;

            // Explore neighbors
            foreach (var d in dirs)
            {
                var next = cur + d;
                if (visited.Contains(next))
                    continue;
                if (IsBlocked(next))
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        // No path found
        return false;
    }


}
