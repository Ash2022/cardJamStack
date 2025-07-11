﻿// GameManager.cs
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Start Settings")]
    [SerializeField] private int currLevelIndex = -1;
    [SerializeField] public LevelVisualizer visualizer;
    [SerializeField] GameOverView gameOverView;
    [SerializeField] InGameUIView inGameUIView;
    [SerializeField] UnlocksManagerView unlocksManagerView;
    [SerializeField]TutorialView tutorialView;

    
    float baseAspect = 9f / 16f;   // Your target design aspect
    float baseSize = 6f;

    private Dictionary<string, CardView> _cardViewsByID = new Dictionary<string, CardView>();
    private Dictionary<string, BoxView> _boxViewsByID = new Dictionary<string, BoxView>();

    bool gameOver = false;
    bool gameActive = false;
    bool gameIsLostButStillPlaying = false;

    public LevelData CurrentLevelData { get; private set; }
    public int CurrLevelIndex { get => currLevelIndex; set => currLevelIndex = value; }
    public bool GameIsLostButStillPlaying { get => gameIsLostButStillPlaying; set => gameIsLostButStillPlaying = value; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ensure ModelManager is initialized
        if (ModelManager.Instance == null)
        {
            Debug.LogError("GameManager: ModelManager is missing in scene.");
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        float currentAspect = (float)Screen.width / Screen.height;
        Camera.main.orthographicSize = baseSize * (baseAspect / currentAspect);

        //PlayerPrefs.DeleteAll();

        if (currLevelIndex == -1)
        {
            currLevelIndex = ModelManager.Instance.GetLastPlayedLevelIndex();

            currLevelIndex++;

        }


        LoadAndStartCurrLevel();
    }

    private void LoadAndStartCurrLevel()
    {
        _cardViewsByID.Clear();
        _boxViewsByID.Clear();

        // Load a fresh copy of the requested level
        CurrentLevelData = ModelManager.Instance.GetLevelByIndex(currLevelIndex);

        visualizer.VisualizeLevel(CurrentLevelData);

        inGameUIView.InitUI(CurrentLevelData,currLevelIndex);

        //check for unlocks
        ModelManager.UnlockTypes? unlockType = ModelManager.Instance.GetUnlocksForLevel(currLevelIndex);

        if(unlockType==null)
            gameActive = true;
        else
        {
            unlocksManagerView.ShowUnlockScreen((ModelManager.UnlockTypes)unlockType, () =>
            {
                gameActive = true;
            });
        }
        gameOver = false;
        gameIsLostButStillPlaying = false;


        //check if need to show tutorial
        if(currLevelIndex == 0)
        {
            //B005 -- start box id
            if(TryGetBoxView("B005", out BoxView boxView))
                tutorialView.ShowTutorialOnBoxID(boxView.transform.position);            
        }

    }

    void Update()
    {
        if (!gameActive)
            return;

        if (gameOver)
            return;

        if (Input.GetKeyUp(KeyCode.A))
        {
            CheatLevelUp(true);
            return;
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            CheatLevelUp(false);
            return;
        }

        if (!Input.GetMouseButtonDown(0))
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        var boxView = hit.collider.GetComponent<BoxView>();
        if (boxView == null)
            return;

        string boxID = boxView.Data.boxID;
        var level = GameManager.Instance.CurrentLevelData;

        // only play if unlocked
        if (GameLogic.IsBoxUnlocked(level, boxID))
        {
            SoundsManager.Instance.BoxClicked(true);
            PlayBox(boxView);
        }
        else
        {
            SoundsManager.Instance.BoxClicked(false);
        }
        
    }

    /// <summary>
    /// Play the box with the given ID: update the model by moving the box to the middle area,
    /// sending its cards to top slots, resolving any matches, and clearing filled boxes.
    /// </summary>
    /// <summary>
    /// Called by BoxView when the player clicks an unlocked box.
    /// </summary>
    public void PlayBox(BoxView boxView)
    {
        //check if need to hide tutorial
        if (currLevelIndex == 0)
        {
            tutorialView.HideHand();
        }

            var level = CurrentLevelData;
        var boxData = boxView.Data;

        // 1) Place the box into the middle‐slot model
        int midIndex = level.middleSlotBoxes.FindIndex(b => b == null);
        
        if (midIndex < 0)
        {
            //all is full - but maybe not game over
            return;
        }

        // 0) Remove the box from its grid slot
        var gridSlot = level.gridSlots.FirstOrDefault(s => s.type == SlotType.Box && s.box != null && s.box.boxID == boxData.boxID);

        if (gridSlot != null)
            gridSlot.box = null;


        level.middleSlotBoxes[midIndex] = boxData;
        boxData.resolved = false;
        boxData.assignedCards.Clear();

        // Handle direct in-box card resolution for matching colors
        foreach (var card in boxData.initialCards.ToList()) // iterate on copy to modify safely
        {
            if (card.colorIndex == boxData.colorIndex && boxData.assignedCards.Count < 3)
            {
                card.resolvedBox = boxView;
                card.assignedMiddleSlotIndex = boxData.assignedCards.Count;
                boxData.assignedCards.Add(card);

                //not likely - but who knows
                if (boxData.assignedCards.Count == 3)
                    boxData.resolved = true;

                // Remove from initialCards so it's not reprocessed
                boxData.initialCards.Remove(card);

                //notify the CardView - so it doesnt need to fly anywhere
                TryGetCardView(card.cardID, out CardView matchingCard);

                if (matchingCard != null)
                    matchingCard.CardDoesntNeedToFlyAtAll();

            }
        }



        /////// HANDLE PIPES
        // Handle pipe‐spawned boxes
        var clickedBox = boxView.Data;
        Vector2Int exitPos;
        GridSlot pipeSlot = level.gridSlots
            .FirstOrDefault(s =>
                s.type == SlotType.Pipe &&
                s.pipe != null &&
                s.pipe.boxes.Contains(clickedBox));

        if (pipeSlot != null)
        {
            // For pipe‐spawned boxes, the exit is one row *above* the pipe:
            exitPos = new Vector2Int(pipeSlot.x, pipeSlot.y - 1);
        }
        else
        {
            // Normal grid box:
            exitPos = clickedBox.gridPosition;
        }

        // 1) Remove the box from its grid cell *in the model*
        //    (we do this *before* animation, but we do NOT touch the type or respawn yet)
        var exitSlot = level.gridSlots.First(s => s.x == exitPos.x && s.y == exitPos.y);
        exitSlot.box = null;

        //end handle pipe


        // 2) Distribute this box’s cards into the top‐slot model
        foreach (var card in boxData.initialCards)
        {
            int topIndex = level.topSlotsCards.FindIndex(c => c == null);
            if (topIndex < 0)
                topIndex = level.topSlotsCards.FindIndex(c => c != null && c.resolvedBox != null);
            
            if (topIndex < 0)
            {
                //if we reached here it means that there are cards
                //in the box WITHOUT a top slot to move them to
                //need to play the box to fly to its location
                //need to play the cards that do have spaces
                //and once the last card reached its slot - than we need to show the game over.

                //GameOver(false);
                Debug.Log("Game Over: no free top slot");
                //return;

                gameIsLostButStillPlaying = true;
            }
            else
            {
                level.topSlotsCards[topIndex] = card;
                card.assignedTopSlot = topIndex;
                card.resolvedBox = null;
            }
        }

        // 3) Resolve any top‐slot cards against the updated middle‐slot model

        if(GameIsLostButStillPlaying == false)
        {
            for (int i = 0; i < level.topSlotsCards.Count; i++)
            {
                var card = level.topSlotsCards[i];
                if (card == null || card.resolvedBox != null)
                    continue;

                // find the boxes that have the most cards assigned to them and first matching middle box with space 
                var targetBox = level.middleSlotBoxes.Where(b => b != null && b.colorIndex == card.colorIndex && b.assignedCards.Count < 3)
                .OrderByDescending(b => b.assignedCards.Count)  // prefer fuller boxes
                .ThenBy(b => level.middleSlotBoxes.IndexOf(b))  // stable fallback by order
                .FirstOrDefault();

                if (targetBox != null)
                {
                    // assign card to that box in the model
                    TryGetBoxView(targetBox.boxID, out BoxView bv);

                    if(bv!=null)
                        card.resolvedBox = bv;
                
                    card.assignedMiddleSlotIndex = targetBox.assignedCards.Count;
                    targetBox.assignedCards.Add(card);

                    // remove it from the top‐slot model
                    level.topSlotsCards[i] = null;

                    if (targetBox.assignedCards.Count == 3)
                        targetBox.resolved = true;

                }
            }
        }

        //Debug.Log("midIndex: " + midIndex);

        //slot 0 of the middle boxes start locked - and this means all the boxes need to go to next index

        var slotTf = LevelVisualizer.Instance.MiddleHolder.GetChild(0).GetChild(1).GetChild(midIndex);

        boxView.StartFlyToMiddle(slotTf, midIndex);


        if (pipeSlot != null)
        {
            // pop the clicked box from the pipe’s queue
            pipeSlot.pipe.boxes.Remove(clickedBox);

            GridCellView pipeCellView = LevelVisualizer.Instance.GetGridCellView(pipeSlot.x, pipeSlot.y);

            PipeView pipeView = pipeCellView.GetComponentInChildren<PipeView>();

            // if there’s another in the queue, spawn it into exitSlot
            if (pipeSlot.pipe.boxes.Count > 0)
            {
                var nextBox = pipeSlot.pipe.boxes[0];
                nextBox.gridPosition = exitPos;
                exitSlot.type = SlotType.Box;
                exitSlot.box = nextBox;               

                pipeView.UpdatePipeCounter(pipeSlot.pipe.boxes.Count);

            }
            else
            {
                // pipe is drained: leave exitSlot.box null but passable
                pipeView.PipeCompleted();
                exitSlot.type = SlotType.Box;
            }

            // re‐draw that one grid cell
            var cellView = visualizer.GetGridCellView(exitPos.x, exitPos.y);
            cellView.Initialize(exitSlot, visualizer.BoxPrefab, visualizer.PipePrefab, visualizer.CardPrefab,LevelVisualizer.SCALE_MULTI);
        }

        bool anythingUnlocked = UpdateUnlocks();

        if(anythingUnlocked)
            SoundsManager.Instance.NormalBoxUnlocks();

        // Check if all middle slots are full and none are resolved
        bool allFullAndBlocked = level.middleSlotBoxes.All(b => b != null && !b.resolved);

        //added second condition so i only show the GameOver after the box played and player saw there is no room
        if (allFullAndBlocked && GameIsLostButStillPlaying==false)
        {
            //GameOver(false);
            GameIsLostButStillPlaying = true;
            Debug.Log("Game Over: All middle boxes are blocked");
        }

    }



    /// <summary>
    /// Tell all cards from the given box to fly into their top slots.
    /// </summary>
    public void SendBoxCardsToTop(BoxData boxData)
    {
        float addedTime = 0;

        foreach (var card in boxData.initialCards)
        {
            if (_cardViewsByID.TryGetValue(card.cardID, out var cv))
                cv.FlyToTopSlot(addedTime);

            addedTime += 0.2f;
        }
    }

    /// <summary>
    /// Call this when a BoxView finishes its flight into the middle slot.
    /// It will animate any cards in the model that are already assigned to that box.
    /// </summary>
    public void OnBoxArrived(BoxView boxView)
    {

        if(GameIsLostButStillPlaying)
        {
            GameOver(false);
            return;
        }

        //check if need to show tutorial
        if (currLevelIndex == 0)
        {
            string boxID = GetRandomUnlockedBox(CurrentLevelData);

            if(boxID != "")
                if (TryGetBoxView(boxID, out BoxView boxView2))
                    tutorialView.ShowTutorialOnBoxID(boxView2.transform.position);
        }


        // 1) Find the BoxData for this view
        var boxData = boxView.Data;

        // 2) For each card that the model says belongs in this box, animate it
        foreach (var card in boxData.assignedCards)
        {
            // look up the CardView
            if (_cardViewsByID.TryGetValue(card.cardID, out var cv))
            {
                // fly the card into this box’s designated slot
                cv.TargetBoxReachedItsMiddleSlot(boxView);
            }
            else
            {
                Debug.LogWarning($"Missing CardView for {card.cardID} during box arrival");
            }
        }
    }

    public bool UpdateUnlocks()
    {
        bool anythingUnlocked = false;
        var level = CurrentLevelData;

        // 1) For each grid slot that still holds a box…
        foreach (var slot in level.gridSlots)
        {
            if (slot.type != SlotType.Box || slot.box == null)
                continue;

            string boxID = slot.box.boxID;

            // 2) Ask GameLogic whether it's unlocked now
            bool unlocked = GameLogic.IsBoxUnlocked(level, boxID);

            // 3) Find its view and update its locked state
            if (_boxViewsByID.TryGetValue(boxID, out var bv))
            {
                if(unlocked && bv.IsLocked)
                {
                    bv.BoxViewUnblocked();
                    anythingUnlocked = true;
                }

            }
        }

        return anythingUnlocked;
    }

    /// <summary>
    /// Called by each CardView when it initializes.
    /// </summary>
    public void RegisterCardView(CardView cv)
    {
        var id = cv.Data.cardID;
        if (!_cardViewsByID.ContainsKey(id))
            _cardViewsByID[id] = cv;
    }

    /// <summary>
    /// Called by each BoxView when it initializes.
    /// </summary>
    public void RegisterBoxView(BoxView bv)
    {
        var id = bv.Data.boxID;
        if (!_boxViewsByID.ContainsKey(id))
            _boxViewsByID[id] = bv;
    }

    /// <summary>
    /// Lookup a CardView by its ID.
    /// </summary>
    public bool TryGetCardView(string cardID, out CardView cv) =>
        _cardViewsByID.TryGetValue(cardID, out cv);

    /// <summary>
    /// Lookup a BoxView by its ID.
    /// </summary>
    public bool TryGetBoxView(string boxID, out BoxView bv) =>
        _boxViewsByID.TryGetValue(boxID, out bv);

    public void OnBoxCompleted(BoxView boxView, int slotIndex)
    {
        // 1) model cleanup: free the middle‐slot
        if (slotIndex >= 0 && slotIndex < CurrentLevelData.middleSlotBoxes.Count)
            CurrentLevelData.middleSlotBoxes[slotIndex] = null;

        //instantiate the dissappear effect
        GenerateBoxDisappearEffect(boxView);

        // 2) destroy view
        Destroy(boxView.gameObject);

        var level = CurrentLevelData;

        bool anyGrid = level.gridSlots.Any(s => s.type == SlotType.Box && s.box != null);

        bool anyMiddle = level.middleSlotBoxes.Any(b => b != null);

        bool anyPipe = level.gridSlots.Where(s => s.type == SlotType.Pipe && s.pipe != null).Any(s => s.pipe.boxes != null && s.pipe.boxes.Count > 0);

        if (!anyGrid && !anyMiddle && !anyPipe)
        {
            GameOver(true);
        }
    }

    private void GenerateBoxDisappearEffect(BoxView boxView)
    {
        GameObject disappearEffect = Instantiate(LevelVisualizer.Instance.DisappearEffect, LevelVisualizer.Instance.MiddleHolder);
        disappearEffect.transform.localPosition = boxView.transform.parent.transform.localPosition;
        MergeParticleView mergeParticleView = disappearEffect.GetComponent<MergeParticleView>();

        Color baseColor = Helper.GetColor(boxView.Data.colorIndex);

        // Create a lighter version (25% toward white)
        Color lighter = Color.Lerp(baseColor, Color.white, 0.25f);

        // Create gradient 1
        Gradient grad1 = new Gradient();
        grad1.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(baseColor, 0f),
            new GradientColorKey(lighter, 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(baseColor.a, 0f),
            new GradientAlphaKey(lighter.a, 1f)
            }
        );

        // Create gradient 2 (same logic — or you can customize it if needed)
        Gradient grad2 = new Gradient();
        grad2.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(baseColor, 0f),
            new GradientColorKey(lighter, 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(baseColor.a, 0f),
            new GradientAlphaKey(lighter.a, 1f)
            }
        );


        mergeParticleView.InitParticles(0.1f, baseColor, grad1, grad2);
    }

    /// <summary>
    /// Called whenever the game ends, either in failure or success.
    /// </summary>
    public void GameOver(bool win)
    {
        if(gameOver) 
            return;

        tutorialView.CloseTutorial();

        SoundsManager.Instance.PlayHaptics(SoundsManager.TapticsStrenght.High);
        SoundsManager.Instance.PlayLevelCompelte(win);

        gameOver = true;

        if (win)
        {
            
            Debug.Log("Game Over: YOU WIN!");
            // TODO: show win UI, stop input, etc.
            ModelManager.Instance.SetLastPlayedLevelIndex(currLevelIndex);

        }
        else
        {
            Debug.Log("Game Over: YOU LOSE!");
            // TODO: show fail UI, stop input, etc.
        }

        gameOverView.ShowGameOver(win,CurrLevelIndex, GameOverNextClicked);
    }

    private void GameOverNextClicked(bool win)
    {
        if(win)
        {
            currLevelIndex++;

            if(currLevelIndex == ModelManager.Instance.GetNumLevels())
                currLevelIndex = 0;

        }

        LoadAndStartCurrLevel();
    }

    public static string GetRandomUnlockedBox(LevelData levelData)
    {
        var unlockedBoxes = levelData.gridSlots
            .Where(slot => slot.box != null && GameLogic.IsBoxUnlocked(levelData, slot.box.boxID))
            .Select(slot => slot.box)
            .ToList();

        if (unlockedBoxes.Count == 0)
            return "";

        int index = UnityEngine.Random.Range(0, unlockedBoxes.Count);
        return unlockedBoxes[index].boxID;
    }

    private void CheatLevelUp(bool up)
    {
        currLevelIndex += up ? 1 : -1;
        LoadAndStartCurrLevel();
    }
}
