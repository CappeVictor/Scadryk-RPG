using Godot;
using System;
using System.Collections;
using System.Reflection;

// Executado com: Godot --headless --path . res://Tests/SmokeTest.tscn
public partial class SmokeTest : Node
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private Game game;
    private object Get(string name) => typeof(Game).GetField(name, Flags).GetValue(game);
    private void Set(string name, object value) => typeof(Game).GetField(name, Flags).SetValue(game, value);
    private void Call(string name) => typeof(Game).GetMethod(name, Flags).Invoke(game, null);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public override void _Ready()
    {
        try {
            game = new Game(); AddChild(game); game.SetPhysicsProcess(false);
            Set("savePath", "user://scadryk-smoketest.cfg");
            Call("StartGame");
            var enemies = (IList)Get("enemies");
            Check(enemies.Count == 3, "A primeira sala deve ter três inimigos.");
            var first = enemies[0];
            var pos = (Vector2)first.GetType().GetField("Pos").GetValue(first);
            Set("player", pos - new Vector2(25, 0)); Set("facing", Vector2.Right);
            float initialHp = (float)first.GetType().GetField("Hp").GetValue(first);
            Call("Attack");
            Check((float)first.GetType().GetField("Hp").GetValue(first) < initialHp, "Ataque não causou dano.");
            Call("UseWhirlwind");
            float afterSkill = (float)first.GetType().GetField("Hp").GetValue(first);
            Call("UseWhirlwind");
            Check((float)first.GetType().GetField("Hp").GetValue(first) == afterSkill, "Redemoinho ignorou recarga.");
            game._PhysicsProcess(0.01);
            Check(enemies.Count == 2, "Inimigo derrotado não foi removido.");
            var loot = (IList)Get("loot");
            Check(loot.Count > 0, "Inimigo não deixou recompensa.");
            int goldBefore = (int)Get("gold");
            var drop = loot[0]; Set("player", (Vector2)drop.GetType().GetField("Pos").GetValue(drop));
            game._PhysicsProcess(0.01);
            Check((int)Get("gold") > goldBefore, "Ouro não foi coletado.");
            Call("SaveGame");
            Check((string)Get("notice") == "LIMPE A SALA ANTES DE SALVAR", "Salvamento deve exigir sala limpa.");
            enemies.Clear(); loot.Clear(); Set("gold", 123);
            Set("ownedWeapons", 5);
            typeof(Game).GetMethod("EquipWeapon", Flags).Invoke(game, new object[] { 2 });
            Check((int)Get("weaponLevel") == 2 && (int)Get("damage") == 29, "Equipar a lâmina rápida não aplicou o dano.");
            Set("attackCooldown", 0f); Call("Attack");
            Check(Math.Abs((float)Get("attackCooldown") - 0.23f) < 0.001f, "Lâmina rápida não alterou a velocidade.");
            Call("SaveGame");
            Check((string)Get("notice") == "PROGRESSO SALVO", "Falha ao salvar.");
            Set("gold", 0); Call("LoadGame");
            Check((int)Get("gold") == 123 && enemies.Count == 0 && (int)Get("weaponLevel") == 2 && (int)Get("ownedWeapons") == 5 && (int)Get("damage") == 29, "Salvamento não restaurou ouro, armas ou sala limpa.");
            var invalid = new ConfigFile();
            Check(invalid.Load("user://scadryk-smoketest.cfg") == Error.Ok, "Falha ao abrir salvamento de teste.");
            invalid.SetValue("player", "gold", "inválido");
            Check(invalid.Save("user://scadryk-smoketest.cfg") == Error.Ok, "Falha ao criar teste de arquivo inválido.");
            Call("LoadGame");
            Check((string)Get("notice") == "SALVAMENTO INVÁLIDO" && (int)Get("gold") == 123, "Dados inválidos alteraram o progresso atual.");
            invalid.SetValue("player", "gold", 123); invalid.SetValue("player", "room", 100);
            Check(invalid.Save("user://scadryk-smoketest.cfg") == Error.Ok, "Falha ao criar teste de valores fora do intervalo.");
            Call("LoadGame");
            Check((string)Get("notice") == "SALVAMENTO INVÁLIDO" && (int)Get("gold") == 123, "Valor fora do intervalo alterou o progresso.");
            invalid.SetValue("player", "room", 2); invalid.SetValue("player", "weapon", 1); invalid.SetValue("player", "damage", 35);
            invalid.EraseSectionKey("player", "owned_weapons");
            Check(invalid.Save("user://scadryk-smoketest.cfg") == Error.Ok, "Falha ao criar salvamento antigo.");
            Call("LoadGame");
            Check((int)Get("weaponLevel") == 1 && (int)Get("ownedWeapons") == 7, "Salvamento antigo não migrou as armas.");
            Call("StartGame");
            foreach (object enemy in enemies) enemy.GetType().GetField("Hp").SetValue(enemy, 0f);
            game._PhysicsProcess(0.01);
            object weaponDrop = null;
            foreach (object item in loot) if ((int)item.GetType().GetField("Weapon").GetValue(item) == 2) weaponDrop = item;
            Check(weaponDrop != null, "A sala não gerou a lâmina rápida no chão.");
            Set("player", (Vector2)weaponDrop.GetType().GetField("Pos").GetValue(weaponDrop));
            game._PhysicsProcess(0.01);
            Check(((int)Get("ownedWeapons") & 4) != 0, "A arma coletada não entrou no inventário.");
            Call("StartGame");
            first = enemies[0];
            first.GetType().GetField("Pos").SetValue(first, new Vector2(870, 330));
            Set("player", new Vector2(850, 330)); Set("facing", Vector2.Right);
            Call("Attack");
            Check(((Vector2)first.GetType().GetField("Pos").GetValue(first)).X <= 875, "O golpe empurrou o inimigo para fora da sala.");
            GD.Print("TESTES OK: combate, recarga, loot e salvamento");
            GetTree().Quit(0);
        } catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
