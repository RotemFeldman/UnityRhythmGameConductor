using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

#if USE_FMOD
using FMODUnity;
using FMOD.Studio;
#endif

[RequireComponent(typeof(AudioSource))]
public class Conductor : MonoBehaviour
{
    public static Conductor Instance { get; private set; }

    [SerializeField] private TimeSignature _timeSignature;
    [SerializeField] private float _bpm;
    [SerializeField] private float _offset;
    [SerializeField] private AudioSource _audioSource;

    #if USE_FMOD
    [SerializeField] private EventReference _fmodEvent;
    private EventInstance _fmodInstance;
    private Timeline _fmodTimeline;
    #endif

    private NativeArray<IntervalData> _intervalData;
    private IntervalJob _intervalJob;
    private JobHandle _intervalJobHandle;

    private static readonly Dictionary<NoteValue, float> NoteValues = new()
    {
        { NoteValue.Whole, 0.25f },
        { NoteValue.Half, 0.5f },
        { NoteValue.Quarter, 1f },
        { NoteValue.Eighth, 2f },
        { NoteValue.Sixteenth, 4f },
        { NoteValue.ThirtySecond, 8f }
    };

    public enum NoteValue
    {
        Whole,
        Half,
        Quarter,
        Eighth,
        Sixteenth,
        ThirtySecond,
    }

    private static int CurrentMeasure { get; set; }
    private static int CurrentBeat { get; set; }
    private static float CurrentBeatFraction { get; set; }
    private static TimeSignature CurrentTimeSignature { get; set; }

    // Burst-compatible interval data structure
    [BurstCompile]
    private struct IntervalData
    {
        public float Value;
        public float IntervalLength;
        public int LastInterval;
        public bool HasTriggered;
    }

    [BurstCompile]
    private struct IntervalJob : IJobParallelFor
    {
        [ReadOnly] public float CurrentTime;
        public NativeArray<IntervalData> Intervals;

        public void Execute(int index)
        {
            var interval = Intervals[index];
            int currentInterval = (int)math.floor(CurrentTime / interval.IntervalLength);
            
            if (currentInterval != interval.LastInterval)
            {
                interval.LastInterval = currentInterval;
                interval.HasTriggered = true;
                Intervals[index] = interval;
            }
        }
    }

    private readonly List<Action<ConductorEventArgs>>[] _callbacks;
    private readonly List<Action<ConductorEventArgs>>[] _oneShots;

    public Conductor()
    {
        int enumCount = Enum.GetValues(typeof(NoteValue)).Length;
        _callbacks = new List<Action<ConductorEventArgs>>[enumCount];
        _oneShots = new List<Action<ConductorEventArgs>>[enumCount];
        
        for (int i = 0; i < enumCount; i++)
        {
            _callbacks[i] = new List<Action<ConductorEventArgs>>();
            _oneShots[i] = new List<Action<ConductorEventArgs>>();
        }
    }

    public void Register(NoteValue note, Action<ConductorEventArgs> callback, bool isOneShot = false)
    {
        var list = isOneShot ? _oneShots[(int)note] : _callbacks[(int)note];
        list.Add(callback);
    }

    public void Unregister(NoteValue note, Action<ConductorEventArgs> callback)
    {
        _callbacks[(int)note].Remove(callback);
        _oneShots[(int)note].Remove(callback);
    }

    public void SetBpm(float bpm)
    {
        _bpm = bpm;
        UpdateIntervalLengths();
        
        #if USE_FMOD
        if (_fmodInstance.isValid())
        {
            _fmodInstance.setParameterByName("BPM", bpm);
        }
        #endif
    }

