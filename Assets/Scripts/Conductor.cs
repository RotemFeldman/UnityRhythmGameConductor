using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Conductor : MonoBehaviour
{
	public static Conductor Instance;
	
	[SerializeField] private float _bpm;
	[SerializeField] private AudioSource _audioSource;

	private Interval[] _intervals;

	public enum NoteValue
	{
		Whole,
		Half,
		Quarter ,
		Eighth ,
		Sixteenth ,
	}
	
	public static readonly Dictionary<NoteValue, float> NoteValues = new Dictionary<NoteValue, float>()
	{
		{ NoteValue.Whole, 0.25f },
		{ NoteValue.Half, 0.5f },
		{ NoteValue.Quarter, 1f },
		{ NoteValue.Eighth, 2f },
		{ NoteValue.Sixteenth, 4f },
	};

	public void Register(NoteValue note, Action callback,bool isOneShot = false)
	{
		_intervals[(int)note].Register(callback, isOneShot);
	}

	public void Unregister(NoteValue note, Action callback)
	{
		_intervals[(int)note].Unregister(callback);
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
		};
	}

	private void Update()
	{
		foreach (var interval in _intervals)
		{
			
			float sampledTime = _audioSource.timeSamples/(_audioSource.clip.frequency*interval.GetIntervalLength());
			interval.CheckForNewInterval(sampledTime);
		}
	}
	
	
	
	[System.Serializable]
     public class Interval
     {
	     private Action _action = delegate { };
	     private Action _oneShots = delegate { };
	     private float _value;
     	
     	private int _lastInterval;
     	private float _intervalLength;
     
     	public Interval(NoteValue value, float bpm)
        {
	        Conductor.NoteValues.TryGetValue(value, out _value);
     		SetIntervalLength(bpm);
        }

        public void Register(Action callback, bool isOneShot = false)
        {

	        if (isOneShot)
	        {
		        _oneShots+=callback;
	        }
	        else
		        _action += callback;
        }

        public void Unregister(Action callback)
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
			       _action.Invoke();
			       _oneShots.Invoke();
			       _oneShots = delegate { };
		       } 
     	}
        
     }
}

