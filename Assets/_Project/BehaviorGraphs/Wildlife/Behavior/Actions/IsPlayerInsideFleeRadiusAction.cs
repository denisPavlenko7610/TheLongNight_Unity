using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Is Player Inside Flee Radius", story: "[Self] checks if player is inside flee radius", category: "Action", id: "ba12ab22423e82a0a152f74615f50720")]
public partial class IsPlayerInsideFleeRadiusAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || !Self.Value.TryGetComponent(out AnimalActor animal))
        {
            return Status.Failure;
        }

        return animal.IsPlayerInsideFleeRadius()
            ? Status.Success
            : Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}
