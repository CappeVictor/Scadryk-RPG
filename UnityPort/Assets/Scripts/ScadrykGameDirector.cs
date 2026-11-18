using UnityEngine;

/// <summary>Builds the playable three-phase, nine-room Scadryk Unity vertical slice.</summary>
public sealed class ScadrykGameDirector : MonoBehaviour
{
    private const int RoomsPerPhase = 3;
    private const float RoomSpacing = 23f;
    private static readonly string[] PhaseNames = { "Margens do Primeiro Eco", "Bacia do Sal Imóvel", "Costa das Badaladas" };
    private static readonly Color[] PhaseColors = { new Color(0.09f, 0.16f, 0.17f), new Color(0.17f, 0.13f, 0.1f), new Color(0.09f, 0.13f, 0.2f) };
    private static Sprite SquareSprite;
    private static Sprite CircleSprite;

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
            var renderer = playerObject.AddComponent<SpriteRenderer>(); renderer.sprite = CircleSprite; renderer.color = new Color(0.85f, 0.83f, 0.71f); renderer.sortingOrder = 5;
            playerObject.transform.localScale = new Vector3(0.88f, 0.88f, 1f);
            playerObject.AddComponent<CircleCollider2D>().radius = 0.42f;
            playerObject.AddComponent<Rigidbody2D>();
            player = playerObject.AddComponent<ScadrykPlayerController>();
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
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            float x = i * RoomSpacing;
            MakeBox(parent, "Room floor " + (i + 1), new Vector2(x, 0), new Vector2(19f, 13f), floor, false, -10);
            MakeBox(parent, "North wall", new Vector2(x, 6.6f), new Vector2(19.5f, 0.5f), new Color(0.22f, 0.26f, 0.25f), true, 0);
            MakeBox(parent, "South wall", new Vector2(x, -6.6f), new Vector2(19.5f, 0.5f), new Color(0.22f, 0.26f, 0.25f), true, 0);
            if (i == 0) MakeBox(parent, "West outer wall", new Vector2(x - 9.7f, 0), new Vector2(0.5f, 13f), new Color(0.22f, 0.26f, 0.25f), true, 0);
            else
            {
                MakeBox(parent, "West wall upper", new Vector2(x - 9.7f, 4.7f), new Vector2(0.5f, 4.3f), new Color(0.22f, 0.26f, 0.25f), true, 0);
                MakeBox(parent, "West wall lower", new Vector2(x - 9.7f, -4.7f), new Vector2(0.5f, 4.3f), new Color(0.22f, 0.26f, 0.25f), true, 0);
            }
            if (i == RoomsPerPhase - 1)
            {
                MakeBox(parent, "East wall upper", new Vector2(x + 9.7f, 4.7f), new Vector2(0.5f, 4.3f), new Color(0.22f, 0.26f, 0.25f), true, 0);
                MakeBox(parent, "East wall lower", new Vector2(x + 9.7f, -4.7f), new Vector2(0.5f, 4.3f), new Color(0.22f, 0.26f, 0.25f), true, 0);
            }
            MakeBox(parent, "Floor inlay", new Vector2(x, 0), new Vector2(0.1f, 11.8f), new Color(1f, 1f, 1f, 0.045f), false, -9);
            if (i < RoomsPerPhase - 1)
            {
                MakeBox(parent, "Connector floor", new Vector2(x + 11.35f, 0), new Vector2(3.3f, 3.4f), floor * 1.12f, false, -9);
                MakeBox(parent, "Connector north rail", new Vector2(x + 11.35f, 1.92f), new Vector2(3.4f, 0.28f), new Color(0.22f, 0.26f, 0.25f), true, 0);
                MakeBox(parent, "Connector south rail", new Vector2(x + 11.35f, -1.92f), new Vector2(3.4f, 0.28f), new Color(0.22f, 0.26f, 0.25f), true, 0);
            }
            DrawRoomObstacles(parent, i, x);
            MakeLabelMarker(parent, new Vector2(x, 5.35f), (i + 1).ToString(), new Color(0.78f, 0.67f, 0.4f));
        }
    }

    private void DrawRoomObstacles(Transform parent, int index, float x)
    {
        Vector2[] layouts = index switch
        {
            0 => new[] { new Vector2(-2.8f, 1.9f), new Vector2(2.9f, -2.1f), new Vector2(0.1f, 0f) },
            1 => new[] { new Vector2(-3.8f, -1.8f), new Vector2(3.7f, 1.8f), new Vector2(0f, 3f), new Vector2(0f, -3f) },
            _ => new[] { new Vector2(-3.3f, 2.6f), new Vector2(-3.3f, -2.6f), new Vector2(2.8f, 0f) }
        };
        foreach (var offset in layouts)
        {
            Vector2 size = (index == 2 && offset.x > 0) ? new Vector2(1.3f, 3.4f) : new Vector2(1.15f, 1.15f);
            MakeBox(parent, "Stone cover", new Vector2(x + offset.x, offset.y), size, new Color(0.19f, 0.24f, 0.24f), true, 2);
        }
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
            var enemy = new GameObject(boss ? "Phase guardian" : "Echo");
            enemy.transform.SetParent(levelRoot.transform);
            enemy.transform.position = position;
            enemy.transform.localScale = Vector3.one * (boss ? 1.65f : 0.9f);
            string sheet = boss ? (phaseIndex == 0 ? "Bosses" : phaseIndex == 1 ? "BossesMidgame" : "BossesFinalWave") : "Enemies";
            Sprite[] frames = LoadSheetRow(sheet, boss ? phaseIndex : i % 4, false);
            var renderer = enemy.AddComponent<SpriteRenderer>(); renderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; renderer.color = Color.white; renderer.sortingOrder = 4;
            var collider = enemy.AddComponent<CircleCollider2D>(); collider.radius = 0.48f;
            enemy.AddComponent<Rigidbody2D>();
            var health = enemy.AddComponent<ScadrykEnemyHealth>();
            health.Configure(boss ? 340f + phaseIndex * 90f : 75f + phaseIndex * 12f, boss ? 23f : 11f + phaseIndex * 2f, boss ? 1.65f : 2f + phaseIndex * 0.12f, boss, frames);
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
        if (solid) { var collider = go.AddComponent<BoxCollider2D>(); collider.size = Vector2.one; }
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
