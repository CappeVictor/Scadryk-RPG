using System.Collections.Generic;
using System.Linq;

public sealed partial class Game
{
    private sealed record EnemyDefinition(
        string Name,
        float BaseHealth,
        float HealthPerRoom = 0,
        int HealthStepStartsAtRoom = 0,
        float AlternateEndingHealth = 0,
        bool IsBoss = false,
        float BodyRadius = 14,
        float ContactDamage = 10,
        float RangedImpactRadius = 0)
    {
        public float HealthAt(int room, int ending) => AlternateEndingHealth > 0
            ? ending == 1 ? BaseHealth : AlternateEndingHealth
            : BaseHealth + Math.Max(0, room - HealthStepStartsAtRoom) * HealthPerRoom;
    }

    // Enemy kinds are save- and campaign-stable; add definitions without changing enum values.
    private static readonly IReadOnlyDictionary<EnemyKind, EnemyDefinition> EnemyCatalog = new Dictionary<EnemyKind, EnemyDefinition>
    {
        [EnemyKind.Grunt] = new("", 58, HealthPerRoom: 8),
        [EnemyKind.Archer] = new("ARQUEIRO", 72, ContactDamage: 10, RangedImpactRadius: 26),
        [EnemyKind.Mage] = new("CONJURADOR", 72, ContactDamage: 16),
        [EnemyKind.Elite] = new("", 120, HealthPerRoom: 6, HealthStepStartsAtRoom: 1, BodyRadius: 18, ContactDamage: 20),
        [EnemyKind.Guardian] = new("GUARDIÃO SEM NOME", 160, IsBoss: true, BodyRadius: 23, ContactDamage: 26),
        [EnemyKind.InkEcho] = new("ECO DE TINTA", 72, ContactDamage: 18, RangedImpactRadius: 30),
        [EnemyKind.BlindScribe] = new("ESCRIBA CEGO", 245, IsBoss: true, BodyRadius: 23, ContactDamage: 23),
        [EnemyKind.DrownedMemory] = new("LEMBRANÇA AFOGADA", 72, ContactDamage: 18, RangedImpactRadius: 28),
        [EnemyKind.RiverFerryman] = new("BARQUEIRO OCO", 285, IsBoss: true, BodyRadius: 23, ContactDamage: 28),
        [EnemyKind.EmberWisp] = new("VESTÍGIO DE BRASAS", 72, ContactDamage: 18, RangedImpactRadius: 28),
        [EnemyKind.AshWarden] = new("GUARDIÃO DAS CINZAS", 340, IsBoss: true, BodyRadius: 23, ContactDamage: 31),
        [EnemyKind.NameMoth] = new("MARIPOSA DE NOMES", 72, ContactDamage: 18, RangedImpactRadius: 28),
        [EnemyKind.HollowName] = new("O NOME VAZIO", 390, IsBoss: true, BodyRadius: 23, ContactDamage: 36),
        [EnemyKind.NameWeaver] = new("A TECELÃ DE NOMES", 410, AlternateEndingHealth: 455, IsBoss: true, BodyRadius: 23, ContactDamage: 39),
        [EnemyKind.FirstKeeper] = new("A PRIMEIRA VIGÍLIA", 560, AlternateEndingHealth: 620, IsBoss: true, BodyRadius: 23, ContactDamage: 44),
        [EnemyKind.MudCantor] = new("CANTOR DO LODO", 72, ContactDamage: 18, RangedImpactRadius: 36),
        [EnemyKind.OathCartographer] = new("CARTÓGRAFA DO VÃO", 610, AlternateEndingHealth: 690, IsBoss: true, BodyRadius: 23, ContactDamage: 42),
        [EnemyKind.PaperWraith] = new("ESPECTRO DE PAPEL", 72, ContactDamage: 18, RangedImpactRadius: 36),
        [EnemyKind.NameBroker] = new("ESCRIVÃO DE CONTRATO", 720, AlternateEndingHealth: 790, IsBoss: true, BodyRadius: 23, ContactDamage: 48),
        [EnemyKind.SaltEcho] = new("ECO SALINO", 72, ContactDamage: 18, RangedImpactRadius: 36),
        [EnemyKind.TideWitness] = new("TESTEMUNHA DA MARÉ", 820, AlternateEndingHealth: 920, IsBoss: true, BodyRadius: 23, ContactDamage: 52),
        [EnemyKind.ReedKeeper] = new("GUARDIÃ DO LEITO", 970, AlternateEndingHealth: 1060, IsBoss: true, BodyRadius: 23, ContactDamage: 56),
        [EnemyKind.FlowKeeper] = new("VIGIA DO VEIO PRESO", 1120, AlternateEndingHealth: 1230, IsBoss: true, BodyRadius: 23, ContactDamage: 62),
        [EnemyKind.CounterCurrent] = new("AFINADOR DAS MARGENS", 1320, AlternateEndingHealth: 1440, IsBoss: true, BodyRadius: 23, ContactDamage: 68),
        [EnemyKind.RiftCantor] = new("A CONTRAVOZ", 1480, AlternateEndingHealth: 1600, IsBoss: true, BodyRadius: 23, ContactDamage: 74),
        [EnemyKind.GlassHerald] = new("ARAUTO DE VIDRO", 1740, AlternateEndingHealth: 1880, IsBoss: true, BodyRadius: 23, ContactDamage: 82),
        [EnemyKind.LanternRegent] = new("REGENTE DAS LANTERNAS", 1920, AlternateEndingHealth: 2080, IsBoss: true, BodyRadius: 23, ContactDamage: 88),
        [EnemyKind.RouteScribe] = new("ESCRIBA DO TRAÇADO", 2250, AlternateEndingHealth: 2450, IsBoss: true, BodyRadius: 23, ContactDamage: 96),
        [EnemyKind.DeepSilence] = new("SILÊNCIO DE BAIXO", 3450, AlternateEndingHealth: 3750, IsBoss: true, BodyRadius: 23, ContactDamage: 112),
        [EnemyKind.BorrowedChorus] = new("ARQUIVISTA DAS MARÉS", 4050, AlternateEndingHealth: 4450, IsBoss: true, BodyRadius: 23, ContactDamage: 118),
        [EnemyKind.LastTuner] = new("AFINADOR DO CONTRATEMPO", 4700, AlternateEndingHealth: 5150, IsBoss: true, BodyRadius: 23, ContactDamage: 126),
        [EnemyKind.CooldownLeech] = new("MORDENTE DE COMPASSO", 72, ContactDamage: 14, RangedImpactRadius: 42),
        [EnemyKind.SlagWeaver] = new("ECO DE ESCÓRIA", 88, ContactDamage: 18, RangedImpactRadius: 48)
    };

