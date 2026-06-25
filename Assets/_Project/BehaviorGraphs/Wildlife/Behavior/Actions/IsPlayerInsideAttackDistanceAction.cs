using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Is Player Inside Attack Distance", story: "[self] checks if player is inside attack distance", category: "Action", id: "04c60f96512a7ea0aaef38bfe2fe7315")]
public partial class IsPlayerInsideAttackDistanceAction : Action
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

        return animal.IsPlayerInsideAttackDistance()
            ? Status.Success
            : Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}
