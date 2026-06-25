using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flee From Player", story: "[self] Flee From Player", category: "Action", id: "29f156255e6903be4306efc06f47c5d6")]
public partial class FleeFromPlayerAction : Action
{
	[SerializeReference] public BlackboardVariable<GameObject> Self;

	protected override Status OnStart()
	{
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Self == null || Self.Value == null || !Self.Value.TryGetComponent(out AnimalActor animal))
		{
			return Status.Failure;
		}

		animal.FleeFromPlayer();
		return Status.Success;
	}

	protected override void OnEnd() { }
}
