using PlanetoidGen.Client.Contracts.ScriptableObjects.Storytelling;

namespace PlanetoidGen.Client.Contracts.Services.Storytelling
{
    public interface IStoryParser
    {
        public const string AnchorIdPrefix = "a_";

        StorySO ParseStory(string rawText);
    }
}
