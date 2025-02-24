using UnityEngine;
using Rhythmic;

namespace Demo
{
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
            beatFrac = Conductor.Instance.GetBeatFraction(beatToTrack);

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
    
    }
}