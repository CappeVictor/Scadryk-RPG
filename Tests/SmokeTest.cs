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
            enemies.Clear(); loot.Clear(); Set("gold", 123); Call("SaveGame");
            Check((string)Get("notice") == "PROGRESSO SALVO", "Falha ao salvar.");
            Set("gold", 0); Call("LoadGame");
            Check((int)Get("gold") == 123 && enemies.Count == 0, "Salvamento não restaurou ouro ou sala limpa.");
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
