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
        pipeCounter.text = (pipe.boxes.Count).ToString();
    }

    public void UpdatePipeCounter(int boxesLeft)
    {
        pipeCounter.text = boxesLeft.ToString();
    }

    internal void PipeCompleted()
    {
        pipeCounter.gameObject.SetActive(false);
    }
}
