using UnityEngine;

// A small "pop" when something gets hit. A ball grows fast and then goes away.
public class HitEffect : MonoBehaviour
{
    float timer;
    float duration = 0.25f;
    float maxSize = 1f;

    public static void Spawn(Vector3 position, Color color, float size)
    {
        GameObject go = GameManager.MakeShape(PrimitiveType.Sphere, null, color);
        go.name = "HitEffect";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.1f;
        HitEffect fx = go.AddComponent<HitEffect>();
        fx.maxSize = size;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float k = timer / duration;
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, maxSize, k);
        if (k >= 1f) Destroy(gameObject);
    }
}