using DotSpatial.Projections.Transforms;
using DotSpatial.Projections;
using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Client.Contracts.ScriptableObjects.Storytelling;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using UnityEngine;
using PlanetoidGen.Domain.Enums;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace PlanetoidGen.Client.Tests.Behaviors
{
    public enum ProjectionRelativeType
    {
        Service = 0,
        Relative = 1,
        Simplified = 2,
        Cubic = 3,
    }

    [ExecuteInEditMode()]
    public class ProjectionDebugger : MonoBehaviour
    {
        public StorySO story;
        private CoordinateMappingService coordinateMapping;
        private GeometryConversionService geometryConversion;
        private PlanetoidInfoModel planetoid;
        private SpatialReferenceSystemModel srsGeographic, srsProjected;
        private bool init = false;

        public ProjectionRelativeType selectedFunctionType = ProjectionRelativeType.Relative;
        public bool drawBoundingBoxes;
        public bool drawRelativeTiles;
        public List<RelativeTileDirectionType> relativeTiles = new()
        {
            RelativeTileDirectionType.Right,
            RelativeTileDirectionType.Left,
            RelativeTileDirectionType.Up,
            RelativeTileDirectionType.Down,
        };

        public CoordinateMappingService CoordinateMapping
        {
            get
            {
                if (coordinateMapping == null && init)
                {
                    init = false;
                    Init();
                }

                return coordinateMapping;
            }
        }

        public GeometryConversionService GeometryConversion
        {
            get
            {
                if (geometryConversion == null && init)
                {
                    init = false;
                    Init();
                }

                return geometryConversion;
            }
        }

        public PlanetoidInfoModel Planetoid
        {
            get
            {
                if (planetoid == null && init)
                {
                    init = false;
                    Init();
                }

                return planetoid;
            }
        }

        public SpatialReferenceSystemModel SrsGeographic
        {
            get
            {
                if (srsGeographic == null && init)
                {
                    init = false;
                    Init();
                }

                return srsGeographic;
            }
        }

        public SpatialReferenceSystemModel SrsProjected
        {
            get
            {
                if (srsProjected == null && init)
                {
                    init = false;
                    Init();
                }

                return srsProjected;
            }
        }

        private void Init()
        {
            if (init)
            {
                return;
            }

            coordinateMapping = new CoordinateMappingService(new ProjCubeProjectionService());
            geometryConversion = new GeometryConversionService(null);
            planetoid = new PlanetoidInfoModel(0, "Earth", 42, 6_371_000);

            var WGS1984 = ProjectionInfo.FromProj4String("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs ");
            var WebMercator = ProjectionInfo.FromProj4String("+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0.0 +k=1.0 +units=m +nadgrids=@null +no_defs ");
            WebMercator.Transform = new MercatorAuxiliarySphere();
            WebMercator.ScaleFactor = 1.0;
            WebMercator.AuxiliarySphereType = AuxiliarySphereType.SemimajorAxis;
            WebMercator.GeographicInfo.Datum.Spheroid = new Spheroid(WebMercator.GeographicInfo.Datum.Spheroid.EquatorialRadius);
            WebMercator.Transform.Init(WebMercator);

            srsGeographic = new SpatialReferenceSystemModel(4236, "EPSG", 4236, WGS1984.ToEsriString(), WGS1984.ToProj4String());
            srsProjected = new SpatialReferenceSystemModel(3857, "EPSG", 3857, WebMercator.ToEsriString(), WebMercator.ToProj4String());

            init = true;
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            Init();
        }
    }
}
#endif
