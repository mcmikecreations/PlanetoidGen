using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Domain.Models.Generation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.BusinessLogic
{
    public class SpatialReferenceSystemStubRepository : ISpatialReferenceSystemRepository
    {
        public ValueTask<Result<int>> ClearCustomSRS(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Result<int>> CountCustomSRS(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Result> DeleteSRS(int srid, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public int GetAuthoritySridGeographic(int planetoidId)
        {
            throw new NotImplementedException();
        }

        public int GetAuthoritySridProjected(int planetoidId)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAuthorityName()
        {
            throw new NotImplementedException();
        }

        public ValueTask<Result<SpatialReferenceSystemModel>> GetSRS(int srid, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Result<SpatialReferenceSystemModel>> GetSRS(int authoritySrid, string authorityName, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Result<int>> InsertOrUpdateSRS(string wktString, string proj4String, int authoritySrid, string authorityName, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
