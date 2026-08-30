using System;
using Fusion.Analyzer;
using UnityEngine;

namespace Fusion.Addons.Physics {
  [DefaultExecutionOrder(1000)]
  public class RunnerSimulatePhysics : SimulationBehaviour, IBeforeTick, ISpawned, IDespawned {
  
    /// <summary>
    /// Callback invoked prior to Simulate() being called.
    /// </summary>
    public event Action<NetworkRunner> OnBeforeSimulate;
    /// <summary>
    /// Callback invoked prior to Simulate() being called.
    /// </summary>
    public event Action<NetworkRunner> OnAfterSimulate;
    
    /// <summary>
    /// Returns true if physics has simulated for the current tick.
    /// </summary>
    public bool HasSimulatedThisTick { get; private set; }

    /// <summary>
    /// Timescale used to step physics.
    /// </summary>
    public float TimeScale {
      get => Time.timeScale;
      set {
        if (Runner == false || Runner.IsServer == false) return;
        _timeScale = value;
      }
    }
    
    /// <summary>
    /// Tracked number of started NetworkRunners. Used to determine when last Runner has stopped,
    /// and original Unity physics settings should be restored.
    /// </summary>
    private static int _enabledRunnersCount;
    
#if UNITY_2022_2_OR_NEWER
    private static SimulationMode _originalSimulationMode;
#else
    private static bool _originalAutoSimulation;
#endif
    
    private static SimulationMode2D _original2DSimulationMode;

    public bool Update2DPhysicsScene;
    public bool Update3DPhysicsScene;
    
    [SerializeField] private float _timeScale = 1f;

    private bool _initialized;

    void ISpawned.Spawned() {
      Startup();
    }

    void IDespawned.Despawned(NetworkRunner runner, bool hasState) {
      Shutdown();
    }

    void IBeforeTick.BeforeTick() {
      HasSimulatedThisTick = false;
    }

    /// <summary>
    /// Initialization code.
    /// </summary>
    private void Startup() {
      _initialized = true;
      _enabledRunnersCount++;
      
      // first runner, take over auto simulate
      if (_enabledRunnersCount == 1) {
#if UNITY_2022_2_OR_NEWER
        _originalSimulationMode            = UnityEngine.Physics.simulationMode;
        UnityEngine.Physics.simulationMode = SimulationMode.Script;
#else
        _originalAutoSimulation = UnityEngine.Physics.autoSimulation;
        UnityEngine.Physics.autoSimulation = false;
#endif
        
        _original2DSimulationMode = Physics2D.simulationMode;
        
        Physics2D.simulationMode = SimulationMode2D.Script;
      }
    }

    private void Shutdown() {
      if (!_initialized) return;

      _initialized = false;
      _enabledRunnersCount--;

      // last runner, restore auto simulate
      if (_enabledRunnersCount == 0) {
#if UNITY_2022_2_OR_NEWER
        UnityEngine.Physics.simulationMode = _originalSimulationMode;
#else
        UnityEngine.Physics.autoSimulation = _originalAutoSimulation;
#endif
        Physics2D.simulationMode = _original2DSimulationMode;
      }
    }

    public override void FixedUpdateNetwork() {
      
      // Update timescale
      if (Runner.TryGetPhysicsInfo(out NetworkPhysicsInfo info)) {
        if (Runner.IsServer) {
          info.TimeScale = _timeScale;
          Runner.TrySetPhysicsInfo(info);
        } else {
          _timeScale = info.TimeScale;
        }
      }
      
      var  deltaTime = Runner.DeltaTime * _timeScale;
      SimulationExecute(deltaTime);
    }
    
    private void SimulationExecute(float deltaTime) {
      if (deltaTime <= 0) {
        return;
      }

      DoSimulatePhysicsScene(deltaTime);
    }
    
    /// <summary>
    /// Executes the simulation of the physics scene and triggers the associated callback.
    /// </summary>
    private void DoSimulatePhysicsScene(float deltaTime) {
      OnBeforeSimulate?.Invoke(Runner);
      SimulatePhysicsScene(deltaTime);
      HasSimulatedThisTick = true;
      OnAfterSimulate?.Invoke(Runner);
    }

    // Simulate both 2D and 3D scenes if applicable.
    private void SimulatePhysicsScene(float deltaTime) {
      if (Update2DPhysicsScene && Runner.SceneManager.TryGetPhysicsScene2D(out var physicsScene2D)) {
        if (physicsScene2D.IsValid()) {
          physicsScene2D.Simulate(deltaTime);
        } else {
          Physics2D.Simulate(deltaTime);
        }
      }
      
      if (Update3DPhysicsScene && Runner.SceneManager.TryGetPhysicsScene3D(out var physicsScene3D)) {
        if (physicsScene3D.IsValid()) {
          physicsScene3D.Simulate(deltaTime);
        } else {
          UnityEngine.Physics.Simulate(deltaTime);
        }
      }
    }
  }
}