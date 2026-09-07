using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZeroHero.Editor
{
    public static class ZeroHeroBuild
    {
        [MenuItem("Zero Hero/Build Windows Game")]
        public static void BuildWindows()
        {
            ValidateRules();
            Directory.CreateDirectory("Assets/ZeroHero/Resources");
            const string materialPath = "Assets/ZeroHero/Resources/ZeroSprite.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                if (shader == null) throw new Exception("Sprite shader unavailable.");
                AssetDatabase.CreateAsset(new Material(shader), materialPath);
            }
            AssetDatabase.SaveAssets();
            PlayerSettings.productName = "제로의 용사";
            PlayerSettings.companyName = "Zero Chamber";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            string output = Path.GetFullPath("Builds/Windows/ZeroHero.exe");
            foreach (string arg in Environment.GetCommandLineArgs()) if (arg.StartsWith("--zero-build=")) output = arg.Substring(13);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/ZeroHero.unity" }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("ZERO_BUILD_SUCCESS " + output);
        }

        [MenuItem("Zero Hero/Validate Zero-Sum Rules")]
        public static void ValidateRules()
        {
            int count = 0;
            Action<bool, string> assert = (value, label) => { if (!value) throw new Exception("ZERO TEST FAILED: " + label); count++; };
            var stats = new ZeroStats();
            assert(stats.Total == 10, "Starting total");
            var rng = new System.Random(12);
            for (int i = 0; i < 10000; i++)
            {
                int up = rng.Next(4), down = (up + 1 + rng.Next(3)) % 4, amount = rng.Next(1, 5);
                stats.Exchange(up, down, amount); assert(stats.Total == 10, "Total after randomized exchange " + i);
            }
            stats = new ZeroStats(); stats.Exchange(1, 0, 5);
            assert(stats.AttackDamage == -12, "Signed attack");
            assert(ZeroStats.ApplyAttack(10, 30, stats.AttackDamage) == 22, "Negative damage heals");
            assert(ZeroStats.ApplyAttack(25, 30, stats.AttackDamage) == 30, "Healing capped at max HP");
            assert(ZeroStats.ApplyAttack(10, 30, 0) == 10, "Zero attack is neutral");
            stats = new ZeroStats(); stats.Exchange(0, 1, 5);
            assert(stats.DamageTaken(10) == 16, "Negative defense amplifies damage");
            stats = new ZeroStats(); stats.Exchange(1, 2, 5);
            assert(stats.MoveSpeed < 0, "Negative speed reverses movement");
            stats = new ZeroStats(); stats.Exchange(0, 2, 3);
            assert(stats.MoveSpeed == 0, "Zero speed stops normal movement");
            stats = new ZeroStats(); stats.Exchange(0, 3, 5);
            assert(ZeroStats.CollectCoins(10, 2, stats) == 4, "Negative fortune removes money");
            assert(ZeroStats.CollectCoins(1, 2, stats) == 0, "Wallet cannot underflow");
            bool rejected = false; try { stats.Exchange(0, 0, 3); } catch (ArgumentException) { rejected = true; }
            assert(rejected, "Reject same-stat exchange");
            rejected = false; try { stats.Exchange(0, 1, 0); } catch (ArgumentException) { rejected = true; }
            assert(rejected, "Reject zero exchange");
            assert(ZeroContent.Weapons.Length == 20, "Twenty weapons");
            stats = new ZeroStats();
            var weaponNames = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < ZeroContent.Weapons.Length; i++)
            {
                var weapon = ZeroContent.Weapons[i];
                assert(weapon.id == i && weaponNames.Add(weapon.name), "Unique weapon identity " + i);
                assert(weapon.gun == (i < 10), "Ten guns and ten swords " + i);
                assert(weapon.rate > 0 && weapon.power > 0 && weapon.price >= 0, "Valid weapon tuning " + i);
                assert(weapon.gun ? weapon.bulletSpeed > 0 : weapon.reach > 0, "Weapon range or projectile speed " + i);
                assert(weapon.Damage(stats) == weapon.power, "Base attack reproduces listed weapon damage " + i);
            }
            assert(ZeroContent.ResolveDamage(EnemyKind.Undead, -13) == 26, "Undead reverse healing at double strength");
            assert(ZeroContent.ResolveDamage(EnemyKind.Undead, 13) == 13, "Undead take ordinary positive damage");
            assert(ZeroContent.ResolveDamage(EnemyKind.Human, -13) == -13, "Other enemies retain healing");
            var human = ZeroContent.Enemies[(int)EnemyKind.Human]; var giant = ZeroContent.Enemies[(int)EnemyKind.Giant];
            assert(giant.speed < human.speed / 3 && giant.health > human.health * 6 && giant.damage > human.damage * 3 && giant.size > human.size * 2, "Giant is slow, large and powerful");
            assert(ZeroContent.Enemies[2].health < human.health && ZeroContent.Enemies[3].health < human.health, "Swarm units are individually weak");
            for (int seed = 0; seed < 50; seed++)
                for (int map = 1; map <= ZeroContent.TotalRooms; map++)
                {
                    var offers = ZeroContent.CreateOffers(map, new System.Random(seed));
                    assert(offers.Length == 3, "Three map offers");
                    var layouts = new System.Collections.Generic.HashSet<int>();
                    foreach (var offer in offers)
                    {
                        assert(offer.Total > 0 && layouts.Add(offer.layout), "Nonempty encounter and distinct terrain");
                        assert(offer.Boss == (map % 4 == 0), "Three regular maps before each boss");
                        if (offer.Boss) assert(offer.Total == 1 && offer.counts[(int)ZeroContent.BossForRoom(map)] == 1 && offer.theme == ZeroContent.BossTheme(ZeroContent.BossForRoom(map)), "Correct boss and background");
                        else
                        {
                            if (map == 1) assert(offer.counts[0] == offer.Total, "Only ordinary humans in the opening map");
                            assert(offer.counts[2] % 4 == 0 && offer.counts[3] % 6 == 0, "Full orc and goblin groups");
                            for (int kind = 6; kind < 11; kind++) assert(offer.counts[kind] == 0, "No boss in an ordinary map");
                        }
                    }
                }
            Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/rules-result.txt", "PASS: " + count + " rule assertions.");
            Debug.Log("ZERO_RULES_SUCCESS " + count);
        }
    }
}
