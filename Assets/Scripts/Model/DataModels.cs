// DataModels.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MiddleSlot
{
    public string slotID;         // unique identifier
    public int unlocksAtLevel;    // level at which this slot becomes available
}

[Serializable]
public class LevelData
{
    public int levelID;
    public int numColors;

    // ── replace old middleSlotCount with a list of slots ──
    public List<MiddleSlot> middleSlots;

    // ── replace fixed topRows/topColumns with two parameters ──
    public int numberOfRows;             // e.g. 2
    public int numberOfTopSlotsPerRow;   // e.g. 7–9

    public List<GridSlot> gridSlots;

    // runtime state remains unchanged or you can add:
    public List<BoxData> middleSlotBoxes;    // length = middleSlots.Count
    public List<CardData> topSlotsCards;     // length = numberOfRows * numberOfTopSlotsPerRow
}

[Serializable]
public class GridSlot
{
    public int x;
    public int y;
    public SlotType type;
    public BoxData box;        // used if type == Box
    public PipeData pipe;      // used if type == Pipe
}

public enum SlotType { Empty, Box, Pipe }

[Serializable]
public class BoxData
{
    public string boxID;
    public Vector2Int gridPosition;
    public int colorIndex;
    public bool hidden;
    public List<CardData> initialCards = new List<CardData>();
    // new runtime flag
    public bool resolved = false;
    // new runtime list of cards currently “in” this box
    public List<CardData> assignedCards = new List<CardData>();
}

[Serializable]
public class PipeData
{
    public int spawnCount;
    public List<BoxData> boxes = new List<BoxData>();
}

[Serializable]
public class CardData
{
    public string cardID;      // new: unique per card
    public int colorIndex;     // the card’s color

    // ── new runtime fields ──
    public int assignedTopSlot = -1;      // which top‐area slot this card occupies
    public BoxView resolvedBox = null;   // boxID of the middle‐area BoxData it will ultimately fill
    public int assignedMiddleSlotIndex = -1;

}

