namespace PlanetoidGen.Contracts.Constants.StringMessages
{
    public static class FileStringMessages
    {
        public const string FileIdIsEmpty = "File ID cannot be null or whitespace.";
        public const string FileNameIsEmpty = "File name cannot be null or whitespace.";
        public const string TileInfoIsEmpty = "Tile info cannot be null.";
        public const string FileModelIsEmpty = "File model cannot be null.";
        public const string FileContentNotExist = "File content '{id}' is missing.";
        public const string SelectError = "File select error: {error}";
        public const string RemoveError = "File delete error: {error}";
        public const string InsertError = "File insert error: {error}";
        public const string ExistsError = "File exists error: {error}";
        public const string CountError = "File count error: {error}";
        public const string FileIsReferenced = "File is referenced and cannot be deleted.";
        public const string FileIsReferencedError = FileIsReferenced + " File ID: {id}";
        public const string LargeContentIdShouldBeEmpty = "Large content ID property should be empty";
    }
}
