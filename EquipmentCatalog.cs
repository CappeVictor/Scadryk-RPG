using System.Drawing;

internal sealed record WeaponDefinition(
    string Name,
    int BaseDamage,
    float AttackCooldown,
    float NormalReach,
    float BossReach,
    float SweepDegrees,
    float ActiveSweepFraction,
    float VisualRadius,
    float BaseInset,
    float GlowWidth,
    float EdgeWidth,
    Color AttackColor,
    float BaseCriticalChance,
    float ArmoredDamageMultiplier,
    bool InterruptsCommon,
    bool IsThrust,
    string TraitSummary,
    string TraitDescription);

internal static class EquipmentCatalog
{
    // The stable list order is part of the existing save format: never reorder entries.
    public static readonly IReadOnlyList<WeaponDefinition> Weapons = Array.AsReadOnly(new[]
    {
        new WeaponDefinition("ESPADA DE PEDRA", 0, 0.33f, 78f, 95f, 128f, 0.35f, 57f, 11f, 9f, 3f, Color.FromArgb(224, 213, 181), 0.12f, 0.75f, false, false,
            "médio • equilibrado", "ESPADA EQUILIBRADA • ALCANCE MÉDIO"),
        new WeaponDefinition("LÂMINA DO ARQUIVO", 10, 0.52f, 108f, 122f, 142f, 0.42f, 72f, 16f, 13f, 5f, Color.FromArgb(255, 220, 155), 0.12f, 0.9f, true, false,
            "longo • perfura • interrompe", "LÂMINA PESADA • ALCANCE LONGO • PERFURA ARMADURA • INTERROMPE ECOS"),
        new WeaponDefinition("FACA DO CREPÚSCULO", 4, 0.23f, 68f, 80f, 112f, 0.28f, 46f, 11f, 9f, 3f, Color.FromArgb(163, 222, 223), 0.25f, 0.75f, false, false,
            "curto • veloz • crítico", "FACA RÁPIDA • CURTO ALCANCE • 25% DE CRÍTICO"),
        new WeaponDefinition("LANÇA DA BADALADA", 6, 0.43f, 128f, 148f, 58f, 0.35f, 57f, 11f, 9f, 3f, Color.FromArgb(224, 213, 181), 0.12f, 1f, false, true,
            "alcance máximo • preciso • perfura", "LANÇA PRECISA • ALCANCE MÁXIMO • PERFURA ARMADURA • ARCO ESTREITO")
    });

    public static WeaponDefinition Weapon(int index) => Weapons[index];

    public static bool IsValid() => Weapons.Count == 4
        && Weapons.All(weapon => !string.IsNullOrWhiteSpace(weapon.Name)
            && weapon.BaseDamage >= 0 && weapon.AttackCooldown > 0
            && weapon.NormalReach > 0 && weapon.BossReach >= weapon.NormalReach
            && weapon.SweepDegrees is > 0 and <= 180
            && weapon.ActiveSweepFraction is > 0 and <= 1
            && weapon.VisualRadius > 0 && weapon.BaseInset > 0 && weapon.GlowWidth > 0 && weapon.EdgeWidth > 0
            && weapon.BaseCriticalChance is >= 0 and <= 1
            && weapon.ArmoredDamageMultiplier is > 0 and <= 1
            && !string.IsNullOrWhiteSpace(weapon.TraitSummary)
            && !string.IsNullOrWhiteSpace(weapon.TraitDescription))
        && Weapons.Select(weapon => weapon.Name).Distinct(StringComparer.Ordinal).Count() == Weapons.Count
        && Weapons.Count(weapon => weapon.IsThrust) == 1;
}
