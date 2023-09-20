namespace PlanetoidGen.DataAccess.Constants.StringMessages
{
    public static class StoredProcedureStringMessages
    {
        public static string TileInfoInsert = $"{nameof(TableStringMessages.TileInfo)}Insert";
        public static string TileInfoLastModfiedInfoUpdate = $"{nameof(TableStringMessages.TileInfo)}LastModfiedInfoUpdate";
        public static string TileInfoSelect = $"{nameof(TableStringMessages.TileInfo)}Select";

        public static string AgentInfoInsert = $"{nameof(TableStringMessages.AgentInfo)}Insert";
        public static string AgentInfoSelect = $"{nameof(TableStringMessages.AgentInfo)}Select";
        public static string AgentInfoSelectByIndex = $"{nameof(TableStringMessages.AgentInfo)}SelectByIndex";
        public static string AgentInfoClear = $"{nameof(TableStringMessages.AgentInfo)}Clear";

        public static string PlanetoidInfoInsert = $"{nameof(TableStringMessages.PlanetoidInfo)}Insert";
        public static string PlanetoidInfoSelect = $"{nameof(TableStringMessages.PlanetoidInfo)}Select";
        public static string PlanetoidInfoSelectAll = $"{nameof(TableStringMessages.PlanetoidInfo)}SelectAll";
        public static string PlanetoidInfoDelete = $"{nameof(TableStringMessages.PlanetoidInfo)}Delete";
        public static string PlanetoidInfoClear = $"{nameof(TableStringMessages.PlanetoidInfo)}Clear";

        public static string GenerationLODInsert = $"{nameof(TableStringMessages.GenerationLODs)}Insert";
        public static string GenerationLODSelect = $"{nameof(TableStringMessages.GenerationLODs)}Select";
        public static string GenerationLODClear = $"{nameof(TableStringMessages.GenerationLODs)}Clear";

        public static string SpatialReferenceSystemInsertOrUpdate = $"{nameof(TableStringMessages.SpatialReferenceSystems)}Insert";
        public static string SpatialReferenceSystemSelectBySrid = $"{nameof(TableStringMessages.SpatialReferenceSystems)}SelectBySrid";
        public static string SpatialReferenceSystemSelectByAuthority = $"{nameof(TableStringMessages.SpatialReferenceSystems)}SelectByAuthority";
        public static string SpatialReferenceSystemDelete = $"{nameof(TableStringMessages.SpatialReferenceSystems)}Delete";
        public static string SpatialReferenceSystemClearCustom = $"{nameof(TableStringMessages.SpatialReferenceSystems)}ClearCustom";
        public static string SpatialReferenceSystemCountCustom = $"{nameof(TableStringMessages.SpatialReferenceSystems)}CountCustom";

        public static string MetaDynamicInsert = $"{nameof(TableStringMessages.MetaDynamic)}Insert";
        public static string MetaDynamicSelectById = $"{nameof(TableStringMessages.MetaDynamic)}SelectById";
        public static string MetaDynamicSelectByName = $"{nameof(TableStringMessages.MetaDynamic)}SelectByName";
        public static string MetaDynamicDelete = $"{nameof(TableStringMessages.MetaDynamic)}Delete";
        public static string MetaDynamicClear = $"{nameof(TableStringMessages.MetaDynamic)}Clear";

        public static string FileInfoInsert = $"{nameof(TableStringMessages.FileInfo)}Insert";
        public static string FileInfoSelect = $"{nameof(TableStringMessages.FileInfo)}Select";
        public static string FileInfoDelete = $"{nameof(TableStringMessages.FileInfo)}Delete";
        public static string FileInfoExists = $"{nameof(TableStringMessages.FileInfo)}Exists";

        public static string TileBasedFileInfoInsert = $"{nameof(TableStringMessages.TileBasedFileInfo)}Insert";
        public static string TileBasedFileInfoSelectById = $"{nameof(TableStringMessages.TileBasedFileInfo)}SelectById";
        public static string TileBasedFileInfoSelectAllByTile = $"{nameof(TableStringMessages.TileBasedFileInfo)}SelectAllByTile";
        public static string TileBasedFileInfoDelete = $"{nameof(TableStringMessages.TileBasedFileInfo)}Delete";
        public static string TileBasedFileInfoDeleteAllByTile = $"{nameof(TableStringMessages.TileBasedFileInfo)}DeleteAllByTile";

        public static string FileDependencyInsert = $"{nameof(TableStringMessages.FileDependency)}Insert";
        public static string FileDependencySelect = $"{nameof(TableStringMessages.FileDependency)}Select";
        public static string FileDependencyDelete = $"{nameof(TableStringMessages.FileDependency)}Delete";
        public static string FileDependencyCountByReferenceId = $"{nameof(TableStringMessages.FileDependency)}CountByReferenceId";
    }
}
