using PlanetoidGen.Domain.Models.Descriptions.Building;

namespace PlanetoidGen.Agents.Osm.Constants.KindValues
{
    public static class PorchKindValues
    {
        public const string PorchPrefix = "Door_";
        public const string PorchFlat = "_Flat";
        public const string PorchBox = "_Box";
        public const string PorchOverhang = "_Overhang";
        public const string PorchFrame = "_Frame";
        public const string PorchAluminum = "_Aluminum";
        public const string PorchBitumen = "_Bitumen";
        public const string PorchConcrete = "_Concrete";
        public const string PorchPlaster = "_Plaster";

        public static string PickPorchType(BuildingModel description, int randVal)
        {
            var doorType = randVal % 2 + 1;
            if (description.Kind == BuildingKindValues.BuildingHouse)
            {
                if (description.Levels == 1)
                {
                    return PorchPrefix + doorType + PorchFlat;
                }
                else
                {
                    return PorchPrefix +
                        doorType + PorchOverhang +
                        (description.Roof.Material == RoofMaterialKindValues.BuildingRoofMaterialBitumen
                            ? PorchBitumen : PorchAluminum) +
                        (description.Material == BuildingMaterialKindValues.BuildingMaterialPlaster
                            ? PorchPlaster : PorchConcrete);
                }
            }
            else
            {
                // Overhang, Frame, Flat or Box
                // Aluminum or Bitumen
                // Concrete or Plaster

                var shape = (randVal >> 1) % 4;
                string doorShape;
                switch (shape)
                {
                    case 0:
                        doorShape = PorchFlat;
                        break;
                    case 1:
                        doorShape = PorchOverhang;
                        break;
                    case 2:
                        doorShape = PorchFrame;
                        break;
                    case 3:
                    default:
                        doorShape = PorchBox;
                        break;
                }

                if (shape == 0) return PorchPrefix + doorType;

                var roof = (randVal >> 3) % 2;
                string doorRoof;

                switch (description.Roof.Material)
                {
                    case RoofMaterialKindValues.BuildingRoofMaterialBitumen:
                        doorRoof = PorchBitumen;
                        break;
                    case RoofMaterialKindValues.BuildingRoofMaterialAluminum:
                        doorRoof = PorchAluminum;
                        break;
                    default:
                        switch (roof)
                        {
                            case 0:
                                doorRoof = PorchAluminum;
                                break;
                            case 1:
                            default:
                                doorRoof = PorchBitumen;
                                break;
                        }
                        break;
                }

                var wall = (randVal >> 4) % 2;
                string doorWall;

                switch (description.Material)
                {
                    case BuildingMaterialKindValues.BuildingMaterialPlaster:
                        doorWall = PorchPlaster;
                        break;
                    case BuildingMaterialKindValues.BuildingMaterialConcrete:
                        doorWall = PorchConcrete;
                        break;
                    default:
                        switch (wall)
                        {
                            case 0:
                                doorWall = PorchPlaster;
                                break;
                            case 1:
                            default:
                                doorWall = PorchConcrete;
                                break;
                        }
                        break;
                }

                return PorchPrefix + doorType + doorShape + doorRoof + doorWall;
            }
        }
    }
}
