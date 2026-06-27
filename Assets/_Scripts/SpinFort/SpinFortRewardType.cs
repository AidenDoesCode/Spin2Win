public enum SpinFortRewardType
{
    Points,
    FireRateBuff,
    DamageBuff,
    MovementSpeedBuff,
    HealPlayer,
    BonusEnemiesNextRound,
    Tower,
    BaseHeal,
    GlobalAttackSpeed,
    GlobalTowerRange,
    RerollDiscount,
    GlobalTowerDamage,
    GoldPerRoundGain,
    AllInMultiplier,
    TowersExplodeOnDeath,
    // --- NEW REWARDS ---
    MaxTowerHealthBuff,
    LuckBuff,
    TowerRotationSpeedBuff,
    GlobalTowerDamageMultiplier,
    // Per-instance damage multiplier -- unlike GlobalTowerDamageMultiplier
    // (a permanent buff to every tower, used by the spin wheel), this only
    // affects whichever single tower the card gets dragged onto.
    TowerDamageMultiplier
}