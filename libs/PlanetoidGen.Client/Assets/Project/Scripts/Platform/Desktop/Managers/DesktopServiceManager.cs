using PlanetoidGen.Client.BusinessLogic.Managers;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Implementations;
using PlanetoidGen.Client.Platform.Desktop.Services.Controllers;
using UnityEditor;
using UnityEngine;

namespace PlanetoidGen.Client.Platform.Desktop.Managers
{
    public class DesktopServiceManager : SingletonManager<DesktopServiceManager>
    {
        protected override void OnAwakeInvoked()
        {
            ServiceManager.Instance.AddSingletonService<IConnectionContext, ConnectionContext>();
            ServiceManager.Instance.AddSingletonService<IPlanetoidController, PlanetoidController>();
            ServiceManager.Instance.AddSingletonService<IAgentController, AgentController>();
            ServiceManager.Instance.AddSingletonService<IBinaryContentController, BinaryContentController>();
            ServiceManager.Instance.AddSingletonService<IBinaryContentStreamController, BinaryContentStreamController>();
            ServiceManager.Instance.AddSingletonService<IGenerationLODController, GenerationLODController>();
            ServiceManager.Instance.AddSingletonService<ITileGenerationStreamController, TileGenerationStreamController>();
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            if (options.HasFlag(EnterPlayModeOptions.DisableDomainReload))
            {
                OnDomainReload();
            }
        }
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnRuntimeLoad()
        {
            OnDomainReload();
        }
#endif

        static void OnDomainReload()
        {
            Debug.Log($"On domain reload in {nameof(DesktopServiceManager)}.");
            Instance = null;
        }
    }
}
