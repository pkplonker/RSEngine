using System.Numerics;

namespace Engine;

public static class MathExtensions
{
	public static float DegreesToRadians(float degrees) => MathF.PI / 180f * degrees;

	public static Vector3 ToEulerDegrees(this Quaternion q)
	{
		Vector3 euler = q.ToEulerRadians();

		euler.X = RadiansToDegrees(euler.X);
		euler.Y = RadiansToDegrees(euler.Y);
		euler.Z = RadiansToDegrees(euler.Z);

		return euler;
	}

	public static Vector3 ToEulerRadians(this Quaternion q)
	{
		Vector3 euler;

		double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
		double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
		euler.X = (float) Math.Atan2(sinr_cosp, cosr_cosp);

		double sinp = 2 * (q.W * q.Y - q.Z * q.X);
		if (Math.Abs(sinp) >= 1)
			euler.Y = (float) Math.CopySign(Math.PI / 2, sinp);
		else
			euler.Y = (float) Math.Asin(sinp);

		double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
		double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
		euler.Z = (float) Math.Atan2(siny_cosp, cosy_cosp);

		return euler;
	}

	public static Quaternion ToQuaternionFromDegrees(this Vector3 eulerDegrees)
	{
		var eulerRadians = new Vector3(
			DegreesToRadians(eulerDegrees.X),
			DegreesToRadians(eulerDegrees.Y),
			DegreesToRadians(eulerDegrees.Z));

		return eulerRadians.ToQuaternionFromRadians();
	}

	public static Quaternion ToQuaternionFromRadians(this Vector3 euler)
	{
		var yaw = euler.Z;
		var pitch = euler.Y;
		var roll = euler.X;

		double cy = Math.Cos(yaw * 0.5);
		double sy = Math.Sin(yaw * 0.5);
		double cp = Math.Cos(pitch * 0.5);
		double sp = Math.Sin(pitch * 0.5);
		double cr = Math.Cos(roll * 0.5);
		double sr = Math.Sin(roll * 0.5);

		Quaternion q;
		q.W = (float) (cr * cp * cy + sr * sp * sy);
		q.X = (float) (sr * cp * cy - cr * sp * sy);
		q.Y = (float) (cr * sp * cy + sr * cp * sy);
		q.Z = (float) (cr * cp * sy - sr * sp * cy);

		return q;
	}

	public static float RadiansToDegrees(float radians) => (float) (radians * 180.0 / Math.PI);
}