using UnityEngine;
using UnityEngine.UIElements;

namespace TLN.UI.Common
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class UIDocumentOrder : MonoBehaviour
	{
		[SerializeField] private UILayer _layer = UILayer.HUD;
		[SerializeField] private int _offset;

		private void Awake()
		{
			UIDocument document = GetComponent<UIDocument>();
			document.sortingOrder = (int)_layer + _offset;
		}
	}
}
