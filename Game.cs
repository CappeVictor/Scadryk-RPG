using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    private class Enemy { public Vector2 Pos, AttackTarget; public float Hp, MaxHp, Cooldown, Windup, HitTextTime, BleedTime; public string HitText = ""; public int BossPattern; public bool Boss, Archer, Elite, Mage; }
    private class Projectile { public Vector2 Pos, Velocity; public float Life = 3f; }
    private class Loot { public Vector2 Pos; public bool Potion; public int Amount; public int Weapon = -1; public bool Armor, Relic; }
    private readonly List<Enemy> enemies = new();
    private readonly List<Projectile> projectiles = new();
    private readonly List<Loot> loot = new();
    private readonly string[] rooms = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO" };
    private Vector2 player = new(150, 330), facing = Vector2.Right;
    private int room, level = 1, xp, gold, potions = 2, damage = 25;
    private float health = 100, maxHealth = 100, attackCooldown, attackEffect, dashCooldown, dashTime, invulnerable, hitFlash;
    private string state = "title";
    private string savePath = "user://scadryk-save.cfg";
    private int weaponLevel;
    private int ownedWeapons = 1;
    private bool inventoryOpen;
    private bool hasArmor, armorEquipped;
    private bool hasRelic, relicEquipped;
    private bool talentChoice;
    private int pendingTalentChoices;
    private int healthTalent, skillTalent, dodgeTalent;
    private static readonly Rect2[] obstacles = { new Rect2(345, 235, 48, 90), new Rect2(565, 375, 58, 80) };
    private static readonly string[] WeaponNames = { "ESPADA COMUM", "LÂMINA RARA", "LÂMINA DO CREPÚSCULO" };
    private static readonly int[] WeaponBonuses = { 0, 10, 4 };
    private string notice = "";
    private float noticeTime;
    private float skillCooldown, skillEffect;
    public override void _Ready() => QueueRedraw();
    private void StartGame() {
        room = 0; level = 1; xp = 0; gold = 0; potions = 2; damage = 25; weaponLevel = 0; ownedWeapons = 1; inventoryOpen = false; hasArmor = armorEquipped = hasRelic = relicEquipped = false; talentChoice = false; pendingTalentChoices = 0; healthTalent = skillTalent = dodgeTalent = 0;
        health = maxHealth = 100; attackCooldown = attackEffect = dashCooldown = dashTime = invulnerable = hitFlash = skillCooldown = skillEffect = 0;
        state = "playing"; SetupRoom();
    }
    private void SetupRoom() {
        player = new Vector2(150, 330); enemies.Clear(); projectiles.Clear(); loot.Clear();
        int count = room == 0 ? 3 : room == 1 ? 5 : 4;
        for (int i = 0; i < count; i++) {
            bool boss = room == 2 && i == 0; bool archer = room > 0 && i == 1; bool mage = room == 2 && i == 2; bool elite = room == 1 && i == count - 1;
            float hp = boss ? 125 : elite ? 105 : archer || mage ? 40 : 50 + room * 8;
            enemies.Add(new Enemy { Pos = new Vector2(480 + i % 3 * 120, 220 + i / 3 * 170 + i % 2 * 60), Boss = boss, Archer = archer, Mage = mage, Elite = elite, Hp = hp, MaxHp = hp, Cooldown = 1 });
        }
    }
    public override void _UnhandledInput(InputEvent e) {
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (state == "chest_choice") {
            if (key.Keycode == Key.Key1) gold += 75;
            else if (key.Keycode == Key.Key2) potions += 2;
            else if (key.Keycode == Key.Key3) health = maxHealth;
            else return;
            state = "victory"; notice = "RECOMPENSA ESCOLHIDA"; noticeTime = 2; QueueRedraw(); return;
        }
        if (state != "playing") { if (key.Keycode == Key.F5 && state == "victory") SaveGame(); else if (key.Keycode == Key.Enter || key.Keycode == Key.Space) StartGame(); else if (key.Keycode == Key.C && state == "title") LoadGame(); return; }
        if (talentChoice) {
            if (key.Keycode == Key.Key1) { healthTalent++; maxHealth += 20; health = Math.Min(maxHealth, health + 20); }
            else if (key.Keycode == Key.Key2) skillTalent++;
            else if (key.Keycode == Key.Key3) dodgeTalent++;
            else return;
            pendingTalentChoices = Math.Max(0, pendingTalentChoices - 1);
            talentChoice = pendingTalentChoices > 0; QueueRedraw(); return;
        }
        if (key.Keycode == Key.I) { inventoryOpen = !inventoryOpen; QueueRedraw(); return; }
        if (inventoryOpen) {
            if (key.Keycode == Key.Key1) EquipWeapon(0);
            else if (key.Keycode == Key.Key2) EquipWeapon(1);
            else if (key.Keycode == Key.Key3) EquipWeapon(2);
            else if (key.Keycode == Key.Key4) EquipArmor();
            else if (key.Keycode == Key.Key5) EquipRelic();
            else if (key.Keycode == Key.Escape) inventoryOpen = false;
            QueueRedraw(); return;
        }
        switch (key.Keycode) {
            case Key.Space: case Key.J: Attack(); break;
            case Key.Shift: case Key.K: if (dashCooldown <= 0) { dashTime = 0.17f; dashCooldown = Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f); } break;
            case Key.Q: if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); } break;
            case Key.R: UseWhirlwind(); break;
            case Key.F5: SaveGame(); break;
            case Key.E:
                if (enemies.Count == 0 && loot.Count > 0) { notice = "RECOLHA OS ITENS ANTES DE SAIR"; noticeTime = 3; }
                else if (enemies.Count == 0 && room < 2 && player.DistanceTo(new Vector2(850, 330)) < 95) { room++; SetupRoom(); }
                else if (enemies.Count == 0 && room == 2 && player.DistanceTo(new Vector2(750, 330)) < 100) state = "chest_choice";
                break;
        }
        QueueRedraw();
    }
    private void EquipWeapon(int index) {
        if (index < 0 || index >= WeaponNames.Length || (ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 2; return; }
        damage += WeaponBonuses[index] - WeaponBonuses[weaponLevel];
        weaponLevel = index;
        notice = $"EQUIPADA: {WeaponNames[index]}"; noticeTime = 2;
    }
    private void EquipArmor() {
        if (!hasArmor) { notice = "ARMADURA AINDA NÃO ENCONTRADA"; noticeTime = 2; return; }
        armorEquipped = !armorEquipped;
        notice = armorEquipped ? "PEITORAL DE PEDRA EQUIPADO • -30% DANO" : "PEITORAL DE PEDRA DESEQUIPADO";
        noticeTime = 2;
    }
    private void EquipRelic() {
        if (!hasRelic) { notice = "RELÍQUIA AINDA NÃO ENCONTRADA"; noticeTime = 2; return; }
        relicEquipped = !relicEquipped;
        notice = relicEquipped ? "RELICÁRIO VITAL EQUIPADO • CURA AO ABATER" : "RELICÁRIO VITAL DESEQUIPADO";
        noticeTime = 2;
    }
    private void SaveGame() {
        if (talentChoice) { notice = "ESCOLHA SEU TALENTO ANTES DE SALVAR"; noticeTime = 3; return; }
        if (state != "victory" && enemies.Count != 0) { notice = "LIMPE A SALA ANTES DE SALVAR"; noticeTime = 3; return; }
        if (loot.Count != 0) { notice = "RECOLHA OS ITENS ANTES DE SALVAR"; noticeTime = 3; return; }
        var file = new ConfigFile();
        file.SetValue("player", "room_cleared", true);
        file.SetValue("player", "completed", state == "victory");
        file.SetValue("player", "room", room); file.SetValue("player", "level", level);
        file.SetValue("player", "xp", xp); file.SetValue("player", "gold", gold);
        file.SetValue("player", "potions", potions); file.SetValue("player", "damage", damage);
        file.SetValue("player", "weapon", weaponLevel); file.SetValue("player", "health", health);
        file.SetValue("player", "owned_weapons", ownedWeapons);
        file.SetValue("player", "has_armor", hasArmor); file.SetValue("player", "armor_equipped", armorEquipped);
        file.SetValue("player", "has_relic", hasRelic); file.SetValue("player", "relic_equipped", relicEquipped);
        file.SetValue("player", "health_talent", healthTalent); file.SetValue("player", "skill_talent", skillTalent); file.SetValue("player", "dodge_talent", dodgeTalent);
        file.SetValue("player", "max_health", maxHealth);
        Error error = file.Save(savePath);
        notice = error == Error.Ok ? "PROGRESSO SALVO" : "ERRO AO SALVAR"; noticeTime = 3;
    }
    private void LoadGame() {
        var file = new ConfigFile();
        if (file.Load(savePath) != Error.Ok) { notice = "NENHUM SALVAMENTO ENCONTRADO"; noticeTime = 3; return; }
        string[] integers = { "room", "level", "xp", "gold", "potions", "damage", "weapon" };
        foreach (string key in integers) {
            if (!file.HasSectionKey("player", key) || file.GetValue("player", key).VariantType != Variant.Type.Int) {
                notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return;
            }
        }
        foreach (string key in new[] { "health", "max_health" }) {
            if (!file.HasSectionKey("player", key)) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
            var type = file.GetValue("player", key).VariantType;
            if (type != Variant.Type.Float && type != Variant.Type.Int) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        }
        if (file.HasSectionKey("player", "room_cleared") && file.GetValue("player", "room_cleared").VariantType != Variant.Type.Bool) {
            notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return;
        }
        if (file.HasSectionKey("player", "completed") && file.GetValue("player", "completed").VariantType != Variant.Type.Bool) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        foreach (string key in new[] { "has_armor", "armor_equipped" }) if (file.HasSectionKey("player", key) && file.GetValue("player", key).VariantType != Variant.Type.Bool) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        foreach (string key in new[] { "health_talent", "skill_talent", "dodge_talent" }) {
            if (file.HasSectionKey("player", key) && (file.GetValue("player", key).VariantType != Variant.Type.Int || file.GetValue("player", key).AsInt64() < 0 || file.GetValue("player", key).AsInt64() > 20)) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        }
        bool savedHasArmor = (bool)file.GetValue("player", "has_armor", false);
        bool savedArmorEquipped = (bool)file.GetValue("player", "armor_equipped", false);
        if (savedArmorEquipped && !savedHasArmor) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        foreach (string key in new[] { "has_relic", "relic_equipped" }) if (file.HasSectionKey("player", key) && file.GetValue("player", key).VariantType != Variant.Type.Bool) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        bool savedHasRelic = (bool)file.GetValue("player", "has_relic", false), savedRelicEquipped = (bool)file.GetValue("player", "relic_equipped", false);
        if (savedRelicEquipped && !savedHasRelic) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        int savedHealthTalent = ReadTalent(file, "health_talent"); int savedSkillTalent = ReadTalent(file, "skill_talent"); int savedDodgeTalent = ReadTalent(file, "dodge_talent");
        long savedRoom = file.GetValue("player", "room").AsInt64();
        long savedLevel = file.GetValue("player", "level").AsInt64();
        long savedXp = file.GetValue("player", "xp").AsInt64();
        long savedGold = file.GetValue("player", "gold").AsInt64();
        long savedPotions = file.GetValue("player", "potions").AsInt64();
        long savedDamage = file.GetValue("player", "damage").AsInt64();
        long savedWeapon = file.GetValue("player", "weapon").AsInt64();
        double savedHealth = file.GetValue("player", "health").AsDouble();
        double savedMaxHealth = file.GetValue("player", "max_health").AsDouble();
        if (savedRoom < 0 || savedRoom > 2 || savedLevel < 1 || savedLevel > 10000 ||
            savedXp < 0 || savedXp >= savedLevel * 60 || savedGold < 0 || savedGold > int.MaxValue ||
            savedPotions < 0 || savedPotions > int.MaxValue || savedDamage < 1 || savedDamage > int.MaxValue ||
            savedWeapon < 0 || savedWeapon > 2 || !double.IsFinite(savedHealth) || !double.IsFinite(savedMaxHealth) ||
            savedMaxHealth < 1 || savedMaxHealth > 1000000 || savedHealth <= 0 || savedHealth > savedMaxHealth) {
            notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return;
        }
        // Salvamentos anteriores não tinham inventário; conceder armas já ultrapassadas.
        long savedOwned = (savedWeapon == 1 ? 3 : 1) | (savedRoom >= 1 ? 4 : 0);
        if (file.HasSectionKey("player", "owned_weapons")) {
            Variant value = file.GetValue("player", "owned_weapons");
            if (value.VariantType != Variant.Type.Int) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
            savedOwned = value.AsInt64();
        }
        if (savedOwned < 1 || savedOwned > 7 || (savedOwned & 1) == 0 || (savedOwned & (1 << (int)savedWeapon)) == 0) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        bool completed = (bool)file.GetValue("player", "completed", false);
        if (completed && savedRoom != 2) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        StartGame();
        room = Mathf.Clamp((int)file.GetValue("player", "room", 0), 0, 2);
        level = Math.Max(1, (int)file.GetValue("player", "level", 1));
        xp = Math.Max(0, (int)file.GetValue("player", "xp", 0)); gold = Math.Max(0, (int)file.GetValue("player", "gold", 0));
        potions = Math.Max(0, (int)file.GetValue("player", "potions", 2)); damage = Math.Max(1, (int)file.GetValue("player", "damage", 25));
        weaponLevel = (int)savedWeapon; ownedWeapons = (int)savedOwned; hasArmor = savedHasArmor; armorEquipped = savedArmorEquipped;
        healthTalent = savedHealthTalent; skillTalent = savedSkillTalent; dodgeTalent = savedDodgeTalent; talentChoice = false; pendingTalentChoices = 0; hasRelic = savedHasRelic; relicEquipped = savedRelicEquipped;
        maxHealth = Math.Max(1, (float)file.GetValue("player", "max_health", 100f));
        health = Mathf.Clamp((float)file.GetValue("player", "health", 100f), 1, maxHealth);
        SetupRoom();
        if ((bool)file.GetValue("player", "room_cleared", false)) enemies.Clear();
        state = completed ? "victory" : "playing";
        notice = "PROGRESSO CARREGADO"; noticeTime = 3;
    }
    private static int ReadTalent(ConfigFile file, string key) {
        if (!file.HasSectionKey("player", key)) return 0;
        return (int)file.GetValue("player", key);
    }
    private void Attack() {
        if (attackCooldown > 0) return;
        attackCooldown = weaponLevel == 2 ? 0.23f : 0.33f; attackEffect = 0.17f;
        foreach (var enemy in enemies) {
            Vector2 offset = enemy.Pos - player;
            if (offset.Length() < (enemy.Boss ? 95 : 78) && (offset.Length() < 20 || facing.Dot(offset.Normalized()) > 0.1f) && !ProjectileBlocked(player, enemy.Pos)) {
                bool critical = GD.Randf() < (weaponLevel == 2 ? 0.25f : 0.12f); int dealt = critical ? damage * 2 : damage;
                enemy.Hp -= dealt; enemy.HitText = critical ? $"CRÍTICO! -{dealt}" : $"-{dealt}"; enemy.HitTextTime = 0.65f;
                if (weaponLevel == 1) enemy.BleedTime = 3f;
                PushEnemy(enemy, facing * 15);
            }
        }
    }
    private void UseWhirlwind() {
        if (skillCooldown > 0) return;
        skillCooldown = Math.Max(1.5f, 6f - skillTalent * 0.5f); skillEffect = 0.3f;
        foreach (var enemy in enemies) {
            Vector2 offset = enemy.Pos - player;
            if (offset.Length() < 110) {
                enemy.Hp -= damage * 1.6f;
                if (offset.LengthSquared() > 0) PushEnemy(enemy, offset.Normalized() * 25);
            }
        }
    }
    private static void PushEnemy(Enemy enemy, Vector2 displacement) {
        MoveEnemy(enemy, displacement);
    }
    private static bool Blocked(Vector2 position) {
        Rect2 body = new(position - new Vector2(12, 12), new Vector2(24, 24));
        foreach (Rect2 obstacle in obstacles) if (body.Intersects(obstacle)) return true;
        return false;
    }
    private static void MoveEnemy(Enemy enemy, Vector2 displacement) {
        Vector2 next = enemy.Pos + displacement;
        if (!Blocked(new Vector2(next.X, enemy.Pos.Y))) enemy.Pos.X = next.X;
        if (!Blocked(new Vector2(enemy.Pos.X, next.Y))) enemy.Pos.Y = next.Y;
        enemy.Pos.X = Mathf.Clamp(enemy.Pos.X, 85, 875); enemy.Pos.Y = Mathf.Clamp(enemy.Pos.Y, 128, 533);
    }
    private void SeparateFromPack(Enemy enemy, float dt) {
        Vector2 separation = Vector2.Zero;
        foreach (Enemy other in enemies) {
            if (ReferenceEquals(enemy, other)) continue;
            Vector2 away = enemy.Pos - other.Pos;
            float distance = away.Length();
            if (distance > 0.01f && distance < 44f) separation += away / distance * ((44f - distance) / 44f);
        }
        if (separation.LengthSquared() > 0.001f) MoveEnemy(enemy, separation.Normalized() * 58f * dt);
    }
    private bool ProjectileBlocked(Vector2 from, Vector2 to) {
        foreach (Rect2 obstacle in obstacles) {
            Rect2 hitbox = new(obstacle.Position - new Vector2(4, 4), obstacle.Size + new Vector2(8, 8));
            if (hitbox.HasPoint(to) || hitbox.HasPoint(from)) return true;
            Vector2 a = hitbox.Position, b = hitbox.Position + new Vector2(hitbox.Size.X, 0);
            Vector2 c = hitbox.Position + hitbox.Size, d = hitbox.Position + new Vector2(0, hitbox.Size.Y);
            if (Geometry2D.SegmentIntersectsSegment(from, to, a, b).VariantType != Variant.Type.Nil || Geometry2D.SegmentIntersectsSegment(from, to, b, c).VariantType != Variant.Type.Nil || Geometry2D.SegmentIntersectsSegment(from, to, c, d).VariantType != Variant.Type.Nil || Geometry2D.SegmentIntersectsSegment(from, to, d, a).VariantType != Variant.Type.Nil) return true;
        }
        return false;
    }
    public override void _PhysicsProcess(double delta) {
        noticeTime = Math.Max(0, noticeTime - (float)delta);
        if (state != "playing") { QueueRedraw(); return; }
        if (inventoryOpen) { QueueRedraw(); return; }
        float dt = (float)delta;
        skillCooldown = Math.Max(0, skillCooldown - dt); skillEffect = Math.Max(0, skillEffect - dt);
        attackCooldown = Math.Max(0, attackCooldown - dt); attackEffect = Math.Max(0, attackEffect - dt);
        dashCooldown = Math.Max(0, dashCooldown - dt); invulnerable = Math.Max(0, invulnerable - dt); hitFlash = Math.Max(0, hitFlash - dt);
        Vector2 dir = new((Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right) ? 1 : 0) - (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left) ? 1 : 0), (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down) ? 1 : 0) - (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up) ? 1 : 0));
        dir = dir.Normalized(); if (dir != Vector2.Zero && dashTime <= 0) facing = dir;
        bool dashing = dashTime > 0;
        Vector2 candidate = player + (dashing ? facing : dir) * (dashing ? 530 : 190) * dt;
        Rect2 playerBox = new(candidate - new Vector2(12, 12), new Vector2(24, 24));
        bool hitsObstacle = false; foreach (Rect2 obstacle in obstacles) if (playerBox.Intersects(obstacle)) { hitsObstacle = true; break; }
        if (!hitsObstacle) player = candidate;
        dashTime = Math.Max(0, dashTime - dt);
        player.X = Mathf.Clamp(player.X, 85, 875); player.Y = Mathf.Clamp(player.Y, 128, 533);
        for (int i = loot.Count - 1; i >= 0; i--) {
            var item = loot[i];
            if (player.DistanceTo(item.Pos) < 30) {
                if (item.Relic) { hasRelic = true; notice = "RELÍQUIA ENCONTRADA: RELICÁRIO VITAL"; }
                else if (item.Armor) { hasArmor = true; notice = "ARMADURA ENCONTRADA: PEITORAL DE PEDRA"; }
                else if (item.Weapon >= 0) { ownedWeapons |= 1 << item.Weapon; notice = $"ARMA ENCONTRADA: {WeaponNames[item.Weapon]}"; }
                else if (item.Potion) { potions++; notice = "+1 POÇÃO"; }
                else { gold += item.Amount; notice = $"+{item.Amount} OURO"; }
                noticeTime = 1.5f; loot.RemoveAt(i);
            }
        }
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            var shot = projectiles[i]; Vector2 previous = shot.Pos; shot.Pos += shot.Velocity * dt; shot.Life -= dt;
            if (ProjectileBlocked(previous, shot.Pos)) { projectiles.RemoveAt(i); continue; }
            if (shot.Pos.DistanceTo(player) < 18 && !dashing && invulnerable <= 0) {
                health -= MitigatedDamage(9); invulnerable = 0.55f; hitFlash = 0.2f; projectiles.RemoveAt(i);
                if (health <= 0) { health = 0; state = "dead"; }
            } else if (shot.Life <= 0 || shot.Pos.X < 65 || shot.Pos.X > 895 || shot.Pos.Y < 108 || shot.Pos.Y > 553) projectiles.RemoveAt(i);
        }
        if (talentChoice) { QueueRedraw(); return; }
        if (state != "playing") { QueueRedraw(); return; }
        for (int i = enemies.Count - 1; i >= 0; i--) {
            var enemy = enemies[i];
            enemy.HitTextTime = Math.Max(0, enemy.HitTextTime - dt);
            if (enemy.BleedTime > 0) { enemy.BleedTime = Math.Max(0, enemy.BleedTime - dt); enemy.Hp -= 2f * dt; }
            if (enemy.Hp <= 0) {
                loot.Add(new Loot { Pos = enemy.Pos, Amount = enemy.Boss ? 20 : enemy.Elite ? 15 : 6 });
                if (relicEquipped) health = Math.Min(maxHealth, health + 5);
                xp += enemy.Boss ? 45 : enemy.Elite ? 30 : 20;
                if (enemy.Boss && !hasRelic) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(-20, 0), Relic = true });
                if (enemy.Elite && !hasArmor) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(20, 0), Armor = true });
                else if (!enemy.Boss && GD.Randf() < 0.18f) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(18, 0), Potion = true });
                while (xp >= level * 60) { xp -= level * 60; level++; damage += 5; pendingTalentChoices++; }
                talentChoice = pendingTalentChoices > 0;
                enemies.RemoveAt(i);
                if (enemies.Count == 0 && room < 2) {
                    int found = room == 0 ? 2 : 1;
                    if ((ownedWeapons & (1 << found)) == 0) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(-18, 0), Weapon = found });
                }
                if (talentChoice) { QueueRedraw(); return; }
                continue;
            }
            if (!enemy.Boss && enemy.Windup <= 0) SeparateFromPack(enemy, dt);
            enemy.Cooldown -= dt; Vector2 offset = player - enemy.Pos;
            if (enemy.Mage && enemy.Windup <= 0) {
                if (offset.Length() < 170 && offset.LengthSquared() > 0) MoveEnemy(enemy, -offset.Normalized() * 48 * dt);
                if (enemy.Cooldown <= 0) { enemy.AttackTarget = player; enemy.Windup = 0.8f; enemy.Cooldown = 2.3f; }
                continue;
            }
            if (enemy.Archer) {
                if (offset.Length() > 250) MoveEnemy(enemy, offset.Normalized() * 60 * dt);
                else if (offset.Length() < 150 && offset.LengthSquared() > 0) MoveEnemy(enemy, -offset.Normalized() * 60 * dt);
                enemy.Pos.X = Mathf.Clamp(enemy.Pos.X, 85, 875); enemy.Pos.Y = Mathf.Clamp(enemy.Pos.Y, 128, 533);
                if (enemy.Cooldown <= 0 && offset.LengthSquared() > 0) {
                    projectiles.Add(new Projectile { Pos = enemy.Pos, Velocity = offset.Normalized() * 210 }); enemy.Cooldown = 1.8f;
                }
                continue;
            }
            if (enemy.Windup > 0) {
                enemy.Windup -= dt;
                float strikeDistance = enemy.Mage ? 52 : enemy.Elite ? 62 : enemy.BossPattern == 0 ? 90 : 42;
                bool targetedStrike = enemy.Mage || enemy.Elite || enemy.BossPattern == 1;
                if (enemy.Windup <= 0 && (targetedStrike ? player.DistanceTo(enemy.AttackTarget) < strikeDistance : offset.Length() < strikeDistance) && invulnerable <= 0 && !dashing) {
                    float incomingDamage = enemy.Mage ? 16 : enemy.Elite ? 20 : enemy.BossPattern == 0 ? 22 : 30;
                    health -= MitigatedDamage(incomingDamage); invulnerable = 0.55f; hitFlash = 0.2f;
                    if (health <= 0) { health = 0; state = "dead"; }
                }
                if (state != "playing") break;
                continue;
            }
            if (enemy.Elite && enemy.Cooldown <= 0 && offset.Length() < 220) {
                enemy.AttackTarget = player; enemy.Windup = 0.75f; enemy.Cooldown = 2.6f; continue;
            }
            bool enraged = enemy.Boss && enemy.Hp <= enemy.MaxHp * 0.5f;
            if (enemy.Boss && enemy.Cooldown <= 0 && offset.Length() < 125) {
                enemy.BossPattern = (enemy.BossPattern + 1) % 2;
                enemy.AttackTarget = player;
                enemy.Windup = enraged ? 0.55f : 0.85f; enemy.Cooldown = enraged ? 1.35f : 2.2f; continue;
            }
            if (offset.Length() > (enemy.Boss ? 39 : 30)) MoveEnemy(enemy, offset.Normalized() * (enemy.Boss ? 58 : 76 + room * 8) * dt);
            else if (enemy.Cooldown <= 0) {
                enemy.Cooldown = enemy.Boss ? 1.1f : 1.35f;
                if (invulnerable <= 0 && !dashing) { health -= MitigatedDamage(enemy.Boss ? 18 : enemy.Elite ? 14 : 10); invulnerable = 0.55f; hitFlash = 0.2f; if (health <= 0) { health = 0; state = "dead"; } }
            }
            if (state != "playing") break;
        }
        QueueRedraw();
    }
    private float MitigatedDamage(float incoming) => armorEquipped ? incoming * 0.7f : incoming;
    private void Text(string s, Vector2 p, int size, Color c) => DrawString(ThemeDB.FallbackFont, p, s, HorizontalAlignment.Left, -1, size, c);
    public override void _Draw() {
        Color goldColor = new("e6c181"), text = new("ded8c9");
        DrawRect(new Rect2(0, 0, 960, 640), new Color("101317"));
        DrawRect(new Rect2(0, 0, 960, 83), new Color("1c2023"));
        Text("SCADRYK RPG", new Vector2(25, 45), 22, goldColor);
        Text($"VIDA {health:0}/{maxHealth:0}   NÍVEL {level}   XP {xp}/{level * 60}   OURO {gold}   POÇÕES {potions}", new Vector2(240, 45), 15, text);
        DrawRect(new Rect2(25, 61, 185, 9), new Color("292929"));
        DrawRect(new Rect2(25, 61, 185 * Mathf.Clamp(health / maxHealth, 0, 1), 9), health / maxHealth <= 0.25f ? new Color("d84f49") : new Color("7eb77d"));
        DrawRect(new Rect2(45, 95, 870, 472), new Color("3c3e3a")); DrawRect(new Rect2(65, 108, 830, 445), new Color("303530"));
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) DrawRect(new Rect2(70 + x * 68, 112 + y * 62, 67, 61), new Color("39403a"), false);
        foreach (Rect2 obstacle in obstacles) { DrawRect(obstacle, new Color("514c40")); DrawRect(obstacle, new Color("82765f"), false, 3); }
        DrawRect(new Rect2(65, 108, 830, 445), new Color("7c7866"), false, 4);
        DrawRect(new Rect2(873, 293, 22, 75), enemies.Count == 0 && loot.Count == 0 ? new Color("8ac4a9") : new Color("252526"));
        if (room == 2) DrawRect(new Rect2(730, 318, 40, 27), new Color("ac7b3e"));
        foreach (var enemy in enemies) {
            float radius = enemy.Boss ? 23 : 15;
            if (enemy.Windup > 0) {
                if (enemy.Mage || enemy.Elite) {
                    float baseRadius = enemy.Elite ? 62 : 52;
                    float windupDuration = enemy.Elite ? 0.75f : 0.8f;
                    float warningRadius = baseRadius * Mathf.Lerp(0.35f, 1f, Mathf.Clamp(enemy.Windup / windupDuration, 0, 1));
                    Color warningColor = enemy.Elite ? new Color("e6a45e") : new Color("bd91e8");
                    DrawCircle(enemy.AttackTarget, warningRadius, new Color(warningColor.R, warningColor.G, warningColor.B, 0.2f));
                    DrawArc(enemy.AttackTarget, warningRadius, 0, Mathf.Tau, 40, warningColor, 4, true);
                }
                else if (enemy.BossPattern == 0) DrawArc(enemy.Pos, 90, 0, Mathf.Tau, 48, new Color("e67b58"), 3, true);
                else { DrawLine(enemy.Pos, enemy.AttackTarget, new Color("e67b58"), 12); DrawCircle(enemy.AttackTarget, 42, new Color(0.9f, 0.35f, 0.25f, 0.35f)); }
            }
            DrawCircle(enemy.Pos, radius, enemy.Boss ? new Color("8f6459") : enemy.Elite ? new Color("a67dc0") : enemy.Mage ? new Color("8d72c7") : enemy.Archer ? new Color("8b9f75") : new Color("b7b5a6"));
            string role = enemy.Elite ? "ELITE" : enemy.Archer ? "ARQUEIRO" : enemy.Mage ? "CONJURADOR" : "";
            if (role.Length > 0) Text(role, enemy.Pos + new Vector2(-28, -31), 10, enemy.Elite ? new Color("d4a7e8") : enemy.Mage ? new Color("d7b6f0") : new Color("b9cc9f"));
            DrawCircle(enemy.Pos + new Vector2(-5, -4), 2.5f, new Color("bc5145")); DrawCircle(enemy.Pos + new Vector2(5, -4), 2.5f, new Color("bc5145"));
            if (enemy.HitTextTime > 0) Text(enemy.HitText, enemy.Pos + new Vector2(-22, -radius - 25), 11, enemy.HitText.StartsWith("CRÍTICO") ? new Color("ffd16f") : text);
            DrawRect(new Rect2(enemy.Pos.X - 22, enemy.Pos.Y - radius - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 4), new Color("b8584f"));
            if (enemy.Boss) {
                bool enraged = enemy.Hp <= enemy.MaxHp * 0.5f;
                Text(enraged ? "GUARDIÃO DA CRIPTA • ENFURECIDO" : "GUARDIÃO DA CRIPTA", new Vector2(enraged ? 340 : 373, 104), 13, enraged ? new Color("ed725d") : goldColor);
                DrawRect(new Rect2(360, 112, 240, 7), new Color("191919"));
                DrawRect(new Rect2(360, 112, 240 * Mathf.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 7), enraged ? new Color("e3654d") : new Color("b8584f"));
            }
        }
        foreach (var shot in projectiles) DrawCircle(shot.Pos, 5, new Color("e5ae69"));
        foreach (var item in loot) {
            DrawCircle(item.Pos, 8, item.Relic ? new Color("e7a4ed") : item.Armor ? new Color("72b9a3") : item.Weapon >= 0 ? new Color("719ede") : item.Potion ? new Color("d55857") : new Color("e6c181"));
            Text(item.Relic ? "RELICÁRIO" : item.Armor ? "PEITORAL" : item.Weapon >= 0 ? WeaponNames[item.Weapon] + " [RARO]" : item.Potion ? "POÇÃO" : "OURO", item.Pos + new Vector2(-18, -12), 10, text);
        }
        DrawCircle(player, 16, hitFlash > 0 ? new Color("e88975") : new Color("6c8b8a")); DrawCircle(player + new Vector2(0, -5), 8, new Color("dfbd94"));
        DrawLine(player + facing * 8, player + facing * 24, new Color("e7d7b9"), 4);
        if (attackEffect > 0) DrawArc(player, 56, facing.Angle() - 0.8f, facing.Angle() + 0.8f, 16, goldColor, 6, true);
        if (skillEffect > 0) DrawArc(player, 110, 0, Mathf.Tau, 48, new Color("edb96d"), 5, true);
        Text($"{room + 1} / 3    {rooms[room]}", new Vector2(68, 590), 14, text);
        string dashStatus = dashCooldown > 0 ? $"{dashCooldown:0.0}s" : "PRONTA";
        Text($"WASD: mover   ESPAÇO: atacar   SHIFT: esquivar [{dashStatus}]   Q: poção   E: interagir", new Vector2(68, 618), 13, text);
        if (noticeTime > 0) Text(notice, new Vector2(260, 175), 15, goldColor);
        if (state == "playing") Text($"R: redemoinho {(skillCooldown > 0 ? skillCooldown.ToString("0.0") + "s" : "pronto")}   I: arma   F5: salvar", new Vector2(570, 590), 12, text);
        if (enemies.Count == 0 && state == "playing") Text(loot.Count > 0 ? "RECOLHA OS ITENS NO CHÃO" : room == 2 ? "APROXIME-SE DO BAÚ E PRESSIONE E" : "SALA LIMPA: PORTA ABERTA", new Vector2(300, 145), 16, goldColor);
        if (inventoryOpen && state == "playing") {
            DrawRect(new Rect2(150, 145, 660, 360), new Color(0.04f, 0.05f, 0.05f, 0.96f));
            Text("INVENTÁRIO  •  1–3 ARMAS  •  4 ARMADURA  •  5 RELÍQUIA  •  I FECHAR", new Vector2(190, 195), 14, goldColor);
            for (int i = 0; i < WeaponNames.Length; i++) {
                bool owned = (ownedWeapons & (1 << i)) != 0;
                string status = !owned ? "NÃO ENCONTRADA" : i == weaponLevel ? "EQUIPADA" : "DISPONÍVEL";
                string speed = i == 2 ? "RÁPIDA" : "NORMAL";
                int comparison = WeaponBonuses[i] - WeaponBonuses[weaponLevel];
                Text($"{i + 1}. {WeaponNames[i]}  |  DANO {comparison:+#;-#;0}  |  {speed}  |  {status}", new Vector2(190, 245 + i * 38), 14, owned ? text : new Color("777777"));
            }
            Text($"4. PEITORAL DE PEDRA  |  REDUZ DANO EM 30%  |  {(hasArmor ? armorEquipped ? "EQUIPADO" : "DISPONÍVEL" : "NÃO ENCONTRADO")}", new Vector2(190, 375), 14, hasArmor ? text : new Color("777777"));
            Text($"PROTEÇÃO ATUAL: {(armorEquipped ? "30%" : "0%")}", new Vector2(190, 405), 14, new Color("9fd0bb"));
            Text($"5. RELICÁRIO VITAL  |  CURA 5 VIDA AO ABATER  |  {(hasRelic ? relicEquipped ? "EQUIPADO" : "DISPONÍVEL" : "NÃO ENCONTRADO")}", new Vector2(190, 440), 13, hasRelic ? text : new Color("777777"));
            Text($"DANO ATUAL: {damage}   OURO: {gold}", new Vector2(190, 475), 15, goldColor);
        }
        if (talentChoice && state == "playing") {
            DrawRect(new Rect2(185, 200, 590, 225), new Color(0.04f, 0.05f, 0.05f, 0.97f));
            Text(pendingTalentChoices > 1 ? $"SUBIU DE NÍVEL — ESCOLHA UMA MELHORIA ({pendingTalentChoices} PENDENTES)" : "SUBIU DE NÍVEL — ESCOLHA UMA MELHORIA", new Vector2(220, 238), 18, goldColor);
            Text($"1. VIGOR  +20 VIDA MÁXIMA  (nível {healthTalent})", new Vector2(220, 290), 15, text);
            Text($"2. DOMÍNIO  Redemoinho recarrega mais rápido  (nível {skillTalent})", new Vector2(220, 330), 15, text);
            Text($"3. AGILIDADE  esquiva recarrega mais rápido  (nível {dodgeTalent})", new Vector2(220, 370), 15, text);
        }
        if (state == "chest_choice") {
            DrawRect(new Rect2(185, 200, 590, 225), new Color(0.04f, 0.05f, 0.05f, 0.97f));
            Text("BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", new Vector2(220, 245), 18, goldColor);
            Text("1. 75 OURO", new Vector2(220, 300), 16, text);
            Text("2. DUAS POÇÕES", new Vector2(220, 340), 16, text);
            Text("3. RECUPERAR TODA A VIDA", new Vector2(220, 380), 16, text);
        }
        if (state != "playing" && state != "chest_choice") {
            DrawRect(new Rect2(0, 0, 960, 640), new Color(0.025f, 0.03f, 0.035f, 0.92f));
            Text(state == "title" ? "SCADRYK RPG" : state == "victory" ? "VITÓRIA!" : "VOCÊ CAIU", new Vector2(330, 260), 38, goldColor);
            Text(state == "victory" ? "F5 PARA SALVAR • ENTER PARA RECOMEÇAR" : "ENTER PARA COMEÇAR", new Vector2(300, 370), 18, text);
            if (state == "title") Text("C PARA CARREGAR JOGO", new Vector2(350, 395), 16, text);
            if (noticeTime > 0) Text(notice, new Vector2(300, 460), 15, text);
            Text("WASD mover  •  ESPAÇO atacar  •  SHIFT esquivar", new Vector2(240, 420), 16, text);
        }
    }
}
