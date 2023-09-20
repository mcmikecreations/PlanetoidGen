using PlanetoidGen.Client.BusinessLogic.Managers;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class WorldLoader : MonoBehaviour
{
    async void Start()
    {
        // Examples of the GRPC controllers usage

        //try
        //{
        //    var planetoidController = ServiceManager.Instance.GetService<IPlanetoidController>();
        //    var lodsController = ServiceManager.Instance.GetService<IGenerationLODController>();
        //    var agentsController = ServiceManager.Instance.GetService<IAgentController>();
        //    var tileGenerationStreamController = ServiceManager.Instance.GetService<ITileGenerationStreamController>();
        //    var binaryContentController = ServiceManager.Instance.GetService<IBinaryContentController>();
        //    var binaryContentStreamController = ServiceManager.Instance.GetService<IBinaryContentStreamController>();

        //    var clearAgentsResult = await agentsController.ClearAgents(0);
        //    var deleteLODsResult = await lodsController.ClearLODs(0);
        //    //var deleteResult = await planetoidController.RemovePlanetoid(0);

        //    var addResult = await planetoidController.AddPlanetoid(new PlanetoidGen.Domain.Models.Info.PlanetoidInfoModel(0, "test", 1, 1));
        //    var getPlanetoidsResult = await planetoidController.GetAllPlanetoids();
        //    var getPlanetoidResult = await planetoidController.GetPlanetoid(0);

        //    var insertLODsResult = await lodsController.InsertLODs(new List<GenerationLODModel> { new GenerationLODModel(0, 12, 12) });
        //    var getLODsResult = await lodsController.GetLODs(0);

        //    var setAgentsResult = await agentsController.SetAgents(0, new List<AgentInfoModel> { new AgentInfoModel(0, 0, "PlanetoidGen.DummyAgent", "{}", false), new AgentInfoModel(0, 1, "PlanetoidGen.DataReportingAgent", "{}", true) });
        //    var getAgentsResult = await agentsController.GetAgents(0);

        //    tileGenerationStreamController.Subscribe((sender, args) =>
        //    {
        //        Debug.Log($"Tile generation response.");
        //    });
        //    await tileGenerationStreamController.StartStream(default);
        //    await tileGenerationStreamController.SendTileGenerationRequest(0, new List<SphericalLODCoordinateModel>() { new SphericalLODCoordinateModel(0.66997779463381130036412025160896, 0.85512581234387377054333921571176, 12) });

        //    var addFileResult = await binaryContentController.SaveFileContent(new PlanetoidGen.Contracts.Models.Documents.FileModel
        //    {
        //        FileId = "test",
        //        Content = new PlanetoidGen.Domain.Models.Documents.FileContentModel
        //        {
        //            Id = "test",
        //            Content = new byte[] { (byte)'1' },
        //            FileName = "test",
        //            LocalPath = "test"
        //        }
        //    });
        //    var source = new CancellationTokenSource();
        //    binaryContentStreamController.Subscribe((sender, args) =>
        //    {
        //        Debug.Log($"File response.");
        //    });
        //    await binaryContentStreamController.StartStream(source.Token);
        //    await binaryContentStreamController.SendFileContentRequest("test");
        //}
        //catch (InvalidOperationException ex)
        //{
        //    Debug.Log(ex.ToString());
        //}
    }
}
