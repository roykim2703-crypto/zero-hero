using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        // Opt-in verification for a standalone build: --zero-smoke --zero-output=<folder>.
        IEnumerator SmokeTest()
        {
            var checks = new List<string>(); var errors = new List<string>();
            string folder = Path.Combine(Application.persistentDataPath, "Verification");
            foreach (string arg in Environment.GetCommandLineArgs()) if (arg.StartsWith("--zero-output=")) folder = arg.Substring(14);
            Directory.CreateDirectory(folder);
            Application.LogCallback captureError = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack); };
            Application.logMessageReceived += captureError;
            Action<bool, string> check = (condition, name) => { checks.Add((condition ? "PASS " : "FAIL ") + name); if (!condition) errors.Add(name); };
            muted = true;
            yield return new WaitForSeconds(.8f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "01-title.png"));
            yield return new WaitForSeconds(.3f);
            if (Keyboard.current != null)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                yield return null;
                check(phase == Phase.Combat, "Enter key starts a run through the Input System");
            }
            else { StartRun(); check(false, "Keyboard device exists"); }
            ClearActors(); pendingSpawns = 0; spawnTimer = 99;
            SpawnEnemy(0, new Vector2(5, 0));
            Vector2 before = player;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            check(player.x > before.x, "D key moves right with positive speed");
            stats.Exchange(0, 2, 5); before = player;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            check(player.x < before.x, "D key moves left with negative speed");
            stats = new ZeroStats(); stats.Exchange(1, 0, 5);
            Enemy target = enemies[0]; target.hp = 10; float beforeHp = target.hp;
            HitEnemy(target, stats.AttackDamage);
            check(target.hp > beforeHp && target.hp <= target.maxHp, "Negative attack heals an actual enemy without exceeding max HP");
            stats = new ZeroStats(); stats.Exchange(0, 1, 5); hp = 100; invulnerable = 0;
            Hurt(10); check(hp == 84, "Defense -3 adds 6 incoming damage");
            stats = new ZeroStats(); stats.Exchange(0, 3, 5); wallet = 10;
            Collect(new Pickup { pos = player, value = 2 });
            check(wallet == 4, "Fortune -3 removes 6 coins on pickup");
            Collect(new Pickup { pos = player, value = 5 }); check(wallet == 0, "Negative fortune stops wallet at zero");
            stats = new ZeroStats(); stats.Exchange(1, 0, 5); target.pos = player + Vector2.right; target.root.position = target.pos; target.hp = 25;
            Bomb(); check(enemies.Count == 0, "Fixed-damage bomb kills while attack is negative");
            StartRun(); ClearActors(); pendingSpawns = 0; spawnTimer = 99; invulnerable = 999;
            SpawnEnemy(0, new Vector2(3, 0));
            CreateShot(Vector2.zero, Vector2.right * 17, 999, false);
            for (int i = 0; i < 20 && enemies.Count > 0; i++) TickShots(.02f);
            check(enemies.Count == 0, "Swept projectile collision kills a target");
            BeginRoom(); pendingSpawns = 0; invulnerable = 999; roomBanner = 0;
            SpawnEnemy(0, new Vector2(-5, 1)); SpawnEnemy(1, new Vector2(6, 3)); SpawnEnemy(2, new Vector2(4, -3));
            player = new Vector2(-1, -.5f); aim = Vector2.right;
            CreateShot(new Vector2(0, -.5f), Vector2.right * 4, 18, false);
            yield return new WaitForSeconds(.25f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "02-combat.png"));
            yield return new WaitForSeconds(.2f);
            paused = true;
            Vector2 frozen = player; float timeBefore = runTime;
            yield return new WaitForSeconds(.12f);
            check(player == frozen && runTime == timeBefore, "Pause freezes combat and the run timer");
            paused = false;
            stats = new ZeroStats(); stats.Exchange(0, 3, 4); wallet = 20;
            foreach (var coin in coins) Destroy(coin.root.gameObject); coins.Clear();
            var coinSprite = Sprite("Test coin", art.coin, Color.white, player, Vector2.one, 20);
            coins.Add(new Pickup { root = coinSprite.transform, pos = player, value = 3 });
            ClearActorsExceptCoinsForSmoke(); ClearRoom();
            check(wallet == 14, "Room-clear auto-collection applies negative fortune");
            check(phase == Phase.Trade, "Clearing a room requires a trade");
            NextRoom(); check(phase == Phase.Trade && room == 1, "Mandatory exchange cannot be skipped");
            trades[0] = new ZeroTrade(0, 2, 4); trades[1] = new ZeroTrade(1, 0, 4); trades[2] = new ZeroTrade(2, 3, 3);
            selected = 0;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "03-trade.png"));
            yield return new WaitForSeconds(.15f);
            for (int n = 1; n <= LastRoom; n++)
            {
                selected = 0; int totalBefore = stats.Total; ApplyTrade();
                check(stats.Total == totalBefore, "Room " + n + " exchange preserves the total");
                if (n < LastRoom)
                {
                    check(phase == Phase.Camp, "Room " + n + " opens camp");
                    if (n == 1)
                    {
                        yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, "06-shop.png"));
                        yield return new WaitForSeconds(.2f);
                    }
                    NextRoom();
                    if (n + 1 == 4 || n + 1 == 8) check(enemies.Exists(e => e.kind == 3), "Boss spawns in room " + (n + 1));
                    ClearActors(); pendingSpawns = 0; ClearRoom();
                }
            }
            check(phase == Phase.Victory && room == 8, "Final room requires its exchange before victory");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, "04-victory.png"));
            yield return new WaitForSeconds(.2f);
            StartRun(); hp = 1; invulnerable = 0; Hurt(50); check(phase == Phase.Dead, "Lethal hit opens death screen");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, "07-death.png"));
            yield return new WaitForSeconds(.2f);
            StartRun(); check(hp == 100 && stats.Total == 10 && stats[0] == 3 && room == 1 && wallet == 12, "Restart resets the full run");
            help = true;
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, "05-guide.png"));
            yield return new WaitForSeconds(.3f);
            Application.logMessageReceived -= captureError;
            File.WriteAllLines(Path.Combine(folder, "runtime-checks.txt"), checks);
            File.WriteAllText(Path.Combine(folder, "runtime-result.txt"), errors.Count == 0 ? "PASS: " + checks.Count + " runtime checks; no Unity errors." : "FAIL\n" + string.Join("\n", errors));
            Application.Quit(errors.Count == 0 ? 0 : 1);
        }

        void ClearActorsExceptCoinsForSmoke()
        {
            foreach (var enemy in enemies) Destroy(enemy.root.gameObject); enemies.Clear();
            foreach (var shot in shots) Destroy(shot.root.gameObject); shots.Clear(); pendingSpawns = 0;
        }
    }
}
