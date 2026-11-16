using System.Collections.Generic;
using System.Linq;

public sealed partial class Game
{
    private enum EnemyAiStyle { Melee, RangedKiter, Mage, CooldownLeech, AreaSower, Elite }

    private sealed record EnemyDefinition(
        string Name,
        float BaseHealth,
        float HealthPerRoom = 0,
        int HealthStepStartsAtRoom = 0,
        float AlternateEndingHealth = 0,
        bool IsBoss = false,
        float BodyRadius = 14,
        float ContactDamage = 10,
        float RangedImpactRadius = 0,
        EnemyAiStyle AiStyle = EnemyAiStyle.Melee,
        float PreferredDistance = 0,
        float MaximumPreferredDistance = 0,
        float RetreatSpeed = 0,
        float ApproachSpeed = 0,
        float AttackRange = 0,
        float AttackWindup = 0,
        float AttackCooldown = 0,
        float SpearboundPreferredDistance = 0,
        float SpearboundMaximumPreferredDistance = 0,
        float SpearboundRetreatSpeed = 0,
        float SpearboundApproachSpeed = 0,
        float SpearboundAttackRange = 0,
        float SpearboundAttackWindup = 0,
        float SpearboundAttackCooldown = 0)
    {
        public float HealthAt(int room, int ending) => AlternateEndingHealth > 0
            ? ending == 1 ? BaseHealth : AlternateEndingHealth
            : BaseHealth + Math.Max(0, room - HealthStepStartsAtRoom) * HealthPerRoom;
    }

