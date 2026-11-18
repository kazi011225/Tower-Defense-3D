using UnityEngine;

// A tower finds an enemy in range, turns its head to aim, and shoots at a set rate.
public class Tower : MonoBehaviour
{
    TowerType type;
    Transform head;
    float cooldown;

    public void Setup(TowerType towerType, Transform headPart)
    {
        type = towerType;
        head = headPart;
        cooldown = 0f;
    }

    void Update()
    {
        if (type == null || GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);

        Enemy target = PickTarget();
        if (target == null) return;

        Vector3 firePoint = head.position;
        if (!AimAt(target, firePoint, out Vector3 launchVelocity)) return; // too far to reach

        // turn the head so it points the same way the ball will fly
        head.rotation = Quaternion.LookRotation(launchVelocity);

        // keep shooting at a fixed rate
        if (cooldown <= 0f)
        {
            Projectile.Spawn(firePoint, launchVelocity, type.damage, type.color);
            cooldown = type.fireCooldown;
        }
    }

    // pick the enemy in range that is FURTHEST along the path (the most dangerous one)
    Enemy PickTarget()
    {
        Enemy best = null;
        float bestProgress = -1f;
        foreach (Enemy e in Enemy.All)
        {
            if (e == null || e.IsDead) continue;
            Vector3 d = e.Position - transform.position;
            d.y = 0f;
            if (d.magnitude > type.range) continue;
            if (e.DistanceTraveled > bestProgress)
            {
                best = e;
                bestProgress = e.DistanceTraveled;
            }
        }
        return best;
    }

    // Works out the launch velocity.
    // DIRECT mode: aim at where the enemy is right now.
    // PREDICTIVE mode: guess where the enemy will be when the ball lands,
    // using its current position + current velocity. We repeat the guess a few
    // times because a new aim point changes the flight time a little.
    bool AimAt(Enemy target, Vector3 firePoint, out Vector3 launch)
    {
        float g = -Physics.gravity.y;
        Vector3 aimPoint = target.Position;

        if (!Ballistics.Solve(aimPoint - firePoint, type.projectileSpeed, g, out launch, out float flightTime))
            return false;

        if (!GameManager.Instance.usePrediction) return true;

        for (int i = 0; i < 4; i++)
        {
            aimPoint = target.Position + target.Velocity * flightTime;
            if (!Ballistics.Solve(aimPoint - firePoint, type.projectileSpeed, g, out launch, out flightTime))
                return false;
        }
        return true;
    }
}