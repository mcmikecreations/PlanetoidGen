using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using System;

namespace PlanetoidGen.BusinessLogic.Common.Services.Generation
{
    public class QuadSphereCubeProjectionService : ICubeProjectionService
    {
        /// <returns>Lon, lat pair.</returns>
        private (double, double) Inverse(ICubeProjectionService.FaceSide face, double x, double y)
        {
            var chi = InverseDistort(x, y);
            var psi = InverseDistort(y, x);

            return Tangential.Inverse(face, chi, psi);
        }

        /// <returns>Face, x, y.</returns>
        private (ICubeProjectionService.FaceSide, double, double) Forward(double phi, double theta)
        {
            (var face, var chi, var psi) = Tangential.Forward(phi, theta);

            return (face, ForwardDistort(chi, psi), ForwardDistort(psi, chi));
        }

        private double ForwardDistort(double chi, double psi)
        {
            var chi2 = chi * chi;
            var chi3 = chi * chi * chi;
            var psi2 = psi * psi;
            var omchi2 = 1.0 - chi2;

            return chi * (1.37484847732 - 0.37484847732 * chi2) +
                    chi * psi2 * omchi2 * (-0.13161671474 +
                                     0.136486206721 * chi2 +
                                     (1.0 - psi2) *
                                     (0.141189631152 +
                                      psi2 * (-0.281528535557 + 0.106959469314 * psi2) +
                                      chi2 * (0.0809701286525 +
                                            0.15384112876 * psi2 -
                                            0.178251207466 * chi2))) +
                    chi3 * omchi2 * (-0.159596235474 -
                                (omchi2 * (0.0759196200467 - 0.0217762490699 * chi2)));
        }

        private double InverseDistort(double x, double y)
        {
            var x2 = x * x;
            var x4 = x2 * x2;
            var x6 = x4 * x2;
            var x8 = x4 * x4;
            var x10 = x8 * x2;
            var x12 = x8 * x4;

            var y2 = y * y;
            var y4 = y2 * y2;
            var y6 = y4 * y2;
            var y8 = y4 * y4;
            var y10 = y8 * y2;
            var y12 = y8 * y4;

            return x + x * (1 - x2) *
                    (-0.27292696 - 0.07629969 * x2 -
                     0.22797056 * x4 + 0.54852384 * x6 -
                     0.62930065 * x8 + 0.25795794 * x10 +
                     0.02584375 * x12 - 0.02819452 * y2 -
                     0.01471565 * x2 * y2 + 0.48051509 * x4 * y2 -
                     1.74114454 * x6 * y2 + 1.71547508 * x8 * y2 -
                     0.53022337 * x10 * y2 + 0.27058160 * y4 -
                     0.56800938 * x2 * y4 + 0.30803317 * x4 * y4 +
                     0.98938102 * x6 * y4 - 0.83180469 * x8 * y4 -
                     0.60441560 * y6 + 1.50880086 * x2 * y6 -
                     0.93678576 * x4 * y6 + 0.08693841 * x6 * y6 +
                     0.93412077 * y8 - 1.41601920 * x2 * y8 +
                     0.33887446 * x4 * y8 - 0.63915306 * y10 +
                     0.52032238 * x2 * y10 + 0.14381585 * y12);
        }

        /// <inheritdoc/>
        (ICubeProjectionService.FaceSide, double, double) ICubeProjectionService.Forward(double lon, double lat)
        {
            (var face, var x, var y) = Forward(lon, lat);

            return (face, x, y);
        }

        /// <inheritdoc/>
        (double, double) ICubeProjectionService.Inverse(ICubeProjectionService.FaceSide face, double x, double y)
        {
            return Inverse(face, x, y);
        }

