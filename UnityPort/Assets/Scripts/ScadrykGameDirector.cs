using UnityEngine;

/// <summary>Builds the playable three-phase, nine-room Scadryk Unity vertical slice.</summary>
public sealed class ScadrykGameDirector : MonoBehaviour
{
    private const int RoomsPerPhase = 3;
    private const float RoomSpacing = 23f;
    private static readonly string[] PhaseNames = { "Margens do Primeiro Eco", "Bacia do Sal Imóvel", "Costa das Badaladas" };
    private static readonly Color[] PhaseColors = { new Color(0.11f, 0.22f, 0.20f), new Color(0.27f, 0.20f, 0.12f), new Color(0.09f, 0.17f, 0.25f) };
    private static readonly Color[] PhaseWallColors = { new Color(0.17f, 0.29f, 0.26f), new Color(0.35f, 0.27f, 0.16f), new Color(0.13f, 0.28f, 0.36f) };
    private static readonly Color[] PhaseAccentColors = { new Color(0.36f, 0.62f, 0.48f), new Color(0.78f, 0.59f, 0.30f), new Color(0.30f, 0.68f, 0.76f) };
    private static Sprite SquareSprite;
    private static Sprite CircleSprite;
    private Sprite[] environmentSprites;
    private Sprite[] phaseGroundSprites;
    private Sprite[] phaseEnemySprites;
    private Sprite fallenWillowLogSprite;

    private int phaseIndex, roomIndex, enemiesRemaining;
    private bool isPaused, victory;
    private Vector2 roomCenter, exitPosition;
    private ScadrykPlayerController player;
    private ScadrykCameraFollow follow;
    private GameObject levelRoot, gateObject;
    private readonly bool[] clearedRooms = new bool[RoomsPerPhase];
    private readonly string[][] roomNames = {
        new[] { "Margem Alagada", "Vau de Cinza", "Casa das Comportas" },
        new[] { "Juncal Raso", "Bacia Imóvel", "Câmara do Sal" },
        new[] { "Costa das Badaladas", "Fileira das Bóias", "Coro da Maré Falsa" }
    };
    private string statusMessage = "Derrote os ecos para abrir a passagem.";
    private float statusUntil;
    private GUIStyle titleStyle, bodyStyle, smallStyle;
    private GUIStyle centeredTitleStyle, centeredBodyStyle;

    public bool IsPaused => isPaused || victory;
    public int PhaseIndex => phaseIndex;
    public int RoomIndex => roomIndex;
    public int EnemiesRemaining => enemiesRemaining;
    public Vector2 CurrentRoomCenter => roomCenter;
    public Vector2 ExitPosition => exitPosition;

    private void Start()
    {
        EnsureSprites();
        environmentSprites = LoadSheetRow("ScadrykEnvironment", 0, true);
        phaseGroundSprites = new[] { LoadSingleSprite("ForestClearing", 80f), LoadSingleSprite("SaltBasin", 80f), LoadSingleSprite("BellCoast", 80f) };
        phaseEnemySprites = new[] { LoadSingleSprite("Rootbound", 1200f), LoadSingleSprite("SaltCrawler", 1200f), LoadSingleSprite("BellWraith", 1200f) };
        fallenWillowLogSprite = LoadSingleSprite("FallenWillowLog", 300f);
        SetupCamera();
        BuildPhase(0, 0, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !victory) isPaused = !isPaused;
        if (!isPaused && !victory && gateObject == null && enemiesRemaining == 0)
            UnlockExit();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 7.3f;
        cam.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
        follow = cam.GetComponent<ScadrykCameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<ScadrykCameraFollow>();
    }