    // Enemy kinds are save- and campaign-stable; add definitions without changing enum values.
    private static readonly IReadOnlyDictionary<EnemyKind, EnemyDefinition> EnemyCatalog = new Dictionary<EnemyKind, EnemyDefinition>
    {
        [EnemyKind.Grunt] = new("", 58, HealthPerRoom: 8),
        [EnemyKind.Archer] = new("ARQUEIRO", 72, ContactDamage: 10, RangedImpactRadius: 26, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 170, RetreatSpeed: 55, AttackWindup: 0.6f, AttackCooldown: 1.8f),
        [EnemyKind.Mage] = new("CONJURADOR", 72, ContactDamage: 16, RangedImpactRadius: 52, AiStyle: EnemyAiStyle.Mage, PreferredDistance: 165, MaximumPreferredDistance: 315, RetreatSpeed: 42, ApproachSpeed: 50, AttackRange: 315, AttackWindup: 0.8f, AttackCooldown: 2.2f),
        [EnemyKind.Elite] = new("", 120, HealthPerRoom: 6, HealthStepStartsAtRoom: 1, BodyRadius: 18, ContactDamage: 20, AiStyle: EnemyAiStyle.Elite, AttackRange: 220, AttackWindup: 0.58f, AttackCooldown: 2.1f, SpearboundPreferredDistance: 150, SpearboundMaximumPreferredDistance: 210, SpearboundRetreatSpeed: 66, SpearboundApproachSpeed: 48, SpearboundAttackRange: 380, SpearboundAttackWindup: 0.82f, SpearboundAttackCooldown: 2.45f),
        [EnemyKind.Guardian] = new("GUARDIÃO SEM NOME", 160, IsBoss: true, BodyRadius: 23, ContactDamage: 26),
        [EnemyKind.InkEcho] = new("ECO DE TINTA", 72, ContactDamage: 18, RangedImpactRadius: 30, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 135, RetreatSpeed: 68, AttackWindup: 0.75f, AttackCooldown: 2f),
        [EnemyKind.BlindScribe] = new("ESCRIBA CEGO", 245, IsBoss: true, BodyRadius: 23, ContactDamage: 23),
        [EnemyKind.DrownedMemory] = new("LEMBRANÇA AFOGADA", 72, ContactDamage: 18, RangedImpactRadius: 28, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 110, RetreatSpeed: 72, AttackWindup: 0.9f, AttackCooldown: 2.25f),
        [EnemyKind.RiverFerryman] = new("BARQUEIRO OCO", 285, IsBoss: true, BodyRadius: 23, ContactDamage: 28),
        [EnemyKind.EmberWisp] = new("VESTÍGIO DE BRASAS", 72, ContactDamage: 18, RangedImpactRadius: 28, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 145, RetreatSpeed: 72, AttackWindup: 0.7f, AttackCooldown: 1.9f),
        [EnemyKind.AshWarden] = new("GUARDIÃO DAS CINZAS", 340, IsBoss: true, BodyRadius: 23, ContactDamage: 31),
        [EnemyKind.NameMoth] = new("MARIPOSA DE NOMES", 72, ContactDamage: 18, RangedImpactRadius: 28, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 175, RetreatSpeed: 72, AttackWindup: 0.62f, AttackCooldown: 1.7f),
        [EnemyKind.HollowName] = new("O NOME VAZIO", 390, IsBoss: true, BodyRadius: 23, ContactDamage: 36),
        [EnemyKind.NameWeaver] = new("A TECELÃ DE NOMES", 410, AlternateEndingHealth: 455, IsBoss: true, BodyRadius: 23, ContactDamage: 39),
        [EnemyKind.FirstKeeper] = new("A PRIMEIRA VIGÍLIA", 560, AlternateEndingHealth: 620, IsBoss: true, BodyRadius: 23, ContactDamage: 44),
        [EnemyKind.MudCantor] = new("CANTOR DO LODO", 72, ContactDamage: 18, RangedImpactRadius: 36, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 215, RetreatSpeed: 52, AttackWindup: 0.82f, AttackCooldown: 2.15f),
        [EnemyKind.OathCartographer] = new("CARTÓGRAFA DO VÃO", 610, AlternateEndingHealth: 690, IsBoss: true, BodyRadius: 23, ContactDamage: 42),
        [EnemyKind.PaperWraith] = new("ESPECTRO DE PAPEL", 72, ContactDamage: 18, RangedImpactRadius: 36, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 185, RetreatSpeed: 52, AttackWindup: 0.68f, AttackCooldown: 1.95f),
        [EnemyKind.NameBroker] = new("ESCRIVÃO DE CONTRATO", 720, AlternateEndingHealth: 790, IsBoss: true, BodyRadius: 23, ContactDamage: 48),
        [EnemyKind.SaltEcho] = new("ECO SALINO", 72, ContactDamage: 18, RangedImpactRadius: 36, AiStyle: EnemyAiStyle.RangedKiter, PreferredDistance: 155, RetreatSpeed: 62, AttackWindup: 0.68f, AttackCooldown: 1.85f),
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
        [EnemyKind.CooldownLeech] = new("MORDENTE DE COMPASSO", 72, ContactDamage: 14, RangedImpactRadius: 42, AiStyle: EnemyAiStyle.CooldownLeech, PreferredDistance: 150, MaximumPreferredDistance: 285, RetreatSpeed: 58, ApproachSpeed: 36, AttackWindup: 1.05f, AttackCooldown: 3.25f),
        [EnemyKind.SlagWeaver] = new("ECO DE ESCÓRIA", 88, ContactDamage: 18, RangedImpactRadius: 48, AiStyle: EnemyAiStyle.AreaSower, PreferredDistance: 185, MaximumPreferredDistance: 260, RetreatSpeed: 54, ApproachSpeed: 36, AttackWindup: 0.95f, AttackCooldown: 3.75f)
    };

