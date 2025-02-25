using System;
using Rhythmic;
using UnityEngine;
using UnityEngine.UI;

public class Judger : MonoBehaviour
{
    public GameObject QuarterImage;
    public GameObject EighthImage;
    public GameObject _quarterImageMirror;
    public GameObject _eighthImageMirror;
    private RectTransform _rectTransform;

    private Vector3 _startPositionRight;
    private Vector3 _startPositionLeft;
    private Vector3 _middlePosition;
    private Vector3 _midDistance;
    private Color _startColor;
    private Image _image;
    private void Start()
    {
        _image = GetComponent<Image>();
        _startColor = _image.color;
        _rectTransform = GetComponent<RectTransform>();
        
        var x = _rectTransform.rect.width * 0.5f;
        _middlePosition = _rectTransform.position;
        _startPositionRight = _middlePosition + new Vector3(x, 0, 0);
        _startPositionLeft = _middlePosition + new Vector3(-x, 0, 0);

        _midDistance = new Vector3(x * 0.5f, 0, 0);

        Conductor.Instance.Register(Conductor.NoteValue.Quarter, (args =>
        {
            _image.color += Color.yellow*0.2f;
        }));

    }

    private void Update()
    {
        var beatQ = Conductor.Instance.GetBeatFraction(Conductor.NoteValue.Quarter);
        var beatE = Conductor.Instance.GetBeatFraction(Conductor.NoteValue.Quarter)+0.5f;
        if(beatE > 1){beatE-=1f;}
        
        QuarterImage.transform.position = Vector3.Lerp(_startPositionRight,_middlePosition, beatQ);
        _quarterImageMirror.transform.position = Vector3.Lerp(_startPositionLeft,_middlePosition, beatQ);
        
        EighthImage.transform.position = Vector3.Lerp(_startPositionRight, _middlePosition, beatE);
        _eighthImageMirror.transform.position = Vector3.Lerp(_startPositionLeft, _middlePosition, beatE);
        _image.color = Color.Lerp(_image.color,_startColor,Time.deltaTime*8f);
    }

    [ContextMenu("setbpm120")]
    public void setbpm()
    {
        Conductor.Instance.SetBpm(120);
    }
    
    [ContextMenu("setbpm60")]
    public void setbpm2()
    {
        Conductor.Instance.SetBpm(60);
    }
}
