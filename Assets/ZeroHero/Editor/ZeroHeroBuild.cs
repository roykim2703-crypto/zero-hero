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
                int up = rng.Next(ZeroStats.Count), down = (up + 1 + rng.Next(ZeroStats.Count - 1)) % ZeroStats.Count, amount = rng.Next(1, 5);
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
            assert(ZeroStats.CollectCoins(10, 2, stats) == 7, "Reduced negative fortune removes money at half rate");
            assert(ZeroStats.CollectCoins(1, 2, stats) == 0, "Wallet cannot underflow");
            assert(Math.Abs(ZeroStats.CoinRate(2)-1f)<.001f && Math.Abs(ZeroStats.CoinRate(3)-1.5f)<.001f, "Fortune changes coin value by fifty percent per point");
            bool rejected = false; try { stats.Exchange(0, 0, 3); } catch (ArgumentException) { rejected = true; }
            assert(rejected, "Reject same-stat exchange");
            rejected = false; try { stats.Exchange(0, 1, 0); } catch (ArgumentException) { rejected = true; }
            assert(rejected, "Reject zero exchange");
            assert(ZeroContent.Weapons.Length == 20, "Twenty weapons");
            int[] weaponPrices = { 0,200,250,350,500,650,850,1050,1300,2000 };
            int totalWeaponCost = 0;
            for (int i = 0; i < weaponPrices.Length; i++)
            {
                assert(ZeroContent.Weapons[i].price == weaponPrices[i], "Gun price " + i);
                assert(ZeroContent.Weapons[10+i].price == weaponPrices[i], "Sword price " + i);
                totalWeaponCost += weaponPrices[i] * 2;
            }
            assert(totalWeaponCost == 14300, "Full gun and sword catalog price");
            assert(ZeroContent.BaseCoinValue(EnemyKind.Human) == 5 && ZeroContent.BaseCoinValue(EnemyKind.Giant) == 8 && ZeroContent.BaseCoinValue(EnemyKind.Medusa) == 20, "Low opening coin values by enemy tier");
            assert(ZeroContent.CoinMultiplier(1) == 1 && ZeroContent.CoinMultiplier(5) == 2 && ZeroContent.CoinMultiplier(17) == 5, "Coin value grows once per act");
            int[] minimumRawGold = { 0,60,60,60,240,160,160,160,480,360,360,360,720,480,480,480,960,800,800 };
            int baselineFortune = new ZeroStats()[3], economyWallet = ZeroContent.StartingGold;
            float baselineCoinRate = ZeroStats.CoinRate(baselineFortune);
            assert(economyWallet < weaponPrices[1], "Paid weapons require earned gold");
            assert(Mathf.RoundToInt(minimumRawGold[1] * baselineCoinRate) < weaponPrices[1], "First-map baseline income cannot buy any paid weapon");
            for (int map = 1; map <= 18; map++)
            {
                if (map == 18) assert(economyWallet + weaponPrices[1] + weaponPrices[2] < totalWeaponCost, "Full catalog is unavailable on the poorest route before map eighteen");
                economyWallet += Mathf.RoundToInt(minimumRawGold[map] * baselineCoinRate);
                if (map == 3) assert(economyWallet < weaponPrices[1], "Reduced income delays the first paid weapon through map three");
                if (map == 4) { assert(economyWallet >= weaponPrices[1], "First paid weapon is affordable after map four"); economyWallet -= weaponPrices[1]; }
                if (map == 5) { assert(economyWallet >= weaponPrices[2], "Second paid weapon is affordable after map five"); economyWallet -= weaponPrices[2]; }
            }
            assert(economyWallet + weaponPrices[1] + weaponPrices[2] < totalWeaponCost, "Reduced baseline income no longer buys the full catalog by map eighteen");
            int rawGoldThroughSix = 0; for (int map = 1; map <= 6; map++) rawGoldThroughSix += minimumRawGold[map];
            assert(Mathf.RoundToInt(rawGoldThroughSix*ZeroStats.CoinRate(baselineFortune+1)) < weaponPrices[9], "Reduced fortune scaling prevents an excessively early final-weapon rush");
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
            assert(ZeroContent.Weapons[10].power == 30 && ZeroContent.Weapons[10].rate == 2f, "Rusty sword is stronger and attacks twice per second");
            assert(ZeroContent.Weapons[11].rate == 8.4f && ZeroContent.Weapons[11].reach == 1.2f, "Dagger doubles its attack rate and shortens its reach");
            assert(ZeroContent.Weapons[12].power == 45 && ZeroContent.Weapons[12].rate == 2f, "Military sword trades speed for higher damage");
            assert(ZeroContent.Weapons[13].reach == 4f, "Rapier has extra-long thrust reach");
            assert(ZeroContent.Weapons[15].reach == 3.5f && ZeroContent.Weapons[16].reach == 4f, "Longsword and greatsword have extended reach");
            assert(ZeroContent.Weapons[17].rate == 10f && ZeroContent.Weapons[17].reach == 1.8f, "Curved sword is short and attacks ten times per second");
            int[] magazines = { 8,6,30,10,12,25,100,5,2,-1 };
            for (int i = 0; i < 10; i++)
            {
                var gun = ZeroContent.Weapons[i];
                assert(gun.magazine == magazines[i], "Gun magazine size " + gun.name);
                assert(i == 9 ? gun.reloadTime == 0 : gun.reloadTime > 0, "Gun reload tuning " + gun.name);
            }
            assert(ZeroContent.Weapons[6].reloadTime > ZeroContent.Weapons[7].reloadTime, "Heavy machine gun reload is the slowest");
            for (int i = 0; i < 10; i++) if (i != 6 && i != 7) assert(ZeroContent.Weapons[7].reloadTime > ZeroContent.Weapons[i].reloadTime, "Sniper reload is second slowest " + i);
            for (int i = 0; i < 9; i++) if (i != 8) assert(ZeroContent.Weapons[8].reloadTime < ZeroContent.Weapons[i].reloadTime, "Double-barrel reload is fastest " + i);
            assert(ZeroContent.Weapons[9].magazine < 0, "Electron cannon has an infinite magazine");
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
                    assert(offers.Length == (ZeroContent.IsBossRoom(map) ? 1 : 3), "Three regular offers and one boss offer");
                    var layouts = new System.Collections.Generic.HashSet<int>();
                    foreach (var offer in offers)
                    {
                        assert(offer.Total > 0 && layouts.Add(offer.layout), "Nonempty encounter and distinct terrain");
                        if (map <= 18) assert(ZeroContent.RawCoinReward(offer,map) >= minimumRawGold[map], "Map reward meets the economy floor " + map);
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
            stats = new ZeroStats(); stats.Exchange(0,1,5);
            assert(stats.HealingReceived(12)==18 && stats.HealingReceived(40)==46, "Negative defense amplifies healing consistently");
            assert(stats.HealingReceived(0)==0, "No free healing from zero");
            stats = new ZeroStats(); stats.Exchange(0,4,3);
            assert(stats.CriticalDamage(100)==40 && stats.CriticalDamage(100,true)==160, "Negative critical inversion");
            stats = new ZeroStats(); stats.Exchange(4,0,3);
            assert(stats.CriticalDamage(100)==160 && stats.CriticalDamage(100,true)==40, "Positive critical inversion");
            stats.Exchange(0,4,20);
            assert(stats.CriticalDamage(100)==10, "Critical multiplier has a damage floor");
            stats = new ZeroStats(); stats.Exchange(0,1,4); int attackBefore=stats[0], defenseBefore=stats[1]; stats.Swap(0,1);
            assert(stats[0]==defenseBefore && stats[1]==attackBefore && stats.Total==10,"Swap preserves zero-sum total");
            stats.Exchange(0,1,10); assert(stats.Total==10,"Extreme exchange preserves zero-sum total");
            assert(!ZeroContent.Enemies[(int)EnemyKind.RearGuard].Boss && !ZeroContent.Enemies[(int)EnemyKind.Inverter].Boss, "New inversion enemies are ordinary enemies");
            bool foundGuard=false, foundInverter=false;
            for(int seed=0;seed<50;seed++) foreach(var offer in ZeroContent.CreateOffers(6,new System.Random(seed)))
            { foundGuard |= offer.counts[(int)EnemyKind.RearGuard]>0; foundInverter |= offer.counts[(int)EnemyKind.Inverter]>0; }
            assert(foundGuard && foundInverter,"Both inversion enemies appear in route cards");
            Directory.CreateDirectory("Verification"); File.WriteAllText("Verification/rules-result.txt", "PASS: " + count + " rule assertions.");
            Debug.Log("ZERO_RULES_SUCCESS " + count);
        }
    }
}
