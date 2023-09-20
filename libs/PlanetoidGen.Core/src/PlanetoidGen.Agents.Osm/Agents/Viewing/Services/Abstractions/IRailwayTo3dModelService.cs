﻿using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    internal interface IRailwayTo3dModelService
    {
        void ProcessEntity(
            RailwayEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<Vector3D[]> lines,
            Vector3D pivot,
            Scene scene,
            Node parent,
            int z);

        Scene ProcessEntity(
            RailwayEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<Vector3D[]> lines,
            Vector3D pivot,
            int z);
    }
}
