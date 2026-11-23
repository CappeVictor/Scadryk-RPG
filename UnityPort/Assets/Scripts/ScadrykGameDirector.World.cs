using System.Collections.Generic;
using UnityEngine;

public sealed partial class ScadrykGameDirector
{
    private SpriteRenderer witnessRenderer;
    private SpriteRenderer witnessReflectionRenderer;
    private Transform witnessVisualTransform;

    private void BuildRoomAnchors()
    {
        roomAnchors[0] = roomAnchors[1] = null;
        anchorsActivated = 0;
        int id = phaseIndex * RoomsPerPhase + roomIndex;
        Vector2[] offsets = RoomAnchorOffsets(phaseIndex, roomIndex);
        anchorsNeedSequence = id == 7;
        anchorsRequired = offsets.Length;
        anchorsNeedPulse = id == 4 || id == 7;
        if (anchorsRequired == 0) return;
        for (int i = 0; i < anchorsRequired; i++)
        {
            Vector2 offset = offsets[i];
            string anchorName = anchorsNeedSequence ? (i == 0 ? "Coast beacon resonator" : "Reef bell resonator") :
                anchorsNeedPulse ? "Resonant world object" : "Memory world object";
            var anchor = new GameObject(anchorName);
            anchor.transform.SetParent(levelRoot.transform);
            anchor.transform.position = roomCenter + offset;
            var renderer = anchor.AddComponent<SpriteRenderer>();
            renderer.sprite = environmentSprites != null && environmentSprites.Length > (phaseIndex == 2 ? 14 : 8) ?
                environmentSprites[phaseIndex == 0 ? 6 : phaseIndex == 1 ? 8 : 14] : CircleSprite;
            renderer.color = new Color(0.8f, 0.9f, 0.88f, 0.95f);
            renderer.sortingOrder = 3;
            anchor.transform.localScale = Vector3.one * 1.35f;
            roomAnchors[i] = anchor;
            MakeDecoration(levelRoot.transform, "Resonance mark", anchor.transform.position, new Vector2(1.5f, 1.5f), PhaseAccentColors[phaseIndex] * 0.48f, true, 2);
        }
    }

    private void BuildWitness()
    {
        witnessObject = null;
        witnessRenderer = witnessReflectionRenderer = null;
        witnessVisualTransform = null;
        witnessLine = smokeRun ? 0 : ReadWitnessProgress("Scadryk.", phaseIndex);
        // Nima returns in the two quiet arrival rooms after the first and second guardians.
        if (roomIndex != 0 || phaseIndex == 0) return;

        Vector2 position = roomCenter + new Vector2(-5.4f, 1.1f);
        witnessObject = new GameObject("Nima — voz libertada das raízes");
        witnessObject.transform.SetParent(levelRoot.transform);
        witnessObject.transform.position = position;

        float presence = WitnessPresenceForConversation(phaseIndex, witnessLine);
        Color witnessTint = WitnessTintForConversation(phaseIndex, witnessLine);
        GameObject reflection = MakeDecoration(levelRoot.transform, "Nima's reflected pool", position + Vector2.down * 0.52f,
            new Vector2(0.68f + presence * 0.22f, 0.2f + presence * 0.08f),
            new Color(witnessTint.r, witnessTint.g, witnessTint.b, 0.26f + presence * 0.2f), true, 2);
        witnessReflectionRenderer = reflection.GetComponent<SpriteRenderer>();
        var visual = new GameObject("Nima — Echo witness");
        visual.transform.SetParent(witnessObject.transform, false);
        witnessRenderer = visual.AddComponent<SpriteRenderer>();
        witnessRenderer.sprite = witnessSprite != null ? witnessSprite : CircleSprite;
        witnessRenderer.color = witnessTint;
        witnessRenderer.sortingOrder = 5;
        witnessVisualTransform = visual.transform;
        witnessVisualTransform.localScale = Vector3.one * WitnessScaleForConversation(phaseIndex, witnessLine);
        witnessObject.AddComponent<ScadrykDepthSort>().SetBias(4);
    }

    internal static float WitnessPresenceForPhase(int phase) => phase switch
    {
        1 => 0.68f,
        2 => 1f,
        _ => 0f
    };

    internal static float WitnessScaleForPhase(int phase) =>
        Mathf.Lerp(0.3f, 0.44f, WitnessPresenceForPhase(phase));

    internal static Color WitnessTintForPhase(int phase) => phase switch
    {
        1 => new Color(0.68f, 0.96f, 0.8f, WitnessPresenceForPhase(phase)),
        2 => new Color(1f, 0.84f, 0.59f, WitnessPresenceForPhase(phase)),
        _ => Color.clear
    };

    internal static float WitnessPresenceForConversation(int phase, int memoriesShared) =>
        phase == 1 ? Mathf.Min(0.95f, WitnessPresenceForPhase(phase) + Mathf.Clamp(memoriesShared, 0, 3) * 0.09f) : WitnessPresenceForPhase(phase);

    internal static float WitnessScaleForConversation(int phase, int memoriesShared) =>
        Mathf.Lerp(0.3f, 0.44f, WitnessPresenceForConversation(phase, memoriesShared));

    internal static Color WitnessTintForConversation(int phase, int memoriesShared)
    {
        Color tint = WitnessTintForPhase(phase);
        tint.a = WitnessPresenceForConversation(phase, memoriesShared);
        return tint;
    }

    private void ApplyWitnessConversationGrowth()
    {
        float presence = WitnessPresenceForConversation(phaseIndex, witnessLine);
        if (witnessRenderer != null) witnessRenderer.color = WitnessTintForConversation(phaseIndex, witnessLine);
        if (witnessVisualTransform != null) witnessVisualTransform.localScale = Vector3.one * WitnessScaleForConversation(phaseIndex, witnessLine);
        if (witnessReflectionRenderer != null)
        {
            Color tint = WitnessTintForConversation(phaseIndex, witnessLine);
            witnessReflectionRenderer.color = new Color(tint.r, tint.g, tint.b, 0.26f + presence * 0.2f);
            witnessReflectionRenderer.transform.localScale = new Vector3(0.68f + presence * 0.22f, 0.2f + presence * 0.08f, 1f);
        }
    }

    public void TryInteract(Vector2 position)
    {
        if (IsPaused) return;
        if (witnessObject != null && Vector2.Distance(position, witnessObject.transform.position) <= 1.9f)
        {
            string[] lines = phaseIndex == 1 ? BasinWitnessLines : CoastWitnessLines;
            ShowMemory(lines[witnessLine % lines.Length], 8f);
            witnessLine++;
            if (!smokeRun) WriteWitnessProgress("Scadryk.", phaseIndex, witnessLine);
            ApplyWitnessConversationGrowth();
            return;
        }
        for (int i = 0; i < anchorsRequired; i++)
        {
            if (roomAnchors[i] == null || Vector2.Distance(position, roomAnchors[i].transform.position) > 1.8f) continue;
            if (anchorsNeedPulse) { ShowStatus("Este objeto responde ao Pulso (" + InputBinding(2) + ").", 3f); return; }
            ActivateAnchor(i);
            return;
        }
        TryUseExit(position);
    }

