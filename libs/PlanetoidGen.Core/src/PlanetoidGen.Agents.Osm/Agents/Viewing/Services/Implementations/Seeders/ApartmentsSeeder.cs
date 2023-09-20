using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Agents.Standard.Helpers;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Seeders
{
    internal class ApartmentsSeeder : IBuildingSeeder
    {
        private const double LengthMax = 4.0;

        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly string _title;

        public ApartmentsSeeder(ILogger logger, JsonSerializerOptions serializerOptions, string title)
        {
            _logger = logger;
            _serializerOptions = serializerOptions;
            _title = title;
        }

        public BuildingEntity ProcessBuilding(
            BuildingInformationSeedingAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingEntity entity,
            Litdex.Random.Random random)
        {
            var levels = entity.Levels;
            var desc = entity.DetailedDescription;
            var buildingMaterial = entity.Material;
            var roofKind = entity.RoofKind;
            var roofMaterial = entity.RoofMaterial;

            if (levels == null)
            {
                var level = random.NextInt(0, 20);
                if (level < 6) levels = 4;
                else if (level < 12) levels = 5;
                else if (level < 18) levels = 9;
                else levels = 20;
            }
            else if (levels == 0)
            {
                levels = 1;
            }

            if (desc == null)
            {
                var multiPolygon = entity.Path;
                var sb = new StringBuilder();

                var floorHeight = options.DefaultFloorHeight;

                var buildings = new List<BuildingModel>();

                foreach (var geom in multiPolygon)
                {
                    if (geom is Polygon polygon)
                    {
                        var ring = polygon.Shell;

                        if (ring.IsRing)
                        {
                            var coordArray = ring.Coordinates; // Last == First
                            var lengths = new List<double>();
                            var sideCount = coordArray.Length - 1;

                            for (var i = 0; i < sideCount; ++i)
                            {
                                var start = coordArray[i];
                                var end = coordArray[i + 1];

                                // Distance on a sphere
                                var length = MathHelpers.HaversineDistance(
                                    start.Y / 180.0 * Math.PI,
                                    start.X / 180.0 * Math.PI,
                                    end.Y / 180.0 * Math.PI,
                                    end.X / 180.0 * Math.PI,
                                    planetoid.Radius);

                                lengths.Add(length);
                            }

                            var floors = new List<LevelModel>();
                            var doorSide = random.NextInt(0, sideCount);
                            var doorIndex = random.NextInt(0, Math.Max((int)Math.Floor(lengths[doorSide] / LengthMax), 1));

                            for (var j = 0; j < levels.Value; ++j)
                            {
                                var floorSides = new List<SurfaceSideModel>();

                                for (var i = 0; i < sideCount; ++i)
                                {
                                    var parts = Enumerable
                                        .Range(0, (int)Math.Floor(lengths[i] / LengthMax))
                                        .Select(y =>
                                        {
                                            int partRnd = random.NextInt(0, j == 0 && levels! < 3 ? 2 : 3);

                                            var part = GetSurfacePartModel(partRnd, out var w);
                                            return part;
                                        })
                                        .ToList();

                                    if (parts.Count == 0)
                                    {
                                        parts = new List<SurfacePartModel>()
                                        {
                                            new SurfacePartModel()
                                            {
                                                Kind = LevelSidePartKindValues.PartWall,
                                                Width = lengths[i],
                                            }
                                        };
                                    }

                                    if (parts.Count == 1)
                                    {
                                        parts[0].Width = lengths[i];
                                    }
                                    else
                                    {
                                        parts[^1].Width += lengths[i] - parts.Count * LengthMax;
                                    }

                                    if (i == doorSide)
                                    {
                                        if (j == 0)
                                        {
                                            parts[doorIndex].Kind = LevelSidePartKindValues.PartPorch;
                                        }
                                        else
                                        {
                                            parts[doorIndex].Kind = LevelSidePartKindValues.PartWindow;
                                        }
                                    }

                                    floorSides.Add(new SurfaceSideModel()
                                    {
                                        Width = lengths[i],
                                        Parts = parts,
                                    });
                                }

                                var floor = new LevelModel()
                                {
                                    Height = floorHeight,
                                    Sides = floorSides,
                                };
                                floors.Add(floor);
                            }

                            if (buildingMaterial == null)
                            {
                                var buildingMaterialIndex = random.NextByte() % 3;
                                switch (buildingMaterialIndex)
                                {
                                    case 0:
                                        buildingMaterial = BuildingMaterialKindValues.BuildingMaterialBrick;
                                        break;
                                    case 1:
                                        buildingMaterial = BuildingMaterialKindValues.BuildingMaterialConcrete;
                                        break;
                                    case 2:
                                        buildingMaterial = BuildingMaterialKindValues.BuildingMaterialPlaster;
                                        break;
                                    default:
                                        buildingMaterial = BuildingMaterialKindValues.BuildingMaterialPlaster;
                                        break;
                                }
                            }

                            var isRoofComplex = entity.Path.OfType<Polygon>().Any(x => x.Shell.Coordinates.Length > 5);

                            if (roofKind == null || isRoofComplex)
                            {
                                var roofKindIndex = random.NextByte() % (isRoofComplex ? 2 : 4);
                                switch (roofKindIndex)
                                {
                                    case 0:
                                        roofKind = RoofKindValues.RoofFlat;
                                        break;
                                    case 1:
                                        roofKind = RoofKindValues.RoofSkillion;
                                        break;
                                    case 2:
                                        roofKind = RoofKindValues.RoofGabled;
                                        break;
                                    case 3:
                                        roofKind = RoofKindValues.RoofHipped;
                                        break;
                                }
                            }

                            if (roofMaterial == null)
                            {
                                var roofMaterialIndex = random.NextByte() % 2;
                                switch (roofMaterialIndex)
                                {
                                    case 0:
                                        roofMaterial = RoofMaterialKindValues.BuildingRoofMaterialBitumen;
                                        break;
                                    case 1:
                                        roofMaterial = RoofMaterialKindValues.BuildingRoofMaterialAluminum;
                                        break;
                                }
                            }

                            var building = new BuildingModel()
                            {
                                Height = (levels * floorHeight).Value,
                                MinLevel = 0,
                                Levels = levels,
                                LevelCollection = floors,
                                Kind = entity.Kind,
                                Material = buildingMaterial,
                                Roof = new RoofModel()
                                {
                                    Shape = roofKind,
                                    Material = roofMaterial,
                                    Height = floorHeight,
                                    Levels = 0,
                                }
                            };

                            buildings.Add(building);
                        }
                        else
                        {
                            // Self-intersections, self-tangency, not closed edge array
                            _logger!.LogWarning("Building part {Part} is not a ring in {Agent}.", ring.ToText(), _title);
                        }
                    }
                    else
                    {
                        _logger!.LogWarning("Building part {Part} of type {Type} is not supported in {Agent}.", geom.ToText(), geom.GetType().Name, _title);
                    }
                }

                sb.Append(JsonSerializer.Serialize(buildings, _serializerOptions));

                desc = sb.ToString();
            }

            return new BuildingEntity(
                entity.GID,
                entity.Kind,
                buildingMaterial,
                roofKind,
                roofMaterial,
                levels,
                entity.Amenity,
                entity.Religion,
                entity.Office,
                entity.Abandoned,
                desc,
                entity.Geom);
        }

        private SurfacePartModel GetSurfacePartModel(int index, out double width)
        {
            switch (index)
            {
                case 0:
                    width = LengthMax;
                    return new SurfacePartModel()
                    {
                        Width = LengthMax,
                        Kind = LevelSidePartKindValues.PartWall,
                    };
                case 1:
                    width = LengthMax;
                    return new SurfacePartModel()
                    {
                        Width = LengthMax,
                        Kind = LevelSidePartKindValues.PartWindow,
                    };
                case 2:
                    width = LengthMax;
                    return new SurfacePartModel()
                    {
                        Width = LengthMax,
                        Kind = LevelSidePartKindValues.PartBalcony,
                    };
                case 3:
                default:
                    width = LengthMax;
                    return new SurfacePartModel()
                    {
                        Width = LengthMax,
                        Kind = LevelSidePartKindValues.PartPorch,
                    };
            }
        }
    }
}
