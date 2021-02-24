#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace ActionPreviewer
{
	public class SliderChange : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		ActionPreview preview;

		public void OnPointerDown( PointerEventData eventData )
		{
			preview.isDrag = true;
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			preview.isDrag = false;
		}

		private void Awake()
		{
			preview = GameObject.Find( "ActionPreviewManager" ).GetComponent<ActionPreview>();
		}
	}
}
#endif