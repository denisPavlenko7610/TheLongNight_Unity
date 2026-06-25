using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase Player", story: "[Self] chases player", category: "Action", id: "04448045ac76db2dae1c55a14e9a99aa")]
public partial class ChasePlayerAction : Action
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

        animal.ChasePlayer();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
