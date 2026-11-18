using UnityEngine;

// Math for throwing a ball with gravity so it lands on a target.
// Same idea as GetLaunchVector3D in the prof's Projectile.cs,
// but it also gives back the flight time (we need it for prediction).
public static class Ballistics
{
    // displacement = target - start
    // speed = launch speed
    // g = gravity strength (positive number, like 9.81)
    // returns false if the target is too far for this speed
    public static bool Solve(Vector3 displacement, float speed, float g, out Vector3 velocity, out float time)
    {
        velocity = Vector3.zero;
        time = 0f;

        // We solve A*(t^2)^2 + B*(t^2) + C = 0 for t^2
        float A = 0.25f * g * g;
        float B = -(speed * speed - g * displacement.y);
        float C = displacement.sqrMagnitude;

        float discriminant = B * B - 4f * A * C;
        if (discriminant < 0f) return false; // can't reach it

        // smaller answer = low, flat shot (faster to hit)
        float t2 = (-B - Mathf.Sqrt(discriminant)) / (2f * A);
        if (t2 <= 0f) return false;

        time = Mathf.Sqrt(t2);
        velocity = new Vector3(
            displacement.x / time,
            displacement.y / time + 0.5f * g * time,
            displacement.z / time);
        return true;
    }
}