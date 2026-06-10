using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TLN.Bootstrap
{
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-4500)]
	public sealed class SceneInjectable : MonoBehaviour
	{
		private bool _wasInjected;

		internal bool HasParentInjectable
		{
			get
			{
				Transform parent = transform.parent;

				while (parent != null)
				{
					if (parent.TryGetComponent(out SceneInjectable _))
					{
						return true;
					}

					parent = parent.parent;
				}

				return false;
			}
		}

		private void Awake()
		{
			if (_wasInjected || HasParentInjectable)
			{
				return;
			}

			LifetimeScope lifetimeScope = LifetimeScope.Find<LifetimeScope>(gameObject.scene);

			if (lifetimeScope == null)
			{
				Debug.LogError($"Cannot scene-inject '{name}' because no LifetimeScope was found in the scene.", this);
				return;
			}

			if (lifetimeScope.Container == null)
			{
				Debug.LogError($"Cannot scene-inject '{name}' because the LifetimeScope container is not built yet.", this);
				return;
			}

			InjectHierarchy(lifetimeScope.Container);
		}

		internal void InjectHierarchy(IObjectResolver resolver)
		{
			if (_wasInjected)
			{
				return;
			}

			if (resolver == null)
			{
				Debug.LogError($"Cannot scene-inject '{name}' because resolver is missing.", this);
				return;
			}

			resolver.InjectGameObject(gameObject);
			MarkHierarchyInjected();
		}

		private void MarkHierarchyInjected()
		{
			SceneInjectable[] injectables = GetComponentsInChildren<SceneInjectable>(true);

			for (int i = 0; i < injectables.Length; i++)
			{
				SceneInjectable injectable = injectables[i];

				if (injectable == null)
				{
					continue;
				}

				injectable._wasInjected = true;
			}
		}
	}
}
