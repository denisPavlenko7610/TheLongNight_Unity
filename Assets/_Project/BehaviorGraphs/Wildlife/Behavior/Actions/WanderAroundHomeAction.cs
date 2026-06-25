using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wander Around Home", story: "[Self] wanders around home", category: "Action", id: "c9c9a649fe31f0c84a186afed1361efd")]
public partial class WanderAroundHomeAction : Action
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

		animal.WanderAroundHome();
		return Status.Success;
	}

	protected override void OnEnd() { }
}
