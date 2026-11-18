using System.Collections.Generic;
using UnityEngine;

// A cannon ball. It uses a real Rigidbody with gravity (3D physics).
public class Projectile : MonoBehaviour
{
    public static readonly List<Projectile> All = new List<Projectile>();

    int damage;
    float lifeLeft = 6f; // remove it after 6 seconds no matter what

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetList() { All.Clear(); }

    public static void Spawn(Vector3 position, Vector3 velocity, int damage, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.45f;
        go.GetComponent<Renderer>().material.color = color;

        // trigger = it doesn't bounce off things, it just tells us when it touches something
        go.GetComponent<SphereCollider>().isTrigger = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = velocity;

        Projectile p = go.AddComponent<Projectile>();
        p.damage = damage;
        All.Add(p);
    }

    void Update()
    {
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f || transform.position.y < -5f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // did we hit an enemy?
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            if (enemy.IsDead) return; // it's already dying, keep flying
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // did we hit the ground?
        if (other.gameObject.name == "Ground")
        {
            HitEffect.Spawn(transform.position, new Color(0.5f, 0.4f, 0.3f), 0.6f);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }
}