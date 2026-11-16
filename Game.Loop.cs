using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed partial class Game
{
    private void PauseForLostFocus() { focusPaused = true; down.Clear(); dashTime = 0; Invalidate(); }
    private void ResumeAfterFocus() { focusPaused = false; Invalidate(); }
    private bool GameplayPaused() => focusPaused || title || dead || victory || storyOverlay || memoryOverlay || mapOpen || journalOpen || inventoryOpen || skillMenuOpen || characterSheetOpen || forgeMenuOpen || talentChoice || chestChoice || endingChoiceOpen || vigilChoiceOpen || settingsOpen;

    private void TickGame()
    {
        float dt = timer.Interval / 1000f;
        GameAudio.SetAmbientActive(!GameplayPaused(), room);
        if (GameplayPaused()) { Invalidate(); return; }
        if (hitStopTime > 0) { hitStopTime = Math.Max(0, hitStopTime - dt); Invalidate(); return; }
        noticeTime = Math.Max(0, noticeTime - dt);
        screenShakeTime = Math.Max(0, screenShakeTime - dt); screenShakeStrength = screenShakeTime > 0 ? screenShakeStrength * Math.Max(0, 1 - dt * 5.5f) : 0; screenShakePhase += dt * 55;
        attackCd = Math.Max(0, attackCd - dt); attackFlash = Math.Max(0, attackFlash - dt); skillCd = Math.Max(0, skillCd - dt); shardCd = Math.Max(0, shardCd - dt); bellCd = Math.Max(0, bellCd - dt); bellWaveTime = Math.Max(0, bellWaveTime - dt); pullCd = Math.Max(0, pullCd - dt); pullWaveTime = Math.Max(0, pullWaveTime - dt); wardCd = Math.Max(0, wardCd - dt); wardTime = Math.Max(0, wardTime - dt); refrainCd = Math.Max(0, refrainCd - dt); refrainWaveTime = Math.Max(0, refrainWaveTime - dt); beaconCd = Math.Max(0, beaconCd - dt); returnVoiceCd = Math.Max(0, returnVoiceCd - dt); returnVoiceWaveTime = Math.Max(0, returnVoiceWaveTime - dt); finalStrikeCd = Math.Max(0, finalStrikeCd - dt); finalStrikeFlash = Math.Max(0, finalStrikeFlash - dt); stoneBastionCd = Math.Max(0, stoneBastionCd - dt); stoneBastionTime = Math.Max(0, stoneBastionTime - dt); nameWeaverCd = Math.Max(0, nameWeaverCd - dt); listenerMarkCd = Math.Max(0, listenerMarkCd - dt); dashCd = Math.Max(0, dashCd - dt); dashTime = Math.Max(0, dashTime - dt); invulnerable = Math.Max(0, invulnerable - dt); veinResonanceTime = Math.Max(0, veinResonanceTime - dt); windupPulse += dt; TickFloatingTexts(dt);
        PointF dir = Normalize(MovementInput(), PointF.Empty); if (dashTime > 0) dir = dashDirection;
        PointF playerBeforeMove = player;
        MovePlayer(Scale(dir, (dashTime > 0 ? 530 : ClassMovementSpeed() * FirstEchoTerrainSpeed()) * dt));
        if (lockedTarget is not null && !TargetVisible(lockedTarget)) lockedTarget = null;
        PointF drift = CurrentDrift();
        if (dashTime <= 0) MovePlayer(Scale(drift, dt));
        UpdateFordRoute(Distance(playerBeforeMove, player), dt, dashTime > 0);
        fordBurstTime = Math.Max(0, fordBurstTime - dt);
        ApplyVigilTide(dt);
        PointF animationDirection = Length(dir) > 0.001f ? dir : drift;
        if (Length(animationDirection) > 0.001f) playerAnimationTime += dt;
        else playerAnimationTime = 0;
        for (int i = drops.Count - 1; i >= 0; i--) if (drops[i].Kind != "weapon" && Distance(player, drops[i].Pos) < 30) { GameAudio.Play(GameAudio.Cue.Loot); Collect(drops[i]); }
        TickBeacon(dt); int roomBeingUpdated = room; foreach (Enemy enemy in enemies.ToArray()) { if ((room is 43 or 46 or 49) && Distance(enemy.Pos, player) > 500) { enemy.Moving = false; continue; } PointF previousPos = enemy.Pos; UpdateEnemy(enemy, dt); enemy.Moving = Distance(previousPos, enemy.Pos) > 0.01f; if (enemy.Moving) enemy.AnimationTime += dt; if (dead || room != roomBeingUpdated) break; }
        TickVigilMinions(dt); TickVigilHazards(dt); TickGroundHazards(dt); TickEnemyWounds(dt); nameRiftBursts.RemoveAll(burst => (burst.TimeLeft -= dt) <= 0);
        AdvanceVoiceShards(dt);
        AdvanceBowArrows(dt);
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
        MarkRoomCleared();
        Invalidate();
    }
}
