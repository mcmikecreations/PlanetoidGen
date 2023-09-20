using Insight.Database;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Dynamic
{
    public class GeometricDynamicRepository<TData> : DynamicRepository<TData>, IGeometricDynamicRepository<TData>, INamedRepository<TData>, IRepositoryAccessWrapper, IDynamicRepository<TData>, IStaticRepository
    {
        protected readonly GeometryFactory _geometryFactory;

        public GeometricDynamicRepository(
            DbConnectionStringBuilder connection,
            IMetaProcedureRepository meta,
            ITableProcedureGenerator generator,
            IRowSerializer<TData> serializer,
            IConfiguration configuration)
            : base(connection, meta, generator, serializer, configuration)
        {
            var srid = generator.Schema.Columns
                .Where(x => x.DataType == ColumnSchema.ColumnType.Geometry && x.Properties.ContainsKey(ColumnSchema.PropertyKeys.SpatialRefSys))
                .Select(x =>
                {
                    return int.TryParse(x.Properties[ColumnSchema.PropertyKeys.SpatialRefSys], out var res) ? res : (int?)null;
                })
                .FirstOrDefault(x => x != null);

            _geometryFactory = srid == null
                ? new GeometryFactory()
                : new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), srid.Value);
        }

        public async ValueTask<Result<IEnumerable<TData>>> ReadMultipleByBoundingBox(
            AxisAlignedBoundingBoxCoordinateModel boundingBox,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            var result = await RunMultipleFunction<TData>(
                    _generator.RowReadMultipleByBoundingBoxName(),
                    new
                    {
                        dbbox = (object)_geometryFactory.ToGeometry(
                        new Envelope(
                            boundingBox.MinLongtitude * 180.0 / Math.PI,
                            boundingBox.MaxLongtitude * 180.0 / Math.PI,
                            boundingBox.MinLatitude * 180.0 / Math.PI,
                            boundingBox.MaxLatitude * 180.0 / Math.PI)),
                    }, token, connection);

            return Result<IEnumerable<TData>>.Convert(result);
        }

        public async ValueTask<Result<IEnumerable<TData>>> ReadMultipleByBoundingBox(
            BoundingBoxCoordinateModel boundingBox,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            var coordsRadians = boundingBox.GetCoordinateArray();
            var coordsDegrees = new[]
            {
                new Coordinate(coordsRadians[0].X * 180.0 / Math.PI, coordsRadians[0].Y * 180.0 / Math.PI),
                new Coordinate(coordsRadians[1].X * 180.0 / Math.PI, coordsRadians[1].Y * 180.0 / Math.PI),
                new Coordinate(coordsRadians[2].X * 180.0 / Math.PI, coordsRadians[2].Y * 180.0 / Math.PI),
                new Coordinate(coordsRadians[3].X * 180.0 / Math.PI, coordsRadians[3].Y * 180.0 / Math.PI),
                null,
            };
            coordsDegrees[4] = coordsDegrees[0];

            var result = await RunMultipleFunction<TData>(
                    _generator.RowReadMultipleByBoundingBoxName(),
                    new
                    {
                        dbbox = (object)_geometryFactory.CreatePolygon(coordsDegrees),
                    }, token, connection);

            return Result<IEnumerable<TData>>.Convert(result);
        }

        protected override async Task<Result> CreateObjects(DbConnectionWrapper c, CancellationToken token)
        {
            var result = await base.CreateObjects(c, token);

            if (!result.Success)
            {
                return result;
            }

            // Check for function. If schema missing, function is missing too.
            var schemaExists = await _metaRepo.SchemaExists(_schema.Schema, token);
            var functionExists = schemaExists.Success ? await _metaRepo.FunctionNameExists(_generator.RowReadMultipleByBoundingBoxName(), token) : schemaExists;

            if (!functionExists.Success || _recreateTables)
            {
                var taskSqls = new List<string>()
                {
                    _generator.RowReadMultipleByBoundingBoxProcedure(),
                };

                foreach (var sql in taskSqls)
                {
                    result = await RunQuery(sql, c, token);
                    if (!result.Success)
                    {
                        return result;
                    }
                }
            }

            return result;
        }
    }
}
