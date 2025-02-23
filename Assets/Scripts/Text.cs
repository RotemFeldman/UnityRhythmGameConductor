using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Text : MonoBehaviour
{
    public TMP_Text text;
    private int i = 0;

    private void Start()
    {
        Conductor.Instance.Register(Conductor.NoteValue.ThirtySecond,(args =>
        {
            text.text= $"Bar:{args.BarNumber}, Beat:{args.Beat}, BeatFraction:{args.BeatFraction.ToString().Truncate(3,String.Empty) }, {i}";
            i++;
        } ));
    }
}