    private bool UpdateDataDrivenEnemyBehavior(Enemy enemy, float dt, float distance, PointF offset)
    {
        EnemyDefinition profile = EnemyData(enemy.Kind);
        PointF retreat = Normalize(Sub(enemy.Pos, player), PointF.Empty);
        switch (profile.AiStyle)
        {
            case EnemyAiStyle.RangedKiter:
                if (distance < profile.PreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(retreat, profile.RetreatSpeed * dt));
                else if (enemy.Cooldown <= 0) { BeginWindup(enemy, player, profile.AttackWindup); enemy.Cooldown = profile.AttackCooldown; }
                SeparateFromPack(enemy, dt); return true;
            case EnemyAiStyle.CooldownLeech:
                if (distance < profile.PreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(retreat, profile.RetreatSpeed * dt));
                else if (distance > profile.MaximumPreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(offset, PointF.Empty), profile.ApproachSpeed * dt));
                else if (enemy.Cooldown <= 0) { BeginWindup(enemy, player, profile.AttackWindup); enemy.Cooldown = profile.AttackCooldown; }
                SeparateFromPack(enemy, dt); return true;
            case EnemyAiStyle.AreaSower:
                if (distance < profile.PreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(retreat, profile.RetreatSpeed * dt));
                else if (distance > profile.MaximumPreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(offset, PointF.Empty), profile.ApproachSpeed * dt));
                else if (enemy.Cooldown <= 0) { BeginWindup(enemy, player, profile.AttackWindup); enemy.Cooldown = profile.AttackCooldown; }
                SeparateFromPack(enemy, dt); return true;
            case EnemyAiStyle.Mage:
                if (distance < profile.PreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(retreat, profile.RetreatSpeed * dt));
                else if (distance > profile.AttackRange) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(offset, PointF.Empty), profile.ApproachSpeed * dt));
                else if (enemy.Cooldown <= 0) { BeginWindup(enemy, player, profile.AttackWindup); enemy.Cooldown = profile.AttackCooldown; }
                SeparateFromPack(enemy, dt); return true;
            case EnemyAiStyle.Elite when enemy.EliteAffix == EliteAffix.Spearbound:
                if (distance < profile.SpearboundPreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(retreat, profile.SpearboundRetreatSpeed * dt));
                else if (distance < profile.SpearboundAttackRange && enemy.Cooldown <= 0) { BeginWindup(enemy, player, profile.SpearboundAttackWindup); enemy.Cooldown = profile.SpearboundAttackCooldown; }
                else if (distance > profile.SpearboundMaximumPreferredDistance) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(offset, PointF.Empty), profile.SpearboundApproachSpeed * dt));
                SeparateFromPack(enemy, dt); return true;
            default:
                return false;
        }
    }

    private static EnemyDefinition EnemyData(EnemyKind kind) => EnemyCatalog[kind];

    private static bool IsEnemyCatalogValid() => EnemyCatalog.Count == Enum.GetValues<EnemyKind>().Length
        && Enum.GetValues<EnemyKind>().All(kind => EnemyCatalog.TryGetValue(kind, out EnemyDefinition? definition)
            && (kind is EnemyKind.Grunt or EnemyKind.Elite || !string.IsNullOrWhiteSpace(definition.Name))
            && definition.BaseHealth > 0 && definition.HealthPerRoom >= 0 && definition.AlternateEndingHealth >= 0
            && definition.BodyRadius > 0 && definition.ContactDamage > 0 && definition.RangedImpactRadius >= 0
            && (definition.AiStyle is EnemyAiStyle.Melee or EnemyAiStyle.Elite || definition.PreferredDistance > 0 && definition.RetreatSpeed > 0 && definition.AttackWindup > 0 && definition.AttackCooldown > 0)
            && (definition.AiStyle is not (EnemyAiStyle.CooldownLeech or EnemyAiStyle.AreaSower or EnemyAiStyle.Mage) || definition.MaximumPreferredDistance > definition.PreferredDistance && definition.ApproachSpeed > 0)
            && (definition.AiStyle != EnemyAiStyle.Mage || definition.AttackRange == definition.MaximumPreferredDistance && definition.RangedImpactRadius > 0)
            && (definition.AiStyle != EnemyAiStyle.Elite || definition.AttackRange > 0 && definition.AttackWindup > 0 && definition.AttackCooldown > 0 && definition.SpearboundPreferredDistance > 0 && definition.SpearboundMaximumPreferredDistance > definition.SpearboundPreferredDistance && definition.SpearboundRetreatSpeed > 0 && definition.SpearboundApproachSpeed > 0 && definition.SpearboundAttackRange > definition.SpearboundMaximumPreferredDistance && definition.SpearboundAttackWindup > 0 && definition.SpearboundAttackCooldown > 0)
            && Enum.IsDefined(definition.AiStyle))
        && Enum.GetValues<EnemyKind>().Count(kind => EnemyData(kind).IsBoss) == 20;

    private float EnemyContactDamage(Enemy enemy)
    {
        if (enemy.Kind == EnemyKind.SlagWeaver) return 18;
        if (vigilActive) return 112;
        if (enemy.Kind == EnemyKind.Elite) return HasEliteAffix(enemy, EliteAffix.Bloodbound) ? 24 : enemy.EliteAffix == EliteAffix.Spearbound ? 18 : 20;
        return EnemyData(enemy.Kind).ContactDamage;
    }
}
