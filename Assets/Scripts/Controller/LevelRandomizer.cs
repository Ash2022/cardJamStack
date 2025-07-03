// LevelRandomizer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class LevelRandomizer
{
    /// <summary>
    /// Populates LevelData.gridSlots with random, but solvable box colors and card distributions.
    /// </summary>
    /// <param name="level">The LevelData to populate.</param>
    /// <param name="seed">Optional seed for reproducibility (0 uses time-based seed).</param>
    public static void Randomize(LevelData level, int seed = 0)
    {
        var rnd = seed == 0 ? new Random() : new Random(seed);

        // 1) Gather all BoxData (grid + pipe) into one list
        var boxesList = new List<BoxData>();

        foreach (var slot in level.gridSlots)
        {
            bool isExit = level.gridSlots.Any(p => p.type == SlotType.Pipe && p.x == slot.x && p.y == slot.y + 1);

            if (slot.type == SlotType.Box && slot.box != null && !isExit)
            {
                boxesList.Add(slot.box);
            }
            else if (slot.type == SlotType.Pipe && slot.pipe != null)
            {
                // rebuild the pipe queue
                slot.pipe.boxes = Enumerable.Range(0, slot.pipe.spawnCount)
                    .Select(_ => new BoxData())
                    .ToList();

                // give them positions and collect
                foreach (var b in slot.pipe.boxes)
                {
                    b.gridPosition = new Vector2Int(slot.x, slot.y - 1);
                    boxesList.Add(b);
                }
            }
        }

        int nColors = level.numColors;
        int nBoxes = boxesList.Count;

        // 2) Assign each box a random color
        foreach (var b in boxesList)
            b.colorIndex = rnd.Next(0, nColors);

        // 3) Build a perfect card‐pool: 3 entries per box color
        var cardPool = new List<int>(nBoxes * 3);
        foreach (var b in boxesList)
            for (int i = 0; i < 3; i++)
                cardPool.Add(b.colorIndex);

        // 4) Shuffle (Fisher–Yates)
        for (int i = cardPool.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            var tmp = cardPool[i];
            cardPool[i] = cardPool[j];
            cardPool[j] = tmp;
        }

        // 5) Deal cards back to boxes
        int idx = 0;
        foreach (var b in boxesList)
        {
            b.initialCards = new List<CardData>(3);
            for (int k = 0; k < 3; k++)
                b.initialCards.Add(new CardData
                {
                    colorIndex = cardPool[idx++]
                });
            b.hidden = false;
        }

        

        // 7) Debug log counts
        var boxCounts = boxesList.GroupBy(b => b.colorIndex)
            .OrderBy(g => g.Key)
            .Select(g => $"Color {g.Key}: {g.Count()} boxes");
        Debug.Log("Randomize – box counts:\n" + string.Join("\n", boxCounts));

        var cardCounts = boxesList
            .SelectMany(b => b.initialCards)
            .GroupBy(c => c.colorIndex)
            .OrderBy(g => g.Key)
            .Select(g => $"Color {g.Key}: {g.Count()} cards");
        Debug.Log("Randomize – card counts:\n" + string.Join("\n", cardCounts));
    }


}
