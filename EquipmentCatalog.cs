using System.Drawing;
using System.Globalization;

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

internal sealed record WeaponAffixDefinition(
    int Id,
    string Name,
    float RareValue,
    float RelicValue,
    string GroundFormat,
    string DescriptionFormat,
    bool RelicOnly = false)
{
    public float ValueForRarity(int rarity) => rarity == 2 ? RelicValue : RareValue;
    public bool IsAvailableAtRarity(int rarity) => rarity == 2 || !RelicOnly;
    public string GroundText(int rarity) => IsAvailableAtRarity(rarity) ? Format(GroundFormat, rarity) : "";
    public string Description(int rarity) => IsAvailableAtRarity(rarity) ? Format(DescriptionFormat, rarity) : "";

    private string Format(string template, int rarity) => template.Replace("{value}", ValueForRarity(rarity).ToString("0.##", CultureInfo.GetCultureInfo("pt-BR")), StringComparison.Ordinal);
}

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

    // IDs remain stable because weapon saves store affixes as integers.
    public static readonly IReadOnlyList<WeaponAffixDefinition> WeaponAffixes = Array.AsReadOnly(new[]
    {
        new WeaponAffixDefinition(1, "Trégua", 3, 6, "Trégua • cura {value} por abate", "da Trégua: recupera {value} vida por abate"),
        new WeaponAffixDefinition(2, "Contracanto", 7, 11, "Contracanto • crítico +{value} p.p.", "do Contracanto: +{value} p.p. de crítico"),
        new WeaponAffixDefinition(3, "Maré", 12, 18, "Maré • técnica +{value}%", "da Maré: +{value}% de dano de técnica"),
        new WeaponAffixDefinition(4, "Passo Célere", 10, 16, "Passo Célere • recarga -{value}%", "do Passo Célere: recarga de técnicas -{value}%"),
        new WeaponAffixDefinition(5, "Lastro", 8, 12, "Lastro • dano recebido -{value}%", "do Lastro: dano recebido -{value}%"),
        new WeaponAffixDefinition(6, "Fio Esticado", 12, 20, "Fio Esticado • alcance +{value}%", "do Fio Esticado: alcance básico +{value}%"),
        new WeaponAffixDefinition(7, "Cadência", 0.15f, 0.25f, "Cadência • crítico adianta recargas em {value} s", "da Cadência: crítico adianta recargas em {value} s por golpe"),
        new WeaponAffixDefinition(8, "Ruptura", 6, 8, "Ruptura • crítico aplica ferida por 3 s", "da Ruptura: críticos ferem por 3 s ({value}% do dano da arma/s)"),
        new WeaponAffixDefinition(9, "Nome Riscado", 0, 0, "Nome Riscado • ecos marcados explodem ao morrer", "do Nome Riscado: ecos marcados explodem ao morrer", true)
    });

    public static WeaponDefinition Weapon(int index) => Weapons[index];
    public static WeaponAffixDefinition WeaponAffix(int id) => id is >= 1 and <= 9 ? WeaponAffixes[id - 1] : new WeaponAffixDefinition(0, "", 0, 0, "", "");
    public static int RandomlyRolledAffixCount => WeaponAffixes.Count(affix => !affix.RelicOnly);
    public static int RolledWeaponAffix(int roll) => WeaponAffixes.Where(affix => !affix.RelicOnly).ElementAt(roll % RandomlyRolledAffixCount).Id;
    public static bool IsValidPrimaryAffix(int id) => id >= 1 && id <= WeaponAffixes.Count && !WeaponAffix(id).RelicOnly;
    public static bool IsValidSecondaryAffix(int id, int rarity) => id == 0 || id >= 1 && id <= WeaponAffixes.Count && WeaponAffix(id).IsAvailableAtRarity(rarity);

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
        && Weapons.Count(weapon => weapon.IsThrust) == 1
        && WeaponAffixes.Count == 9
        && WeaponAffixes.Select((affix, index) => affix.Id == index + 1 && !string.IsNullOrWhiteSpace(affix.Name)
            && affix.RareValue >= 0 && affix.RelicValue >= affix.RareValue
            && !string.IsNullOrWhiteSpace(affix.GroundFormat) && !string.IsNullOrWhiteSpace(affix.DescriptionFormat)
            && (!affix.RelicOnly || affix.Id == 9 && affix.RareValue == 0 && affix.RelicValue == 0)).All(isValid => isValid)
        && WeaponAffixes.Select(affix => affix.Name).Distinct(StringComparer.Ordinal).Count() == WeaponAffixes.Count;
}
