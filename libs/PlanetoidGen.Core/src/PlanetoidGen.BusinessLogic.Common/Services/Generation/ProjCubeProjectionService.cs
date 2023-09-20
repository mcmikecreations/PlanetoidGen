/*
 * This implements the Quadrilateralized Spherical Cube (QSC) projection.
 *
 * Copyright (c) 2011, 2012  Martin Lambers <marlam@marlam.de>
 *
 * The QSC projection was introduced in:
 * [OL76]
 * E.M. O'Neill and R.E. Laubscher, "Extended Studies of a Quadrilateralized
 * Spherical Cube Earth Data Base", Naval Environmental Prediction Research
 * Facility Tech. Report NEPRF 3-76 (CSC), May 1976.
 *
 * The preceding shift from an ellipsoid to a sphere, which allows to apply
 * this projection to ellipsoids as used in the Ellipsoidal Cube Map model,
 * is described in
 * [LK12]
 * M. Lambers and A. Kolb, "Ellipsoidal Cube Maps for Accurate Rendering of
 * Planetary-Scale Terrain Data", Proc. Pacific Graphics (Short Papers), Sep.
 * 2012
 *
 * You have to choose one of the following projection centers,
 * corresponding to the centers of the six cube faces:
 * phi0 = 0.0, lam0 = 0.0       ("front" face)
 * phi0 = 0.0, lam0 = 90.0      ("right" face)
 * phi0 = 0.0, lam0 = 180.0     ("back" face)
 * phi0 = 0.0, lam0 = -90.0     ("left" face)
 * phi0 = 90.0                  ("top" face)
 * phi0 = -90.0                 ("bottom" face)
 * Other projection centers will not work!
 *
 * In the projection code below, each cube face is handled differently.
 * See the computation of the face parameter in the PROJECTION(qsc) function
 * and the handling of different face values (FACE_*) in the forward and
 * inverse projections.
 *
 * Furthermore, the projection is originally only defined for theta angles
 * between (-1/4 * PI) and (+1/4 * PI) on the current cube face. This area
 * of definition is named AREA_0 in the projection code below. The other
 * three areas of a cube face are handled by rotation of AREA_0.
 */
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using System;

namespace PlanetoidGen.BusinessLogic.Common.Services.Generation
{
    /// <summary>
    /// The QSC implementation from https://github.com/OSGeo/PROJ/blob/9.1/src/projections/qsc.cpp
    /// </summary>
    public class ProjCubeProjectionService : ICubeProjectionService
    {
        private const double Eps10 = 1e-10;
        /// <summary>
        /// Pi / 2
        /// </summary>
        private const double MHalfPi = 1.57079632679489661923;
        /// <summary>
        /// Pi / 4
        /// </summary>
        private const double MQuarterPi = 0.78539816339744830962;
        /// <summary>
        /// 1.5 * Pi
        /// </summary>
        private const double MPiHalfPi = 4.71238898038468985769;
        /// <summary>
        /// 2 * Pi
        /// </summary>
        private const double MTwoPi = 6.28318530717958647693;


        public (ICubeProjectionService.FaceSide, double, double) Forward(double lon, double lat)
        {
            return SphericalForward(lon, lat);
        }

        public (double, double) Inverse(ICubeProjectionService.FaceSide face, double x, double y)
        {
            return SphericalInverse(face, x, y);
        }

        public BoundingBoxCoordinateModel ToBoundingBox(CubicCoordinateModel point, ICoordinateMappingService parent)
        {
            var pointC = parent.ToCubic(parent.ToPlanar(point));

            int planetoidId = pointC.PlanetoidId;
            short face = pointC.Face;
            short z = pointC.Z;
            // Map x,y index + zoom to [-1,1]
            var max = parent.TileSizeCubic(z);

            pointC = new CubicCoordinateModel(planetoidId, face, z, pointC.X, pointC.Y + max);
            var x = pointC.X;
            var y = pointC.Y;

            var mx = x + max;
            var my = y + max;

            var upC = new CubicCoordinateModel(planetoidId, face, z, x, my);
            var oppositeC = new CubicCoordinateModel(planetoidId, face, z, mx, my);
            var rightC = new CubicCoordinateModel(planetoidId, face, z, mx, y);

            var bbox = new BoundingBoxCoordinateModel(
                planetoidId,
                parent.ToSpherical(pointC).ToCoordinatesRadians(),
                parent.ToSpherical(upC).ToCoordinatesRadians(),
                parent.ToSpherical(oppositeC).ToCoordinatesRadians(),
                parent.ToSpherical(rightC).ToCoordinatesRadians()
                );

            return bbox;
        }

