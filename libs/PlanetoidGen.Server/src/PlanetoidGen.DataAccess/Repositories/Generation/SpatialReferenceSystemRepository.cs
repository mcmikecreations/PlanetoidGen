using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Generation;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Generation
{
    public class SpatialReferenceSystemRepository : RepositoryAccessWrapper<SpatialReferenceSystemModel>, ISpatialReferenceSystemRepository
    {
        private static readonly Func<IDataReader, SpatialReferenceSystemModel> _reader = (r) => new SpatialReferenceSystemModel(
                (int)r["srid"],
                (string)r["auth_name"],
                (int)r["auth_srid"],
                (string)r["srtext"],
                (string)r["proj4text"]
                );

        private readonly int _rangeStart;
        private readonly int _rangeEnd;

        public SpatialReferenceSystemRepository(
            DbConnectionStringBuilder connection,
            IOptions<GeoInfoServiceOptions> options,
            IMetaProcedureRepository meta)
            : base(connection, meta)
        {
            _rangeStart = options.Value.AvailableMinSrid ?? 0;
            _rangeEnd = options.Value.AvailableMaxSrid ?? int.MaxValue;

            if (_rangeStart > _rangeEnd)
            {
                (_rangeStart, _rangeEnd) = (_rangeEnd, _rangeStart);
            }
        }

        public override string Name => TableStringMessages.SpatialReferenceSystems;

        public override Func<IDataReader, SpatialReferenceSystemModel>? Reader => _reader;

        public async ValueTask<Result<int>> ClearCustomSRS(CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.SpatialReferenceSystemClearCustom,
                new
                {
                    dmin = _rangeStart,
                    dmax = _rangeEnd,
                },
                token);
        }

        public async ValueTask<Result<int>> CountCustomSRS(CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.SpatialReferenceSystemCountCustom,
                new
                {
                    dmin = _rangeStart,
                    dmax = _rangeEnd,
                },
                token);
        }

        public async ValueTask<Result> DeleteSRS(int srid, CancellationToken token)
        {
            var result = await RunSingleFunction<bool>(
                StoredProcedureStringMessages.SpatialReferenceSystemDelete,
                new { dsrid = srid },
                token);

            return result.Success
                ? result.Data
                    ? Result.CreateSuccess()
                    : Result.CreateFailure(GeneralStringMessages.ObjectNotExist, srid.ToString())
                : Result.CreateFailure(result);
        }

        public async ValueTask<Result<SpatialReferenceSystemModel>> GetSRS(int srid, CancellationToken token)
        {
            return await RunSingleFunction(
                StoredProcedureStringMessages.SpatialReferenceSystemSelectBySrid,
                new { dsrid = srid },
                token, constructor: Reader);
        }

        public async ValueTask<Result<SpatialReferenceSystemModel>> GetSRS(int authoritySrid, string authorityName, CancellationToken token)
        {
            return await RunSingleFunction(
                StoredProcedureStringMessages.SpatialReferenceSystemSelectByAuthority,
                new { dauthName = authorityName, dauthSrid = authoritySrid },
                token, constructor: Reader);
        }

        public async ValueTask<Result<int>> InsertOrUpdateSRS(string wktString, string proj4String, int authoritySrid, string authorityName, CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.SpatialReferenceSystemInsertOrUpdate,
                new
                {
                    dsrid = (authoritySrid % (_rangeEnd - _rangeStart)) + _rangeStart,
                    dauthName = authorityName,
                    dauthSrid = authoritySrid,
                    dwktString = wktString,
                    dproj4String = proj4String,
                },
                token);
        }

        public int GetAuthoritySridGeographic(int planetoidId)
        {
            return 2 * planetoidId;
        }

        public int GetAuthoritySridProjected(int planetoidId)
        {
            return 2 * planetoidId + 1;
        }

        public string GetDefaultAuthorityName()
        {
            return nameof(PlanetoidGen);
        }
    }
}
