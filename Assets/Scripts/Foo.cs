using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace
{
	public class Foo : MonoBehaviour,IPointerClickHandler
	{
		private Vector3 startSize;
		private Color startColor;
		private Image spriteRenderer;
		public float sizeDiff;
		public Conductor.NoteValue note;
		public AudioSource audioClip;

		private void Start()
		{
			spriteRenderer = GetComponent<Image>();
			startColor = spriteRenderer.color;
			startSize = transform.localScale;
			Conductor.Instance.Register(note,Change);
		}

		private void Change()
		{
			transform.localScale =startSize* sizeDiff;
			//spriteRenderer.color = Color.white - startColor;
		}

		private void Update()
		{
			var size = Vector3.Lerp(transform.localScale, startSize, Time.deltaTime);
			transform.localScale = size;
			
			var color = Color.Lerp(spriteRenderer.color, startColor, Time.deltaTime*2f);
			spriteRenderer.color = color;
		}

		[ContextMenu("Change")]
		private void ChangeColor()
		{
			Conductor.Instance.Register(note,Bar,true);
		}
		
		private void Bar()
		{
			spriteRenderer.color = Color.black;
			audioClip?.Play();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			ChangeColor();
		}
	}
}