using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Standard.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Helpers
{
    public static class AssimpHelpers
    {
        public static Vector3D Lerp(Vector3D a, Vector3D b, float percentage)
        {
            return (1f - percentage) * a + percentage * b;
        }

        /// <summary>
        /// Calculate the plane equation from 3 points.
        /// </summary>
        /// <returns><c>v.x * x + v.y * y + v.z * z + f = 0</c></returns>
        public static (Vector3D, float) PlaneNormal(Vector3D x1, Vector3D x2, Vector3D x3)
        {
            var p = x2 - x1;
            var q = x3 - x1;
            var cross = Vector3D.Cross(p, q);
            float d = -Vector3D.Dot(cross, x1);
            return (cross, d);
        }

        /// <summary>
        /// Calculate the plane equation from 3 points.
        /// </summary>
        /// <returns>Pair of point on a plane and normalized normal.</returns>
        public static (Vector3D, Vector3D) PlanePointNormal(Vector3D x1, Vector3D x2, Vector3D x3, bool normalize = true)
        {
            var p = x2 - x1;
            var q = x3 - x1;
            var normal = Vector3D.Cross(p, q);
            if (normalize) normal.Normalize();
            return (x1, normal);
        }

        /// <summary>
        /// Calculate the plane-line intersection.
        /// </summary>
        /// <param name="p0">Line segment start.</param>
        /// <param name="p1">Line segment end.</param>
        /// <param name="planeP">Plane point.</param>
        /// <param name="planeN">Plane normal. Doesn't need to be normalized.</param>
        /// <returns>Intersection point of p0p1 and plane with the p0p1 distance factor, if any. Else returns null and NaN.</returns>
        public static Vector3D? PlaneLineIntersect(Vector3D p0, Vector3D p1, Vector3D planeP, Vector3D planeN, out float fac, float eps = 1e-6f)
        {
            // Inspired by https://stackoverflow.com/a/18543221
            var u = p1 - p0;
            var dot = Vector3D.Dot(planeN, u);

            if (Math.Abs(dot) < eps)
            {
                fac = float.NaN;
                return null;
            }

            var w = p0 - planeP;

            fac = -Vector3D.Dot(planeN, w) / dot;
            return p0 + u * fac;
        }

        /// <summary>
        /// Calculate distance from point to line. Not line segment.
        /// </summary>
        /// <param name="v">Point.</param>
        /// <param name="a">Line start.</param>
        /// <param name="b">Line end.</param>
        /// <returns></returns>
        public static float LinePointDistance(Vector3D v, Vector3D a, Vector3D b)
        {
            var ab = b - a;
            var av = v - a;
            return Vector3D.Cross(ab, av).Length() / ab.Length();
        }

        /// <summary>
        /// Calculate distance from point to line segment.
        /// </summary>
        /// <param name="v">Point.</param>
        /// <param name="a">Segment start.</param>
        /// <param name="b">Segment end.</param>
        /// <returns></returns>
        public static float LineSegmentPointDistance(Vector3D v, Vector3D a, Vector3D b)
        {
            // Inspired by https://stackoverflow.com/a/36425155
            var ab = b - a;
            var av = v - a;

            // Point is lagging behind start of the segment, so perpendicular distance is not viable.
            if (Vector3D.Dot(av, ab) <= 0.0f)
                return av.Length(); // Use distance to start of segment instead.

            var bv = v - b;

            // Point is advanced past the end of the segment, so perpendicular distance is not viable.
            if (Vector3D.Dot(bv, ab) >= 0.0f)
                return bv.Length(); // Use distance to end of the segment instead.

            return Vector3D.Cross(ab, av).Length() / ab.Length();
        }

        /// <summary>
        /// Calculate the point position in plane-relative coordinate system.
        /// </summary>
        /// <param name="planeP">Plane origin point.</param>
        /// <param name="planeN">Normalized plane normal.</param>
        /// <param name="e1">First plane basis vector.</param>
        /// <param name="e2">Second plane basis vector.</param>
        /// <param name="v">Point to project.</param>
        /// <returns>A pair of e1 factor, e2 factor and planeN factor.</returns>
        public static Vector3D ProjectOntoPlane(Vector3D planeP, Vector3D planeN, Vector3D e1, Vector3D e2, Vector3D v)
        {
            // Inspired by https://stackoverflow.com/a/23474396

            var rel = v - planeP;
            float t1 = Vector3D.Dot(e1, rel);
            float t2 = Vector3D.Dot(e2, rel);
            float s = Vector3D.Dot(planeN, rel);

            return new Vector3D(t1, t2, s);
        }

        public static float LengthSquared(Vector3D a, Vector3D b)
        {
            return (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y) + (b.Z - a.Z) * (b.Z - a.Z);
        }

        public static float Length(Vector3D a, Vector3D b)
        {
            return MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y) + (b.Z - a.Z) * (b.Z - a.Z));
        }

        public static Face Clone(Face source)
        {
            return new Face(source.Indices.ToArray());
        }

        public static Mesh Clone(Mesh source)
        {
            var mesh = new Mesh(source.Name, source.PrimitiveType)
            {
                MaterialIndex = source.MaterialIndex,
                MorphMethod = source.MorphMethod,
            };

            mesh.BiTangents.AddRange(source.BiTangents);
            mesh.Tangents.AddRange(source.Tangents);
            mesh.Bones.AddRange(source.Bones);
            mesh.Faces.AddRange(source.Faces);
            mesh.MeshAnimationAttachments.AddRange(source.MeshAnimationAttachments);
            mesh.Normals.AddRange(source.Normals);
            mesh.Vertices.AddRange(source.Vertices);

            for (var i = 0; i < source.TextureCoordinateChannelCount; ++i)
            {
                mesh.TextureCoordinateChannels[i].AddRange(source.TextureCoordinateChannels[i]);
            }

            for (var i = 0; i < source.VertexColorChannelCount; ++i)
            {
                mesh.VertexColorChannels[i].AddRange(source.VertexColorChannels[i]);
            }

            return mesh;
        }

        public static Material Clone(Material source)
        {
            return new Material()
            {
                BlendMode = source.BlendMode,
                BumpScaling = source.BumpScaling,
                ColorAmbient = source.ColorAmbient,
                ColorDiffuse = source.ColorDiffuse,
                ColorSpecular = source.ColorSpecular,
                ColorEmissive = source.ColorEmissive,
                ColorReflective = source.ColorReflective,
                ColorTransparent = source.ColorTransparent,
                IsTwoSided = source.IsTwoSided,
                IsWireFrameEnabled = source.IsWireFrameEnabled,
                Name = source.Name,
                Opacity = source.Opacity,
                Reflectivity = source.Reflectivity,
                ShadingMode = source.ShadingMode,
                Shininess = source.Shininess,
                ShininessStrength = source.ShininessStrength,
                TextureAmbient = source.TextureAmbient,
                TextureDiffuse = source.TextureDiffuse,
                TextureSpecular = source.TextureSpecular,
                TextureDisplacement = source.TextureDisplacement,
                TextureEmissive = source.TextureEmissive,
                TextureHeight = source.TextureHeight,
                TextureLightMap = source.TextureLightMap,
                TextureNormal = source.TextureNormal,
                TextureOpacity = source.TextureOpacity,
                TextureReflection = source.TextureReflection,
            };
        }

        public static Vector3D Clone(Vector3D source) => new Vector3D(source.X, source.Y, source.Z);

        public static Vector3D VerticalPartRingCenter(VertexRing ring, bool yUp)
        {
            /// v3 --> v4
            /// |       |
            /// v2 <-- v1
            var v1 = ring.Vertices[0]; // We assume that we start from furthest bottom point (reversed order).
            var v4 = ring.Vertices[ring.Vertices.Count - 1];
            var v2 = yUp ? ring.Vertices.Last(x => x.Y == v1.Y) : ring.Vertices.Last(x => x.Z == v1.Z);
            var v3 = yUp ? ring.Vertices.First(x => x.Y == v4.Y) : ring.Vertices.First(x => x.Z == v4.Z);

            return new Vector3D(
                (v1.X + v2.X + v3.X + v4.X) / 4f,
                (v1.Y + v2.Y + v3.Y + v4.Y) / 4f,
                (v1.Z + v2.Z + v3.Z + v4.Z) / 4f
                );
        }

        public static Vector3D SwapYZInPlace(Vector3D source)
        {
            (source.Y, source.Z) = (source.Z, source.Y);
            return source;
        }

        public static Vector3D VerticalPartRingBottomCenter(VertexRing ring, bool yUp)
        {
            /// v3 --> v4
            /// |       |
            /// v2 <-- v1
            var v1 = ring.Vertices[0]; // We assume that we start from furthest bottom point (reversed order).
            var v2 = yUp ? ring.Vertices.Last(x => x.Y == v1.Y) : ring.Vertices.Last(x => x.Z == v1.Z);

            return new Vector3D(
                (v1.X + v2.X) / 2f,
                (v1.Y + v2.Y) / 2f,
                (v1.Z + v2.Z) / 2f
                );
        }

        public static float AngleBetweenVectors(Vector3D v1, Vector3D v2, Vector3D vn)
        {
            // Inspired by https://stackoverflow.com/a/16544330
            /// dot = x1*x2 + y1*y2 + z1*z2
            /// det =
            ///     x1 * y2 * zn +
            ///     x2 * yn * z1 +
            ///     xn * y1 * z2 - z1 * y2 * xn
            ///                  - z2 * yn * x1
            ///                  - zn * y1 * x2
            /// angle = atan2(det, dot)
            float dot = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            float det =
                v1.X * v2.Y * vn.Z +
                v2.X * vn.Y * v1.Z +
                vn.X * v1.Y * v2.Z - v1.Z * v2.Y * vn.X
                                   - v2.Z * vn.Y * v1.X
                                   - vn.Z * v1.Y * v2.X;
            var angle = MathF.Atan2(det, dot);
            return angle;
        }

        [Obsolete("Unverified")]
        public static Vector3D ProjectOntoVector(Vector3D a, Vector3D ontoB)
        {
            var aLen = a.Length();
            var bLen = ontoB.Length();

            return ontoB * (Vector3D.Dot(a, ontoB) / bLen / bLen);
        }

        public static Vector3D[] InsetRing(IList<Vector3D> outerRing, float insetDistance, Vector3D up)
        {
            Vector3D p0, p1, p2, forward1, forward2;
            var result = new Vector3D[outerRing.Count];

            p1 = outerRing[0];
            p2 = outerRing[1];
            var right = p2 - p1;
            right.Normalize();
            forward1 = Vector3D.Cross(right, up);
            var testPoint = (p2 + p1) / 2f + forward1 * insetDistance;
            var negateForward = IsPointInside(outerRing, testPoint);

            for (var i = 0; i < outerRing.Count; ++i)
            {
                p0 = outerRing[MathHelpers.Modulo(i - 1, outerRing.Count)];
                p1 = outerRing[i];
                p2 = outerRing[(i + 1) % outerRing.Count];

                right = p1 - p0;
                right.Normalize();
                forward1 = Vector3D.Cross(right, up) * insetDistance;

                right = p2 - p1;
                right.Normalize();
                forward2 = Vector3D.Cross(right, up) * insetDistance;

                if (negateForward)
                {
                    forward1.X = -forward1.X;
                    forward1.Y = -forward1.Y;
                    forward1.Z = -forward1.Z;
                    forward2.X = -forward2.X;
                    forward2.Y = -forward2.Y;
                    forward2.Z = -forward2.Z;
                }

                result[i] = p1 + forward1 + forward2;
            }

            return result;
        }

        public static bool IsPointInside(IList<Vector3D> polygon, Vector3D point, float maxCrosssectionLength = 9999f, float eps = 1e-6f)
        {
            // https://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/
            if (polygon == null || polygon.Count < 3)
            {
                return false;
            }

            // Create a point at infinity, y is same as point p
            var point2 = new Vector3D(maxCrosssectionLength, point.Y, point.Z);
            var count = 0;
            var i = 0;
            do
            {

                // Forming a line from two consecutive points of
                // poly
                var poly1 = polygon[i];
                var poly2 = polygon[(i + 1) % polygon.Count];
                if (IsIntersect(poly1, poly2, point, point2))
                {

                    // If side is intersects exline
                    if (Direction(poly1, point, poly2, eps) == 0)
                    {
                        return IsOnLine(poly1, poly2, point);
                    }

                    count++;
                }

                i = (i + 1) % polygon.Count;
            } while (i != 0);

            // When count is odd
            return count % 2 == 1;
        }

        private static int Direction(Vector3D a, Vector3D b, Vector3D c, float eps = 1e-6f)
        {
            float val = (b.Y - a.Y) * (c.X - b.X)
            - (b.X - a.X) * (c.Y - b.Y);

            if (MathF.Abs(val) < eps)

                // Colinear
                return 0;

            else if (val < 0f)

                // Anti-clockwise direction
                return 2;

            // Clockwise direction
            return 1;
        }

        private static bool IsOnLine(Vector3D lineStart, Vector3D lineEnd, Vector3D p)
        {
            // Check whether p is on the line or not
            return p.X <= MathF.Max(lineStart.X, lineEnd.X)
                && p.X <= MathF.Min(lineStart.X, lineEnd.X)
                && p.Y <= MathF.Max(lineStart.Y, lineEnd.Y)
                && p.Y <= MathF.Min(lineStart.Y, lineEnd.Y);
        }

        private static bool IsIntersect(Vector3D l1Start, Vector3D l1End, Vector3D l2Start, Vector3D l2End)
        {
            // Four direction for two lines and points of other line
            var dir1 = Direction(l1Start, l1End, l2Start);
            var dir2 = Direction(l1Start, l1End, l2End);
            var dir3 = Direction(l2Start, l2End, l1Start);
            var dir4 = Direction(l2Start, l2End, l1End);

            // When intersecting
            if (dir1 != dir2 && dir3 != dir4)
                return true;

            // When p2 of line2 are on the line1
            if (dir1 == 0 && IsOnLine(l1Start, l1End, l2Start))
                return true;

            // When p1 of line2 are on the line1
            if (dir2 == 0 && IsOnLine(l1Start, l1End, l2End))
                return true;

            // When p2 of line1 are on the line2
            if (dir3 == 0 && IsOnLine(l2Start, l2End, l1Start))
                return true;

            // When p1 of line1 are on the line2
            if (dir4 == 0 && IsOnLine(l2Start, l2End, l1End))
                return true;

            return false;
        }

        public class Vector3DEqualityComparer : IEqualityComparer<Vector3D>
        {
            private readonly float _eps;

            public Vector3DEqualityComparer(float epsilon = 1e-4f)
            {
                _eps = epsilon;
            }

            public bool Equals(Vector3D x, Vector3D y)
            {
                return (x - y).LengthSquared() <= _eps;
            }

            public int GetHashCode(Vector3D obj)
            {
                var hash = new HashCode();
                hash.Add(obj.X);
                hash.Add(obj.Y);
                hash.Add(obj.Z);
                return hash.ToHashCode();
            }
        }
    }
}
