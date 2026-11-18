using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// ---------- Tower settings ----------
// One of these for each kind of tower. You can change the numbers in the Inspector.
[System.Serializable]
public class TowerType
{
    public string name = "Tower";
    public float height = 2f;            // how tall the tower is
    public float fireCooldown = 1f;      // seconds between shots (small number = shoots faster)
    public float projectileSpeed = 15f;  // how fast the ball leaves the tower
    public int damage = 1;               // how much HP one hit takes away
    public float range = 12f;            // how far the tower can see enemies
    public Color color = Color.blue;
    public GameObject model;             // OPTIONAL: drag a 3D model here (empty = simple cylinder)
    public float modelScale = 1f;        // make the model bigger or smaller
}

// ---------- Enemy settings ----------
[System.Serializable]
public class EnemyType
{
    public string name = "Enemy";
    public string waveLetter = "M";      // letter we use in the waves list
    public float speed = 3f;             // units per second
    public int hp = 5;
    public float size = 1f;
    public bool boxShape = false;        // true = cube, false = capsule
    public Color color = Color.red;
    public GameObject model;             // OPTIONAL: drag a 3D model here
    public float modelScale = 1f;
}

// This script runs the whole game.
// Just put it on ONE empty GameObject in an empty scene and press Play.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Towers")]
    public TowerType[] towerTypes =
    {
        new TowerType { name = "Cannon", height = 2.5f, fireCooldown = 1.0f, projectileSpeed = 16f, damage = 2, range = 14f, color = new Color(0.25f, 0.5f, 1f) },
        new TowerType { name = "Rapid",  height = 1.5f, fireCooldown = 0.3f, projectileSpeed = 13f, damage = 1, range = 10f, color = new Color(1f, 0.8f, 0.2f) },
        new TowerType { name = "Sniper", height = 6f,   fireCooldown = 2.2f, projectileSpeed = 30f, damage = 5, range = 24f, color = new Color(0.7f, 0.3f, 1f) },
    };
    public int startingTowers = 5;       // player can only build this many
    public int bonusTowersPerWave = 1;   // small reward after each wave

    [Header("Enemies")]
    public EnemyType[] enemyTypes =
    {
        new EnemyType { name = "Scout",   waveLetter = "S", speed = 5.5f, hp = 3,  size = 0.7f, color = new Color(1f, 0.9f, 0.1f) },
        new EnemyType { name = "Soldier", waveLetter = "M", speed = 3.5f, hp = 6,  size = 1.0f, color = new Color(0.9f, 0.2f, 0.2f) },
        new EnemyType { name = "Tank",    waveLetter = "T", speed = 2.0f, hp = 20, size = 1.6f, boxShape = true, color = new Color(0.3f, 0.3f, 0.3f) },
    };

    // Each string is one wave. Each letter is one enemy (S = Scout, M = Soldier, T = Tank).
    public string[] waves = { "MMMMM", "SSMMSSMM", "MMTMMSSSM", "SSSSTTMMMM", "TSSTSSTMMMMT" };
    public float secondsBetweenSpawns = 1f;

    [Header("Map")]
    // Enemies walk from the first point to the last point. It zig-zags, so it's not a straight line.
    public Vector3[] pathPoints =
    {
        new Vector3(-24, 0, -12), new Vector3(-10, 0, -12), new Vector3(-10, 0, 8),
        new Vector3(2, 0, 8),     new Vector3(2, 0, -6),    new Vector3(14, 0, -6),
        new Vector3(14, 0, 12),   new Vector3(24, 0, 12),
    };
    public Vector2 mapSize = new Vector2(50, 34);
    public float minDistanceFromPath = 2.2f;
    public float minDistanceBetweenTowers = 2.2f;

    [Header("Aiming")]
    public bool usePrediction = true;    // true = aim where the enemy WILL be (press P in game to switch)

    // ---------- game state ----------
    public bool IsGameOver { get; private set; }
    bool playerWon;
    int towersLeft;
    int nextWave;
    bool waveRunning;
    bool spawning;
    int kills;
    int selectedTower;
    string placeProblem;

    Camera cam;
    GameObject ghost;
    Renderer[] ghostRenderers;
    readonly List<GameObject> towers = new List<GameObject>();

    GUIStyle textStyle, bigStyle, hpStyle;
    Rect helpPanelRect;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        SetupCamera();
        BuildMap();
        ResetGame();
    }

    // ================= INPUT + MAIN LOOP =================

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rKey.wasPressedThisFrame) ResetGame();
            if (kb.pKey.wasPressedThisFrame) usePrediction = !usePrediction;
            if (kb.spaceKey.wasPressedThisFrame) TryStartWave();
            if (kb.digit1Key.wasPressedThisFrame) SelectTower(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectTower(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectTower(2);

            // Q and E turn the camera around the map (like the prof's project)
            float turn = 0f;
            if (kb.qKey.isPressed) turn += 1f;
            if (kb.eKey.isPressed) turn -= 1f;
            if (turn != 0f) cam.transform.RotateAround(Vector3.zero, Vector3.up, turn * 60f * Time.unscaledDeltaTime);
        }

        CheckIfWaveIsDone();
        UpdatePlacement();
    }

    void SelectTower(int index)
    {
        if (index < 0 || index >= towerTypes.Length) return;
        selectedTower = index;
        MakeGhost();
    }

    // ================= TOWER PLACEMENT =================

    void UpdatePlacement()
    {
        placeProblem = null;
        Mouse mouse = Mouse.current;
        if (ghost == null) return;
        if (IsGameOver || mouse == null)
        {
            ghost.SetActive(false);
            return;
        }

        Vector2 mousePos = mouse.position.ReadValue();

        // don't build when the mouse is on top of the help box
        float s = Screen.height / 900f;
        Vector2 guiPos = new Vector2(mousePos.x / s, (Screen.height - mousePos.y) / s);
        if (helpPanelRect.Contains(guiPos))
        {
            ghost.SetActive(false);
            return;
        }

        // shoot a ray from the camera to the flat ground (y = 0)
        Ray ray = cam.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float dist))
        {
            ghost.SetActive(false);
            return;
        }

        Vector3 spot = ray.GetPoint(dist);
        spot.y = 0f;
        placeProblem = CheckSpot(spot);
        bool ok = placeProblem == null;

        ghost.SetActive(true);
        ghost.transform.position = spot;
        Color c = ok ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.25f, 0.25f);
        foreach (Renderer r in ghostRenderers) r.material.color = c;

        if (ok && mouse.leftButton.wasPressedThisFrame)
        {
            PlaceTower(spot);
        }
    }

    // returns null if the spot is OK, or a short reason if it's not
    string CheckSpot(Vector3 p)
    {
        if (towersLeft <= 0) return "no towers left";
        if (Mathf.Abs(p.x) > mapSize.x / 2f - 1f || Mathf.Abs(p.z) > mapSize.y / 2f - 1f) return "outside the map";

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            if (DistanceToSegmentXZ(p, pathPoints[i], pathPoints[i + 1]) < minDistanceFromPath)
                return "too close to the path";
        }

        foreach (GameObject t in towers)
        {
            Vector3 d = t.transform.position - p;
            d.y = 0f;
            if (d.magnitude < minDistanceBetweenTowers) return "too close to another tower";
        }
        return null;
    }

    void PlaceTower(Vector3 spot)
    {
        TowerType type = towerTypes[selectedTower];
        GameObject go = BuildTowerVisual(type, out Transform head);
        go.name = type.name + " Tower";
        go.transform.position = spot;

        Tower tower = go.AddComponent<Tower>();
        tower.Setup(type, head);

        towers.Add(go);
        towersLeft--;
    }

    // Makes the tower body + the turning head on top.
    // Used for the real tower AND the see-through "ghost" preview.
    GameObject BuildTowerVisual(TowerType t, out Transform head)
    {
        GameObject root = new GameObject(t.name);

        if (t.model != null)
        {
            GameObject m = Instantiate(t.model, root.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localScale = Vector3.one * t.modelScale;
            RemoveColliders(m);
        }
        else
        {
            // Unity's cylinder is 2 units tall, so we use height / 2
            GameObject body = MakeShape(PrimitiveType.Cylinder, root.transform, t.color);
            body.transform.localScale = new Vector3(1.4f, t.height / 2f, 1.4f);
            body.transform.localPosition = new Vector3(0f, t.height / 2f, 0f);
        }

        // the head is the part that turns to aim
        GameObject h = MakeShape(PrimitiveType.Cube, root.transform, Darker(t.color));
        h.name = "Head";
        h.transform.localPosition = new Vector3(0f, t.height + 0.4f, 0f);
        h.transform.localScale = new Vector3(0.6f, 0.6f, 1.4f);
        head = h.transform;

        return root;
    }

    void MakeGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = BuildTowerVisual(towerTypes[selectedTower], out _);
        ghost.name = "Ghost (preview)";
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
    }

    // ================= WAVES + ENEMIES =================

    void TryStartWave()
    {
        if (IsGameOver || waveRunning || nextWave >= waves.Length) return;
        StartCoroutine(SpawnWave(waves[nextWave]));
        nextWave++;
        waveRunning = true;
    }

    IEnumerator SpawnWave(string wave)
    {
        spawning = true;
        foreach (char letter in wave)
        {
            EnemyType type = FindEnemyType(letter);
            if (type != null) SpawnEnemy(type);
            yield return new WaitForSeconds(secondsBetweenSpawns);
        }
        spawning = false;
    }

    EnemyType FindEnemyType(char letter)
    {
        foreach (EnemyType t in enemyTypes)
        {
            if (!string.IsNullOrEmpty(t.waveLetter) && char.ToUpper(t.waveLetter[0]) == char.ToUpper(letter)) return t;
        }
        return null;
    }

    void SpawnEnemy(EnemyType t)
    {
        // capsule is 2 units tall, cube is 1 unit tall, so the center height is different
        float halfHeight = t.boxShape ? t.size / 2f : t.size;

        GameObject go = new GameObject(t.name);
        go.transform.position = pathPoints[0] + Vector3.up * halfHeight;

        if (t.model != null)
        {
            GameObject m = Instantiate(t.model, go.transform);
            m.transform.localPosition = new Vector3(0f, -halfHeight, 0f);
            m.transform.localScale = Vector3.one * t.modelScale;
            RemoveColliders(m);
        }
        else
        {
            GameObject shape = MakeShape(t.boxShape ? PrimitiveType.Cube : PrimitiveType.Capsule, go.transform, t.color);
            shape.transform.localScale = Vector3.one * t.size;
        }

        // physics parts: a trigger collider so the projectiles can hit it,
        // and a kinematic rigidbody so we can move it along the path
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = Mathf.Max(0.6f, t.size * 0.7f);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Enemy e = go.AddComponent<Enemy>();
        e.Setup(t, pathPoints, halfHeight);
    }

    void CheckIfWaveIsDone()
    {
        if (IsGameOver || !waveRunning || spawning || Enemy.All.Count > 0) return;

        waveRunning = false;
        if (nextWave >= waves.Length)
        {
            EndGame(true);
        }
        else
        {
            towersLeft += bonusTowersPerWave;
        }
    }

    public void OnEnemyKilled()
    {
        kills++;
    }

    // called by an enemy when it gets to the end of the path
    public void OnEnemyReachedEnd()
    {
        EndGame(false);
    }

    void EndGame(bool won)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        playerWon = won;
        Time.timeScale = 0f; // freeze everything
        if (ghost != null) ghost.SetActive(false);
    }

    void ResetGame()
    {
        StopAllCoroutines();

        // clean up everything from the last game
        foreach (Enemy e in new List<Enemy>(Enemy.All)) if (e != null) Destroy(e.gameObject);
        Enemy.All.Clear();
        foreach (Projectile p in new List<Projectile>(Projectile.All)) if (p != null) Destroy(p.gameObject);
        Projectile.All.Clear();
        foreach (GameObject t in towers) if (t != null) Destroy(t);
        towers.Clear();

        Time.timeScale = 1f;
        IsGameOver = false;
        playerWon = false;
        towersLeft = startingTowers;
        nextWave = 0;
        waveRunning = false;
        spawning = false;
        kills = 0;
        MakeGhost();
    }

    // ================= MAP + CAMERA =================

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0f, 38f, -32f);
        cam.transform.LookAt(new Vector3(0f, 0f, 1f));
    }

    void BuildMap()
    {
        GameObject map = new GameObject("Map");

        // the ground. Projectiles look for the name "Ground", so don't rename it
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(map.transform);
        ground.transform.localScale = new Vector3(mapSize.x, 0.2f, mapSize.y);
        ground.transform.position = new Vector3(0f, -0.1f, 0f);
        ground.GetComponent<Renderer>().material.color = new Color(0.35f, 0.6f, 0.3f);

        // the dirt road that enemies follow
        Color roadColor = new Color(0.75f, 0.6f, 0.4f);
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 a = pathPoints[i];
            Vector3 b = pathPoints[i + 1];
            GameObject road = MakeShape(PrimitiveType.Cube, map.transform, roadColor);
            road.name = "Road " + i;
            road.transform.position = (a + b) / 2f + Vector3.up * 0.02f;
            road.transform.rotation = Quaternion.LookRotation(b - a);
            road.transform.localScale = new Vector3(2.4f, 0.05f, Vector3.Distance(a, b) + 2.4f);
        }

        // green circle = start, red circle = end
        GameObject start = MakeShape(PrimitiveType.Cylinder, map.transform, Color.green);
        start.transform.position = pathPoints[0] + Vector3.up * 0.05f;
        start.transform.localScale = new Vector3(3f, 0.05f, 3f);

        GameObject end = MakeShape(PrimitiveType.Cylinder, map.transform, Color.red);
        end.transform.position = pathPoints[pathPoints.Length - 1] + Vector3.up * 0.05f;
        end.transform.localScale = new Vector3(3f, 0.05f, 3f);
    }

    // ================= ON SCREEN TEXT =================

    void OnGUI()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true, wordWrap = true };
            textStyle.normal.textColor = Color.white;
            bigStyle = new GUIStyle(textStyle) { fontSize = 44, alignment = TextAnchor.MiddleCenter };
            hpStyle = new GUIStyle(textStyle) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        }

        // scale the UI so it looks the same on any screen size
        float s = Screen.height / 900f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float screenW = Screen.width / s;
        float screenH = 900f;

        // HP numbers over each enemy
        if (cam != null)
        {
            foreach (Enemy e in Enemy.All)
            {
                if (e == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * (e.HalfHeight + 0.8f));
                if (sp.z < 0f) continue;
                GUI.Label(new Rect(sp.x / s - 40f, (Screen.height - sp.y) / s - 10f, 80f, 20f), e.Hp + " / " + e.MaxHp, hpStyle);
            }
        }

        // help box in the top-left corner
        string help = BuildHelpText();
        float panelW = 480f;
        float textH = textStyle.CalcHeight(new GUIContent(help), panelW - 24f);
        helpPanelRect = new Rect(10f, 10f, panelW, textH + 24f);
        GUI.Box(helpPanelRect, GUIContent.none);
        GUI.Box(helpPanelRect, GUIContent.none); // drawn twice so it's darker and easier to read
        GUI.Label(new Rect(22f, 22f, panelW - 24f, textH), help, textStyle);

        // big message in the middle when the game ends
        if (IsGameOver)
        {
            string msg = playerWon
                ? "<color=#7CFC00>YOU WIN!</color>\n<size=24>All waves cleared. Press R to play again.</size>"
                : "<color=#FF5050>GAME OVER</color>\n<size=24>An enemy made it to the end. Press R to try again.</size>";
            Rect r = new Rect(screenW / 2f - 330f, screenH / 2f - 90f, 660f, 180f);
            GUI.Box(r, GUIContent.none);
            GUI.Box(r, GUIContent.none);
            GUI.Label(r, msg, bigStyle);
        }
        else if (!waveRunning && nextWave < waves.Length)
        {
            Rect r = new Rect(screenW / 2f - 330f, screenH - 80f, 660f, 60f);
            GUI.Box(r, GUIContent.none);
            GUI.Label(r, "<size=24>Build your towers, then press <b>SPACE</b> to send wave " + (nextWave + 1) + "</size>", bigStyle);
        }
    }

    string BuildHelpText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b><size=22>TOWER DEFENSE</size></b>");
        sb.AppendLine("<b>HOW TO PLACE A TOWER</b>");
        sb.AppendLine("1. Press <b>1</b>, <b>2</b> or <b>3</b> to pick a tower type");
        sb.AppendLine("2. Move the mouse over the grass: <color=#7CFC00>green</color> preview = OK, <color=#FF5050>red</color> = can't build there");
        sb.AppendLine("3. <b>Left-click</b> to build it (not on the road)");
        sb.AppendLine();

        for (int i = 0; i < towerTypes.Length; i++)
        {
            TowerType t = towerTypes[i];
            string line = "[" + (i + 1) + "] " + t.name + ": shoots every " + t.fireCooldown.ToString("0.0") +
                          "s, ball speed " + t.projectileSpeed + ", height " + t.height + ", damage " + t.damage;
            if (i == selectedTower) sb.AppendLine("<color=#FFD700><b>> " + line + "</b></color>");
            else sb.AppendLine("   " + line);
        }
        sb.AppendLine();

        sb.AppendLine("Towers left: <b>" + towersLeft + "</b>   (+" + bonusTowersPerWave + " for each wave you clear)");
        if (placeProblem != null && !IsGameOver)
            sb.AppendLine("<color=#FF5050>Can't build here: " + placeProblem + "</color>");
        sb.AppendLine("Wave: <b>" + nextWave + " / " + waves.Length + "</b>     Kills: <b>" + kills + "</b>");
        sb.AppendLine("Aim mode: <b>" + (usePrediction ? "PREDICTIVE" : "DIRECT") + "</b>  (press P to switch)");
        sb.Append("SPACE = next wave    Q / E = turn camera    R = restart");
        return sb.ToString();
    }

    // ================= SMALL HELPERS =================

    // makes a Unity shape with NO collider (just for looks)
    public static GameObject MakeShape(PrimitiveType type, Transform parent, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        if (parent != null) go.transform.SetParent(parent, false);
        go.GetComponent<Renderer>().material.color = color;
        return go;
    }

    static void RemoveColliders(GameObject go)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Destroy(c);
    }

    static Color Darker(Color c)
    {
        return new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 1f);
    }

    static float DistanceToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 P = new Vector2(p.x, p.z), A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z);
        Vector2 ab = B - A;
        float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(P - A, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(P, A + ab * t);
    }
}