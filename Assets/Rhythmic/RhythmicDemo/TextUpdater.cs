using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Rhythmic;

namespace Demo
{
    public class TextUpdater : MonoBehaviour
    { 
        public TMP_Text Text;

        private void Start()
        {
            Conductor.Instance.Register(Conductor.NoteValue.ThirtySecond,(args =>
            {
                Text.text= $"Bar:{args.BarNumber}, Beat:{args.Beat}, BeatFraction:{args.BeatFraction.ToString().Truncate(3,String.Empty)}";
            } ));
        }
    }
}