    public void TryPulseAnchor(Vector2 position)
    {
        if (!anchorsNeedPulse || IsPaused) return;
        for (int i = 0; i < anchorsRequired; i++)
        {
            float pulseRadius = player != null ? player.ShockwaveRadius : 3.2f;
            if (roomAnchors[i] == null || Vector2.Distance(position, roomAnchors[i].transform.position) > pulseRadius) continue;
            if (anchorsNeedSequence && i != anchorsActivated)
            {
                ShowStatus(i > anchorsActivated ? "Primeiro ressoe o farol na entrada da costa." : "Esse sinal já respondeu. Siga até o sino do recife.", 3f);
                return;
            }
            int activatedBefore = anchorsActivated;
            ActivateAnchor(i);
            if (anchorsActivated > activatedBefore) RecordResonanceActivation();
            return;
        }
    }

    private void ActivateAnchor(int index)
    {
        GameObject anchor = roomAnchors[index];
        if (anchor == null) return;
        roomAnchors[index] = null;
        anchorsActivated++;
        progressHintSeconds = 0f;
        hintStage = 0;
        ScadrykEffects.Burst(anchor.transform.position, PhaseAccentColors[phaseIndex], 1.8f);
        ScadrykAudio.Play(ScadrykCue.Portal);
        Destroy(anchor);
        if (anchorsActivated >= anchorsRequired)
        {
            ShowStatus("O vestígio respondeu. A passagem pode se abrir.", 5f);
            int id = phaseIndex * RoomsPerPhase + roomIndex;
            if (id == 3) ShowMemory("O sal guardava uma maré inteira, suspensa no instante em que o Arauto ordenou silêncio. Nara reconhece a voz de quem tentou avisar a costa.", 8f);
            if (id == 6) ShowMemory("Um sino encalhado repete o pedido dos navegantes: não apagar a lembrança da onda. Eles não pedem vingança; pedem que alguém a escute.", 8f);
            if (enemiesRemaining == 0) UnlockExit();
        }
        else ShowStatus("Um sinal respondeu. Falta mais um.", 4f);
    }

