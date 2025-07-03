using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PipeView : MonoBehaviour
{

    [SerializeField] TMP_Text pipeCounter;

    internal void Initialize(PipeData pipe)
    {
        pipeCounter.text = pipe.spawnCount.ToString();
    }

}