        /// <summary>
        /// The four areas on a cube face. AREA_0 is the area of definition,
        /// the other three areas are counted counterclockwise.
        /// </summary>
        private enum Area : int
        {
            Area0 = 0,
            Area1 = 1,
            Area2 = 2,
            Area3 = 3,
        }

        /// <summary>
        /// Helper function for forward projection: compute the theta angle
        /// and determine the area number.
        /// </summary>
        private static double ForwardComputeFaceTheta(double phi, double y, double x, out Area area)
        {
            double theta;

            if (phi < Eps10)
            {
                area = Area.Area0;
                theta = 0.0;
            }
            else
            {
                theta = Math.Atan2(y, x);
                if (Math.Abs(theta) <= MQuarterPi)
                {
                    area = Area.Area0;
                }
                else if (theta > MQuarterPi && theta <= MHalfPi + MQuarterPi)
                {
                    area = Area.Area1;
                    theta -= MHalfPi;
                }
                else if (theta > MHalfPi + MQuarterPi || theta <= -(MHalfPi + MQuarterPi))
                {
                    area = Area.Area2;
                    theta = (theta >= 0.0 ? theta - Math.PI : theta + Math.PI);
                }
                else
                {
                    area = Area.Area3;
                    theta += MHalfPi;
                }
            }

            return theta;
        }

        /// <summary>
        /// Helper function: shift the longitude.
        /// </summary>
        private static double ShiftLongitudeOrigin(double lon, double offset)
        {
            double slon = lon + offset;

            if (slon < -Math.PI)
            {
                slon += MTwoPi;
            }
            else if (slon > Math.PI)
            {
                slon -= MTwoPi;
            }

            return slon;
        }

        /// <returns>A tuple of face, x, y.</returns>
        private static (ICubeProjectionService.FaceSide, double, double) SphericalForward(double lon, double lat)
        {
            // This re-implementation in C# assumes doubles are passed as values,
            // so it may reassign lat and lon parameters.
            double theta, phi;
            double t, mu; // nu;
            Area area;

            double x, y;

            var face = GetFaceSide(lon, lat);

            // Skipping geodetic to geocentric conversion, this system assumes coordinates to be
            // stored in the geocentric sphere coordinates.
            // Original comment:
            // Convert the geodetic latitude to a geocentric latitude.
            // This corresponds to the shift from the ellipsoid to the sphere
            // described in [LK12].

            // Convert the input lat, lon into theta, phi as used by QSC.
            // This depends on the cube face and the area on it.
            // For the top and bottom face, we can compute theta and phi
            // directly from phi, lam. For the other faces, we must use
            // unit sphere cartesian coordinates as an intermediate step.

            if (face == ICubeProjectionService.FaceSide.FaceTop)
            {
                phi = MHalfPi - lat;
                if (lon >= MQuarterPi && lon <= MHalfPi + MQuarterPi)
                {
                    area = Area.Area0;
                    theta = lon - MHalfPi;
                }
                else if (lon > MHalfPi + MQuarterPi || lon <= -(MHalfPi + MQuarterPi))
                {
                    area = Area.Area1;
                    theta = (lon > 0.0 ? lon - Math.PI : lon + Math.PI);
                }
                else if (lon > -(MHalfPi + MQuarterPi) && lon <= -MQuarterPi)
                {
                    area = Area.Area2;
                    theta = lon + MHalfPi;
                }
                else
                {
                    area = Area.Area3;
                    theta = lon;
                }
            }
            else if (face == ICubeProjectionService.FaceSide.FaceBottom)
            {
                phi = MHalfPi + lat;
                if (lon >= MQuarterPi && lon <= MHalfPi + MQuarterPi)
                {
                    area = Area.Area0;
                    theta = -lon + MHalfPi;
                }
                else if (lon < MQuarterPi && lon >= -MQuarterPi)
                {
                    area = Area.Area1;
                    theta = -lon;
                }
                else if (lon < -MQuarterPi && lon >= -(MHalfPi + MQuarterPi))
                {
                    area = Area.Area2;
                    theta = -lon - MHalfPi;
                }
                else
                {
                    area = Area.Area3;
                    theta = (lon > 0.0 ? -lon + Math.PI : -lon - Math.PI);
                }
            }
            else
            {
                double q, r, s;
                double sinLat, cosLat;
                double sinLon, cosLon;

                // Original code assumes lon and lat to be relative to lon0 and lat0
                // which are centers of cube faces. In our case lon0 and lat0 always
                // belong to FaceFront and equal to 0, so there's no need to convert
                /*if (face == ICubeProjectionService.FaceSide.FaceRight)
                {
                    lon = ShiftLongitudeOrigin(lon, MHalfPi);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceBack)
                {
                    lon = ShiftLongitudeOrigin(lon, Math.PI);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceLeft)
                {
                    lon = ShiftLongitudeOrigin(lon, -MHalfPi);
                }*/

                sinLat = Math.Sin(lat);
                cosLat = Math.Cos(lat);
                sinLon = Math.Sin(lon);
                cosLon = Math.Cos(lon);
                q = cosLat * cosLon;
                r = cosLat * sinLon;
                s = sinLat;

                if (face == ICubeProjectionService.FaceSide.FaceFront)
                {
                    phi = Math.Acos(q);
                    theta = ForwardComputeFaceTheta(phi, s, r, out area);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceRight)
                {
                    phi = Math.Acos(r);
                    theta = ForwardComputeFaceTheta(phi, s, -q, out area);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceBack)
                {
                    phi = Math.Acos(-q);
                    theta = ForwardComputeFaceTheta(phi, s, -r, out area);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceLeft)
                {
                    phi = Math.Acos(-r);
                    theta = ForwardComputeFaceTheta(phi, s, q, out area);
                }
                else
                {
                    // Impossible
                    phi = theta = 0.0;
                    area = Area.Area0;
                }
            }

            // Compute mu and nu for the area of definition.
            // For mu, see Eq. (3-21) in [OL76], but note the typos:
            // compare with Eq. (3-14). For nu, see Eq. (3-38).

            // -pi/4 <= mu, theta <= pi/4
            // 0 <= phi <= 180; phi <= atan(sqrt(2))=0.95
            mu = Math.Atan((12.0 / Math.PI) * (theta + Math.Acos(Math.Sin(theta) * Math.Cos(MQuarterPi)) - MHalfPi));
            t = Math.Sqrt((1.0 - Math.Cos(phi)) / (Math.Cos(mu) * Math.Cos(mu)) / (1.0 - Math.Cos(Math.Atan(1.0 / Math.Cos(theta)))));
            // nu = Math.Atan(t); // We don't really need nu, just t, see below.

            // Apply the result to the real area.
            if (area == Area.Area1)
            {
                mu += MHalfPi;
            }
            else if (area == Area.Area2)
            {
                mu += Math.PI;
            }
            else if (area == Area.Area3)
            {
                mu += MPiHalfPi;
            }

            // Now compute x, y from mu and nu
            // t = Math.Tan(nu);
            x = t * Math.Cos(mu);
            y = t * Math.Sin(mu);

            return (face, x, y);
        }

