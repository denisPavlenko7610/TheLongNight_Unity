using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusion.Addons.Physics {
  [DisallowMultipleComponent]
  [NetworkBehaviourWeaved(WORDS)]
  public partial class NetworkRigidbody : NetworkTRSP, INetworkTRSPTeleport, IBeforeAllTicks, IAfterTick, IAfterClientPredictionReset {
    const int WORDS = NetworkTRSPData.WORDS + NetworkPhysicsData.WORDS;

    /// <summary>
    /// Enables synchronization of Scale. Keep this disabled if you are not altering the scale of this transform, to reduce CPU utilization.
    /// </summary>
    [InlineHelp] [SerializeField] public bool SyncScale;

    /// <summary>
    /// Enables synchronization of Parent. Keep this disabled if you are not altering the parent of this transform, to reduce CPU utilization.
    /// </summary>
    [InlineHelp] [SerializeField] public bool SyncParent = true;

    /// <summary>
    /// Get the associated Rigidbody or Rigidbody2D position value.
    /// </summary>
    public Vector3 RBPosition => _physicsBody.Position;

    /// <summary>
    /// Get the associated Rigidbody or Rigidbody2D rotation value.
    /// </summary>
    public Quaternion RBRotation => _physicsBody.Rotation;

    /// <summary>
    /// Get the associated Rigidbody or Rigidbody2D isKinematic bool value.
    /// </summary>
    public bool RBIsKinematic => _physicsBody.Kinematic;
    
    /// <summary>
    /// Defined at <see cref="Spawned"/> based on the detected rigidbody.
    /// </summary>
    public bool Is3D { get; private set; }
    
    /// <summary>
    /// The abstract physics body. This class encapsulates most basic interactions with Rigidbody or Rigidbody2D.
    /// </summary>
    public AbstractPhysicsBody PhysicsBody => _physicsBody;

    /// <summary>
    /// Get/Set the Transform (typically a child of the Rigidbody root transform) which will be moved in interpolation.
    /// When set to null, the Rigidbody Transform will be used.
    /// </summary>
    public Transform InterpolationTarget {
      get => _interpolationTarget;
      set => SetInterpolationTarget(value);
    }

    /// <summary>
    /// Change the Transform (typically a child of the Rigidbody root transform) which will be moved in interpolation.
    /// When set to null, the Rigidbody Transform will be used.
    /// </summary>
    public void SetInterpolationTarget(Transform target) {
      if (target == null || target == transform) {
        _interpolationTarget = null;
        _targIsDirtyFromInterpolation = false;
      }
      else {
#if UNITY_EDITOR
        var c = target.GetComponentInChildren<Collider>();
        if (c && c.enabled) {
          Debug.LogWarning(
            $"Assigned Interpolation Target '{target.name}' on GameObject '{name}' contains a non-trigger collider, this may not be intended as interpolation may break physics caching, and prevent the Rigidbody from sleeping");
        }
#endif
        _interpolationTarget = target;
      }
    }

    /// <summary>
    /// Designate a render-only (non-physics) target Transform for all interpolation.
    /// </summary>
    [InlineHelp] [SerializeField] private Transform _interpolationTarget;

    /// <summary>
    /// Dirty flag for the root Transform.
    /// True when interpolation has altered the root transform's position, rotation, or scale in Render().
    /// Is reset to false when the transform is restored to its networked state during the simulation loop.
    /// </summary>
    private bool _rootIsDirtyFromInterpolation;

    /// <summary>
    /// Dirty flag for the Interpolation Target.
    /// True when interpolation has altered the position, rotation, or scale in Render().
    /// Is reset to false when the transform is restored to defaults during the simulation loop.
    /// </summary>
    private bool _targIsDirtyFromInterpolation;

    private ref NetworkTRSPData _transformData => ref State;
    private ref NetworkPhysicsData _physicsData => ref ReinterpretState<NetworkPhysicsData>(NetworkTRSPData.WORDS);

    private AbstractPhysicsBody _physicsBody;
    private Transform _transform;
    private bool _aoiEnabled;
    private int _remainingSimulationsCount;

    public override void Spawned() {
      base.Spawned();
      if (SetupPhysicsBody() == false) return;
      _aoiEnabled = Runner.Config.Simulation.AreaOfInterestEnabled;
      // Don't interpolate on Dedicated Server
      _doNotInterpolate = Runner.Mode == SimulationModes.Server;
      _transform = transform;
      Runner.SetIsSimulated(Object, true); // NetworkRigidbody is always simulated.
      
      // This object could already exist on shared mode, just out of interest. So we need to check if it has snapshots.
      if (Object.HasStateAuthority && Object.LastReceiveTick == default) {
        CopyToBuffer();
      } else {
        CopyToEngine(forceAwake: true);
      }
    }

    /// <returns>If the setup was successful.</returns>
    private bool SetupPhysicsBody() {
      // The 2.1 physics addon should not be used in shared mode.
      if (Runner.Topology == Topologies.Shared) {
        Debug.LogWarning($"{GetType().Name} should not be used in shared mode. Despawning {gameObject.name}.");
        Runner.Despawn(Object);
        return false;
      }
      
      if (TryGetComponent<Rigidbody>(out var rb3D)) {
        _physicsBody = new PhysicsBody3D(rb3D);
        Is3D = true;
        if (Runner.TryGetComponent<RunnerSimulatePhysics>(out var simulatePhysics) == false) {
          simulatePhysics = Runner.gameObject.AddComponent<RunnerSimulatePhysics>();
          Runner.AddGlobal(simulatePhysics);
        }
        
        simulatePhysics.Update3DPhysicsScene = true;
      }
      else if (TryGetComponent<Rigidbody2D>(out var rb2D)) {
        _physicsBody = new PhysicsBody2D(rb2D);
        Is3D = false;
        if (Runner.TryGetComponent<RunnerSimulatePhysics>(out var simulatePhysics) == false) {
          simulatePhysics = Runner.gameObject.AddComponent<RunnerSimulatePhysics>();
          Runner.AddGlobal(simulatePhysics);
        }
        
        simulatePhysics.Update2DPhysicsScene = true;
      } else {
        Debug.LogWarning($"{GetType().Name} requires a {nameof(Rigidbody)} or {nameof(Rigidbody2D)} component attached to the same object. Despawning {gameObject.name}.");
        Runner.Despawn(Object);
        return false;
      }

      return true;
    }

    public void Teleport(Vector3? position = null, Quaternion? rotation = null) {
      if (position.HasValue) {
        _transform.position    = position.Value;
        _transformData.Position = position.Value;
        _physicsBody.Position = position.Value;
      }
      
      if (rotation.HasValue) {
        _transform.rotation    = rotation.Value;
        _transformData.Rotation = rotation.Value;
        _physicsBody.Rotation = rotation.Value;
      }

      ++_transformData.TeleportKey;
    }

    void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount) {
      // Capture the number of ticks about to be simulated.
      // We use this in AfterTick() to limit capturing state to only the last tick.
      _remainingSimulationsCount = tickCount;

      // Recenter the interpolation target.
      if (_targIsDirtyFromInterpolation && _interpolationTarget) {
        _interpolationTarget.localPosition = default;
        _interpolationTarget.localRotation = Quaternion.identity;
        if (SyncScale) {
          _interpolationTarget.localScale = Vector3.one;
        }
      }

      // A dirty root should always reset at the start of the simulation loop (for both state authority and predicted).
      // Predicted objects should always reset at the start of re-simulation.
      if (_rootIsDirtyFromInterpolation || resimulation) {
        CopyToEngine();
      }
    }

    void IAfterTick.AfterTick() {
      // Never capture more than the last two ticks of a complete simulation loop
      // Interpolation will only ever need the last two.
      // StateAuthority will only need the last one fully captured.
      int remainingTicks = _remainingSimulationsCount--;
      if (remainingTicks > 2) {
        return;
      }

      CopyToBuffer();
    }

    public void AfterClientPredictionReset() {
      CopyToEngine(forceAwake: false);
    }
  }

  // Copy partial
  public partial class NetworkRigidbody : IInterestEnter {
    private void CaptureExtras(ref NetworkPhysicsData data) {
      data.LinearVelocity = _physicsBody.LinearVelocity;
      data.AngularVelocity = _physicsBody.AngularVelocity;
    }

    private void ApplyExtras(ref NetworkPhysicsData data) {
      _physicsBody.LinearVelocity = data.LinearVelocity;
      _physicsBody.AngularVelocity = data.AngularVelocity;
      _physicsBody.EncodedConstraints = data.Constraints;
    }

    /// <summary>
    /// Copies the state of the Rigidbody to the Fusion state.
    /// </summary>
    protected virtual void CopyToBuffer() {
      var tr = _transform;
      var syncParent = SyncParent;
      var useWorldSpace = !syncParent;

      // Capture Parenting and handle auto AOI override
      if (syncParent) {
        // Parenting handling only applies to the MainTRSP (the NO root NetworkTRSP).
        if (IsMainTRSP) {
          // If Sync Parent is enabled, set any nested parent NetworkObject as the AreaOfInterestOverride.
          // Player Interest in this object will always be determined by player interest in the current parent NO.
          var parent = tr.parent;

          // If no parent transform is present, this is simple.
          if (parent == null) {
            _transformData.AreaOfInterestOverride = default;
            _transformData.Parent = default;
          }
          // If there is a parent transform, we need to determine if it is a valid NB or a non-networked transform.
          else {
            if (parent.TryGetComponent<NetworkBehaviour>(out var parentNB)) {
              if (_aoiEnabled) {
                SetAreaOfInterestOverride(parentNB.Object);
              }

              _transformData.Parent = parentNB;
            }
            else {
              _transformData.AreaOfInterestOverride = default;
              _transformData.Parent = NetworkTRSPData.NonNetworkedParent;
              useWorldSpace = true;
            }
          }
        }
        else {
          // Reset to default in case SyncParent was enabled/disabled at runtime
          _transformData.AreaOfInterestOverride = default;
        }
      }

      // always send world space if using interpolation target.
      useWorldSpace |= _interpolationTarget;
      var position = useWorldSpace ? _transform.position : _transform.localPosition;
      var rotation = useWorldSpace ? _transform.rotation : _transform.localRotation;

      _transformData.Position = position;
      _transformData.Rotation = rotation;


      // Capture RB State
      if (_physicsBody.Kinematic == false) {
        CaptureExtras(ref _physicsData);
      }

      // sync flags and constraints kinematic or not.
      _physicsData.FlagsAndConstraints = (_physicsBody.Flags, _physicsBody.EncodedConstraints);

      if (SyncScale) {
        _transformData.Scale = tr.localScale;
      }
    }

    /// <summary>
    /// Copies the Fusion snapshot state onto the Rigidbody.
    /// </summary>
    protected virtual void CopyToEngine(bool forceAwake = false) {
      // Spawned not called yet.
      if (_physicsBody == null) {
        return;
      }
      
      var (flags, constraints) = _physicsData.FlagsAndConstraints;
      var tr = _transform;
      var syncParent = SyncParent;
      var networkedIsSleeping = (flags & Fusion.NetworkRigidbodyFlags.IsSleeping) != 0;
      var networkedIsKinematic = (flags & Fusion.NetworkRigidbodyFlags.IsKinematic) != 0;
      var currentIsSleeping = _physicsBody.Sleeping;
      var useWorldSpace = syncParent == false;

      // If sleeping states disagree, we need to intervene.
      if (currentIsSleeping != networkedIsSleeping) {
        _physicsBody.Sleeping = networkedIsSleeping switch {
          // networked is sleeping and bellow thresholds => sleep.
          true when IsRigidbodyBelowSleepingThresholds(_physicsBody) => true,
          // networked is sleeping and bellow thresholds => sleep.
          false when IsRigidbodyBelowSleepingThresholds(_physicsBody) == false => false,
          // default does nothing.
          _ => _physicsBody.Sleeping
        };

        currentIsSleeping = _physicsBody.Sleeping;
      }

      if (syncParent) {
        var currentParent = tr.parent;
        if (_transformData.Parent != default) {
          bool frHasNonNetworkedParent = _transformData.Parent == NetworkTRSPData.NonNetworkedParent;
          useWorldSpace = frHasNonNetworkedParent;
          if (Runner.TryFindBehaviour(_transformData.Parent, out var found)) {
            var foundTransform = found.transform;
            if (ReferenceEquals(foundTransform, currentParent) == false) {
              tr.SetParent(foundTransform);
              if (_interpolationTarget) {
                _interpolationTarget.localPosition = default;
                _interpolationTarget.localRotation = Quaternion.identity;
              }
            }
          }
          else if (frHasNonNetworkedParent == false) {
            Debug.LogError($"Cannot find parent NetworkBehaviour.");
          }
        }
        else {
          // TRSPData indicates no parenting
          if (currentParent) {
            tr.SetParent(null);
          }
        }
      }

      var pos = _transformData.Position;
      var rot = _transformData.Rotation;

      // When using interpolation target, position is in world space.
      if (_interpolationTarget) {
        useWorldSpace = true;
      }

      // Both local and networked state are sleeping and in agreement - avoid waking the RB locally.
      bool avoidWaking = currentIsSleeping && networkedIsSleeping;

      if (avoidWaking == false || forceAwake) {
        SetPosRotToTransform(tr, pos, rot, useWorldSpace);
        _physicsBody.Position = tr.position;
        _physicsBody.Rotation = tr.rotation;
        _rootIsDirtyFromInterpolation = false;
      }

      if (SyncScale) {
        tr.localScale = _transformData.Scale;
      }

      // If the RB's kinematic state was changed as part of re-simulation, change it back.
      if (networkedIsKinematic != _physicsBody.Kinematic) {
        _physicsBody.Kinematic = networkedIsKinematic;
      }

      // These are only applied for non-Kinematics, as this is waste of work for kinematic proxies.
      if (networkedIsKinematic == false && avoidWaking == false) {
        ApplyExtras(ref _physicsData);
      }
    }

    void IInterestEnter.InterestEnter(PlayerRef player) {
      CopyToEngine(true);
    }
  }

  // Render partial
  public partial class NetworkRigidbody {
    private bool _doNotInterpolate;
    private int _lastRenderTeleportKey;
    
    // Used to store the interpolating TO tick when a teleport is detected and know if subsequent render calls are
    // still being called before the teleport tick was reached by interpolation.
    private Tick? _teleportDetectTick;
    
    /// <summary>
    /// Returns true if the passed Rigidbody/Rigidbody2D velocity energies are below the sleep threshold.
    /// </summary>
    private bool IsRigidbodyBelowSleepingThresholds(AbstractPhysicsBody physicsBody) {
      if (Is3D) {
        float sqrMag = physicsBody.LinearVelocity.sqrMagnitude;
        var energy = physicsBody.Mass * sqrMag;
        var angVel = physicsBody.AngularVelocity;
        var inertia = physicsBody.InertiaTensor;

        energy += inertia.x * (angVel.x * angVel.x);
        energy += inertia.y * (angVel.y * angVel.y);
        energy += inertia.z * (angVel.z * angVel.z);

        // Mass-normalized
        energy /= 2.0f * physicsBody.Mass;

        return energy <= UnityEngine.Physics.sleepThreshold;
      }

      // 2D Handling

      float sqrMag2D = ((Vector2)physicsBody.LinearVelocity).sqrMagnitude;
      if (sqrMag2D > Physics2D.linearSleepTolerance * Physics2D.linearSleepTolerance) {
        return false;
      }

      // Angular threshold
      var angularVel = physicsBody.AngularVelocity.z;
      return angularVel * angularVel <= Physics2D.angularSleepTolerance * Physics2D.angularSleepTolerance;
    }

    /// <summary>
    /// Returns true if the passed NetworkPhysicsData velocity energies are below the sleep threshold.
    /// </summary>
    private bool IsStateBelowSleepingThresholds(AbstractPhysicsBody physicsBody, NetworkPhysicsData data) {
      if (Is3D) {
        var mass = physicsBody.Mass;
        var energy = mass * ((Vector3)data.LinearVelocity).sqrMagnitude;
        var angVel = ((Vector3)data.AngularVelocity);
        var inertia = _physicsBody.InertiaTensor;

        energy += inertia.x * (angVel.x * angVel.x);
        energy += inertia.y * (angVel.y * angVel.y);
        energy += inertia.z * (angVel.z * angVel.z);

        // Mass-normalized
        energy /= 2.0f * mass;

        return energy <= UnityEngine.Physics.sleepThreshold;
      }

      // 2D Handling
      // Linear threshold
      if (((Vector2)data.LinearVelocity).sqrMagnitude > Physics2D.linearSleepTolerance * Physics2D.linearSleepTolerance) {
        return false;
      }

      // Angular threshold
      var angularVel = data.AngularVelocity.Z;
      var result = angularVel * angularVel <= Physics2D.angularSleepTolerance * Physics2D.angularSleepTolerance;
      return result;
    }

    public override void Render() {
      // Specifically flagged to not interpolate for cached reasons (ie for Server (non-Host))
      // Do not interpolate if Object setting indicates not to.
      if (_doNotInterpolate || Object.RenderSource == RenderSource.Latest) {
        return;
      }

      var it = _interpolationTarget;
      var hasInterpolationTarget = it != false;

      if (TryGetSnapshotsBuffers(out var fr, out var to, out var alpha)) {
        var frTRSPData = fr.ReinterpretState<NetworkTRSPData>();
        var toTRSPData = to.ReinterpretState<NetworkTRSPData>();

        var toKey = toTRSPData.TeleportKey;
        var frKey = frTRSPData.TeleportKey;
        var syncScale = SyncScale;

        var syncParent = SyncParent;
        var predictingTeleport = toKey != frKey;
        var readingTeleport = _lastRenderTeleportKey != toKey;
        var teleport =  predictingTeleport || readingTeleport;
        var useWorldSpace = SyncParent == false;
        
        // Teleport Handling - Don't interpolate through teleports
        if (teleport) {
          _teleportDetectTick = to.Tick;
        }else if (_teleportDetectTick.HasValue && to.Tick > _teleportDetectTick.Value) {
          _teleportDetectTick = null;
        }
        
        if (_teleportDetectTick.HasValue && to.Tick <= _teleportDetectTick.Value) {
          // If predicting a teleport render it immediately. If not keep rendering FROM until the teleport tick is reached.
          if (predictingTeleport) {
            frTRSPData = toTRSPData;
          } else {
            toTRSPData = frTRSPData;
          }
        }

        
        // Clients that are not predicting the teleport key will not have fromKey != toKey, detect the change using render value.
        _lastRenderTeleportKey = toKey;
        
        // cache the from values for position and rotation as these will almost certainly be needed below.
        var frPosition = frTRSPData.Position;
        var frRotation = frTRSPData.Rotation;
        var toPosition = toTRSPData.Position;
        var toRotation = toTRSPData.Rotation;

        // Parenting specific handling
        if (syncParent) {
          var currentParent = _transform.parent;

          // If the parent is a non-null... (either valid or Non-Networked)
          if (frTRSPData.Parent != default) {
            bool frHasNonNetworkedParent = frTRSPData.Parent == NetworkTRSPData.NonNetworkedParent;
            useWorldSpace = frHasNonNetworkedParent;

            if (Runner.TryFindBehaviour(frTRSPData.Parent, out var found)) {
              var foundParent = found.transform;
              // Set the parent if it currently is not correct, before moving
              if (currentParent != foundParent) {
                _transform.SetParent(foundParent);
                _rootIsDirtyFromInterpolation = true;
              }
            }
            else if (frHasNonNetworkedParent == false) {
              Debug.LogError($"Parent of this object is not present {frTRSPData.Parent} {frTRSPData.Parent.Behaviour}.");
              return;
            }

            // If the parent changes between From and To ... do no try to interpolate (different spaces)
            // We also are skipping sleep detection and teleport testing.
            if (frTRSPData.Parent != toTRSPData.Parent) {
              if (hasInterpolationTarget) {
                SetPosRotToTransform(it, toPosition, toRotation, true);
                _targIsDirtyFromInterpolation = true;
              }
              else {
                SetPosRotToTransform(_transform, toPosition, toRotation, useWorldSpace);
                _rootIsDirtyFromInterpolation = true;
              }

              if (syncScale) {
                _transform.localScale = toTRSPData.Scale;
              }

              return;
            }
          }
          else {
            // else the parent is null
            if (currentParent != null) {
              _transform.SetParent(null);
              _rootIsDirtyFromInterpolation = true;
            }

            // If the parent changes between From and To ... do no try to interpolate (different spaces)
            if (frTRSPData.Parent != toTRSPData.Parent) {
              if (hasInterpolationTarget) {
                SetPosRotToTransform(it, toPosition, toRotation, true);
                _targIsDirtyFromInterpolation = true;
              }
              else {
                SetPosRotToTransform(_transform, toPosition, toRotation, useWorldSpace);
                _rootIsDirtyFromInterpolation = true;
              }
              
              if (syncScale) {
                _transform.localScale = toTRSPData.Scale;
              }

              return;
            }
          }
        }

        // Check thresholds to see if this object is coming to a rest, and stop interpolation to allow for sleep to occur.
        if (IsStateBelowSleepingThresholds(_physicsBody, _physicsData)) {
          return;
        }
        
        var pos = Vector3.Lerp(frPosition, toPosition, alpha);
        var rot = Quaternion.Slerp(frRotation, toRotation, alpha);

        // If we are using the interpolation target, just move the root of the target. No scaling (they are invalid).
        if (hasInterpolationTarget) {
          SetPosRotToTransform(it, pos, rot, true);
          // SyncScale when using interpolation targets is always suspect, but we are allowing it here in case the dev has done things correctly.
          if (syncScale) {
            var scl = Vector3.Lerp(frTRSPData.Scale, toTRSPData.Scale, alpha);
            it.localScale = scl;
          }

          _targIsDirtyFromInterpolation = true;
        }
        // else (no interpolation target set) we are moving the transform itself and not the interp target.
        else {
          var scl = syncScale ? Vector3.Lerp(frTRSPData.Scale, toTRSPData.Scale, alpha) : default;

          SetPosRotToTransform(_transform, pos, rot, useWorldSpace);

          if (syncScale) {
            transform.localScale = scl;
          }

          _rootIsDirtyFromInterpolation = true;
        }
      }
      else {
        Debug.LogWarning($"No interpolation data");
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPosRotToTransform(Transform t, Vector3 pos, Quaternion rot, bool worldSpace) {
      if (worldSpace) {
        t.position = pos;
        t.rotation = rot;
      }
      else {
        t.localPosition = pos;
        t.localRotation = rot;
      }
    }
  }
}