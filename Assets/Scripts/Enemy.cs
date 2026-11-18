using System.Collections.Generic;
using UnityEngine;

// An enemy walks from point to point along the path.
// If it gets to the last point, the player loses.
public class Enemy : MonoBehaviour
{
    // list of all enemies alive right now (towers use this to find targets)
    public static readonly List<Enemy> All = new List<Enemy>();

    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public bool IsDead { get; private set; }
    public float HalfHeight { get; private set; }
    public float DistanceTraveled { get; private set; }
    public Vector3 Velocity { get; private set; }               // towers read this to predict
    public Vector3 Position => rb != null ? rb.position : transform.position;

    EnemyType type;
    Vector3[] path;
    int nextPoint;
    Rigidbody rb;

    // clear the list when you press Play (needed if "domain reload" is turned off)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetList() { All.Clear(); }

    public void Setup(EnemyType enemyType, Vector3[] pathPoints, float halfHeight)
    {
        type = enemyType;
        path = pathPoints;
        HalfHeight = halfHeight;
        MaxHp = Hp = enemyType.hp;
        rb = GetComponent<Rigidbody>();
        nextPoint = 1;
        Velocity = DirectionToNextPoint(transform.position) * type.speed;
        All.Add(this);
    }

    void FixedUpdate()
    {
        if (IsDead || path == null || nextPoint >= path.Length) return;

        Vector3 pos = rb.position;
        Vector3 target = path[nextPoint] + Vector3.up * HalfHeight;
        Vector3 toTarget = target - pos;
        float dist = toTarget.magnitude;
        float step = type.speed * Time.fixedDeltaTime;

        Vector3 newPos;
        if (step >= dist)
        {
            // we reached this corner, go to the next one
            newPos = target;
            nextPoint++;
            if (nextPoint >= path.Length)
            {
                rb.MovePosition(newPos);
                Velocity = Vector3.zero;
                GameManager.Instance.OnEnemyReachedEnd();
                return;
            }
        }
        else
        {
            newPos = pos + toTarget / dist * step;
        }

        rb.MovePosition(newPos);
        DistanceTraveled += step;

        // velocity = which way we're going * how fast
        Vector3 dir = DirectionToNextPoint(newPos);
        Velocity = dir * type.speed;
        if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));
    }

    Vector3 DirectionToNextPoint(Vector3 from)
    {
        if (nextPoint >= path.Length) return Vector3.zero;
        Vector3 d = path[nextPoint] + Vector3.up * HalfHeight - from;
        d.y = 0f;
        return d.normalized;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        Hp -= amount;
        HitEffect.Spawn(transform.position, Color.white, 0.8f);

        if (Hp <= 0)
        {
            IsDead = true;
            All.Remove(this);
            HitEffect.Spawn(transform.position, new Color(1f, 0.5f, 0.1f), HalfHeight * 3f);
            GameManager.Instance.OnEnemyKilled();
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }
}