using Microsoft.Extensions.DependencyInjection;
using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Client.BusinessLogic.Services.Loaders;
using PlanetoidGen.Client.Contracts.Services.Loaders;
using PlanetoidGen.Client.Contracts.Services.Procedural;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Services.Generation;
using System;
using UnityEditor;
using UnityEngine;

namespace PlanetoidGen.Client.BusinessLogic.Managers
{
    public class ServiceManager : SingletonManager<ServiceManager>
    {
        [SerializeField]
        private IServiceProvider _serviceProvider;

        [SerializeField]
        private IServiceCollection _services;

        private bool _enabled;

        public T GetService<T>()
        {
            if (!_enabled || _serviceProvider == null)
            {
                return default;
            }

            return _serviceProvider.GetService<T>() ?? default;
        }

        public bool AddSingletonService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            if (_enabled)
            {
                return false;
            }

            _services.AddSingleton<TService, TImplementation>();

            return true;
        }

        protected override void Initialization()
        {
            _services = new ServiceCollection()
                .AddSingleton<ICubeProjectionService, ProjCubeProjectionService>()
                .AddSingleton<ICoordinateMappingService, CoordinateMappingService>()
                .AddSingleton<IGeometryConversionService, GeometryConversionService>()
                .AddSingleton<ITextureLoadingService, TextureLoadingService>()
                .AddSingleton<ISphericalTileService, SphericalTileService>()
                .AddSingleton<IPlanarTileService, PlanarTileService>()
                .AddSingleton<ISpatialReferenceSystemRepository, SpatialReferenceSystemStubRepository>();

            _enabled = false;
        }

        private void OnEnable()
        {
            if (_services == null)
            {
                // Happens during hot reload in play mode. We try to at least partially recover from it.
                Initialization();
            }

            _enabled = true;
            _serviceProvider = _services.BuildServiceProvider();
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
            Debug.Log($"On domain reload in {nameof(ServiceManager)}.");
            Instance = null;
        }
    }
}
