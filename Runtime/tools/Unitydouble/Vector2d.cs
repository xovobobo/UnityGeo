// Type: UnityEngine.Vector2
// Assembly: UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Assembly location: C:\Program Files (x86)\Unity\Editor\Data\Managed\UnityEngine.dll
using System;

namespace UnityEngineDouble {

    [System.Serializable]
    public struct Vector2d {
		const double EPSILON_MAGNITUDE = 9.99999974737875E-06;		// ~= 1e-5
		const double EPSILON_MAGNITUDE_SQR = EPSILON_MAGNITUDE*EPSILON_MAGNITUDE;

		public const double kEpsilon = 1E-05d;		// Unused? Should be merged with EPSILON_MAGNITUDE?
        public double x;
        public double y;

        public double this[int index] {
            get {
                switch (index) {
                    case 0:
                        return this.x;
                    case 1:
                        return this.y;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2d index!");
                }
            }
            set {
                switch (index) {
                    case 0:
                        this.x = value;
                        break;
                    case 1:
                        this.y = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2d index!");
                }
            }
        }

        public Vector2d normalized {
            get {
                Vector2d vector2d = new Vector2d(this.x, this.y);
                vector2d.Normalize();
                return vector2d;
            }
        }

        public double magnitude {
            get {
                return Mathd.Sqrt(this.x * this.x + this.y * this.y);
            }
        }

        public double sqrMagnitude {
            get {
                return this.x * this.x + this.y * this.y;
            }
        }

        public static Vector2d zero {
            get {
                return new Vector2d(0.0d, 0.0d);
            }
        }

        public static Vector2d one {
            get {
                return new Vector2d(1d, 1d);
            }
        }

        public static Vector2d up {
            get {
                return new Vector2d(0.0d, 1d);
            }
        }

        public static Vector2d right {
            get {
                return new Vector2d(1d, 0.0d);
            }
        }

        public Vector2d(double x, double y) {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2d(Vector3d v) {
            return new Vector2d(v.x, v.y);
        }

        public static implicit operator Vector3d(Vector2d v) {
            return new Vector3d(v.x, v.y, 0.0d);
        }

        public static Vector2d operator +(Vector2d a, Vector2d b) {
            return new Vector2d(a.x + b.x, a.y + b.y);
        }

        public static Vector2d operator -(Vector2d a, Vector2d b) {
            return new Vector2d(a.x - b.x, a.y - b.y);
        }

        public static Vector2d operator -(Vector2d a) {
            return new Vector2d(-a.x, -a.y);
        }

        public static Vector2d operator *(Vector2d a, double d) {
            return new Vector2d(a.x * d, a.y * d);
        }

        public static Vector2d operator *(double d, Vector2d a) {
            return new Vector2d(a.x * d, a.y * d);
        }

        public static Vector2d operator /(Vector2d a, double d) {
            return new Vector2d(a.x / d, a.y / d);
        }

        public static bool operator ==(Vector2d lhs, Vector2d rhs) {
			// Implementation similar to Vector3
            return Vector2d.SqrMagnitude(lhs - rhs) < EPSILON_MAGNITUDE_SQR;
        }

        public static bool operator !=(Vector2d lhs, Vector2d rhs) {
            return !(lhs==rhs);
        }

        public void Set(double new_x, double new_y) {
            this.x = new_x;
            this.y = new_y;
        }

        public static Vector2d Lerp(Vector2d from, Vector2d to, double t) {
            t = Mathd.Clamp01(t);
            return new Vector2d(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t);
        }

        public static Vector2d MoveTowards(Vector2d current, Vector2d target, double maxDistanceDelta) {
            Vector2d vector2 = target - current;
            double magnitude = vector2.magnitude;
            if (magnitude <= maxDistanceDelta || magnitude == 0.0d)
                return target;
            else
                return current + vector2 / magnitude * maxDistanceDelta;
        }

        public static Vector2d Scale(Vector2d a, Vector2d b) {
            return new Vector2d(a.x * b.x, a.y * b.y);
        }

        public void Scale(Vector2d scale) {
            this.x *= scale.x;
            this.y *= scale.y;
        }

        public void Normalize() {
            double magnitude = this.magnitude;
            if (magnitude > EPSILON_MAGNITUDE)
                this = this / magnitude;
            else
                this = Vector2d.zero;
        }

        public override string ToString() {
            /*
      string fmt = "({0:D1}, {1:D1})";
      object[] objArray = new object[2];
      int index1 = 0;
      // ISSUE: variable of a boxed type
      __Boxed<double> local1 = (ValueType) this.x;
      objArray[index1] = (object) local1;
      int index2 = 1;
      // ISSUE: variable of a boxed type
      __Boxed<double> local2 = (ValueType) this.y;
      objArray[index2] = (object) local2;
      */
            return "not implemented";
        }

        public string ToString(string format) {
            /* TODO:
      string fmt = "({0}, {1})";
      object[] objArray = new object[2];
      int index1 = 0;
      string str1 = this.x.ToString(format);
      objArray[index1] = (object) str1;
      int index2 = 1;
      string str2 = this.y.ToString(format);
      objArray[index2] = (object) str2;
      */
            return "not implemented";
        }

        public override int GetHashCode() {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2;
        }

        public override bool Equals(object other) {
            if (!(other is Vector2d))
                return false;
            Vector2d vector2d = (Vector2d)other;
            if (this.x.Equals(vector2d.x))
                return this.y.Equals(vector2d.y);
            else
                return false;
        }

        public static double Dot(Vector2d lhs, Vector2d rhs) {
            return lhs.x * rhs.x + lhs.y * rhs.y;
        }

        public static double Angle(Vector2d from, Vector2d to) {
            return Mathd.Acos(Mathd.Clamp(Vector2d.Dot(from.normalized, to.normalized), -1d, 1d)) * 57.29578d;
        }

        public static double Distance(Vector2d a, Vector2d b) {
            return (a - b).magnitude;
        }

        public static Vector2d ClampMagnitude(Vector2d vector, double maxLength) {
            if (vector.sqrMagnitude > maxLength * maxLength)
                return vector.normalized * maxLength;
            else
                return vector;
        }

        public static double SqrMagnitude(Vector2d a) {
            return (a.x * a.x + a.y * a.y);
        }

        public double SqrMagnitude() {
            return (this.x * this.x + this.y * this.y);
        }

        public static Vector2d Min(Vector2d lhs, Vector2d rhs) {
            return new Vector2d(Mathd.Min(lhs.x, rhs.x), Mathd.Min(lhs.y, rhs.y));
        }

        public static Vector2d Max(Vector2d lhs, Vector2d rhs) {
            return new Vector2d(Mathd.Max(lhs.x, rhs.x), Mathd.Max(lhs.y, rhs.y));
        }
    }
}