    private static EnemyDefinition EnemyData(EnemyKind kind) => EnemyCatalog[kind];

    private static bool IsEnemyCatalogValid() => EnemyCatalog.Count == Enum.GetValues<EnemyKind>().Length
        && Enum.GetValues<EnemyKind>().All(kind => EnemyCatalog.TryGetValue(kind, out EnemyDefinition? definition)
            && (kind is EnemyKind.Grunt or EnemyKind.Elite || !string.IsNullOrWhiteSpace(definition.Name))
            && definition.BaseHealth > 0 && definition.HealthPerRoom >= 0 && definition.AlternateEndingHealth >= 0
            && definition.BodyRadius > 0 && definition.ContactDamage > 0 && definition.RangedImpactRadius >= 0)
        && Enum.GetValues<EnemyKind>().Count(kind => EnemyData(kind).IsBoss) == 20;

    private float EnemyContactDamage(Enemy enemy)
    {
        if (enemy.Kind == EnemyKind.SlagWeaver) return 18;
        if (vigilActive) return 112;
        if (enemy.Kind == EnemyKind.Elite) return HasEliteAffix(enemy, EliteAffix.Bloodbound) ? 24 : enemy.EliteAffix == EliteAffix.Spearbound ? 18 : 20;
        return EnemyData(enemy.Kind).ContactDamage;
    }
}
