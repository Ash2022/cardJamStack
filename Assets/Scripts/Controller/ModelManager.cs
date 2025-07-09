// ModelManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    const int LOOP_SIZE = 15;
    const string LAST_PLAYED_LEVEL = "LastPlayerLevel";
    public enum UnlockTypes
    {
        Pipe,
        Hidden
    }

    public static ModelManager Instance { get; private set; }

    [Header("Level Data Assets")]
    [SerializeField] private List<TextAsset> levelJsonAssets;

    Dictionary<int,UnlockTypes> unlocksIndexList = new Dictionary<int, UnlockTypes>();

    private List<LevelData> _templates = new List<LevelData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //build unlocks list
        unlocksIndexList.Add(6, UnlockTypes.Pipe);
        unlocksIndexList.Add(13, UnlockTypes.Hidden);

        // Parse all TextAssets into LevelData templates
        _templates.Clear();
        foreach (var ta in levelJsonAssets)
        {
            if (ta == null) continue;
            var ld = JsonUtility.FromJson<LevelData>(ta.text);
            _templates.Add(ld);
        }
    }

    /// <summary>
    /// Returns a deep copy of the LevelData at the given index.
    /// </summary>
    public LevelData GetLevelByIndex(int index)
    {
        

        int numLevels = GetNumLevels();

        int loopedIndex = index;

        while (loopedIndex >= numLevels)
            loopedIndex -= LOOP_SIZE;


            // deep clone via JSON round-trip
        string json = JsonUtility.ToJson(_templates[loopedIndex], true);
        return JsonUtility.FromJson<LevelData>(json);
    }

    public int GetNumLevels()
    {
        return _templates.Count;
    }

    internal UnlockTypes? GetUnlocksForLevel(int currLevelIndex)
    {
        if(unlocksIndexList.TryGetValue(currLevelIndex, out UnlockTypes unlockType))
            return unlockType;
        else
            return null;

    }

    public int GetLastPlayedLevelIndex()
    {
        return PlayerPrefs.GetInt(LAST_PLAYED_LEVEL, -1);
    }

    public void SetLastPlayedLevelIndex(int level)
    {
        PlayerPrefs.SetInt(LAST_PLAYED_LEVEL, level);
    }
}
