using System;

namespace UnityEngineDouble
{
    public struct QuaternionD
    {
        public const double kEpsilon = 1E-06;
        public double x;
        public double y;
        public double z;
        public double w;

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.x;
                    case 1:
                        return this.y;
                    case 2:
                        return this.z;
                    case 3:
                        return this.w;
                    default:
                        throw new IndexOutOfRangeException("Invalid Quaternion index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.x = value;
                        break;
                    case 1:
                        this.y = value;
                        break;
                    case 2:
                        this.z = value;
                        break;
                    case 3:
                        this.w = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Quaternion index!");
                }
            }
        }

        public static QuaternionD identity
        {
            get
            {
                return new QuaternionD(0, 0, 0, 1);
            }
        }

        public static void OrthoNormalize(ref Vector3d normal, ref Vector3d tangent)
        {
            normal.Normalize();
            tangent.Normalize(); // Additional normalization, avoids large tangent norm
            var proj = normal * Vector3d.Dot(tangent, normal);
            tangent = tangent - proj;
            tangent.Normalize();
        }

        public static QuaternionD LookRotation(Vector3d forward, Vector3d up)
        {
            OrthoNormalize(ref forward, ref up);
            Vector3d right = Vector3d.Cross(up, forward);
            double w = Math.Sqrt(1.0d + right.x + up.y + forward.z) * 0.5d;
            double r = 0.25d / w;
            double x = (up.z - forward.y) * r;
            double y = (forward.x - right.z) * r;
            double z = (right.y - up.x) * r;
            return new QuaternionD(x, y, z, w);
        }

        public static QuaternionD AngleAxis(double angle, Vector3d axis)
        {
            axis.Normalize();
            double rad = angle * Math.PI / 180.0; // Convert to radians
            double halfAngle = rad * 0.5;
            double sinHalfAngle = Math.Sin(halfAngle);
            return new QuaternionD(
                axis.x * sinHalfAngle,
                axis.y * sinHalfAngle,
                axis.z * sinHalfAngle,
                Math.Cos(halfAngle)
            );
        }

        public UnityEngine.Quaternion Quaternionf()
        {
            return new UnityEngine.Quaternion((float)this.x, (float)this.y, (float)this.z, (float)this.w);
        }


        public QuaternionD(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public void Set(double new_x, double new_y, double new_z, double new_w)
        {
            this.x = new_x;
            this.y = new_y;
            this.z = new_z;
            this.w = new_w;
        }

        public static QuaternionD EulerDeg(Vector3d vec)
        {
            vec *= 0.0174532924; // Convert to radians
            return CreateFromYawPitchRoll(vec.y, vec.x, vec.z);
        }

        public static QuaternionD EulerRad(Vector3d vec)
        {
            return CreateFromYawPitchRoll(vec.y, vec.x, vec.z);
        }


        public static QuaternionD CreateFromYawPitchRoll(double yaw, double pitch, double roll)
        {
            double sin_roll = (double)Math.Sin((double)roll * 0.5);
            double cos_roll = (double)Math.Cos((double)roll * 0.5);
            double sin_pitch = (double)Math.Sin((double)pitch * 0.5);
            double cos_pitch = (double)Math.Cos((double)pitch * 0.5);
            double sin_yaw = (double)Math.Sin((double)yaw * 0.5);
            double cos_yaw = (double)Math.Cos((double)yaw * 0.5);
            QuaternionD result;
            result.x = cos_yaw * sin_pitch * cos_roll + sin_yaw * cos_pitch * sin_roll;
            result.y = sin_yaw * cos_pitch * cos_roll - cos_yaw * sin_pitch * sin_roll;
            result.z = cos_yaw * cos_pitch * sin_roll - sin_yaw * sin_pitch * cos_roll;
            result.w = cos_yaw * cos_pitch * cos_roll + sin_yaw * sin_pitch * sin_roll;
            return result;
        }

        public static Vector3d operator *(QuaternionD rotation, Vector3d point)
        {
            double num = rotation.x * 2.0;
            double num2 = rotation.y * 2.0;
            double num3 = rotation.z * 2.0;
            double num4 = rotation.x * num;
            double num5 = rotation.y * num2;
            double num6 = rotation.z * num3;
            double num7 = rotation.x * num2;
            double num8 = rotation.x * num3;
            double num9 = rotation.y * num3;
            double num10 = rotation.w * num;
            double num11 = rotation.w * num2;
            double num12 = rotation.w * num3;

            Vector3d result;
            result.x = (1.0 - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
            result.y = (num7 + num12) * point.x + (1 - (num4 + num6)) * point.y + (num9 - num10) * point.z;
            result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (1.0 - (num4 + num5)) * point.z;
            return result;
        }

        public QuaternionD Inverse()
        {
            double norm = x * x + y * y + z * z + w * w;
            if (norm > kEpsilon)
            {
                double invNorm = 1.0 / norm;
                return new QuaternionD(-x * invNorm, -y * invNorm, -z * invNorm, w * invNorm);
            }
            return identity;
        }

        public static QuaternionD operator *(QuaternionD lhs, QuaternionD rhs)
        {
            return new QuaternionD(lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y, lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z, lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x, lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);
        }

        public override string ToString()
        {
            return String.Format("({0:F6}, {1:F6}, {2:F6}, {3:F6})", new object[]
            {
                this.x,
                this.y,
                this.z,
                this.w
            });
        }
        public string ToString(string format)
        {
            return String.Format("({0}, {1}, {2}, {3})", new object[]
            {
                this.x.ToString(format),
                this.y.ToString(format),
                this.z.ToString(format),
                this.w.ToString(format)
            });
        }

    }
}