    private void DrawPhaseMap(Transform parent)
    {
        Color floor = PhaseColors[phaseIndex];
        Color wall = PhaseWallColors[phaseIndex];
        // The camera can see beyond the first and last traversable rooms. Painted
        // non-colliding outskirts keep that view within the biome instead of void.
        MakeBox(parent, "Biome backdrop", new Vector2(RoomSpacing, 0f), new Vector2(RoomSpacing * 6f, 40f), floor * 0.72f, false, -11);
        PlacePhaseGround(parent, -RoomSpacing, 1);
        PlacePhaseGround(parent, RoomsPerPhase * RoomSpacing, 1);
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            float x = i * RoomSpacing;
            Color roomFloor = floor * (i == 1 ? 1.08f : 0.96f);
            bool hasSideRoute = TryGetSideRoute(phaseIndex, i, out int routeSide, out float routeEntry, out float routeExit);
            MakeBox(parent, "Room floor " + (i + 1), new Vector2(x, 0), new Vector2(RoomSpacing + 0.2f, 13f), roomFloor, false, -10);
            PlacePhaseGround(parent, x, i);
            DrawRoomScenery(parent, i, x, roomFloor);
            if (hasSideRoute) DrawRoomSideRoute(parent, x, floor, wall, routeSide, routeEntry, routeExit);
            if (!hasSideRoute || routeSide < 0)
                MakeBox(parent, "North wall", new Vector2(x, 6.6f), new Vector2(19.5f, 0.5f), wall, true, 0);
            else DrawSideRouteWall(parent, x, 6.6f, wall, routeEntry, routeExit);
            if (!hasSideRoute || routeSide > 0)
                MakeBox(parent, "South wall", new Vector2(x, -6.6f), new Vector2(19.5f, 0.5f), wall, true, 0);
            else DrawSideRouteWall(parent, x, -6.6f, wall, routeEntry, routeExit);
            if (i == 0) MakeBox(parent, "West outer wall", new Vector2(x - 9.7f, 0), new Vector2(0.5f, 13f), wall, true, 0);
            else
            {
                MakeBox(parent, "West wall upper", new Vector2(x - 9.7f, 4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "West wall lower", new Vector2(x - 9.7f, -4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "West echo threshold", new Vector2(x - 9.7f, 0f), new Vector2(0.5f, 5.6f), wall, true, 0);
            }
            if (i == RoomsPerPhase - 1)
            {
                MakeBox(parent, "East outer boundary", new Vector2(x + 9.7f, 0f), new Vector2(0.5f, 13f), wall, true, 0);
            }
            else
            {
                MakeBox(parent, "East wall upper", new Vector2(x + 9.7f, 4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "East wall lower", new Vector2(x + 9.7f, -4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                // Rooms advance through their portal transition. The short seal closes
                // the doorway without narrowing the walkable route to the portal.
                MakeBox(parent, "East echo threshold", new Vector2(x + 9.7f, 0f), new Vector2(0.5f, 5.6f), wall, true, 0);
                MakeBox(parent, "Threshold seal sigil", new Vector2(x + 9.7f, 0f), new Vector2(0.22f, 0.78f), PhaseAccentColors[phaseIndex], false, 1);
            }
            MakeBox(parent, "Floor inlay", new Vector2(x, 0), new Vector2(0.1f, 11.8f), new Color(1f, 1f, 1f, 0.055f), false, -9);
            if (i < RoomsPerPhase - 1)
            {
                Vector2 connectorCenter = new Vector2(x + ConnectorCenterOffset, 0f);
                MakeBox(parent, "Connector floor", connectorCenter, new Vector2(3.3f, 3.4f), floor * 1.12f, false, -9);
                MakeBox(parent, "Connector north rail", connectorCenter + Vector2.up * 1.92f, new Vector2(3.4f, 0.28f), wall, true, 0);
                MakeBox(parent, "Connector south rail", connectorCenter + Vector2.down * 1.92f, new Vector2(3.4f, 0.28f), wall, true, 0);
            }
            DrawRoomObstacles(parent, i, x);
            MakeLabelMarker(parent, new Vector2(x, 5.35f), (i + 1).ToString(), PhaseAccentColors[phaseIndex]);
        }
    }

    private static void DrawSideRouteWall(Transform parent, float roomX, float y, Color wall, float firstOpening, float secondOpening)
    {
        const float halfWallWidth = 9.75f;
        const float openingHalfWidth = 1.8f;
        float leftOpening = Mathf.Min(firstOpening, secondOpening);
        float rightOpening = Mathf.Max(firstOpening, secondOpening);
        AddWallSegment(roomX, -halfWallWidth, leftOpening - openingHalfWidth);
        AddWallSegment(roomX, leftOpening + openingHalfWidth, rightOpening - openingHalfWidth);
        AddWallSegment(roomX, rightOpening + openingHalfWidth, halfWallWidth);

        void AddWallSegment(float origin, float minimum, float maximum)
        {
            float width = maximum - minimum;
            if (width < 0.2f) return;
            MakeBox(parent, "Side route threshold", new Vector2(origin + (minimum + maximum) * 0.5f, y),
                new Vector2(width, 0.5f), wall, true, 0);
        }
    }

    private void DrawRoomSideRoute(Transform parent, float roomX, Color floor, Color wall, int side, float entry, float exit)
    {
        const float routeEndExtension = 2f;
        const float routeDepth = 5.4f;
        float minimumX = Mathf.Min(entry, exit) - routeEndExtension;
        float maximumX = Mathf.Max(entry, exit) + routeEndExtension;
        float centerY = side * (6.6f + routeDepth * 0.5f);
        float width = maximumX - minimumX;
        Vector2 center = new Vector2(roomX + (minimumX + maximumX) * 0.5f, centerY);
        string routeName = phaseIndex switch
        {
            0 => "Root causeway",
            1 => "Salt shelf bypass",
            _ => "Buoy lee path"
        };

        MakeBox(parent, routeName + " floor", center, new Vector2(width, routeDepth), floor * 0.98f, false, -9);
        MakeBox(parent, routeName + " outer rail", center + Vector2.up * (side * routeDepth * 0.5f),
            new Vector2(width, 0.36f), wall, true, 0);
        MakeBox(parent, routeName + " west end", new Vector2(roomX + minimumX, centerY),
            new Vector2(0.36f, routeDepth), wall, true, 0);
        MakeBox(parent, routeName + " east end", new Vector2(roomX + maximumX, centerY),
            new Vector2(0.36f, routeDepth), wall, true, 0);

        for (int plank = 0; plank < 6; plank++)
        {
            float step = Mathf.Lerp(minimumX + 1.25f, maximumX - 1.25f, plank / 5f);
            Vector2 mark = new Vector2(roomX + step, centerY + side * (routeDepth * 0.5f - 0.55f));
            Color accent = PhaseAccentColors[phaseIndex];
            MakeDecoration(parent, routeName + " waymark", mark,
                phaseIndex == 1 ? new Vector2(0.48f, 0.7f) : new Vector2(0.85f, 0.12f),
                new Color(accent.r, accent.g, accent.b, 0.38f), phaseIndex == 1, -8,
                phaseIndex == 0 ? -12f : phaseIndex == 2 ? 5f : 0f);
        }
        MakeDecoration(parent, routeName + " entry glow", new Vector2(roomX + entry, side * 6.95f),
            new Vector2(0.72f, 0.72f), new Color(PhaseAccentColors[phaseIndex].r, PhaseAccentColors[phaseIndex].g,
                PhaseAccentColors[phaseIndex].b, 0.2f), true, -8);
        MakeDecoration(parent, routeName + " exit glow", new Vector2(roomX + exit, side * 6.95f),
            new Vector2(0.72f, 0.72f), new Color(PhaseAccentColors[phaseIndex].r, PhaseAccentColors[phaseIndex].g,
                PhaseAccentColors[phaseIndex].b, 0.2f), true, -8);
    }

    internal static bool TryGetSideRoute(int biome, int room, out int side, out float entry, out float exit)
    {
        side = 0;
        entry = exit = 0f;
        if (room != 1 || biome < 0 || biome >= 3) return false;
        side = biome == 1 ? -1 : 1;
        entry = biome == 0 ? -5.4f : 5.4f;
        exit = -entry;
        return true;
    }

    internal static bool SideRoutesAreDistinctAndNavigable()
    {
        int[] expectedSides = { 1, -1, 1 };
        const float doorWidth = 3.6f;
        const float passageDepth = 5.4f;
        const float playerRadius = 0.42f;
        const float routeRailThickness = 0.36f;
        float clearDoorWidth = doorWidth - 2f * playerRadius;
        float clearPassageDepth = passageDepth - routeRailThickness - 2f * playerRadius;
        if (clearDoorWidth < 2f || clearPassageDepth < 2f) return false;
        for (int biome = 0; biome < 3; biome++)
        {
            if (!TryGetSideRoute(biome, 1, out int side, out float entry, out float exit) || side != expectedSides[biome] ||
                Mathf.Abs(Mathf.Abs(entry) - 5.4f) > 0.001f || Mathf.Abs(entry + exit) > 0.001f ||
                Mathf.Abs(entry) + 1.8f >= 9.75f || Mathf.Abs(exit) + 1.8f >= 9.75f)
                return false;
            if (TryGetSideRoute(biome, 0, out _, out _, out _) || TryGetSideRoute(biome, 2, out _, out _, out _)) return false;
        }
        return true;
    }

    private void BuildWorldSecret()
    {
        worldSecret = null;
        worldSecretHintShown = false;
        int requiredPower = RequiredWorldSecretPower();
        if (requiredPower < 0) return;
        if (!ShouldBuildWorldSecret(smokeRun, IsWorldSecretRevealed("Scadryk.", phaseIndex, roomIndex))) return;
        Vector2 spot = WorldSecretPosition(roomCenter, phaseIndex, roomIndex, requiredPower);
        worldSecret = new GameObject("Hidden echo seal — power " + (requiredPower + 1));
        worldSecret.transform.SetParent(levelRoot.transform);
        worldSecret.transform.position = spot;
        var marker = worldSecret.AddComponent<SpriteRenderer>();
        marker.sprite = CircleSprite;
        marker.color = new Color(PhaseAccentColors[phaseIndex].r, PhaseAccentColors[phaseIndex].g, PhaseAccentColors[phaseIndex].b, 0.72f);
        marker.sortingOrder = 2;
        worldSecret.transform.localScale = Vector3.one * 0.75f;
        var sealGlow = worldSecret.AddComponent<ScadrykAmbientProp>();
        sealGlow.Configure(ScadrykAmbientMotion.Glow, 1.2f);
        if (player != null) sealGlow.ConfigureRevealNear(player.transform, 4.2f, 0.16f);
        MakeDecoration(worldSecret.transform, "Seal center", spot, new Vector2(0.2f, 0.2f), new Color(1f, 1f, 1f, 0.22f), true, 3);
    }

    private int RequiredWorldSecretPower() => PowerGrantedBySecretAt(phaseIndex, roomIndex);

    internal static int PowerGrantedBySecretAt(int phase, int room) =>
        phase == 1 && room == 1 ? 0 : phase == 2 && room == 1 ? 1 : phase == 2 && room == 2 ? 2 : -1;

    internal static Vector2 WorldSecretPosition(Vector2 center, int phase, int room, int power)
    {
        if (TryGetSideRoute(phase, room, out int side, out _, out _))
            return center + Vector2.up * (side * 9.3f);
        return center + (power == 1 ? new Vector2(-7.2f, -4.1f) : new Vector2(7.2f, 4.15f));
    }

    internal static string PowerEchoRewardText(int power) => power switch
    {
        0 => "Crescente: recarga 20% mais rápido.",
        1 => "Estouro: a vulnerabilidade dura mais.",
        2 => "Rompante: Ecos comuns são arremessados com mais força.",
        _ => string.Empty
    };

    internal static void RestorePowerEchoes(string keyPrefix, ScadrykPlayerController target)
    {
        if (target == null) return;
        for (int power = 0; power < 3; power++)
            if (ShouldRestorePowerEcho(keyPrefix, power)) target.UnlockPowerEcho(power);
    }

    internal static bool ShouldRestorePowerEcho(string keyPrefix, int power)
    {
        if (power < 0 || power >= 3) return false;
        for (int phase = 0; phase < 3; phase++)
        for (int room = 0; room < 3; room++)
            if (PowerGrantedBySecretAt(phase, room) == power && IsWorldSecretRevealed(keyPrefix, phase, room)) return true;
        return false;
    }

    private static string WorldSecretMemory(int power) => power switch
    {
        0 => "Uma voz recorda a comporta se abrindo pela primeira vez, quando as raízes ainda conheciam todos os nomes.",
        1 => "Uma lembrança solta devolve ao sal o instante da última maré antes de o tempo parar.",
        _ => "Um Eco reconhece sua própria voz no coro dos três sinos. A lembrança já não precisa ficar presa."
    };

    private string WorldSecretHint(int power)
    {
        string[] names = { "Crescente Verdejante", "Estouro de Sal", "Rompante da Maré" };
        if (player == null || !player.HasAbility(power))
            return "Selo de Eco: esta lembrança só poderá ser libertada após derrotar o guardião deste lugar.";
        string binding = InputBinding(power + 3);
        return "Selo de Eco: use " + names[power] + " (" + binding + ") perto dele para libertar uma memória.";
    }

    private bool TryShowWorldSecretHint()
    {
        if (worldSecretHintShown || worldSecret == null || player == null ||
            Vector2.Distance(player.transform.position, worldSecret.transform.position) > 3.2f) return false;
        int power = RequiredWorldSecretPower();
        if (power < 0) return false;
        ShowStatus(WorldSecretHint(power), 6f);
        worldSecretHintShown = true;
        return true;
    }

    public void TryRevealSecret(int power, Vector2 source)
    {
        if (worldSecret == null) return;
        int requiredPower = RequiredWorldSecretPower();
        Vector2 spot = worldSecret.transform.position;
        if (power != requiredPower || player == null || !player.AbilityFootprintContainsPoint(requiredPower, spot, source) ||
            ScadrykPlayerController.AbilityPathIsBlocked(requiredPower, source, spot)) return;
        if (!smokeRun) ScadrykAudio.Play(ScadrykCue.EchoReleased);
        ScadrykEffects.Burst(spot, PhaseAccentColors[phaseIndex], 1.5f);
        ScadrykEffects.ReleaseEcho(witnessSprite != null ? witnessSprite : CircleSprite, spot, PhaseAccentColors[phaseIndex]);
        if (!smokeRun) MarkWorldSecretRevealed("Scadryk.", phaseIndex, roomIndex);
        if (player != null) player.UnlockPowerEcho(requiredPower);
        Destroy(worldSecret);
        worldSecret = null;
        roomSecretsRevealed = SecretRevealCountAfterSuccess(roomSecretsRevealed, smokeRun);
        if (player != null) player.RestoreHealth(35f);
        ShowMemory(WorldSecretMemory(requiredPower), 7f, false);
        ShowStatus("Eco sintonizado — " + PowerEchoRewardText(requiredPower) + " Nara recuperou 35 pontos de vida.", 6f);
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
        renderer.flipX = GroundFlipX(room);
        renderer.flipY = GroundFlipY(room);
        Vector2 size = renderer.sprite.bounds.size;
        // Extend the painted ground through the portal corridor so the camera never reveals void strips.
        ground.transform.localScale = new Vector3((RoomSpacing + 0.2f) / size.x, 13f / size.y, 1f);
    }

    private static bool GroundFlipX(int room) => room == 1;
    private static bool GroundFlipY(int room) => room == 2;

    internal static bool RoomGroundOrientationsAreUnique()
    {
        for (int first = 0; first < RoomsPerPhase; first++)
        for (int second = first + 1; second < RoomsPerPhase; second++)
            if (GroundFlipX(first) == GroundFlipX(second) && GroundFlipY(first) == GroundFlipY(second)) return false;
        return true;
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
                MakeShallowWaterZone(parent, new Vector2(x - 5.9f, y), new Vector2(3.0f, 1.05f));
                MakeDecoration(parent, "Tide pool", new Vector2(x + 5.6f, -y * 0.78f), new Vector2(2.5f, 0.82f), new Color(0.07f, 0.26f, 0.27f, 0.48f), true, -9);
                MakeShallowWaterZone(parent, new Vector2(x + 5.6f, -y * 0.78f), new Vector2(2.5f, 0.82f));
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
            if (room == 1) DrawSaltCrosswind(parent, x);
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
            if (room == 1) DrawCoastalCurrent(parent, x);
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

    private static void MakeShallowWaterZone(Transform parent, Vector2 position, Vector2 size)
    {
        var zoneObject = new GameObject("Shallow water · slows movement");
        zoneObject.transform.SetParent(parent);
        zoneObject.transform.position = position;
        BoxCollider2D trigger = zoneObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = size;
        zoneObject.AddComponent<ScadrykTerrainZone>().Configure(0.78f, default, null, ScadrykTerrainZone.ShallowWaterKey);
    }

    private static void DrawSaltCrosswind(Transform parent, float roomX)
    {
        Vector2 center = new Vector2(roomX + 0.8f, -2.35f);
        Color saltLight = new Color(0.96f, 0.78f, 0.48f, 0.48f);
        MakeDecoration(parent, "Salt crosswind field", center, new Vector2(4.2f, 1.9f),
            new Color(0.74f, 0.57f, 0.32f, 0.12f), false, -8);

        // Left-facing chevrons show the drift direction before Nara enters the field.
        for (int arrow = 0; arrow < 4; arrow++)
        {
            Vector2 head = center + new Vector2(1.45f - arrow * 0.96f, 0f);
            var marker = new GameObject("Salt grains drifting southwest");
            marker.transform.SetParent(parent);
            marker.transform.position = head;
            marker.transform.rotation = Quaternion.Euler(0f, 0f, 24f);
            MakeCurrentFoamStroke(marker.transform, "Upper salt streak", new Vector2(0.16f, 0.17f), saltLight, -34f);
            MakeCurrentFoamStroke(marker.transform, "Lower salt streak", new Vector2(0.16f, -0.17f), saltLight, 34f);
            marker.AddComponent<ScadrykAmbientProp>().ConfigureFlow(0.42f, arrow / 4f);
        }

        var zoneObject = new GameObject("Salt crosswind · drifts southwest");
        zoneObject.transform.SetParent(parent);
        zoneObject.transform.position = center;
        BoxCollider2D trigger = zoneObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(4.2f, 1.9f);
        zoneObject.AddComponent<ScadrykTerrainZone>().Configure(0.96f, new Vector2(-1.05f, -0.48f),
            "O vento arrasta para sudoeste. A esquiva mantém o impulso para cruzar a faixa.", ScadrykTerrainZone.SaltDriftKey);
    }

    public void ShowShallowWaterHint()
    {
        ShowTerrainHint("shallow-water", "A água rasa reduz seu passo. A esquiva mantém o impulso.");
    }

    public void ShowTerrainHint(string key, string hint)
    {
        if (!terrainHintsShown.Add(string.IsNullOrWhiteSpace(key) ? "terrain" : key)) return;
        ShowStatus(string.IsNullOrWhiteSpace(hint) ? "A água rasa reduz seu passo. A esquiva mantém o impulso." : hint, 4f);
    }

    private static void DrawCoastalCurrent(Transform parent, float roomX)
    {
        Vector2 center = new Vector2(roomX + 1.0f, -2.4f);
        MakeDecoration(parent, "Bell coast cross-current", center, new Vector2(4.0f, 1.6f),
            new Color(0.12f, 0.55f, 0.67f, 0.20f), false, -8);

        // Repeated chevrons make the current direction legible before Nara enters it.
        for (int arrow = 0; arrow < 4; arrow++)
        {
            Vector2 head = center + new Vector2(-1.55f + arrow * 1.02f, 0f);
            Color foam = new Color(0.50f, 0.86f, 0.88f, 0.62f);
            var arrowMarker = new GameObject("Current arrow drifting east");
            arrowMarker.transform.SetParent(parent);
            arrowMarker.transform.position = head;
            MakeCurrentFoamStroke(arrowMarker.transform, "Upper foam", new Vector2(-0.16f, 0.20f), foam, -32f);
            MakeCurrentFoamStroke(arrowMarker.transform, "Lower foam", new Vector2(-0.16f, -0.20f), foam, 32f);
            arrowMarker.AddComponent<ScadrykAmbientProp>().ConfigureFlow(0.58f, arrow / 4f);
        }

        var zoneObject = new GameObject("Bell coast cross-current · pushes east");
        zoneObject.transform.SetParent(parent);
        zoneObject.transform.position = center;
        BoxCollider2D trigger = zoneObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(4.0f, 1.6f);
        zoneObject.AddComponent<ScadrykTerrainZone>().Configure(1f, new Vector2(1.8f, 0f),
            "A corrente do recife empurra para leste. Atravesse com a esquiva ou avance contra o fluxo.", ScadrykTerrainZone.CoastalCurrentKey, ScadrykCue.CoastalCurrent);
    }

    private static void MakeCurrentFoamStroke(Transform parent, string name, Vector2 localPosition, Color color, float rotation)
    {
        var stroke = new GameObject(name);
        stroke.transform.SetParent(parent, false);
        stroke.transform.localPosition = localPosition;
        stroke.transform.localScale = new Vector3(0.60f, 0.075f, 1f);
        stroke.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        var renderer = stroke.AddComponent<SpriteRenderer>();
        renderer.sprite = SquareSprite;
        renderer.color = color;
        renderer.sortingOrder = -7;
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
        var groundShadow = new GameObject("Soft contact shadow"); groundShadow.transform.SetParent(go.transform, false);
        groundShadow.transform.localPosition = new Vector3(0.08f, -0.16f, 0f); groundShadow.transform.localScale = new Vector3(1.3f, 0.62f, 1f);
        var shadowRenderer = groundShadow.AddComponent<SpriteRenderer>(); shadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; shadowRenderer.color = new Color(0.035f, 0.05f, 0.07f, 0.8f); shadowRenderer.sortingOrder = 0;
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
        foreach (var offset in RoomObstacleOffsets(phaseIndex, index))
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

    internal static Vector2[] RoomObstacleOffsets(int biome, int room)
    {
        biome = Mathf.Clamp(biome, 0, 2);
        room = Mathf.Clamp(room, 0, 2);
        if (biome == 0)
        {
            if (room == 0) return new[] { new Vector2(-3.4f, 2.9f), new Vector2(3.1f, -2.8f), new Vector2(4.4f, 2.1f), new Vector2(-0.6f, 0.4f), new Vector2(0.6f, -0.4f) };
            if (room == 1) return new[] { new Vector2(-4.0f, -2.5f), new Vector2(1.8f, 3.5f), new Vector2(4.3f, -2.8f), new Vector2(-0.4f, -3.7f), new Vector2(-0.8f, -0.8f), new Vector2(0.8f, 0.8f) };
            return new[] { new Vector2(-4.3f, 2.8f), new Vector2(-2.2f, -3.4f), new Vector2(3.8f, 1.9f), new Vector2(-0.6f, 0.6f), new Vector2(0.6f, -0.6f) };
        }
        if (biome == 1)
        {
            if (room == 0) return new[] { new Vector2(-3.7f, -2.7f), new Vector2(-1.2f, 3.6f), new Vector2(4.2f, -1.9f) };
            if (room == 1) return new[] { new Vector2(-4.1f, 0.8f), new Vector2(-1.7f, 3.7f), new Vector2(1.5f, -3.6f), new Vector2(4.4f, -0.2f), new Vector2(-0.2f, 0.3f), new Vector2(0.8f, -0.9f) };
            return new[] { new Vector2(-4.1f, 2.7f), new Vector2(-4.2f, -2.6f), new Vector2(4.0f, -0.5f), new Vector2(-0.5f, 0.7f), new Vector2(0.5f, -0.7f) };
        }
        if (room == 0) return new[] { new Vector2(-3.9f, 2.9f), new Vector2(-0.8f, -3.5f), new Vector2(3.9f, 2.5f) };
        if (room == 1) return new[] { new Vector2(-4.3f, 3.1f), new Vector2(-4.0f, -3.2f), new Vector2(0.2f, 2.6f), new Vector2(4.2f, -2.9f), new Vector2(-0.7f, 0.5f), new Vector2(0.7f, -0.5f) };
        return new[] { new Vector2(-3.8f, 2.7f), new Vector2(-3.6f, -2.8f), new Vector2(4.2f, 0.8f), new Vector2(-0.6f, 0.6f), new Vector2(0.6f, -0.6f) };
    }

    internal static Vector2[] RoomAnchorOffsets(int biome, int room)
    {
        int roomId = Mathf.Clamp(biome, 0, 2) * RoomsPerPhase + Mathf.Clamp(room, 0, RoomsPerPhase - 1);
        switch (roomId)
        {
            case 1: return new[] { new Vector2(3.6f, 0.1f) };
            case 3: return new[] { new Vector2(-1.5f, -0.5f) };
            case 4: return new[] { new Vector2(-2.2f, 1.25f), new Vector2(4.2f, 2.65f) };
            case 6: return new[] { new Vector2(-1.2f, 0.3f) };
            case 7: return new[] { new Vector2(-3.6f, -0.1f), new Vector2(5.4f, 2.15f) };
            default: return System.Array.Empty<Vector2>();
        }
    }

    internal static bool RoomAnchorsAvoidCover()
    {
        for (int biome = 0; biome < 3; biome++)
        for (int room = 0; room < RoomsPerPhase; room++)
        {
            Vector2[] anchors = RoomAnchorOffsets(biome, room);
            Vector2[] obstacles = RoomObstacleOffsets(biome, room);
            foreach (Vector2 anchor in anchors)
            {
                if (Mathf.Abs(anchor.x + 6.2f) < 1.5f || Mathf.Abs(anchor.x - 8.05f) < 1.5f || Mathf.Abs(anchor.y) > 5.2f)
                    return false;
                foreach (Vector2 obstacle in obstacles)
                {
                    float clearance = ObstacleSpawnCollisionRadius(biome, obstacle, true) + 0.7f + 0.18f;
                    if (Vector2.Distance(anchor, obstacle) < clearance) return false;
                }
            }
        }
        return true;
    }

    private static bool RoomHasWalkableRoute(int biome, int room)
    {
        const float step = 0.35f;
        const float playerRadius = 0.42f;
        float minX = -9.7f + 0.25f + playerRadius;
        float maxX = 9.7f - 0.25f - playerRadius;
        float minY = -6.6f + 0.25f + playerRadius;
        float maxY = 6.6f - 0.25f - playerRadius;
        int columns = Mathf.FloorToInt((maxX - minX) / step) + 1;
        int rows = Mathf.FloorToInt((maxY - minY) / step) + 1;
        var blocked = new bool[columns, rows];
        Vector2[] obstacles = RoomObstacleOffsets(biome, room);
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
        {
            Vector2 point = new Vector2(minX + x * step, minY + y * step);
            foreach (Vector2 obstacle in obstacles)
            {
                float clearance = ObstacleSpawnCollisionRadius(biome, obstacle, true) + playerRadius + 0.12f;
                if (Vector2.Distance(point, obstacle) < clearance) { blocked[x, y] = true; break; }
            }
        }

        int startX = Mathf.RoundToInt((-6.2f - minX) / step);
        int startY = Mathf.RoundToInt(-minY / step);
        int exitX = Mathf.RoundToInt((8.05f - minX) / step);
        int exitY = startY;
        if (blocked[startX, startY] || blocked[exitX, exitY]) return false;

        var visited = new bool[columns, rows];
        var queue = new int[columns * rows];
        int read = 0, write = 0;
        int startIndex = startY * columns + startX;
        queue[write++] = startIndex;
        visited[startX, startY] = true;
        while (read < write)
        {
            int current = queue[read++];
            int x = current % columns;
            int y = current / columns;
            if (x == exitX && y == exitY) return true;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nextX = x + dx, nextY = y + dy;
                if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows ||
                    visited[nextX, nextY] || blocked[nextX, nextY]) continue;
                if (dx != 0 && dy != 0 && (blocked[x + dx, y] || blocked[x, y + dy])) continue;
                visited[nextX, nextY] = true;
                queue[write++] = nextY * columns + nextX;
            }
        }
        return false;
    }

    private bool ActiveRoomHasWalkableRoute(Vector2 center) =>
        ActiveRoomHasWalkableRoute(center, center + Vector2.left * 6.2f, center + Vector2.right * 8.05f);

    private bool ActiveRoomHasWalkableRoute(Vector2 center, Vector2 target) =>
        ActiveRoomHasWalkableRoute(center, center + Vector2.left * 6.2f, target);

    private bool ActiveRoomHasWalkableRoute(Vector2 center, Vector2 start, Vector2 target)
    {
        if (levelRoot == null) return false;
        const float step = 0.35f;
        const float playerRadius = 0.42f;
        float minX = -9.7f + 0.25f + playerRadius;
        float maxX = 9.7f - 0.25f - playerRadius;
        float minY = -12.4f + 0.25f + playerRadius;
        float maxY = 12.4f - 0.25f - playerRadius;
        int columns = Mathf.FloorToInt((maxX - minX) / step) + 1;
        int rows = Mathf.FloorToInt((maxY - minY) / step) + 1;
        var solids = new System.Collections.Generic.List<Collider2D>();
        foreach (Collider2D collider in levelRoot.GetComponentsInChildren<Collider2D>())
        {
            if (collider == null || !collider.enabled || collider.isTrigger ||
                collider.GetComponentInParent<ScadrykPlayerController>() != null ||
                collider.GetComponentInParent<ScadrykEnemyHealth>() != null) continue;
            solids.Add(collider);
        }

        var blocked = new bool[columns, rows];
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
        {
            Vector2 point = center + new Vector2(minX + x * step, minY + y * step);
            foreach (Collider2D solid in solids)
            {
                if ((solid.ClosestPoint(point) - point).sqrMagnitude < playerRadius * playerRadius)
                { blocked[x, y] = true; break; }
            }
        }

        int startX = Mathf.RoundToInt((start.x - center.x - minX) / step);
        int startY = Mathf.RoundToInt((start.y - center.y - minY) / step);
        int targetX = Mathf.RoundToInt((target.x - center.x - minX) / step);
        int targetY = Mathf.RoundToInt((target.y - center.y - minY) / step);
        if (targetX < 0 || targetX >= columns || targetY < 0 || targetY >= rows ||
            blocked[startX, startY] || blocked[targetX, targetY]) return false;

        var visited = new bool[columns, rows];
        var queue = new int[columns * rows];
        int read = 0, write = 0;
        queue[write++] = startY * columns + startX;
        visited[startX, startY] = true;
        while (read < write)
        {
            int current = queue[read++];
            int x = current % columns;
            int y = current / columns;
            if (x == targetX && y == targetY) return true;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nextX = x + dx, nextY = y + dy;
                if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows ||
                    visited[nextX, nextY] || blocked[nextX, nextY]) continue;
                if (dx != 0 && dy != 0 && (blocked[x + dx, y] || blocked[x, y + dy])) continue;
                visited[nextX, nextY] = true;
                queue[write++] = nextY * columns + nextX;
            }
        }
        return false;
    }

    private bool ActiveRoomHasSealedBoundaries()
    {
        if (levelRoot == null) return false;
        bool westIsOuter = roomIndex == 0;
        bool eastIsOuter = roomIndex == RoomsPerPhase - 1;
        float westX = roomCenter.x - 9.7f;
        float eastX = roomCenter.x + 9.7f;
        float westHeight = westIsOuter ? 12f : 5.5f;
        float eastHeight = eastIsOuter ? 12f : 5.5f;
        return HasSolidBoundaryAt(westX, westHeight) && HasSolidBoundaryAt(eastX, eastHeight);
    }

    private bool HasSolidBoundaryAt(float x, float minimumHeight)
    {
        foreach (BoxCollider2D box in levelRoot.GetComponentsInChildren<BoxCollider2D>())
        {
            if (box == null || !box.enabled || box.isTrigger) continue;
            Vector2 position = box.transform.position;
            Vector2 size = box.bounds.size;
            if (Mathf.Abs(position.x - x) <= 0.02f && Mathf.Abs(position.y) <= 0.02f &&
                size.x >= 0.45f && size.y >= minimumHeight) return true;
        }
        return false;
    }

    internal static bool RoomObstacleLayoutsAreDistinctAndOpen()
    {
        for (int biome = 0; biome < 3; biome++)
        for (int room = 0; room < 3; room++)
        {
            Vector2[] layout = RoomObstacleOffsets(biome, room);
            if (layout.Length < 3) return false;
            if (EnemyCountForRoom(biome * RoomsPerPhase + room) > 0 && !RoomHasCoverCluster(biome, layout)) return false;
            foreach (Vector2 offset in layout)
                if (Mathf.Abs(offset.x + 6.2f) < 1.25f || Mathf.Abs(offset.x - 8.05f) < 1.25f || Mathf.Abs(offset.y) > 5.2f)
                    return false;
            if (!RoomHasWalkableRoute(biome, room)) return false;

            for (int priorBiome = 0; priorBiome <= biome; priorBiome++)
            for (int priorRoom = 0; priorRoom < 3; priorRoom++)
            {
                if (priorBiome == biome && priorRoom >= room) break;
                Vector2[] prior = RoomObstacleOffsets(priorBiome, priorRoom);
                if (prior.Length != layout.Length) continue;
                bool same = true;
                foreach (Vector2 offset in layout)
                {
                    bool found = false;
                    foreach (Vector2 candidate in prior) if (candidate == offset) { found = true; break; }
                    if (!found) { same = false; break; }
                }
                if (same) return false;
            }
        }
        return true;
    }

    private static bool RoomHasCoverCluster(int biome, Vector2[] layout)
    {
        const float playerRadius = 0.42f;
        const float routePadding = 0.12f;
        for (int first = 0; first < layout.Length; first++)
        for (int second = first + 1; second < layout.Length; second++)
        {
            float blockedRouteOverlap = ObstacleSpawnCollisionRadius(biome, layout[first], true) +
                ObstacleSpawnCollisionRadius(biome, layout[second], true) + 2f * (playerRadius + routePadding);
            if (Vector2.Distance(layout[first], layout[second]) < blockedRouteOverlap) return true;
        }
        return false;
    }

    private void PlaceFallenLog(Transform parent, Vector2 position, int room, int variant)
    {
        var obstacle = new GameObject("Fallen willow — solid obstacle");
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = position;
        obstacle.transform.rotation = Quaternion.Euler(0f, 0f, ((room + variant) % 2 == 0) ? 13f : -24f);
        var shadow = new GameObject("Long contact shadow"); shadow.transform.SetParent(obstacle.transform, false);
        shadow.transform.localPosition = new Vector3(0.1f, -0.18f, 0f); shadow.transform.localScale = new Vector3(1.85f, 0.68f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>(); shadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; shadowRenderer.color = new Color(0.025f, 0.04f, 0.045f, 0.82f); shadowRenderer.sortingOrder = 0;
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

    private static GameObject MakeDecoration(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool circle, int order, float rotation = 0f)
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
        return go;
    }

    private void SpawnRoomEnemies()
    {
        activeEnemies.Clear();
        int roomId = phaseIndex * RoomsPerPhase + roomIndex;
        int count = EnemyCountForRoom(roomId);
        enemiesRemaining = count;
        var placedOffsets = new Vector2[count];
        var placedRadii = new float[count];
        for (int i = 0; i < count; i++)
        {
            bool boss = roomIndex == RoomsPerPhase - 1;
            int role = EnemyRoleForRoom(roomId, i);
            string[] roleNames = { "Caçador", "Atirador", "Ressonador", "Tecelão" };
            float collisionRadius = EnemySpawnCollisionRadius(role, boss);
            Vector2 offset = FindEnemySpawnOffset(phaseIndex, roomIndex, i, count, boss, role,
                fallenWillowLogSprite != null, placedOffsets, placedRadii, i);
            placedOffsets[i] = offset;
            placedRadii[i] = collisionRadius;
            Vector2 position = roomCenter + offset;
            var enemy = new GameObject(boss ? GuardianNames[phaseIndex] : roleNames[role] + " — " + PhaseNames[phaseIndex]);
            enemy.transform.SetParent(levelRoot.transform);
            enemy.transform.position = position;
            enemy.transform.localScale = Vector3.one * (boss ? 1.85f : role == 0 ? 0.94f : role == 1 ? 0.78f : role == 2 ? 1.08f : 0.86f);
            string sheet = boss ? (phaseIndex == 0 ? "Bosses" : phaseIndex == 1 ? "BossesMidgame" : "BossesFinalWave") : "Enemies";
            Sprite[] frames = LoadSheetRow(sheet, boss ? phaseIndex : EnemyArtworkRowForRole(role), false);
            if (boss && phaseEnemySprites != null && phaseEnemySprites[phaseIndex] != null)
                frames = new[] { phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex] };
            var enemyShadow = new GameObject("Soft ground shadow"); enemyShadow.transform.SetParent(enemy.transform, false);
            enemyShadow.transform.localPosition = new Vector3(0.09f, -0.2f, 0f); enemyShadow.transform.localScale = new Vector3(boss ? 0.92f : 0.65f, boss ? 0.52f : 0.38f, 1f);
            var enemyShadowRenderer = enemyShadow.AddComponent<SpriteRenderer>(); enemyShadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; enemyShadowRenderer.color = new Color(0.04f, 0.055f, 0.07f, 0.82f); enemyShadowRenderer.sortingOrder = 3;
            var enemyVolume = new GameObject("Raised sprite shading"); enemyVolume.transform.SetParent(enemy.transform, false);
            enemyVolume.transform.localPosition = new Vector3(0f, -0.11f, 0f); enemyVolume.transform.localScale = new Vector3(1.02f, 1.02f, 1f);
            var enemyVolumeRenderer = enemyVolume.AddComponent<SpriteRenderer>(); enemyVolumeRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; enemyVolumeRenderer.color = new Color(0.02f, 0.05f, 0.065f, 0.74f); enemyVolumeRenderer.sortingOrder = 3;
            var enemyVisual = new GameObject("Animated visual"); enemyVisual.transform.SetParent(enemy.transform, false);
            var renderer = enemyVisual.AddComponent<SpriteRenderer>(); renderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; renderer.color = Color.white; renderer.sortingOrder = 4;
            var collider = enemy.AddComponent<CircleCollider2D>(); collider.radius = 0.48f;
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<ScadrykDepthSort>().SetBias(4);
            enemy.AddComponent<ScadrykActorAnimator>().SetVisual(enemyVisual.transform, enemyVolumeRenderer);
            var health = enemy.AddComponent<ScadrykEnemyHealth>();
            health.Configure(boss ? 320f + phaseIndex * 80f : 72f + phaseIndex * 12f, boss ? 1.55f : 1.9f + phaseIndex * 0.1f,
                boss, phaseIndex, frames, role, ScadrykEnemyHealth.OpeningAttackDelay(i, boss, phaseIndex == 0 && roomIndex == 0));
            activeEnemies.Add(health);
            if (boss) phaseGuardian = health;
            enemy.AddComponent<ScadrykHealthBar>().Configure(false, boss, boss ? 1.45f : 1.22f);
        }
    }

    internal static int EnemyArtworkRowForRole(int role) => Mathf.Clamp(role, 0, 3) switch
    {
        0 => 0, // Hunter: hooded blade wielder.
        1 => 1, // Ranged: bow and open stance.
        2 => 3, // Area control: heavier armored silhouette.
        _ => 2  // Support: lantern bearer.
    };

    internal static int EnemyCountForRoom(int roomId)
    {
        int room = Mathf.Abs(roomId) % RoomsPerPhase;
        if (room == RoomsPerPhase - 1) return 1;
        if (roomId == 3 || roomId == 6) return 0;
        if (roomId == 0) return 1;
        if (roomId == 1) return 2;
        return room == 1 ? 4 : 3;
    }

    internal static float EnemySpawnCollisionRadius(int role, bool boss)
    {
        if (boss) return 0.58f * 1.85f;
        float scale = role == 0 ? 0.94f : role == 1 ? 0.78f : role == 2 ? 1.08f : 0.86f;
        return 0.34f * scale;
    }

    private static float ObstacleSpawnCollisionRadius(int biome, Vector2 offset, bool hasFallenLog)
    {
        int variation = Mathf.Abs(Mathf.RoundToInt(offset.x * 3f + offset.y * 5f)) % 3;
        if (biome == 0 && variation == 0 && hasFallenLog) return 1.48f;
        if (biome == 0) return variation == 1 ? 0.49f : 0.43f;
        if (biome == 1) return variation == 0 ? 0.45f : 0.42f;
        return variation == 1 ? 0.42f : 0.46f;
    }

    internal static Vector2 FindEnemySpawnOffset(int biome, int room, int enemyIndex, int enemyCount, bool boss, int role,
        bool hasFallenLog, Vector2[] previousOffsets, float[] previousRadii, int previousCount)
    {
        int safeCount = Mathf.Max(1, enemyCount);
        float angle = Mathf.PI * 2f * enemyIndex / safeCount + 0.3f;
        float orbitRadius = boss ? 2.5f : 3.1f + (enemyIndex % 2) * 0.7f;
        float ownRadius = EnemySpawnCollisionRadius(role, boss);
        float bestMargin = float.NegativeInfinity;
        int availablePrevious = previousOffsets == null || previousRadii == null ? 0 : Mathf.Min(previousCount, Mathf.Min(previousOffsets.Length, previousRadii.Length));
        Vector2 best = new Vector2(Mathf.Cos(angle) * orbitRadius, Mathf.Sin(angle) * orbitRadius);
        Vector2[] obstacles = RoomObstacleOffsets(biome, room);
        for (int attempt = 0; attempt < 32; attempt++)
        {
            int step = (attempt + 1) / 2;
            float direction = attempt == 0 || attempt % 2 == 1 ? 1f : -1f;
            if (attempt == 0) step = 0;
            float candidateAngle = angle + direction * step * (Mathf.PI * 2f / 32f);
            Vector2 candidate = new Vector2(Mathf.Cos(candidateAngle) * orbitRadius, Mathf.Sin(candidateAngle) * orbitRadius);
            float margin = float.PositiveInfinity;
            foreach (Vector2 obstacle in obstacles)
                margin = Mathf.Min(margin, Vector2.Distance(candidate, obstacle) - ownRadius - ObstacleSpawnCollisionRadius(biome, obstacle, hasFallenLog) - 0.12f);
            for (int placed = 0; placed < availablePrevious; placed++)
                margin = Mathf.Min(margin, Vector2.Distance(candidate, previousOffsets[placed]) - ownRadius - previousRadii[placed] - 0.12f);
            if (margin > bestMargin) { bestMargin = margin; best = candidate; }
            if (margin >= 0f) return candidate;
        }
        return best;
    }

    private bool EncounterSpawnsAvoidCover()
    {
        for (int biome = 0; biome < PhaseNames.Length; biome++)
        for (int room = 0; room < RoomsPerPhase; room++)
        {
            int roomId = biome * RoomsPerPhase + room;
            int count = EnemyCountForRoom(roomId);
            var offsets = new Vector2[count];
            var radii = new float[count];
            bool boss = room == RoomsPerPhase - 1;
            for (int i = 0; i < count; i++)
            {
                int role = EnemyRoleForRoom(roomId, i);
                float ownRadius = EnemySpawnCollisionRadius(role, boss);
                Vector2 offset = FindEnemySpawnOffset(biome, room, i, count, boss, role, fallenWillowLogSprite != null, offsets, radii, i);
                foreach (Vector2 obstacle in RoomObstacleOffsets(biome, room))
                    if (Vector2.Distance(offset, obstacle) < ownRadius + ObstacleSpawnCollisionRadius(biome, obstacle, fallenWillowLogSprite != null) + 0.119f)
                        return false;
                for (int prior = 0; prior < i; prior++)
                    if (Vector2.Distance(offset, offsets[prior]) < ownRadius + radii[prior] + 0.119f)
                        return false;
                offsets[i] = offset;
                radii[i] = ownRadius;
            }
        }
        return true;
    }

    internal static int EnemyRoleForRoom(int roomId, int enemyIndex)
    {
        if (enemyIndex < 0) return 0;
        if (roomId == 0) return 0;
        if (roomId == 1) return enemyIndex == 0 ? 0 : 1;
        if (roomId == 4) return enemyIndex switch { 0 => 0, 1 => 1, 2 => 2, _ => 3 };
        if (roomId == 7) return enemyIndex switch { 0 => 1, 1 => 1, 2 => 2, _ => 3 };
        return enemyIndex % 4;
    }

    internal static bool GuardianAttackIsPreviewedBeforeBoss(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= 3) return false;
        int previewRoomId = biomeIndex * RoomsPerPhase + RoomsPerPhase - 2;
        int taughtRole = biomeIndex switch { 0 => 0, 1 => 1, _ => 2 };
        for (int enemy = 0; enemy < EnemyCountForRoom(previewRoomId); enemy++)
            if (EnemyRoleForRoom(previewRoomId, enemy) == taughtRole) return true;
        return false;
    }
}
