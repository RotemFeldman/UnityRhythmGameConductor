/*
using UnityEngine;

public class Input : MonoBehaviour
{
    private RhythmScorer scorer;

    void Start()
    {
        scorer = GetComponent<RhythmScorer>();
        
        // Register a note at beat 2
        scorer.RegisterNoteTarget(1, score =>
        {
            switch (score)
            {
                case RhythmScorer.Score.Perfect:
                    print("Perfect");
                    break;
                case RhythmScorer.Score.Great:
                    print("Great");
                    break;
                case RhythmScorer.Score.Good:
                    print("Good");
                    break;
                case RhythmScorer.Score.Miss:
                    break;
            }
        });
    }
}
*/
