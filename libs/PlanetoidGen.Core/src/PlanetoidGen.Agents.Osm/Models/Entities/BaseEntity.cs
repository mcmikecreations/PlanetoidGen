using NetTopologySuite.Geometries;

namespace PlanetoidGen.Agents.Osm.Models.Entities
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// Entity ID in the database, may not correspond to an OpenStreetMap ID.
        /// </summary>
        public long GID { get; }

        /// <summary>
        /// Shape of the entity. The coords are (lon,lat) in degrees.
        /// </summary>
        public Geometry Geom { get; }

        public BaseEntity(long id, Geometry geom)
        {
            GID = id;
            Geom = geom;
        }
    }
}
