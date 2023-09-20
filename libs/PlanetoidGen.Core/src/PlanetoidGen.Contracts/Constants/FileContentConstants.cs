namespace PlanetoidGen.Contracts.Constants
{
    public static class FileContentConstants
    {
        public static class CommonAttributes
        {
            /// <summary>
            /// Typically a MIME type or Media type of the file.
            /// </summary>
            public const string ContentType = "contentType";
        }

        public static class TileMapAttributes
        {
            /// <summary>
            /// Unique integer CRS SRID.
            /// </summary>
            public const string Srid = "srid";
            /// <summary>
            /// Object location in format
            /// <code>lon;lat</code>
            /// </summary>
            public const string Location = "location";
        }

        public static class HeightMapAttributes
        {
            public const string MinHeight = "minHeight";
        }
    }
}
