using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Is Player Inside Detection Radius", story: "[Self] checks if player is inside detection radius", category: "Action", id: "f22b53be549871864ab79d55f69d04f6")]
public partial class IsPlayerInsideDetectionRadiusAction : Action
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

        return animal.IsPlayerInsideDetectionRadius()
            ? Status.Success
            : Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}
