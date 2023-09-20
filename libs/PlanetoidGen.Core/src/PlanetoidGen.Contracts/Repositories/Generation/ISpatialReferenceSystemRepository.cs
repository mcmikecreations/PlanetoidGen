using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Generation;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Generation
{
    public interface ISpatialReferenceSystemRepository
    {
        /// <summary>
        /// Insert a new SRS entry into the system.
        /// </summary>
        /// <param name="wktString">The WKT SRS/CRS description correlating to the srtext column.</param>
        /// <param name="proj4String">The Proj4 SRS/CRS description correlating to the proj4text column.</param>
        /// <param name="authoritySrid">
        /// The system-unique srid of the entry. Can be the same, as one of the defaults,
        /// but needs to be unique regarding to other custom entries. Authority name isn't
        /// taken into account.
        /// </param>
        /// <param name="authorityName">
        /// The name of the authority managing the entry. nameof(PlanetoidGen) is recommended.
        /// </param>
        ValueTask<Result<int>> InsertOrUpdateSRS(string wktString, string proj4String, int authoritySrid, string authorityName, CancellationToken token);

        ValueTask<Result<SpatialReferenceSystemModel>> GetSRS(int srid, CancellationToken token);

        ValueTask<Result<SpatialReferenceSystemModel>> GetSRS(int authoritySrid, string authorityName, CancellationToken token);

        ValueTask<Result> DeleteSRS(int srid, CancellationToken token);

        ValueTask<Result<int>> CountCustomSRS(CancellationToken token);

        ValueTask<Result<int>> ClearCustomSRS(CancellationToken token);

        int GetAuthoritySridGeographic(int planetoidId);

        int GetAuthoritySridProjected(int planetoidId);

        string GetDefaultAuthorityName();
    }
}
