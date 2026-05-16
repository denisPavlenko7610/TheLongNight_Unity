using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public readonly struct InteractionHit
	{
		public IInteractable Interactable { get; }
		public Collider Collider { get; }
		public Vector3 Point { get; }
		public Vector3 Normal { get; }
		public float Distance { get; }

		public InteractionHit(IInteractable interactable, Collider collider, Vector3 point, Vector3 normal, float distance)
		{
			Interactable = interactable;
			Collider = collider;
			Point = point;
			Normal = normal;
			Distance = distance;
		}
	}
}
