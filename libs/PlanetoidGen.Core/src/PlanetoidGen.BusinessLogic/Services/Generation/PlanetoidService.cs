using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Generation
{
    public class PlanetoidService : IPlanetoidService
    {
        private readonly IPlanetoidInfoRepository _planetoidRepo;

        public PlanetoidService(IPlanetoidInfoRepository planetoidRepo)
        {
            _planetoidRepo = planetoidRepo ?? throw new ArgumentNullException(nameof(planetoidRepo));
        }

        public async ValueTask<Result<int>> AddPlanetoid(PlanetoidInfoModel planetoid, CancellationToken token)
        {
            return await _planetoidRepo.InsertPlanetoid(planetoid, token);
        }

        public async ValueTask<Result<int>> ClearPlanetoids(CancellationToken token)
        {
            return await _planetoidRepo.ClearPlanetoids(token);
        }

        public async ValueTask<Result<PlanetoidInfoModel>> GetPlanetoid(int id, CancellationToken token)
        {
            var result = await _planetoidRepo.GetPlanetoidById(id, token);

            if (result.Success)
            {
                return result.Data != null
                    ? Result<PlanetoidInfoModel>.CreateSuccess(result.Data)
                    : Result<PlanetoidInfoModel>.CreateFailure(GeneralStringMessages.ObjectNotExist);
            }

            return Result<PlanetoidInfoModel>.CreateFailure(result);
        }

        public async ValueTask<Result<bool>> RemovePlanetoid(int id, CancellationToken token)
        {
            return await _planetoidRepo.RemovePlanetoidById(id, token);
        }

        public async ValueTask<Result<IReadOnlyList<PlanetoidInfoModel>>> GetAllPlanetoids(CancellationToken token)
        {
            return await _planetoidRepo.GetAllPlanetoids(token);
        }
    }
}
