using PlanetoidGen.Domain.Models.Descriptions.Building;

namespace PlanetoidGen.Agents.Osm.Constants.KindValues
{
    public static class BalconyKindValues
    {
        public const string BalconyAluminumWindow1 = "Balcony_Aluminum_1";
        public const string BalconyAluminumWindow2 = "Balcony_Aluminum_2";
        public const string BalconyAluminumNoWindow = "Balcony_Aluminum_3";
        public const string BalconyAluminumNoOverhang = "Balcony_Aluminum_4";
        public const string BalconyConcreteWindow1 = "Balcony_Concrete_1";
        public const string BalconyConcreteWindow2 = "Balcony_Concrete_2";
        public const string BalconyConcreteNoWindow = "Balcony_Concrete_3";
        public const string BalconyConcreteNoOverhang = "Balcony_Concrete_4";
        public const string BalconyPlasterWindow1 = "Balcony_Plaster_1";
        public const string BalconyPlasterWindow2 = "Balcony_Plaster_2";
        public const string BalconyPlasterNoWindow = "Balcony_Plaster_3";
        public const string BalconyPlasterNoOverhang = "Balcony_Plaster_4";

        public static string PickBalconyType(BuildingModel description, int levelIndex, int randVal)
        {
            var isBottomFloor = levelIndex == 0;
            var isTopFloor = levelIndex == description.Levels! - 1;
            var isHouse = description.Kind == BuildingKindValues.BuildingHouse;
            var material = 0;
            switch (description.Material)
            {
                case BuildingMaterialKindValues.BuildingMaterialConcrete:
                    material = 1;
                    break;
                case BuildingMaterialKindValues.BuildingMaterialBrick:
                    material = 2;
                    break;
                case BuildingMaterialKindValues.BuildingMaterialPlaster:
                    material = 0;
                    break;
                default:
                    material = 0;
                    break;
            }

            const bool Top = true;
            const bool NoTop = false;
            const bool Bottom = true;
            const bool NoBottom = false;
            const int Plaster = 0;
            const int Concrete = 1;
            const int Aluminum = 2;

            switch (isBottomFloor, isTopFloor, material)
            {
                case (Top, Bottom, Aluminum):
                    return randVal % 2 == 0 ? BalconyAluminumWindow1 : BalconyAluminumWindow2;
                case (Top, Bottom, Concrete):
                    return randVal % 2 == 0 ? BalconyConcreteWindow1 : BalconyConcreteWindow2;
                case (Top, Bottom, Plaster):
                    return randVal % 2 == 0 ? BalconyPlasterWindow1 : BalconyPlasterWindow2;

                case (Top, NoBottom, Aluminum):
                    return isHouse ? BalconyAluminumNoOverhang : BalconyAluminumNoWindow;
                case (Top, NoBottom, Concrete):
                    return isHouse ? BalconyConcreteNoOverhang : BalconyConcreteNoWindow;
                case (Top, NoBottom, Plaster):
                    return isHouse ? BalconyPlasterNoOverhang : BalconyPlasterNoWindow;

                case (NoTop, Bottom, Aluminum):
                    return isHouse
                        ? randVal % 2 == 0 ? BalconyAluminumNoWindow : BalconyAluminumNoOverhang
                        : randVal % 2 == 0 ? BalconyAluminumWindow1 : BalconyAluminumWindow2;
                case (NoTop, Bottom, Concrete):
                    return isHouse
                        ? randVal % 2 == 0 ? BalconyConcreteNoWindow : BalconyConcreteNoOverhang
                        : randVal % 2 == 0 ? BalconyConcreteWindow1 : BalconyConcreteWindow2;
                case (NoTop, Bottom, Plaster):
                    return isHouse
                        ? randVal % 2 == 0 ? BalconyPlasterNoWindow : BalconyPlasterNoOverhang
                        : randVal % 2 == 0 ? BalconyPlasterWindow1 : BalconyPlasterWindow2;

                case (NoTop, NoBottom, Aluminum):
                    return randVal % 2 == 0 ? BalconyAluminumWindow1 : BalconyAluminumWindow2;
                case (NoTop, NoBottom, Concrete):
                    return randVal % 2 == 0 ? BalconyConcreteWindow1 : BalconyConcreteWindow2;
                case (NoTop, NoBottom, Plaster):
                    return randVal % 2 == 0 ? BalconyPlasterWindow1 : BalconyPlasterWindow2;

                default:
                    return randVal % 2 == 0 ? BalconyPlasterWindow1 : BalconyPlasterWindow2;
            };
        }
    }
}
