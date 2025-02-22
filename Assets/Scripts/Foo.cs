using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Foo : MonoBehaviour,IPointerClickHandler
{
	private Vector3 _startSize;
	private Color _startColor;
	private Image _spriteRenderer;
	[SerializeField] private float _sizeDiff;
	[SerializeField] private Conductor.NoteValue _note; 
	[SerializeField] private AudioSource _audioClip;

	private void Start()	
	{
		_spriteRenderer = GetComponent<Image>();
		_startColor = _spriteRenderer.color;
		_startSize = transform.localScale;
		Conductor.Instance.Register(_note,Change);
	}

	private void Change(Conductor.ConductorEventArgs args)
	{
		//print($"Measure:{args.BarNumber}, Beat:{args.Beat}");
		transform.localScale =_startSize* _sizeDiff;
	}

	private void Update()
	{
		var size = Vector3.Lerp(transform.localScale, _startSize, Time.deltaTime*2f);
		transform.localScale = size;
			
		var color = Color.Lerp(_spriteRenderer.color, _startColor, Time.deltaTime*2f);
		_spriteRenderer.color = color;
	}

	[ContextMenu("Change")]
	private void ChangeColor()
	{
		Conductor.Instance.Register(_note,Bar,true);
	}
		
	private void Bar(Conductor.ConductorEventArgs args)
	{
		_spriteRenderer.color = Color.black;
		_audioClip?.Play();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		ChangeColor();
	}
}