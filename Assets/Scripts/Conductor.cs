using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

#if USE_FMOD
using FMODUnity;
using FMOD.Studio;
#endif
//todo integrate fmod seamless

[RequireComponent(typeof(AudioSource))]
public class Conductor : MonoBehaviour
{
	public static Conductor Instance;

	// user settings
	[SerializeField] private TimeSignature _timeSignature;
	[SerializeField] private float _bpm;
	[SerializeField] private float _offset;
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
	

	// -1 repeats is infinite
	public void Register(NoteValue note, Action<ConductorEventArgs> callback,int timesToRepeat = -1)
	{
		_intervals[(int)note].Register(callback, timesToRepeat);
	}

	public void Unregister(NoteValue note, Action<ConductorEventArgs> callback)
	{
		_intervals[(int)note].Unregister(callback);
	}

	public void SetBpm(float bpm)zs
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
			float sampledTime = _audioSource.timeSamples/((float)_audioSource.clip.frequency*_intervals[i].GetIntervalLength()) + _offset; 
			interval.CheckForIntervalElapsed(sampledTime);

			if (i == (int)_timeSignature.BeatType)
			{
				CurrentBeatFraction = sampledTime % 1;
			}
		}
	}

	private void KeepTime(ConductorEventArgs conductorEventArgs)
	{
		var beatInterval = _intervals[(int)_timeSignature.BeatType];
		if (beatInterval.HasTriggeredThisFrame)
		{
			CurrentBeat++;
			if (CurrentBeat > _timeSignature.BeatNumber)
			{
				CurrentBeat = 1;
				CurrentMeasure++;
			}
			beatInterval.HasTriggeredThisFrame = false;
		}
	}

	#region structs

	public readonly struct ConductorEventArgs
	{
		public readonly int BarNumber;
		public readonly int Beat;
		public readonly float BeatFraction;
		public readonly TimeSignature TimeSignature;
		public readonly int RemainingExecutions;  // -1 for infinite events
		public readonly int TotalExecutions;      // -1 for infinite events

		public ConductorEventArgs(
			int barNumber, 
			int beat, 
			float beatFraction, 
			TimeSignature timeSignature,
			int remainingExecutions = -1,
			int totalExecutions = -1)
		{
			BarNumber = barNumber;
			Beat = beat;
			BeatFraction = beatFraction;
			TimeSignature = timeSignature;
			RemainingExecutions = remainingExecutions;
			TotalExecutions = totalExecutions;
		}
		

		public float ExecutionProgress => TotalExecutions == -1 ? 0 : 1f - ((float)RemainingExecutions / TotalExecutions);
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
	
     public class Interval
	{
		public bool HasTriggeredThisFrame { get; set; }
	    private Action<ConductorEventArgs> _action = delegate { };
	    
	    private class TimedEvent
	    {
	        public Action<ConductorEventArgs> Callback { get; set; }
	        public int RemainingExecutions { get; set; }
	        public int TotalExecutions { get; set; }
	        public bool IsActive { get; set; } = true;
	    }
	    
	    private List<TimedEvent> _timedEvents = new List<TimedEvent>();
	    
	    private float _value;
	    private int _lastInterval;
	    private float _intervalLength;

	    public Interval(NoteValue value, float bpm)
	    {
	        Conductor.NoteValues.TryGetValue(value, out _value);
	        SetIntervalLength(bpm);
	    }

	    public void Register(Action<ConductorEventArgs> callback, int executionCount = -1)
	    {
	        if (executionCount == -1)
	        {
	            _action += callback;
	        }
	        else if (executionCount > 0)
	        {
	            _timedEvents.Add(new TimedEvent 
	            { 
	                Callback = callback,
	                RemainingExecutions = executionCount,
	                TotalExecutions = executionCount
	            });
	        }
	    }

	    public void RegisterOneShot(Action<ConductorEventArgs> callback)
	    {
	        Register(callback, 1);
	    }

	    public void Unregister(Action<ConductorEventArgs> callback)
	    {
	        _action -= callback;
	        
	        foreach (var timedEvent in _timedEvents)
	        {
	            if (timedEvent.Callback == callback)
	            {
	                timedEvent.IsActive = false;
	            }
	        }
	    }

	    public float GetIntervalLength()
	    {
	        return _intervalLength;
	    }

	    public void SetIntervalLength(float bpm)
	    {
	        _intervalLength = 60f / (bpm * _value);
	    }

	    public void CheckForIntervalElapsed(float interval)
	    {
		    int currentInterval = Mathf.FloorToInt(interval);
		    if (currentInterval != _lastInterval)
		    {
			    _lastInterval = currentInterval;
			    HasTriggeredThisFrame = true;
                
			    var eventArgs = new ConductorEventArgs(
				    CurrentMeasure,
				    CurrentBeat,
				    CurrentBeatFraction,
				    CurrentTimeSignature
			    );
                
			    _action.Invoke(eventArgs);
			    ProcessTimedEvents();
		    }
		    
	        /*if (Mathf.FloorToInt(interval) != _lastInterval)
	        {
	            _lastInterval = Mathf.FloorToInt(interval);
	            
	            var baseEventArgs = new ConductorEventArgs(
	                Conductor.CurrentMeasure,
	                Conductor.CurrentBeat,
	                Conductor.CurrentBeatFraction,
	                Conductor.CurrentTimeSignature,
	                -1,  
	                -1   
	            );
	            
			            _action.Invoke(baseEventArgs);
	            ProcessTimedEvents();
	        }*/
	        
	    }

	    private void ProcessTimedEvents()
	    {
	        List<TimedEvent> completedEvents = new List<TimedEvent>();

	        foreach (var timedEvent in _timedEvents)
	        {
	            if (!timedEvent.IsActive) 
	            {
	                completedEvents.Add(timedEvent);
	                continue;
	            }
	            
	            var eventArgs = new ConductorEventArgs(
	                Conductor.CurrentMeasure,
	                Conductor.CurrentBeat,
	                Conductor.CurrentBeatFraction,
	                Conductor.CurrentTimeSignature,
	                timedEvent.RemainingExecutions,
	                timedEvent.TotalExecutions
	            );

	            timedEvent.Callback(eventArgs);
	            timedEvent.RemainingExecutions--;

	            if (timedEvent.RemainingExecutions <= 0)
	            {
	                completedEvents.Add(timedEvent);
	            }
	        }

	        foreach (var completed in completedEvents)
	        {
	            _timedEvents.Remove(completed);
	        }
	    }
	}

     
	#endregion
}

