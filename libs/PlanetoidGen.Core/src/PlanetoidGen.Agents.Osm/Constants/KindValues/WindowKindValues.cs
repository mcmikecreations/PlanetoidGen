using PlanetoidGen.Domain.Models.Descriptions.Building;

namespace PlanetoidGen.Agents.Osm.Constants.KindValues
{
    public static class WindowKindValues
    {
        public const string WindowPlastic = "Window_Plastic";
        public const string WindowPane = "Window_Pane";
        public const string WindowWarehouse = "Window_Warehouse";
        public const string WindowCottage = "Window_Cottage";

        public static string PickWindowType(BuildingModel description)
        {
            switch (description.Kind)
            {
                case BuildingKindValues.BuildingHouse:
                    return description.Levels == 1 || description.Levels == 2 ? WindowCottage : WindowPlastic;
                case BuildingKindValues.BuildingIndustrial:
                    return description.Levels == 1 ? WindowPane : WindowWarehouse;
                default:
                    return WindowPlastic;
            }
        }
    }
}
