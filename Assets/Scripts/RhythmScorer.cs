using UnityEngine;
using System;

public class RhythmScorer : MonoBehaviour
{
    
    // Timing windows for scoring (in seconds)
    [SerializeField] private float perfectWindow = 0.05f;    // ±50ms
    [SerializeField] private float greatWindow = 0.1f;       // ±100ms
    [SerializeField] private float goodWindow = 0.15f;       // ±150ms
    [SerializeField] private Conductor.NoteValue beatToTrack;

    private float beatFrac;
    
    public enum Score
    {
        Perfect,
        Great,
        Good,
        Miss
    }

    private void Update()
    {
        beatFrac = Conductor.Instance.GetSpecificBeatFraction(beatToTrack);

        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            if (beatFrac < 0.5f)
            {
                
                if (beatFrac <= perfectWindow)
                {
                    print("Perfect");
                    print($"{beatFrac} is less {perfectWindow}");
                }
                else if (beatFrac <= greatWindow)
                {
                    print("Great");
                    print($"{beatFrac} is less {greatWindow}");
                }
                else if (beatFrac <= goodWindow)
                {
                    print("Good");
                    print($"{beatFrac} is less {goodWindow}");
                }
                else
                {
                    print("Miss");
                }
            }
            else
            {
                
                if (beatFrac >= 1-perfectWindow)
                {
                    print("Perfect");
                    print($"{beatFrac} is more {1-perfectWindow}");
                }
                else if (beatFrac >= 1-greatWindow)
                {
                    print("Great");
                    print($"{beatFrac} is more {1-greatWindow}");

                }
                else if (beatFrac >= 1-goodWindow)
                {
                    print("Good");
                    print($"{beatFrac} is more {1-greatWindow}");

                }
                else
                {
                    print("Miss");
                }
            }
        }
    }


    /*private float GetTimingDifference(float targetBeatFraction)
    {
        // Get the current beat length in seconds
        float beatLength = 60f / Conductor.Instance.Bpm;
        
        // Calculate the actual timing difference
        float currentBeatFraction = Conductor.Instance.CurrentBeatFraction;
        float difference = currentBeatFraction - targetBeatFraction;
        
        // Adjust for wrap-around cases (near start/end of beat)
        if (difference > 0.5f) difference -= 1f;
        if (difference < -0.5f) difference += 1f;
        
        // Convert from fraction to seconds
        return difference * beatLength;
    }

    public (Score score, float deviation) ScoreInput(float targetBeatFraction = 0f)
    {
        float timingDifference = GetTimingDifference(targetBeatFraction);
        float absTimingDifference = Mathf.Abs(timingDifference);

        Score score;
        if (absTimingDifference <= perfectWindow)
            score = Score.Perfect;
        else if (absTimingDifference <= greatWindow)
            score = Score.Great;
        else if (absTimingDifference <= goodWindow)
            score = Score.Good;
        else
            score = Score.Miss;

        return (score, timingDifference);
    }

    // Example usage of registering note targets with the conductor
    public void RegisterNoteTarget(float beatNumber, Action<Score> onScored)
    {
        Conductor.Instance.Register(Conductor.NoteValue.Quarter, args =>
        {
            if (Mathf.Approximately(args.BeatFraction, beatNumber))
            {
                // Start listening for input around this timing
                StartCoroutine(ListenForInput(beatNumber, onScored));
            }
        });
    }

    private System.Collections.IEnumerator ListenForInput(float targetBeat, Action<Score> onScored)
    {
        float inputWindow = goodWindow * 2; // Listen for slightly longer than the good window
        float startTime = Time.time;
        bool scored = false;

        while (Time.time - startTime < inputWindow && !scored)
        {
            // Check for input
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) // Replace with your input method
            {
                var (score, deviation) = ScoreInput(targetBeat);
                onScored(score);
                scored = true;
                
                // You could also log the timing deviation for debugging
                Debug.Log($"Hit with {score}: {deviation * 1000:F0}ms");
            }
            yield return null;
        }

        // If no input was received during the window, count it as a miss
        if (!scored)
        {
            onScored(Score.Miss);
        }
    }*/
}