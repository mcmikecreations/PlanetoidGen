using Google.Protobuf.Collections;
using PlanetoidGen.API;
using PlanetoidGen.Client.BusinessLogic.Managers;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using System;
using System.Linq;
using UnityEngine;

public class DummyRequest : MonoBehaviour
{
    public async void OnButtonPress()
    {
        try
        {
            var planetoidController = ServiceManager.Instance.GetService<IPlanetoidController>();
            var addResult = await planetoidController.AddPlanetoid(new PlanetoidGen.Domain.Models.Info.PlanetoidInfoModel(0, "test", 1, 1));
            //var result = await planetoidController.GetAllPlanetoids();

            Debug.Log($"{addResult} planetoids retrieved.");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    private void SetDummyAgents(RepeatedField<SetAgentModel> agents)
    {
        agents.Add(new SetAgentModel { Title = "Terrain Agent", Settings = string.Empty });
        agents.Add(new SetAgentModel { Title = "Climate Agent", Settings = string.Empty });
        agents.Add(new SetAgentModel { Title = "Biome Agent", Settings = string.Empty });
    }
}