        private static (double, double) SphericalInverse(ICubeProjectionService.FaceSide face, double x, double y)
        {
            double mu, nu, cosmu, tannu;
            double tantheta, theta, cosphi, phi;
            double t;
            Area area;

            // lon is lam, lat is phi
            double lon, lat;

            // Convert the input x, y to the mu and nu angles as used by QSC.
            // This depends on the area of the cube face.
            nu = Math.Atan(Math.Sqrt(x * x + y * y));
            mu = Math.Atan2(y, x);
            if (x >= 0.0 && x >= Math.Abs(y))
            {
                area = Area.Area0;
            }
            else if (y >= 0.0 && y >= Math.Abs(x))
            {
                area = Area.Area1;
                mu -= MHalfPi;
            }
            else if (x < 0.0 && -x >= Math.Abs(y))
            {
                area = Area.Area2;
                mu = (mu < 0.0 ? mu + Math.PI : mu - Math.PI);
            }
            else
            {
                area = Area.Area3;
                mu += MHalfPi;
            }

            // Compute phi and theta for the area of definition.
            // The inverse projection is not described in the original paper, but some
            // good hints can be found here (as of 2011-12-14):
            // http://fits.gsfc.nasa.gov/fitsbits/saf.93/saf.9302
            // (search for "Message-Id: <9302181759.AA25477 at fits.cv.nrao.edu>")
            t = (Math.PI / 12.0) * Math.Tan(mu);
            tantheta = Math.Sin(t) / (Math.Cos(t) - (1.0 / Math.Sqrt(2.0)));
            theta = Math.Atan(tantheta);
            cosmu = Math.Cos(mu);
            tannu = Math.Tan(nu);
            cosphi = 1.0 - cosmu * cosmu * tannu * tannu * (1.0 - Math.Cos(Math.Atan(1.0 / Math.Cos(theta))));

            if (cosphi < -1.0)
            {
                cosphi = -1.0;
            }
            else if (cosphi > +1.0)
            {
                cosphi = +1.0;
            }

            // Apply the result to the real area on the cube face.
            // For the top and bottom face, we can compute phi and lam directly.
            // For the other faces, we must use unit sphere cartesian coordinates
            // as an intermediate step.
            if (face == ICubeProjectionService.FaceSide.FaceTop)
            {
                phi = Math.Acos(cosphi);
                lat = MHalfPi - phi;
                if (area == Area.Area0)
                {
                    lon = theta + MHalfPi;
                }
                else if (area == Area.Area1)
                {
                    lon = (theta < 0.0 ? theta + Math.PI : theta - Math.PI);
                }
                else if (area == Area.Area2)
                {
                    lon = theta - MHalfPi;
                }
                else // area == Area.Area3
                {
                    lon = theta;
                }
            }
            else if (face == ICubeProjectionService.FaceSide.FaceBottom)
            {
                phi = Math.Acos(cosphi);
                lat = phi - MHalfPi;
                if (area == Area.Area0)
                {
                    lon = -theta + MHalfPi;
                }
                else if (area == Area.Area1)
                {
                    lon = -theta;
                }
                else if (area == Area.Area2)
                {
                    lon = -theta - MHalfPi;
                }
                else // area == Area.Area3
                {
                    lon = (theta < 0.0 ? -theta - Math.PI : -theta + Math.PI);
                }
            }
            else
            {
                // Compute phi and lam via cartesian unit sphere coordinates.
                double q, r, s;
                q = cosphi;
                t = q * q;

                if (t >= 1.0)
                {
                    s = 0.0;
                }
                else
                {
                    s = Math.Sqrt(1.0 - t) * Math.Sin(theta);
                }

                t += s * s;
                if (t >= 1.0)
                {
                    r = 0.0;
                }
                else
                {
                    r = Math.Sqrt(1.0 - t);
                }
                // Rotate q,r,s into the correct area.
                if (area == Area.Area1)
                {
                    t = r;
                    r = -s;
                    s = t;
                }
                else if (area == Area.Area2)
                {
                    r = -r;
                    s = -s;
                }
                else if (area == Area.Area3)
                {
                    t = r;
                    r = s;
                    s = -t;
                }
                // Rotate q,r,s into the correct cube face.
                if (face == ICubeProjectionService.FaceSide.FaceRight)
                {
                    t = q;
                    q = -r;
                    r = t;
                }
                else if (face == ICubeProjectionService.FaceSide.FaceBack)
                {
                    q = -q;
                    r = -r;
                }
                else if (face == ICubeProjectionService.FaceSide.FaceLeft)
                {
                    t = q;
                    q = r;
                    r = -t;
                }

                // Now compute phi and lam from the unit sphere coordinates.
                lat = Math.Acos(-s) - MHalfPi;
                lon = Math.Atan2(r, q);
                /*if (face == ICubeProjectionService.FaceSide.FaceRight)
                {
                    lon = ShiftLongitudeOrigin(lon, -MHalfPi);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceBack)
                {
                    lon = ShiftLongitudeOrigin(lon, -Math.PI);
                }
                else if (face == ICubeProjectionService.FaceSide.FaceLeft)
                {
                    lon = ShiftLongitudeOrigin(lon, MHalfPi);
                }*/
            }

            // Skipping geocentric to geodetic conversion, this system assumes coordinates to be
            // stored in the geocentric sphere coordinates.
            // Original comment:
            // Apply the shift from the sphere to the ellipsoid as described
            // in [LK12].
            return (lon, lat);
        }

