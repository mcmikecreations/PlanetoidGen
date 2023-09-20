namespace PlanetoidGen.Domain.Models.Generation
{
    public class SpatialReferenceSystemModel
    {
        /// <summary>
        /// Database-unique id of the projection, may be different from <see cref="AuthoritySrid"/>.
        /// </summary>
        public int Srid { get; }

        public string AuthorityName { get; }

        public int AuthoritySrid { get; }

        public string WktString { get; }

        public string Proj4String { get; }

        public SpatialReferenceSystemModel(int srid, string authorityName, int authoritySrid, string wktString, string proj4String)
        {
            Srid = srid;
            AuthorityName = authorityName;
            AuthoritySrid = authoritySrid;
            WktString = wktString;
            Proj4String = proj4String;
        }
    }
}