    private void BuildPhase(int newPhase, int newRoom, bool newGame)
    {
        if (levelRoot != null) Destroy(levelRoot);
        if (gateObject != null) Destroy(gateObject);
        phaseIndex = newPhase; roomIndex = newRoom; victory = false;
        roomCenter = new Vector2(roomIndex * RoomSpacing, 0f);
        exitPosition = roomCenter + Vector2.right * 8.05f;
        levelRoot = new GameObject("Phase " + (phaseIndex + 1) + " map");
        DrawPhaseMap(levelRoot.transform);
        if (player == null)
        {
            var playerObject = new GameObject("Nara — player");
            playerObject.transform.position = roomCenter + Vector2.left * 6.2f;
            playerObject.transform.localScale = new Vector3(0.88f, 0.88f, 1f);
            var playerVisual = new GameObject("Animated visual"); playerVisual.transform.SetParent(playerObject.transform, false);
            var renderer = playerVisual.AddComponent<SpriteRenderer>(); renderer.sprite = CircleSprite; renderer.color = new Color(0.85f, 0.83f, 0.71f); renderer.sortingOrder = 5;
            playerObject.AddComponent<CircleCollider2D>().radius = 0.42f;
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<ScadrykDepthSort>().SetBias(5);
            playerObject.AddComponent<ScadrykActorAnimator>().SetVisual(playerVisual.transform);
            player = playerObject.AddComponent<ScadrykPlayerController>();
            playerObject.AddComponent<ScadrykHealthBar>().Configure(true, false, 1.25f);
            player.SetSpriteFrames(LoadSheetRow("Listener", 0, true));
        }
        else player.ResetForRoom(roomCenter + Vector2.left * 6.2f);
        follow.SetTarget(player.transform);
        SpawnRoomEnemies();
        statusMessage = "Limpe a sala e avance pela passagem.";
        statusUntil = Time.time + 3f;
    }

