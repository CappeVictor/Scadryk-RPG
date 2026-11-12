using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    private class Enemy { public Vector2 Pos; public float Hp, MaxHp, Cooldown, Windup; public bool Boss; }
    private readonly List<Enemy> enemies = new();
    private readonly string[] rooms = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO" };
    private Vector2 player = new(150, 330), facing = Vector2.Right;
    private int room, level = 1, xp, gold, potions = 2, damage = 25;
    private float health = 100, maxHealth = 100, attackCooldown, attackEffect, dashCooldown, dashTime, invulnerable, hitFlash;
    private string state = "title";
    public override void _Ready() => QueueRedraw();
    private void StartGame() {
        room = 0; level = 1; xp = 0; gold = 0; potions = 2; damage = 25;
        health = maxHealth = 100; attackCooldown = attackEffect = dashCooldown = dashTime = invulnerable = hitFlash = 0;
        state = "playing"; SetupRoom();
    }
    private void SetupRoom() {
        player = new Vector2(150, 330); enemies.Clear();
        int count = room == 0 ? 3 : room == 1 ? 5 : 4;
        for (int i = 0; i < count; i++) {
            bool boss = room == 2 && i == 0; float hp = boss ? 125 : 50 + room * 8;
            enemies.Add(new Enemy { Pos = new Vector2(480 + i % 3 * 120, 220 + i / 3 * 170 + i % 2 * 60), Boss = boss, Hp = hp, MaxHp = hp, Cooldown = 1 });
        }
    }
    public override void _UnhandledInput(InputEvent e) {
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (state != "playing") { if (key.Keycode == Key.Enter || key.Keycode == Key.Space) StartGame(); return; }
        switch (key.Keycode) {
            case Key.Space: case Key.J: Attack(); break;
            case Key.Shift: case Key.K: if (dashCooldown <= 0) { dashTime = 0.17f; dashCooldown = 1.15f; } break;
            case Key.Q: if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); } break;
            case Key.E:
                if (enemies.Count == 0 && room < 2 && player.DistanceTo(new Vector2(850, 330)) < 95) { room++; SetupRoom(); }
                else if (enemies.Count == 0 && room == 2 && player.DistanceTo(new Vector2(750, 330)) < 100) { gold += 100; state = "victory"; }
                break;
        }
        QueueRedraw();
    }
    private void Attack() {
        if (attackCooldown > 0) return;
        attackCooldown = 0.33f; attackEffect = 0.17f;
        foreach (var enemy in enemies) {
            Vector2 offset = enemy.Pos - player;
            if (offset.Length() < (enemy.Boss ? 95 : 78) && (offset.Length() < 20 || facing.Dot(offset.Normalized()) > 0.1f)) { enemy.Hp -= damage; enemy.Pos += facing * 15; }
        }
    }
    public override void _PhysicsProcess(double delta) {
        if (state != "playing") { QueueRedraw(); return; }
        float dt = (float)delta;
        attackCooldown = Math.Max(0, attackCooldown - dt); attackEffect = Math.Max(0, attackEffect - dt);
        dashCooldown = Math.Max(0, dashCooldown - dt); invulnerable = Math.Max(0, invulnerable - dt); hitFlash = Math.Max(0, hitFlash - dt);
        Vector2 dir = new((Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right) ? 1 : 0) - (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left) ? 1 : 0), (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down) ? 1 : 0) - (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up) ? 1 : 0));
        dir = dir.Normalized(); if (dir != Vector2.Zero && dashTime <= 0) facing = dir;
        bool dashing = dashTime > 0; player += (dashing ? facing : dir) * (dashing ? 530 : 190) * dt;
        dashTime = Math.Max(0, dashTime - dt);
        player.X = Mathf.Clamp(player.X, 85, 875); player.Y = Mathf.Clamp(player.Y, 128, 533);
        for (int i = enemies.Count - 1; i >= 0; i--) {
            var enemy = enemies[i];
            if (enemy.Hp <= 0) {
                gold += enemy.Boss ? 20 : 6; xp += enemy.Boss ? 45 : 20;
                if (!enemy.Boss && GD.Randf() < 0.18f) potions++;
                if (xp >= level * 60) { xp -= level * 60; level++; maxHealth += 15; health = Math.Min(maxHealth, health + 25); damage += 5; }
                enemies.RemoveAt(i); continue;
            }
            enemy.Cooldown -= dt; Vector2 offset = player - enemy.Pos;
            if (enemy.Windup > 0) {
                enemy.Windup -= dt;
                if (enemy.Windup <= 0 && offset.Length() < 90 && invulnerable <= 0 && !dashing) {
                    health -= 22; invulnerable = 0.55f; hitFlash = 0.2f;
                    if (health <= 0) { health = 0; state = "dead"; }
                }
                continue;
            }
            if (enemy.Boss && enemy.Cooldown <= 0 && offset.Length() < 100) {
                enemy.Windup = 0.75f; enemy.Cooldown = 2.2f; continue;
            }
            if (offset.Length() > (enemy.Boss ? 39 : 30)) enemy.Pos += offset.Normalized() * (enemy.Boss ? 58 : 76 + room * 8) * dt;
            else if (enemy.Cooldown <= 0) {
                enemy.Cooldown = enemy.Boss ? 1.1f : 1.35f;
                if (invulnerable <= 0 && !dashing) { health -= enemy.Boss ? 18 : 10; invulnerable = 0.55f; hitFlash = 0.2f; if (health <= 0) { health = 0; state = "dead"; } }
            }
        }
        QueueRedraw();
    }
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
        DrawRect(new Rect2(873, 293, 22, 75), enemies.Count == 0 ? new Color("8ac4a9") : new Color("252526"));
        if (room == 2) DrawRect(new Rect2(730, 318, 40, 27), new Color("ac7b3e"));
        foreach (var enemy in enemies) {
            float radius = enemy.Boss ? 23 : 15;
            if (enemy.Windup > 0) DrawArc(enemy.Pos, 90, 0, Mathf.Tau, 48, new Color("e67b58"), 3, true);
            DrawCircle(enemy.Pos, radius, enemy.Boss ? new Color("8f6459") : new Color("b7b5a6"));
            DrawCircle(enemy.Pos + new Vector2(-5, -4), 2.5f, new Color("bc5145")); DrawCircle(enemy.Pos + new Vector2(5, -4), 2.5f, new Color("bc5145"));
            DrawRect(new Rect2(enemy.Pos.X - 22, enemy.Pos.Y - radius - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 4), new Color("b8584f"));
        }
        DrawCircle(player, 16, hitFlash > 0 ? new Color("e88975") : new Color("6c8b8a")); DrawCircle(player + new Vector2(0, -5), 8, new Color("dfbd94"));
        DrawLine(player + facing * 8, player + facing * 24, new Color("e7d7b9"), 4);
        if (attackEffect > 0) DrawArc(player, 56, facing.Angle() - 0.8f, facing.Angle() + 0.8f, 16, goldColor, 6, true);
        Text($"{room + 1} / 3    {rooms[room]}", new Vector2(68, 590), 14, text);
        Text("WASD: mover   ESPAÇO: atacar   SHIFT: esquivar   Q: poção   E: interagir", new Vector2(68, 618), 13, text);
        if (enemies.Count == 0 && state == "playing") Text(room == 2 ? "APROXIME-SE DO BAÚ E PRESSIONE E" : "SALA LIMPA: PORTA ABERTA", new Vector2(300, 145), 16, goldColor);
        if (state != "playing") {
            DrawRect(new Rect2(0, 0, 960, 640), new Color(0.025f, 0.03f, 0.035f, 0.92f));
            Text(state == "title" ? "SCADRYK RPG" : state == "victory" ? "VITÓRIA!" : "VOCÊ CAIU", new Vector2(330, 260), 38, goldColor);
            Text("ENTER PARA COMEÇAR", new Vector2(355, 370), 18, text);
            Text("WASD mover  •  ESPAÇO atacar  •  SHIFT esquivar", new Vector2(240, 420), 16, text);
        }
    }
}
