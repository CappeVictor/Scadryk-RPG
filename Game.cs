using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    private class Enemy { public Vector2 Pos; public float Hp, MaxHp, Cooldown, Windup; public bool Boss, Archer, Elite; }
    private class Projectile { public Vector2 Pos, Velocity; public float Life = 3f; }
    private class Loot { public Vector2 Pos; public bool Potion; public int Amount; public int Weapon = -1; public bool Armor; }
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
    private static readonly string[] WeaponNames = { "ESPADA COMUM", "LÂMINA RARA", "LÂMINA DO CREPÚSCULO" };
    private static readonly int[] WeaponBonuses = { 0, 10, 4 };
    private string notice = "";
    private float noticeTime;
    private float skillCooldown, skillEffect;
    public override void _Ready() => QueueRedraw();
    private void StartGame() {
        room = 0; level = 1; xp = 0; gold = 0; potions = 2; damage = 25; weaponLevel = 0; ownedWeapons = 1; inventoryOpen = false; hasArmor = armorEquipped = false;
        health = maxHealth = 100; attackCooldown = attackEffect = dashCooldown = dashTime = invulnerable = hitFlash = skillCooldown = skillEffect = 0;
        state = "playing"; SetupRoom();
    }
    private void SetupRoom() {
        player = new Vector2(150, 330); enemies.Clear(); projectiles.Clear(); loot.Clear();
        int count = room == 0 ? 3 : room == 1 ? 5 : 4;
        for (int i = 0; i < count; i++) {
            bool boss = room == 2 && i == 0; bool archer = room > 0 && i == 1; bool elite = room == 1 && i == count - 1;
            float hp = boss ? 125 : elite ? 105 : archer ? 40 : 50 + room * 8;
            enemies.Add(new Enemy { Pos = new Vector2(480 + i % 3 * 120, 220 + i / 3 * 170 + i % 2 * 60), Boss = boss, Archer = archer, Elite = elite, Hp = hp, MaxHp = hp, Cooldown = 1 });
        }
    }
    public override void _UnhandledInput(InputEvent e) {
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (state != "playing") { if (key.Keycode == Key.Enter || key.Keycode == Key.Space) StartGame(); else if (key.Keycode == Key.C && state == "title") LoadGame(); return; }
        if (key.Keycode == Key.I) { inventoryOpen = !inventoryOpen; QueueRedraw(); return; }
        if (inventoryOpen) {
            if (key.Keycode == Key.Key1) EquipWeapon(0);
            else if (key.Keycode == Key.Key2) EquipWeapon(1);
            else if (key.Keycode == Key.Key3) EquipWeapon(2);
            else if (key.Keycode == Key.Key4) EquipArmor();
            else if (key.Keycode == Key.Escape) inventoryOpen = false;
            QueueRedraw(); return;
        }
        switch (key.Keycode) {
            case Key.Space: case Key.J: Attack(); break;
            case Key.Shift: case Key.K: if (dashCooldown <= 0) { dashTime = 0.17f; dashCooldown = 1.15f; } break;
            case Key.Q: if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); } break;
            case Key.R: UseWhirlwind(); break;
            case Key.F5: SaveGame(); break;
            case Key.E:
                if (enemies.Count == 0 && loot.Count > 0) { notice = "RECOLHA OS ITENS ANTES DE SAIR"; noticeTime = 3; }
                else if (enemies.Count == 0 && room < 2 && player.DistanceTo(new Vector2(850, 330)) < 95) { room++; SetupRoom(); }
                else if (enemies.Count == 0 && room == 2 && player.DistanceTo(new Vector2(750, 330)) < 100) { gold += 100; state = "victory"; }
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
    private void SaveGame() {
        if (enemies.Count != 0) { notice = "LIMPE A SALA ANTES DE SALVAR"; noticeTime = 3; return; }
        if (loot.Count != 0) { notice = "RECOLHA OS ITENS ANTES DE SALVAR"; noticeTime = 3; return; }
        var file = new ConfigFile();
        file.SetValue("player", "room_cleared", true);
        file.SetValue("player", "room", room); file.SetValue("player", "level", level);
        file.SetValue("player", "xp", xp); file.SetValue("player", "gold", gold);
        file.SetValue("player", "potions", potions); file.SetValue("player", "damage", damage);
        file.SetValue("player", "weapon", weaponLevel); file.SetValue("player", "health", health);
        file.SetValue("player", "owned_weapons", ownedWeapons);
        file.SetValue("player", "has_armor", hasArmor); file.SetValue("player", "armor_equipped", armorEquipped);
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
        foreach (string key in new[] { "has_armor", "armor_equipped" }) if (file.HasSectionKey("player", key) && file.GetValue("player", key).VariantType != Variant.Type.Bool) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
        bool savedHasArmor = (bool)file.GetValue("player", "has_armor", false);
        bool savedArmorEquipped = (bool)file.GetValue("player", "armor_equipped", false);
        if (savedArmorEquipped && !savedHasArmor) { notice = "SALVAMENTO INVÁLIDO"; noticeTime = 3; return; }
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
        StartGame();
        room = Mathf.Clamp((int)file.GetValue("player", "room", 0), 0, 2);
        level = Math.Max(1, (int)file.GetValue("player", "level", 1));
        xp = Math.Max(0, (int)file.GetValue("player", "xp", 0)); gold = Math.Max(0, (int)file.GetValue("player", "gold", 0));
        potions = Math.Max(0, (int)file.GetValue("player", "potions", 2)); damage = Math.Max(1, (int)file.GetValue("player", "damage", 25));
        weaponLevel = (int)savedWeapon; ownedWeapons = (int)savedOwned; hasArmor = savedHasArmor; armorEquipped = savedArmorEquipped;
        maxHealth = Math.Max(1, (float)file.GetValue("player", "max_health", 100f));
        health = Mathf.Clamp((float)file.GetValue("player", "health", 100f), 1, maxHealth);
        SetupRoom();
        if ((bool)file.GetValue("player", "room_cleared", false)) enemies.Clear();
        notice = "PROGRESSO CARREGADO"; noticeTime = 3;
    }
    private void Attack() {
        if (attackCooldown > 0) return;
        attackCooldown = weaponLevel == 2 ? 0.23f : 0.33f; attackEffect = 0.17f;
        foreach (var enemy in enemies) {
            Vector2 offset = enemy.Pos - player;
            if (offset.Length() < (enemy.Boss ? 95 : 78) && (offset.Length() < 20 || facing.Dot(offset.Normalized()) > 0.1f)) { enemy.Hp -= damage; PushEnemy(enemy, facing * 15); }
        }
    }
    private void UseWhirlwind() {
        if (skillCooldown > 0) return;
        skillCooldown = 6f; skillEffect = 0.3f;
        foreach (var enemy in enemies) {
            Vector2 offset = enemy.Pos - player;
            if (offset.Length() < 110) {
                enemy.Hp -= damage * 1.6f;
                if (offset.LengthSquared() > 0) PushEnemy(enemy, offset.Normalized() * 25);
            }
        }
    }
    private static void PushEnemy(Enemy enemy, Vector2 displacement) {
        enemy.Pos += displacement;
        enemy.Pos.X = Mathf.Clamp(enemy.Pos.X, 85, 875);
        enemy.Pos.Y = Mathf.Clamp(enemy.Pos.Y, 128, 533);
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
        bool dashing = dashTime > 0; player += (dashing ? facing : dir) * (dashing ? 530 : 190) * dt;
        dashTime = Math.Max(0, dashTime - dt);
        player.X = Mathf.Clamp(player.X, 85, 875); player.Y = Mathf.Clamp(player.Y, 128, 533);
        for (int i = loot.Count - 1; i >= 0; i--) {
            var item = loot[i];
            if (player.DistanceTo(item.Pos) < 30) {
                if (item.Armor) { hasArmor = true; notice = "ARMADURA ENCONTRADA: PEITORAL DE PEDRA"; }
                else if (item.Weapon >= 0) { ownedWeapons |= 1 << item.Weapon; notice = $"ARMA ENCONTRADA: {WeaponNames[item.Weapon]}"; }
                else if (item.Potion) { potions++; notice = "+1 POÇÃO"; }
                else { gold += item.Amount; notice = $"+{item.Amount} OURO"; }
                noticeTime = 1.5f; loot.RemoveAt(i);
            }
        }
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            var shot = projectiles[i]; shot.Pos += shot.Velocity * dt; shot.Life -= dt;
            if (shot.Pos.DistanceTo(player) < 18 && !dashing && invulnerable <= 0) {
                health -= 9; invulnerable = 0.55f; hitFlash = 0.2f; projectiles.RemoveAt(i);
                if (health <= 0) { health = 0; state = "dead"; }
            } else if (shot.Life <= 0 || shot.Pos.X < 65 || shot.Pos.X > 895 || shot.Pos.Y < 108 || shot.Pos.Y > 553) projectiles.RemoveAt(i);
        }
        if (state != "playing") { QueueRedraw(); return; }
        for (int i = enemies.Count - 1; i >= 0; i--) {
            var enemy = enemies[i];
            if (enemy.Hp <= 0) {
                loot.Add(new Loot { Pos = enemy.Pos, Amount = enemy.Boss ? 20 : enemy.Elite ? 15 : 6 });
                xp += enemy.Boss ? 45 : enemy.Elite ? 30 : 20;
                if (enemy.Elite && !hasArmor) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(20, 0), Armor = true });
                else if (!enemy.Boss && GD.Randf() < 0.18f) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(18, 0), Potion = true });
                if (xp >= level * 60) { xp -= level * 60; level++; maxHealth += 15; health = Math.Min(maxHealth, health + 25); damage += 5; }
                enemies.RemoveAt(i);
                if (enemies.Count == 0 && room < 2) {
                    int found = room == 0 ? 2 : 1;
                    if ((ownedWeapons & (1 << found)) == 0) loot.Add(new Loot { Pos = enemy.Pos + new Vector2(-18, 0), Weapon = found });
                }
                continue;
            }
            enemy.Cooldown -= dt; Vector2 offset = player - enemy.Pos;
            if (enemy.Archer) {
                if (offset.Length() > 250) enemy.Pos += offset.Normalized() * 60 * dt;
                else if (offset.Length() < 150 && offset.LengthSquared() > 0) enemy.Pos -= offset.Normalized() * 60 * dt;
                enemy.Pos.X = Mathf.Clamp(enemy.Pos.X, 85, 875); enemy.Pos.Y = Mathf.Clamp(enemy.Pos.Y, 128, 533);
                if (enemy.Cooldown <= 0 && offset.LengthSquared() > 0) {
                    projectiles.Add(new Projectile { Pos = enemy.Pos, Velocity = offset.Normalized() * 210 }); enemy.Cooldown = 1.8f;
                }
                continue;
            }
            if (enemy.Windup > 0) {
                enemy.Windup -= dt;
                if (enemy.Windup <= 0 && offset.Length() < 90 && invulnerable <= 0 && !dashing) {
                    health -= MitigatedDamage(22); invulnerable = 0.55f; hitFlash = 0.2f;
                    if (health <= 0) { health = 0; state = "dead"; }
                }
                if (state != "playing") break;
                continue;
            }
            if (enemy.Boss && enemy.Cooldown <= 0 && offset.Length() < 100) {
                enemy.Windup = 0.75f; enemy.Cooldown = 2.2f; continue;
            }
            if (offset.Length() > (enemy.Boss ? 39 : 30)) enemy.Pos += offset.Normalized() * (enemy.Boss ? 58 : 76 + room * 8) * dt;
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
        DrawRect(new Rect2(45, 95, 870, 472), new Color("3c3e3a")); DrawRect(new Rect2(65, 108, 830, 445), new Color("303530"));
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) DrawRect(new Rect2(70 + x * 68, 112 + y * 62, 67, 61), new Color("39403a"), false);
        DrawRect(new Rect2(65, 108, 830, 445), new Color("7c7866"), false, 4);
        DrawRect(new Rect2(873, 293, 22, 75), enemies.Count == 0 && loot.Count == 0 ? new Color("8ac4a9") : new Color("252526"));
        if (room == 2) DrawRect(new Rect2(730, 318, 40, 27), new Color("ac7b3e"));
        foreach (var enemy in enemies) {
            float radius = enemy.Boss ? 23 : 15;
            if (enemy.Windup > 0) DrawArc(enemy.Pos, 90, 0, Mathf.Tau, 48, new Color("e67b58"), 3, true);
            DrawCircle(enemy.Pos, radius, enemy.Boss ? new Color("8f6459") : enemy.Elite ? new Color("a67dc0") : enemy.Archer ? new Color("8b9f75") : new Color("b7b5a6"));
            if (enemy.Elite) Text("ELITE", enemy.Pos + new Vector2(-18, -31), 10, new Color("d4a7e8"));
            DrawCircle(enemy.Pos + new Vector2(-5, -4), 2.5f, new Color("bc5145")); DrawCircle(enemy.Pos + new Vector2(5, -4), 2.5f, new Color("bc5145"));
            DrawRect(new Rect2(enemy.Pos.X - 22, enemy.Pos.Y - radius - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 4), new Color("b8584f"));
            if (enemy.Boss) {
                Text("GUARDIÃO DA CRIPTA", new Vector2(373, 104), 13, goldColor);
                DrawRect(new Rect2(360, 112, 240, 7), new Color("191919"));
                DrawRect(new Rect2(360, 112, 240 * Mathf.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 7), new Color("b8584f"));
            }
        }
        foreach (var shot in projectiles) DrawCircle(shot.Pos, 5, new Color("e5ae69"));
        foreach (var item in loot) {
            DrawCircle(item.Pos, 8, item.Armor ? new Color("72b9a3") : item.Weapon >= 0 ? new Color("719ede") : item.Potion ? new Color("d55857") : new Color("e6c181"));
            Text(item.Armor ? "PEITORAL" : item.Weapon >= 0 ? WeaponNames[item.Weapon] + " [RARO]" : item.Potion ? "POÇÃO" : "OURO", item.Pos + new Vector2(-18, -12), 10, text);
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
            DrawRect(new Rect2(150, 155, 660, 330), new Color(0.04f, 0.05f, 0.05f, 0.96f));
            Text("INVENTÁRIO  •  1–3 ARMAS  •  4 ARMADURA  •  I FECHAR", new Vector2(190, 195), 16, goldColor);
            for (int i = 0; i < WeaponNames.Length; i++) {
                bool owned = (ownedWeapons & (1 << i)) != 0;
                string status = !owned ? "NÃO ENCONTRADA" : i == weaponLevel ? "EQUIPADA" : "DISPONÍVEL";
                string speed = i == 2 ? "RÁPIDA" : "NORMAL";
                int comparison = WeaponBonuses[i] - WeaponBonuses[weaponLevel];
                Text($"{i + 1}. {WeaponNames[i]}  |  DANO {comparison:+#;-#;0}  |  {speed}  |  {status}", new Vector2(190, 245 + i * 53), 14, owned ? text : new Color("777777"));
            }
            Text($"DANO ATUAL: {damage}   OURO: {gold}", new Vector2(190, 435), 15, goldColor);
            Text($"4. PEITORAL DE PEDRA  |  REDUZ DANO EM 30%  |  {(hasArmor ? armorEquipped ? "EQUIPADO" : "DISPONÍVEL" : "NÃO ENCONTRADO")}", new Vector2(190, 410), 14, hasArmor ? text : new Color("777777"));
            Text($"PROTEÇÃO ATUAL: {(armorEquipped ? "30%" : "0%")}", new Vector2(190, 460), 14, new Color("9fd0bb"));
        }
        if (state != "playing") {
            DrawRect(new Rect2(0, 0, 960, 640), new Color(0.025f, 0.03f, 0.035f, 0.92f));
            Text(state == "title" ? "SCADRYK RPG" : state == "victory" ? "VITÓRIA!" : "VOCÊ CAIU", new Vector2(330, 260), 38, goldColor);
            Text("ENTER PARA COMEÇAR", new Vector2(355, 370), 18, text);
            if (state == "title") Text("C PARA CARREGAR JOGO", new Vector2(350, 395), 16, text);
            if (noticeTime > 0) Text(notice, new Vector2(300, 460), 15, text);
            Text("WASD mover  •  ESPAÇO atacar  •  SHIFT esquivar", new Vector2(240, 420), 16, text);
        }
    }
}