        public BoundingBoxCoordinateModel ToBoundingBox(CubicCoordinateModel point, ICoordinateMappingService parent)
        {
            var pointC = parent.ToCubic(parent.ToPlanar(point));

            int planetoidId = pointC.PlanetoidId;
            short face = pointC.Face;
            short z = pointC.Z;
            // Map x,y index + zoom to [-1,1]
            var max = parent.TileSizeCubic(z);

            pointC = new CubicCoordinateModel(planetoidId, face, z, pointC.X - max * 0.5, pointC.Y);
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
        /// Tangential conversions class. From https://github.com/cix/QuadSphere
        /// </summary>
        private class Tangential
        {
            /// <summary>
            /// Information for each face.
            /// </summary>
            /// <param name="index">Faces are given in the order: top, front, left, back, right, bottom.</param>
            /// <returns>
            /// The direction cosines:
            /// 1. l (cos(θ)*cos(φ))
            /// 2. m (cos(θ)*sin(φ))
            /// 3. n (sin(θ))
            /// </returns>
            public static Func<double, double, double, double[]> ForwardParameters(ICubeProjectionService.FaceSide index)
            {
                switch (index)
                {
                    case ICubeProjectionService.FaceSide.FaceTop:
                        return (l, m, n) => new double[] { m, -l, n };
                    case ICubeProjectionService.FaceSide.FaceFront:
                        return (l, m, n) => new double[] { m, n, l };
                    case ICubeProjectionService.FaceSide.FaceRight:
                        return (l, m, n) => new double[] { -l, n, m };
                    case ICubeProjectionService.FaceSide.FaceBack:
                        return (l, m, n) => new double[] { -m, n, -l };
                    case ICubeProjectionService.FaceSide.FaceLeft:
                        return (l, m, n) => new double[] { l, n, -m };
                    case ICubeProjectionService.FaceSide.FaceBottom:
                        return (l, m, n) => new double[] { m, l, -n };
                    default:
                        return ForwardParameters(ICubeProjectionService.FaceSide.FaceTop);
                }
            }

            /// <summary>
            /// Computes the projection of a point on the surface of the sphere,
            /// given in spherical coordinates (φ,θ), to a point of cartesian
            /// coordinates (χ,ψ) on one of the six cube faces.
            /// </summary>
            /// <param name="phi">[-π;π] or [0;2π] The φ angle in radians, this is the azimuth, or longitude (spherical, not geodetic)</param>
            /// <param name="theta">[-π/2;π/2] The θ angle in radians, this is the elevation, or latitude (spherical, not geodetic)</param>
            /// <returns>
            /// An array of three elements: the identifier of
            /// the face (see constants in <seealso cref="CoordinateMappingService"/>),
            /// the χ coordinate of the projected point,
            /// and the ψ coordinate of the projected point.
            /// Both coordinates will be in the range -1 to 1.
            /// </returns>
            public static (ICubeProjectionService.FaceSide, double, double) Forward(double phi, double theta)
            {
                var l = Math.Cos(theta) * Math.Cos(phi);
                var m = Math.Cos(theta) * Math.Sin(phi);
                var n = Math.Sin(theta);

                var max = double.MinValue;
                var face = ICubeProjectionService.FaceSide.FaceTop;
                var tArr1 = new[]
                {
                    (n, ICubeProjectionService.FaceSide.FaceTop),
                    (l, ICubeProjectionService.FaceSide.FaceFront),
                    (m, ICubeProjectionService.FaceSide.FaceRight),
                    (-l, ICubeProjectionService.FaceSide.FaceBack),
                    (-m, ICubeProjectionService.FaceSide.FaceLeft),
                    (-n, ICubeProjectionService.FaceSide.FaceBottom),
                };
                for (var i = 0; i < tArr1.Length; ++i)
                {
                    var (v, f) = tArr1[i];
                    if (v > max)
                    {
                        max = v;
                        face = f;
                    }
                }

                var tArr2 = ForwardParameters(face)(l, m, n);
                var xi = tArr2[0];
                var eta = tArr2[1];
                var zeta = tArr2[2];

                var chi = xi / zeta;
                var psi = eta / zeta;

                return (face, chi, psi);
            }

            /// <summary>
            /// Information for each face.
            /// </summary>
            /// <param name="index">Faces are given in the order: top, front, left, back, right, bottom.</param>
            /// <returns>
            /// The direction cosines:
            /// 1. l (cos(θ)*cos(φ))
            /// 2. m (cos(θ)*sin(φ))
            /// 3. n (sin(θ))
            /// </returns>
            public static Func<double, double, double, double[]> InverseParameters(ICubeProjectionService.FaceSide index)
            {
                switch (index)
                {
                    case ICubeProjectionService.FaceSide.FaceTop:
                        return (xi, eta, zeta) => new double[] { -eta, xi, zeta };
                    case ICubeProjectionService.FaceSide.FaceFront:
                        return (xi, eta, zeta) => new double[] { zeta, xi, eta };
                    case ICubeProjectionService.FaceSide.FaceRight:
                        return (xi, eta, zeta) => new double[] { -xi, zeta, eta };
                    case ICubeProjectionService.FaceSide.FaceBack:
                        return (xi, eta, zeta) => new double[] { -zeta, -xi, eta };
                    case ICubeProjectionService.FaceSide.FaceLeft:
                        return (xi, eta, zeta) => new double[] { xi, -zeta, eta };
                    case ICubeProjectionService.FaceSide.FaceBottom:
                        return (xi, eta, zeta) => new double[] { eta, xi, -zeta };
                    default:
                        return InverseParameters(ICubeProjectionService.FaceSide.FaceTop);
                }
            }

            /// <summary>
            /// Computes the projection of a point at cartesian coordinates
            /// (χ,ψ) on one of the six cube faces, to a point at spherical
            /// coordinates (φ,θ) on the surface of the sphere.
            /// </summary>
            /// <param name="face">[0,5] Face (Integer) the identifier of the cube face.</param>
            /// <param name="chi">[-1.0;1.0] The χ coordinate of the point within the face.</param>
            /// <param name="psi">[-1.0;1.0] The ψ coordinate of the point within the face.</param>
            /// <returns>
            /// An array of two elements:
            /// The φ angle in radians (azimuth or longitude - spherical, not geodetic), from -π to π;
            /// and the θ angle in radians, from -π/2 to π/2 (elevation or latitude - spherical, not geodetic).
            /// </returns>
            public static (double, double) Inverse(ICubeProjectionService.FaceSide face, double chi, double psi)
            {
                var zeta = 1.0 / Math.Sqrt(1.0 + chi * chi + psi * psi);
                var xi = chi * zeta;
                var eta = psi * zeta;

                var tArr3 = InverseParameters(face)(xi, eta, zeta);
                var l = tArr3[0];
                var m = tArr3[1];
                var n = tArr3[2];

                return (Math.Atan2(m, l), Math.Asin(n)); //φ,θ
            }
        }
    }
}