        /// <summary>
        /// Determine the cube face from the center of projection.
        /// </summary>
        /// <param name="lon">Lam parameter, radians.</param>
        /// <param name="lat">Phi parameter, radians.</param>
        /// <returns>The face in which the coordinates reside.</returns>
        private static ICubeProjectionService.FaceSide GetFaceSide(double lon, double lat)
        {
            ICubeProjectionService.FaceSide face;

            if (lat >= MQuarterPi)
            {
                face = ICubeProjectionService.FaceSide.FaceTop;
            }
            else if (lat <= -MQuarterPi)
            {
                face = ICubeProjectionService.FaceSide.FaceBottom;
            }
            else if (Math.Abs(lon) <= MQuarterPi)
            {
                face = ICubeProjectionService.FaceSide.FaceFront;
            }
            else if (Math.Abs(lon) <= MHalfPi + MQuarterPi)
            {
                face = (lon > 0.0 ? ICubeProjectionService.FaceSide.FaceRight : ICubeProjectionService.FaceSide.FaceLeft);
            }
            else
            {
                face = ICubeProjectionService.FaceSide.FaceBack;
            }

            // Skipping geocentric to geodetic conversion, this system assumes coordinates to be
            // stored in the geocentric sphere coordinates.
            // Original comment:
            // Fill in useful values for the ellipsoid <-> sphere shift
            // described in [LK12].

            return face;
        }
    }
}