    private void DrawPhaseMap(Transform parent)
    {
        Color floor = PhaseColors[phaseIndex];
        Color wall = PhaseWallColors[phaseIndex];
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            float x = i * RoomSpacing;
            Color roomFloor = floor * (i == 1 ? 1.08f : 0.96f);
            MakeBox(parent, "Room floor " + (i + 1), new Vector2(x, 0), new Vector2(19f, 13f), roomFloor, false, -10);
            PlacePhaseGround(parent, x, i);
            DrawRoomScenery(parent, i, x, roomFloor);
            MakeBox(parent, "North wall", new Vector2(x, 6.6f), new Vector2(19.5f, 0.5f), wall, true, 0);
            MakeBox(parent, "South wall", new Vector2(x, -6.6f), new Vector2(19.5f, 0.5f), wall, true, 0);
            if (i == 0) MakeBox(parent, "West outer wall", new Vector2(x - 9.7f, 0), new Vector2(0.5f, 13f), wall, true, 0);
            else
            {
                MakeBox(parent, "West wall upper", new Vector2(x - 9.7f, 4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "West wall lower", new Vector2(x - 9.7f, -4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
            }
            if (i == RoomsPerPhase - 1)
            {
                MakeBox(parent, "East wall upper", new Vector2(x + 9.7f, 4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "East wall lower", new Vector2(x + 9.7f, -4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
            }
            MakeBox(parent, "Floor inlay", new Vector2(x, 0), new Vector2(0.1f, 11.8f), new Color(1f, 1f, 1f, 0.055f), false, -9);
            if (i < RoomsPerPhase - 1)
            {
                MakeBox(parent, "Connector floor", new Vector2(x + 11.35f, 0), new Vector2(3.3f, 3.4f), floor * 1.12f, false, -9);
                MakeBox(parent, "Connector north rail", new Vector2(x + 11.35f, 1.92f), new Vector2(3.4f, 0.28f), wall, true, 0);
                MakeBox(parent, "Connector south rail", new Vector2(x + 11.35f, -1.92f), new Vector2(3.4f, 0.28f), wall, true, 0);
            }
            DrawRoomObstacles(parent, i, x);
            MakeLabelMarker(parent, new Vector2(x, 5.35f), (i + 1).ToString(), PhaseAccentColors[phaseIndex]);
        }
    }

    private void PlacePhaseGround(Transform parent, float x, int room)
    {
        if (phaseGroundSprites == null || phaseGroundSprites[phaseIndex] == null) return;
        var ground = new GameObject("Painted top-down biome — " + PhaseNames[phaseIndex]);
        ground.transform.SetParent(parent);
        ground.transform.position = new Vector3(x, 0f, 0f);
        var renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = phaseGroundSprites[phaseIndex];
        renderer.sortingOrder = -8;
        renderer.flipX = room == 1;
        Vector2 size = renderer.sprite.bounds.size;
        ground.transform.localScale = new Vector3(19f / size.x, 13f / size.y, 1f);
    }

    private void DrawRoomScenery(Transform parent, int room, float x, Color floor)
    {
        Color accent = PhaseAccentColors[phaseIndex];
        Color dim = new Color(accent.r, accent.g, accent.b, 0.19f);
        Color glow = new Color(accent.r, accent.g, accent.b, 0.38f);

        if (phaseIndex == 0)
        {
            // Shallow tide pools and reed banks mark the flooded shore without blocking movement.
            for (int side = -1; side <= 1; side += 2)
            {
                float y = side * (room == 1 ? 4.65f : 4.2f);
                MakeDecoration(parent, "Tide pool", new Vector2(x - 5.9f, y), new Vector2(3.0f, 1.05f), new Color(0.08f, 0.30f, 0.30f, 0.52f), true, -9);
                MakeDecoration(parent, "Tide pool", new Vector2(x + 5.6f, -y * 0.78f), new Vector2(2.5f, 0.82f), new Color(0.07f, 0.26f, 0.27f, 0.48f), true, -9);
                for (int reed = 0; reed < 3; reed++)
                {
                    float rx = x - 7.2f + reed * 0.36f;
                    MakeDecoration(parent, "Reed", new Vector2(rx, y - side * 0.6f), new Vector2(0.12f, 0.88f), glow, false, -8, -18f + reed * 16f);
                }
            }
            DrawRune(parent, new Vector2(x + (room - 1) * 1.15f, 4.1f), 1.8f, dim);
        }
        else if (phaseIndex == 1)
        {
            // Pale salt seams and crystal clusters distinguish the dry basin from the shore.
            for (int seam = 0; seam < 5; seam++)
            {
                float sx = x - 5.4f + seam * 2.7f;
                float sy = ((seam + room) % 2 == 0) ? -3.95f : 3.95f;
                MakeDecoration(parent, "Salt vein", new Vector2(sx, sy), new Vector2(2.5f, 0.075f), dim, false, -9, (seam % 2 == 0) ? 12f : -12f);
                MakeDecoration(parent, "Salt vein branch", new Vector2(sx + 0.45f, sy + 0.35f), new Vector2(0.95f, 0.06f), dim, false, -9, -36f);
            }
            for (int cluster = 0; cluster < 3; cluster++)
            {
                float cx = x + (cluster - 1) * 5.8f;
                float cy = cluster == 1 ? 4.65f : -4.55f;
                MakeDecoration(parent, "Salt crystal base", new Vector2(cx, cy), new Vector2(1.55f, 0.68f), new Color(0.12f, 0.09f, 0.055f, 0.45f), true, -8);
                for (int shard = 0; shard < 3; shard++)
                    MakeDecoration(parent, "Salt crystal", new Vector2(cx + (shard - 1) * 0.28f, cy + 0.18f), new Vector2(0.34f, 0.78f - Mathf.Abs(shard - 1) * 0.18f), new Color(0.82f, 0.68f, 0.39f, 0.66f), false, -7, -18f + shard * 18f);
            }
            DrawRune(parent, new Vector2(x, 4.15f), 1.55f, new Color(0.9f, 0.69f, 0.36f, 0.22f));
        }
        else
        {
            // Tidal bands, buoy lamps and bell marks create a colder coastal palette.
            for (int band = 0; band < 3; band++)
            {
                float y = -4.85f + band * 4.85f;
                for (int segment = 0; segment < 4; segment++)
                {
                    float sx = x - 6.4f + segment * 4.25f + ((band + room) % 2) * 0.55f;
                    MakeDecoration(parent, "Tidal line", new Vector2(sx, y), new Vector2(2.8f, 0.075f), dim, false, -9, (segment % 2 == 0) ? -4f : 4f);
                    MakeDecoration(parent, "Tidal foam", new Vector2(sx + 0.5f, y + 0.18f), new Vector2(1.3f, 0.045f), glow, false, -9, -2f);
                }
            }
            for (int buoy = -1; buoy <= 1; buoy += 2)
            {
                MakeDecoration(parent, "Bell buoy lamp", new Vector2(x + buoy * 7.15f, 0), new Vector2(0.72f, 0.72f), new Color(0.78f, 0.61f, 0.30f, 0.82f), true, -7);
                MakeDecoration(parent, "Bell buoy glow", new Vector2(x + buoy * 7.15f, 0), new Vector2(1.55f, 1.55f), new Color(0.24f, 0.72f, 0.78f, 0.16f), true, -8);
            }
            DrawRune(parent, new Vector2(x + (room - 1) * 1.25f, 4.25f), 1.75f, glow);
        }

        // Add fine, low-contrast ground flecks so each room has more depth than a flat fill.
        for (int fleck = 0; fleck < 8; fleck++)
        {
            int seed = room * 17 + phaseIndex * 31 + fleck * 13;
            float fx = x - 7.3f + (seed % 15) * 0.98f;
            float fy = -3.5f + ((seed * 7) % 70) * 0.1f;
            MakeDecoration(parent, "Ground fleck", new Vector2(fx, fy), new Vector2(0.12f, 0.12f), new Color(accent.r, accent.g, accent.b, 0.16f), true, -9);
        }

        DrawPhaseProps(parent, room, x);
    }

    private void DrawPhaseProps(Transform parent, int room, float x)
    {
        if (environmentSprites == null || environmentSprites.Length < 16) return;

        if (phaseIndex == 0)
        {
            // Flooded first-echo shore: willow canopy, exposed roots, reeds, moss and old stones.
            PlaceProp(parent, 0, "Echo willow", new Vector2(x - 7.15f, 3.2f), 2.15f, true, new Vector2(0.24f, 0.22f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Sway, 0.6f);
            PlaceProp(parent, 1, "Root-twisted willow", new Vector2(x + 7.1f, -6.05f), 1.9f, true, new Vector2(0.27f, 0.2f), new Vector2(0f, 0.18f), ScadrykAmbientMotion.Sway, 0.65f);
            PlaceProp(parent, 2, "Cattails", new Vector2(x - 5.8f, 3.55f), 1.45f, true, new Vector2(0.28f, 0.18f), new Vector2(0f, 0.15f), ScadrykAmbientMotion.Sway, 1.25f);
            PlaceProp(parent, 2, "Cattails", new Vector2(x + 5.65f, -6.0f), 1.35f, true, new Vector2(0.27f, 0.18f), new Vector2(0f, 0.15f), ScadrykAmbientMotion.Sway, 1.2f);
            PlaceProp(parent, 3, "Mossy stump", new Vector2(x - 4.2f, -5.65f), 1.15f, true, new Vector2(0.32f, 0.23f), new Vector2(0f, 0.18f));
            PlaceProp(parent, 4, "Mossy shoreline rocks", new Vector2(x + 4.0f, 3.85f), 1.2f, true, new Vector2(0.55f, 0.28f), new Vector2(0f, 0.18f));
            if (room == 2) PlaceProp(parent, 6, "Flooded arch ruin", new Vector2(x - 0.3f, 3.15f), 1.5f, false, Vector2.zero, Vector2.zero);
        }
        else if (phaseIndex == 1)
        {
            // Salt basin: amber crystal growths, blanched rock and shallow mineral water.
            PlaceProp(parent, 8, "Salt crystal cluster", new Vector2(x - 6.0f, -5.95f), 1.55f, true, new Vector2(0.34f, 0.22f), new Vector2(0f, 0.18f), ScadrykAmbientMotion.Glow, 0.65f);
            PlaceProp(parent, 9, "Salt spire", new Vector2(x - 5.3f, 3.7f), 1.55f, true, new Vector2(0.26f, 0.2f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Glow, 0.8f);
            PlaceProp(parent, 10, "Salt-crusted boulder", new Vector2(x + 5.1f, -5.85f), 1.45f, true, new Vector2(0.46f, 0.28f), new Vector2(0f, 0.18f));
            PlaceProp(parent, 11, "Brine pool", new Vector2(x + 1.0f, 3.65f), 1.8f, false, Vector2.zero, Vector2.zero, ScadrykAmbientMotion.Water, 1f);
            PlaceProp(parent, 4, "Salt-buried boulders", new Vector2(x + 5.8f, 3.95f), 1.18f, true, new Vector2(0.52f, 0.28f), new Vector2(0f, 0.18f));
            if (room == 2) PlaceProp(parent, 7, "Basin's standing stone", new Vector2(x - 1.25f, 3.25f), 1.35f, true, new Vector2(0.25f, 0.22f), new Vector2(0f, 0.18f), ScadrykAmbientMotion.Glow, 0.45f);
        }
        else
        {
            // Bell coast: wind-shaped pines, wave-worn rocks, lantern buoys and a broken shrine.
            PlaceProp(parent, 12, "Wind-shaped pine", new Vector2(x - 7.0f, 3.2f), 1.9f, true, new Vector2(0.25f, 0.22f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Sway, 0.9f);
            PlaceProp(parent, 12, "Coastal pine", new Vector2(x + 7.0f, -6.05f), 1.7f, true, new Vector2(0.25f, 0.2f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Sway, 0.75f);
            PlaceProp(parent, 13, "Wave-worn rocks", new Vector2(x - 4.0f, -5.9f), 1.35f, true, new Vector2(0.52f, 0.28f), new Vector2(0f, 0.16f));
            PlaceProp(parent, 14, "Lantern buoy posts", new Vector2(x + 4.7f, 3.75f), 1.35f, true, new Vector2(0.23f, 0.22f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Flicker, 0.8f);
            if (room == 2) PlaceProp(parent, 15, "Bell shrine", new Vector2(x - 0.5f, 3.1f), 1.6f, false, Vector2.zero, Vector2.zero, ScadrykAmbientMotion.Glow, 0.55f);
        }
    }

    private void PlaceProp(Transform parent, int spriteIndex, string name, Vector2 position, float scale, bool solid, Vector2 hitboxSize, Vector2 hitboxOffset, ScadrykAmbientMotion motion = ScadrykAmbientMotion.None, float motionStrength = 1f, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = environmentSprites[spriteIndex];
        renderer.sortingOrder = 1;
        if (motion != ScadrykAmbientMotion.None) visual.AddComponent<ScadrykAmbientProp>().Configure(motion, motionStrength);
        go.AddComponent<ScadrykDepthSort>().SetBias(2);
        if (solid)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = hitboxSize;
            collider.offset = hitboxOffset;
        }
    }

    private static void DrawRune(Transform parent, Vector2 center, float radius, Color color)
    {
        MakeDecoration(parent, "Ancient mark", center + Vector2.up * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 0f);
        MakeDecoration(parent, "Ancient mark", center + Vector2.down * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 0f);
        MakeDecoration(parent, "Ancient mark", center + Vector2.left * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 90f);
        MakeDecoration(parent, "Ancient mark", center + Vector2.right * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 90f);
        MakeDecoration(parent, "Ancient mark", center, new Vector2(0.36f, 0.36f), color, true, -8);
    }

    private void DrawRoomObstacles(Transform parent, int index, float x)
    {
        Vector2[] layouts;
        if (phaseIndex == 0)
        {
            if (index == 0) layouts = new[] { new Vector2(-3.4f, 2.9f), new Vector2(3.1f, -2.8f), new Vector2(4.4f, 2.1f) };
            else if (index == 1) layouts = new[] { new Vector2(-4.0f, -2.5f), new Vector2(1.8f, 3.5f), new Vector2(4.3f, -2.8f), new Vector2(-0.4f, -3.7f) };
            else layouts = new[] { new Vector2(-4.3f, 2.8f), new Vector2(-2.2f, -3.4f), new Vector2(3.8f, 1.9f) };
        }
        else if (phaseIndex == 1)
        {
            if (index == 0) layouts = new[] { new Vector2(-3.7f, -2.7f), new Vector2(-1.2f, 3.6f), new Vector2(4.2f, -1.9f) };
            else if (index == 1) layouts = new[] { new Vector2(-4.1f, 0.8f), new Vector2(-1.7f, 3.7f), new Vector2(1.5f, -3.6f), new Vector2(4.4f, -0.2f) };
            else layouts = new[] { new Vector2(-4.1f, 2.7f), new Vector2(-4.2f, -2.6f), new Vector2(4.0f, -0.5f) };
        }
        else
        {
            if (index == 0) layouts = new[] { new Vector2(-3.9f, 2.9f), new Vector2(-0.8f, -3.5f), new Vector2(3.9f, 2.5f) };
            else if (index == 1) layouts = new[] { new Vector2(-4.3f, 3.1f), new Vector2(-4.0f, -3.2f), new Vector2(0.2f, 2.6f), new Vector2(4.2f, -2.9f) };
            else layouts = new[] { new Vector2(-3.8f, 2.7f), new Vector2(-3.6f, -2.8f), new Vector2(4.2f, 0.8f) };
        }
        foreach (var offset in layouts)
        {
            Vector2 position = new Vector2(x + offset.x, offset.y);
            int variation = Mathf.Abs(Mathf.RoundToInt(offset.x * 3f + offset.y * 5f)) % 3;
            if (phaseIndex == 0 && variation == 0 && fallenWillowLogSprite != null)
                PlaceFallenLog(parent, position, index, variation);
            else if (phaseIndex == 0)
                PlaceProp(parent, variation == 1 ? 3 : 4, "Forest boulder and stump", position, variation == 1 ? 0.9f : 0.78f, true, new Vector2(0.85f, 0.62f), new Vector2(0f, 0.12f), ScadrykAmbientMotion.None, 1f, ((index + variation) % 2 == 0) ? 18f : -22f);
            else if (phaseIndex == 1)
                PlaceProp(parent, variation == 0 ? 8 : variation == 1 ? 10 : 4, "Salt crystal and crusted rock", position, variation == 0 ? 0.9f : 0.82f, true, new Vector2(0.76f, 0.62f), new Vector2(0f, 0.12f), ScadrykAmbientMotion.None, 1f, ((index + variation) % 2 == 0) ? 12f : -16f);
            else
                PlaceProp(parent, variation == 0 ? 13 : variation == 1 ? 14 : 12, "Tide-worn rock and post", position, variation == 1 ? 0.8f : 0.88f, true, new Vector2(0.78f, 0.64f), new Vector2(0f, 0.12f), ScadrykAmbientMotion.None, 1f, ((index + variation) % 2 == 0) ? 14f : -18f);
        }
    }

    private void PlaceFallenLog(Transform parent, Vector2 position, int room, int variant)
    {
        var obstacle = new GameObject("Fallen willow — solid obstacle");
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = position;
        obstacle.transform.rotation = Quaternion.Euler(0f, 0f, ((room + variant) % 2 == 0) ? 13f : -24f);
        var visual = new GameObject("Fallen tree art");
        visual.transform.SetParent(obstacle.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = fallenWillowLogSprite;
        renderer.sortingOrder = 1;
        Vector2 spriteSize = fallenWillowLogSprite.bounds.size;
        visual.transform.localScale = new Vector3(3.05f / spriteSize.x, 1.12f / spriteSize.y, 1f);
        obstacle.AddComponent<ScadrykDepthSort>().SetBias(2);
        var collider = obstacle.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Horizontal;
        collider.size = new Vector2(2.75f, 0.72f);
    }

    private static void MakeDecoration(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool circle, int order, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = circle ? CircleSprite : SquareSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private void SpawnRoomEnemies()
    {
        int count = roomIndex == RoomsPerPhase - 1 ? 1 : 3 + roomIndex;
        enemiesRemaining = count;
        for (int i = 0; i < count; i++)
        {
            bool boss = roomIndex == RoomsPerPhase - 1;
            float angle = (Mathf.PI * 2f * i / Mathf.Max(1, count)) + 0.3f;
            float radius = boss ? 2.5f : 3.1f + (i % 2) * 0.7f;
            Vector2 position = roomCenter + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            var enemy = new GameObject(boss ? "Phase guardian" : "Echo of " + PhaseNames[phaseIndex]);
            enemy.transform.SetParent(levelRoot.transform);
            enemy.transform.position = position;
            enemy.transform.localScale = Vector3.one * (boss ? 1.85f : 0.9f);
            string sheet = boss ? (phaseIndex == 0 ? "Bosses" : phaseIndex == 1 ? "BossesMidgame" : "BossesFinalWave") : "Enemies";
            Sprite[] frames = LoadSheetRow(sheet, boss ? phaseIndex : i % 4, false);
            if (phaseEnemySprites != null && phaseEnemySprites[phaseIndex] != null)
                frames = new[] { phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex] };
            var enemyVisual = new GameObject("Animated visual"); enemyVisual.transform.SetParent(enemy.transform, false);
            var renderer = enemyVisual.AddComponent<SpriteRenderer>(); renderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; renderer.color = Color.white; renderer.sortingOrder = 4;
            var collider = enemy.AddComponent<CircleCollider2D>(); collider.radius = 0.48f;
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<ScadrykDepthSort>().SetBias(4);
            enemy.AddComponent<ScadrykActorAnimator>().SetVisual(enemyVisual.transform);
            var health = enemy.AddComponent<ScadrykEnemyHealth>();
            health.Configure(boss ? 340f + phaseIndex * 90f : 75f + phaseIndex * 12f, boss ? 23f : 11f + phaseIndex * 2f, boss ? 1.65f : 2f + phaseIndex * 0.12f, boss, phaseIndex, frames);
            enemy.AddComponent<ScadrykHealthBar>().Configure(false, boss, boss ? 1.45f : 1.22f);
        }
    }

    public void OnEnemyDefeated(ScadrykEnemyHealth enemy)
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        if (enemiesRemaining == 0)
        {
            statusMessage = "Sala limpa. A passagem está aberta — pressione E.";
            statusUntil = Time.time + 8f;
            if (player != null) player.RestoreHealth(18f);
            UnlockExit();
        }
    }

    private void UnlockExit()
    {
        if (gateObject != null) return;
        exitPosition = roomCenter + Vector2.right * 8.05f;
        gateObject = new GameObject("Open passage — E");
        gateObject.transform.SetParent(levelRoot.transform);
        gateObject.transform.position = exitPosition;
        gateObject.transform.localScale = new Vector3(0.5f, 1.8f, 1f);
        var renderer = gateObject.AddComponent<SpriteRenderer>(); renderer.sprite = SquareSprite; renderer.color = new Color(0.3f, 0.9f, 0.72f, 0.85f); renderer.sortingOrder = 3;
    }

    public void TryUseExit(Vector2 playerPosition)
    {
        if (isPaused || victory) return;
        if (enemiesRemaining > 0) { ShowStatus("Ainda restam ecos nesta sala.", 1.5f); return; }
        if (Vector2.Distance(playerPosition, exitPosition) > 1.9f) { ShowStatus("Aproxime-se da passagem para avançar.", 1.5f); return; }
        clearedRooms[roomIndex] = true;
        if (roomIndex < RoomsPerPhase - 1) BuildPhase(phaseIndex, roomIndex + 1, false);
        else if (phaseIndex < PhaseNames.Length - 1)
        {
            for (int i = 0; i < clearedRooms.Length; i++) clearedRooms[i] = false;
            BuildPhase(phaseIndex + 1, 0, false);
        }
        else { victory = true; statusMessage = "A maré se aquieta. Você concluiu a demonstração!"; }
    }

    public void RestartCurrentRoom()
    {
        BuildPhase(phaseIndex, roomIndex, false);
        ShowStatus("Você caiu e retornou ao começo da sala.", 2.5f);
    }

    private void ShowStatus(string message, float seconds) { statusMessage = message; statusUntil = Time.time + seconds; }

    private void MakeLabelMarker(Transform parent, Vector2 position, string text, Color color)
    {
        // A small glowing waypoint keeps the room numbering visible in the world.
        MakeBox(parent, "Room waypoint " + text, position, new Vector2(0.18f, 0.18f), color, false, 1);
    }

    private static void MakeBox(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool solid, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = SquareSprite; renderer.color = color; renderer.sortingOrder = order;
        if (solid)
        {
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = Vector2.one;
            if (order >= 2) go.AddComponent<ScadrykDepthSort>().SetBias(2);
        }
    }

    private static void EnsureSprites()
    {
        if (SquareSprite == null) SquareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        if (CircleSprite != null) return;
        const int size = 48; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f - size / 2f) / (size / 2f), dy = (y + 0.5f - size / 2f) / (size / 2f);
            texture.SetPixel(x, y, dx * dx + dy * dy <= 0.94f ? Color.white : Color.clear);
        }
        texture.Apply(); CircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite[] LoadSheetRow(string name, int row, bool wholeSheet)
    {
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
        if (texture == null) return null;
        int frameCount = wholeSheet ? 16 : 4;
        var result = new Sprite[frameCount];
        for (int frame = 0; frame < frameCount; frame++)
        {
            int sheetRow = wholeSheet ? frame / 4 : row % 4;
            int column = frame % 4;
            int xMin = Mathf.RoundToInt(column * texture.width / 4f);
            int xMax = Mathf.RoundToInt((column + 1) * texture.width / 4f);
            int yMin = Mathf.RoundToInt(texture.height - (sheetRow + 1) * texture.height / 4f);
            int yMax = Mathf.RoundToInt(texture.height - sheetRow * texture.height / 4f);
            var rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.06f), 220f);
            sprite.name = name + "_" + (sheetRow * 4 + column).ToString("00");
            result[frame] = sprite;
        }
        return result;
    }

    private static Sprite LoadSingleSprite(string name, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawHud(); DrawMiniMap();
        if (isPaused || victory) DrawOverlay();
        if (Time.time < statusUntil) GUI.Label(new Rect(15, Screen.height - 64, Screen.width - 30, 44), statusMessage, bodyStyle);
        GUI.Label(new Rect(14, Screen.height - 27, Screen.width - 28, 20), "WASD mover  ·  Mouse mirar  ·  Clique/Space atacar  ·  Shift esquivar  ·  Q pulso  ·  E passagem  ·  Esc pausa", smallStyle);
    }

    private void DrawHud()
    {
        GUI.color = new Color(0.035f, 0.05f, 0.06f, 0.9f); GUI.Box(new Rect(14, 14, 325, 113), GUIContent.none); GUI.color = Color.white;
        GUI.Label(new Rect(28, 22, 300, 25), PhaseNames[phaseIndex] + "  ·  Fase " + (phaseIndex + 1) + "/3", titleStyle);
        GUI.Label(new Rect(28, 51, 300, 23), roomNames[phaseIndex][roomIndex] + "  ·  Sala " + (roomIndex + 1) + "/3", bodyStyle);
        GUI.color = new Color(0.18f, 0.19f, 0.2f); GUI.DrawTexture(new Rect(28, 82, 275, 12), Texture2D.whiteTexture);
        GUI.color = new Color(0.79f, 0.25f, 0.22f); GUI.DrawTexture(new Rect(28, 82, 275 * (player != null ? player.Health / player.MaxHealth : 1f), 12), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(28, 98, 290, 19), "Ecos: " + enemiesRemaining + "    Esquiva: " + Cooldown(player != null ? player.DashCooldownLeft : 0) + "    Pulso: " + Cooldown(player != null ? player.ShockwaveCooldownLeft : 0), smallStyle);
    }

    private void DrawMiniMap()
    {
        const float width = 286, height = 126;
        Rect panel = new Rect(Screen.width - width - 16, 14, width, height);
        GUI.color = new Color(0.035f, 0.05f, 0.06f, 0.9f); GUI.Box(panel, GUIContent.none); GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 13, panel.y + 8, 250, 21), "MAPA DA FASE", smallStyle);
        float mapX = panel.x + 15, mapY = panel.y + 43, roomW = 75, roomH = 57, gap = 8;
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            Rect cell = new Rect(mapX + i * (roomW + gap), mapY, roomW, roomH);
            GUI.color = i == roomIndex ? new Color(0.19f, 0.36f, 0.35f) : (clearedRooms[i] ? new Color(0.16f, 0.25f, 0.22f) : new Color(0.12f, 0.15f, 0.16f));
            GUI.Box(cell, GUIContent.none); GUI.color = new Color(0.75f, 0.74f, 0.67f);
            GUI.Label(new Rect(cell.x + 3, cell.y + 2, roomW - 6, 18), (i + 1) + ". " + ShortRoomName(i), smallStyle);
            if (i < RoomsPerPhase - 1) GUI.Label(new Rect(cell.xMax, cell.y + 20, gap + 2, 20), "›", bodyStyle);
        }
        if (player != null)
        {
            Vector2 relative = (Vector2)player.transform.position - roomCenter;
            float dx = Mathf.Clamp(relative.x / 22f, -0.42f, 0.42f) * (roomW - 10);
            float dy = Mathf.Clamp(relative.y / 15f, -0.35f, 0.35f) * (roomH - 12);
            Rect dot = new Rect(mapX + roomIndex * (roomW + gap) + roomW / 2f + dx - 3.5f, mapY + roomH / 2f - dy - 3.5f, 7, 7);
            GUI.color = new Color(1f, 0.82f, 0.38f); GUI.DrawTexture(dot, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    private string ShortRoomName(int index) => index == 0 ? "Entrada" : index == 1 ? "Travessia" : "Guardião";
    private static string Cooldown(float value) => value <= 0.02f ? "pronto" : value.ToString("0.0") + "s";

    private void DrawOverlay()
    {
        GUI.color = new Color(0.015f, 0.02f, 0.025f, 0.88f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
        string heading = victory ? "A MARÉ SE AQUIETA" : "PAUSADO";
        GUI.Label(new Rect(0, Screen.height / 2f - 90, Screen.width, 42), heading, centeredTitleStyle);
        GUI.Label(new Rect(0, Screen.height / 2f - 43, Screen.width, 36), victory ? "Você concluiu as três fases desta demonstração." : "Pressione Esc ou clique abaixo para continuar.", centeredBodyStyle);
        if (!victory && GUI.Button(new Rect(Screen.width / 2f - 80, Screen.height / 2f + 10, 160, 42), "Continuar")) isPaused = false;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.91f, 0.81f, 0.57f) }, alignment = TextAnchor.MiddleLeft };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.88f, 0.89f, 0.83f) }, alignment = TextAnchor.MiddleLeft };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.72f, 0.76f, 0.72f) }, alignment = TextAnchor.MiddleLeft };
        centeredTitleStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 25 };
        centeredBodyStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
    }
}
