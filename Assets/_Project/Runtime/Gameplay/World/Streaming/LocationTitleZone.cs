using TLN.Gameplay.Player;
using UnityEngine;
using VContainer;

namespace TLN.Gameplay.World.Streaming
{
	/// <summary>
	/// Shows the location name popup (The Long Dark style) when the player
	/// enters the zone. Place with a trigger BoxCollider sized over the POI.
	/// </summary>
	[RequireComponent(typeof(BoxCollider))]
	public sealed class LocationTitleZone : MonoBehaviour
	{
		[SerializeField] private string _titleKey;

		private ILocationTitleService _titleService;

		[Inject]
		public void Construct(ILocationTitleService titleService)
		{
			_titleService = titleService;
		}

		private void Reset()
		{
			BoxCollider collider = GetComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = new Vector3(40f, 20f, 40f);
			collider.center = new Vector3(0f, 8f, 0f);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (_titleService == null ||
			    string.IsNullOrWhiteSpace(_titleKey) ||
			    other.GetComponentInParent<PlayerRoot>() == null)
			{
				return;
			}

			_titleService.Show(Application.Localization.Loc.LocationTitle(_titleKey));
		}
	}
}
