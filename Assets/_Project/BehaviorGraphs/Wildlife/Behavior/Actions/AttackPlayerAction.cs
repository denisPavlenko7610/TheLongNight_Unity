using System;
using TLN.Gameplay.Wildlife;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack Player", story: "[Self] attacks player", category: "Action", id: "0a56abe77f723f4b375d9ed3199f2476")]
public partial class AttackPlayerAction : Action
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

        animal.AttackPlayer();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