    private void UpdateIntervalLengths()
    {
        for (int i = 0; i < _intervalData.Length; i++)
        {
            var data = _intervalData[i];
            data.IntervalLength = 60f / (_bpm * data.Value);
            _intervalData[i] = data;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIntervals();
            
            #if USE_FMOD
            InitializeFMOD();
            #endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeIntervals()
    {
        _intervalData = new NativeArray<IntervalData>(
            Enum.GetValues(typeof(NoteValue)).Length,
            Allocator.Persistent
        );

        for (int i = 0; i < _intervalData.Length; i++)
        {
            NoteValues.TryGetValue((NoteValue)i, out float value);
            _intervalData[i] = new IntervalData
            {
                Value = value,
                IntervalLength = 60f / (_bpm * value),
                LastInterval = -1,
                HasTriggered = false
            };
        }

        CurrentMeasure = 1;
        CurrentBeat = 1;
        CurrentBeatFraction = 0;
        CurrentTimeSignature = _timeSignature;
    }

    #if USE_FMOD
    private void InitializeFMOD()
    {
        if (!_fmodEvent.IsNull)
        {
            _fmodInstance = RuntimeManager.CreateInstance(_fmodEvent);
            _fmodInstance.start();
            _fmodInstance.getTimelinePosition(out int timelinePos);
            _fmodTimeline = new Timeline { position = timelinePos };
        }
    }
    #endif

    private void Update()
    {
        float currentTime = GetCurrentTime();
        
        // Schedule the interval check job
        _intervalJob = new IntervalJob
        {
            CurrentTime = currentTime,
            Intervals = _intervalData
        };

        _intervalJobHandle = _intervalJob.Schedule(_intervalData.Length, 64);
        _intervalJobHandle.Complete();

        // Process callbacks after job completion
        ProcessIntervals();
        
        #if USE_FMOD
        UpdateFMOD();
        #endif
    }

    private float GetCurrentTime(int beatType = 0)
    {
        #if USE_FMOD
        if (_fmodInstance.isValid())
        {
            _fmodInstance.getTimelinePosition(out int timelinePos);
            return timelinePos / 1000f;
        }
        #endif
        
        return _audioSource.timeSamples / (_audioSource.clip.frequency * _intervalData[beatType].IntervalLength) + _offset;
    }

    private void ProcessIntervals()
    {
        var eventArgs = new ConductorEventArgs(CurrentMeasure, CurrentBeat, CurrentBeatFraction, CurrentTimeSignature);

        for (int i = _intervalData.Length -1; i >= 0; i--)
        {
            if (i == (int)_timeSignature.BeatType)
            {
                CurrentBeatFraction = GetCurrentTime(i) % 1;
            }
            
            var data = _intervalData[i];
            if (data.HasTriggered)
            {
                // Process regular callbacks
                foreach (var callback in _callbacks[i])
                {
                    callback(eventArgs);
                }

                // Process one-shot callbacks
                foreach (var callback in _oneShots[i])
                {
                    callback(eventArgs);
                }
                _oneShots[i].Clear();

                // Reset trigger flag
                data.HasTriggered = false;
                _intervalData[i] = data;

                // Update timing if this is the beat type
                if (i == (int)_timeSignature.BeatType)
                {
                    UpdateTiming();
                    
                }
            }
            
            
            
        }
    }

    #if USE_FMOD
    private void UpdateFMOD()
    {
        if (_fmodInstance.isValid())
        {
            _fmodInstance.getTimelinePosition(out int timelinePos);
            if (timelinePos != _fmodTimeline.position)
            {
                _fmodTimeline.position = timelinePos;
                // Additional FMOD-specific timing updates can be added here
            }
        }
    }
    #endif

    private void UpdateTiming()
    {
        CurrentBeat++;
        if (CurrentBeat > _timeSignature.BeatNumber)
        {
            CurrentBeat = 1;
            CurrentMeasure++;
        }
    }

    private void OnDestroy()
    {
        if (_intervalData.IsCreated)
        {
            _intervalData.Dispose();
        }

        #if USE_FMOD
        if (_fmodInstance.isValid())
        {
            _fmodInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _fmodInstance.release();
        }
        #endif
    }

    #region Structs
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
}