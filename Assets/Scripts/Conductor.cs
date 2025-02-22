using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Conductor : MonoBehaviour
{
	public static Conductor Instance;

	// user settings
	[SerializeField] private TimeSignature _timeSignature;
	[SerializeField] private float _bpm;
	[SerializeField] private AudioSource _audioSource;

	//inner working
	private Interval[] _intervals;
	private static readonly Dictionary<NoteValue, float> NoteValues = new Dictionary<NoteValue, float>()
	{
		{ NoteValue.Whole, 0.25f },
		{ NoteValue.Half, 0.5f },
		{ NoteValue.Quarter, 1f },
		{ NoteValue.Eighth, 2f },
		{ NoteValue.Sixteenth, 4f },
		{ NoteValue.ThirtySecond ,8f}
	};
	public enum NoteValue
	{
		Whole,
		Half,
		Quarter ,
		Eighth ,
		Sixteenth ,
		ThirtySecond ,
	}
	
	//data
	private static int CurrentMeasure {get; set;}
	private static int CurrentBeat {get; set;}
	private static float CurrentBeatFraction {get; set;}
	private static TimeSignature CurrentTimeSignature {get; set;}
	
	

	public void Register(NoteValue note, Action<ConductorEventArgs> callback,bool isOneShot = false)
	{
		_intervals[(int)note].Register(callback, isOneShot);
	}

	public void Unregister(NoteValue note, Action<ConductorEventArgs> callback)
	{
		_intervals[(int)note].Unregister(callback);
	}

	public void SetBpm(float bpm)
	{
		_bpm = bpm;
		foreach (var interval in _intervals)
		{
			interval.SetIntervalLength(bpm);
		}
	}

	private void Awake()
	{
		//create singleton
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(this);
		}
		else
		{
			Destroy(gameObject);
		}

		_intervals = new Interval[]
		{
			new Interval(NoteValue.Whole, _bpm),
			new Interval(NoteValue.Half, _bpm),
			new Interval(NoteValue.Quarter, _bpm),	
			new Interval(NoteValue.Eighth, _bpm),
			new Interval(NoteValue.Sixteenth, _bpm),
			new Interval(NoteValue.ThirtySecond, _bpm),
		};

		CurrentMeasure = 1;
		CurrentBeat = 1;
		CurrentBeatFraction = 0;
		CurrentTimeSignature = _timeSignature;
		
		_intervals[(int)_timeSignature.BeatType].Register(KeepTime);
	}

	private void Update()
	{
		for (int i = _intervals.Length - 1; i >= 0; i--)
		{
			var interval = _intervals[i];
			float sampledTime = _audioSource.timeSamples/(_audioSource.clip.frequency*interval.GetIntervalLength());
			interval.CheckForNewInterval(sampledTime);

			if (i == (int)_timeSignature.BeatType)
			{
				CurrentBeatFraction = sampledTime%1;
			}
		}
		
		
		/*foreach (var interval in _intervals)
		{
			float sampledTime = _audioSource.timeSamples/(_audioSource.clip.frequency*interval.GetIntervalLength());
			interval.CheckForNewInterval(sampledTime);
		}
		float sample = (_audioSource.timeSamples/(_audioSource.clip.frequency*_intervals[(int)_timeSignature.BeatType].GetIntervalLength()));
		CurrentBeatFraction = sample%1 ;*/
	}

	private void KeepTime(ConductorEventArgs eventArgs)
	{
		CurrentBeat++;
		if (CurrentBeat > _timeSignature.BeatNumber)
		{
			CurrentBeat = 1;
			CurrentMeasure++;
		}
	}

	#region structs

	public struct ConductorEventArgs
    {
    	public int BarNumber;
    	public int Beat;
    	public float BeatFraction;
	    public TimeSignature TimeSignature;

	    public ConductorEventArgs(int barNumber, int beat, float beatFraction, TimeSignature timeSignature)
	    {
		    BarNumber = barNumber;
		    Beat = beat;
		    BeatFraction = beatFraction;
		    TimeSignature = timeSignature;
	    }
    }

	[Serializable]
	public struct TimeSignature
	{
		public int BeatNumber;
		public NoteValue BeatType;

		public TimeSignature(int beatNumber, NoteValue beatType)
		{
			BeatNumber = beatNumber;
			BeatType = beatType;
		}
	}

	#endregion
	
	
	
	#region  interval

	

	
	[System.Serializable]
     public class Interval
     {
	     private Action<ConductorEventArgs> _action = delegate { };
	     private Action<ConductorEventArgs> _oneShots = delegate { };
	     private float _value;
     	
     	private int _lastInterval;
     	private float _intervalLength;
     
     	public Interval(NoteValue value, float bpm)
        {
	        Conductor.NoteValues.TryGetValue(value, out _value);
     		SetIntervalLength(bpm);
        }

        public void Register(Action<ConductorEventArgs> callback, bool isOneShot = false)
        {

	        if (isOneShot)
	        {
		        _oneShots+=callback;
	        }
	        else
		        _action += callback;
        }

        public void Unregister(Action<ConductorEventArgs> callback)
        {
	        _action -= callback;
        }
     
     	public float GetIntervalLength()
        {
	        return _intervalLength;
        }
     	public void SetIntervalLength(float bpm)
     	{
     		_intervalLength = 60f / (bpm * _value);
     	}
     
     	public void CheckForNewInterval(float interval)
     	{
		       if (Mathf.FloorToInt(interval) != _lastInterval)
		       {
			       _lastInterval = Mathf.FloorToInt(interval);
			       _action.Invoke(new ConductorEventArgs(CurrentMeasure,CurrentBeat,CurrentBeatFraction,CurrentTimeSignature));
			       _oneShots.Invoke(new ConductorEventArgs(CurrentMeasure,CurrentBeat,CurrentBeatFraction,CurrentTimeSignature));
			       _oneShots = delegate { };
		       } 
     	}
        
     }
     
	#endregion
}

