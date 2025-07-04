// ModelManager.cs
using System.Collections.Generic;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    public static ModelManager Instance { get; private set; }

    [Header("Level Data Assets")]
    [SerializeField] private List<TextAsset> levelJsonAssets;

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
        if (index < 0 || index >= _templates.Count)
        {
            Debug.LogError($"ModelManager: invalid level index {index}");
            return null;
        }
        // deep clone via JSON round-trip
        string json = JsonUtility.ToJson(_templates[index], true);
        return JsonUtility.FromJson<LevelData>(json);
    }

    public int GetNumLevels()
    {
        return _templates.Count;
    }
}
