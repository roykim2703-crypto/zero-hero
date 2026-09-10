using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame : MonoBehaviour
    {
        enum Phase { Title, Camp, Route, Combat, RoomClear, Trade, Dead, Victory }
        enum ShotKind { Bullet, Arrow, Snake, Blood, Flame, Fly, Meteor, BlackIron }
        enum SwordMotion { None, Swing, Thrust, Overhead, Spin }
        sealed class Enemy
        {
            public Transform root;
            public SpriteRenderer body, health, guard;
            public readonly List<Transform> wings = new List<Transform>();
            public readonly List<SpriteRenderer> flies = new List<SpriteRenderer>();
            public EnemyKind kind;
            public Enemy summoner;
            public Vector2 pos, velocity, target;
            public float hp, maxHp, timer, windup, flash, flyTime, jumpTimer, strength = 1, heartBarrageTimer;
            public int pattern, nextPattern, heartSummons, heartAttacks;
            public bool casting, invulnerable;
            public float facing = 1, turnTimer;
            public EnemyDef Def => ZeroContent.Enemies[(int)kind];
            public int Damage => Mathf.Max(1,Mathf.RoundToInt(Def.damage*strength));
            public float Radius => Def.size * .43f;
            public bool Flying => kind == EnemyKind.Angel || kind == EnemyKind.Beelzebub || kind == EnemyKind.Heart;
        }
        sealed class Shot
        {
            public Transform root;
            public Vector2 pos, velocity;
            public float life, gravity;
            public int damage, pierce;
            public bool hostile, critical;
            public ShotKind kind;
            public readonly HashSet<Enemy> hit = new HashSet<Enemy>();
        }
        sealed class Hazard
        {
            public SpriteRenderer sprite;
            public Vector2 pos, size, direction;
            public float warning, life;
            public int damage;
            public bool beam, stone, blood;
            public Color color;
        }
        sealed class PlatformLedge
        {
            public Rect rect;
            public readonly List<SpriteRenderer> visuals = new List<SpriteRenderer>();
            public float collapse = -1, respawn;
            public bool active = true;
        }
        sealed class Pickup { public Transform root; public Vector2 pos; public int value; public float age; }
        sealed class Effect { public SpriteRenderer sprite; public Vector2 velocity; public float life, maxLife, scale, grow; }
        sealed class Floating { public Vector2 pos; public string text; public Color color; public float life; }

        const int LastRoom = ZeroContent.TotalRooms;
        const float Left = -14.4f, Right = 14.4f, Floor = -4.8f, HeroHalf = .67f;
        const float RoomClearRevealDelay = .6f, RoomClearHoldDuration = 2.5f;
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly List<Pickup> coins = new List<Pickup>();
        readonly List<Effect> effects = new List<Effect>();
        readonly List<Floating> floating = new List<Floating>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly List<PlatformLedge> platforms = new List<PlatformLedge>();
        readonly Queue<EnemyKind> spawnQueue = new Queue<EnemyKind>();
        readonly HashSet<int> owned = new HashSet<int>();
        readonly ZeroTrade[] trades = new ZeroTrade[3];
        readonly int[] metaLevels = new int[ZeroStats.Count];
        readonly int[] runStatBonuses = new int[ZeroStats.Count];
        readonly int[] bagItems = { -1, -1, -1 };
        readonly int[] gunAmmo = new int[10];
        readonly float[] gunReload = new float[10];
        readonly int[] swordCombo = new int[10];
        ZeroStats stats = new ZeroStats();
        MapOffer[] routes;
        MapOffer currentMap;
        ZeroHeroArt art;
        Material spriteMaterial;
        Camera cam;
        Transform world, scenery, heroRoot, aimRoot;
        SpriteRenderer heroBody, heroShadow, cursorRing, heldWeapon;
        AudioSource audioSource;
        Phase phase = Phase.Title;
        Vector2 player, velocity, aim = Vector2.right;
        System.Random random;
        float hp = 100, attackTimer, dashTimer, dashTime, dashDirection, invulnerable, bombTimer, roomTime, runTime, visualTime, shake;
        float noticeTime, roomBanner, spawnTimer, petrified, dropTimer, roomClearTime, swordAnimTime, swordAnimDuration;
        string notice = "", bossNotice = "";
        int room = 1, completedRooms, wallet, kills, seed, best, selected = -1, routeSelected = -1, jumps;
        int lastUp = -1, lastDown = -1, lastAmount, shields, activeWeapon, gunSlot, swordSlot = 10, shopTab;
        int activeInvert = -1, pendingInvert = -1, legacyPoints, totalRuns, totalDeaths, totalVictories, lifetimeKills, lifetimeGold, deathReward;
        SwordMotion swordMotion;
        bool pendingTrade;
        bool paused, muted, help, campHealed, campShield, smokeMode, grounded, roomClearRevealed, shopOpen;
        WeaponDef Weapon => ZeroContent.Weapons[activeWeapon];
        int Act => (room - 1) / 4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string name = SceneManager.GetActiveScene().name;
            if ((name == "SampleScene" || name == "ZeroHero") && FindAnyObjectByType<ZeroHeroGame>() == null)
                new GameObject("Zero Hero").AddComponent<ZeroHeroGame>();
        }

        void Awake()
        {
            Application.targetFrameRate = 120;
            art = new ZeroHeroArt(); random = new System.Random(120);
            spriteMaterial = Resources.Load<Material>("ZeroSprite");
            if (spriteMaterial == null) spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.orthographic = true; cam.orthographicSize = 9; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = C("141613");
            var cameraData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.renderPostProcessing = false;
            cam.transform.position = new Vector3(0, 0, -10);
            world = new GameObject("Combat").transform;
            heroRoot = new GameObject("Hero").transform; heroRoot.SetParent(world);
            heroShadow = Sprite("Shadow", art.disc, new Color(0,0,0,.3f), Vector2.zero, new Vector2(.9f,.15f), 10, heroRoot);
            heroBody = Sprite("Hero", art.hero, Color.white, Vector2.zero, Vector2.one * 1.2f, 20, heroRoot);
            aimRoot = new GameObject("Held weapon").transform; aimRoot.SetParent(heroRoot, false);
            heldWeapon = Sprite("Weapon", art.weaponGun, Color.white, new Vector2(.53f,0), Vector2.one * .85f, 23, aimRoot);
            cursorRing = Sprite("Reticle", art.ring, ZeroHeroArt.Gold, Vector2.zero, Vector2.one * .3f, 65);
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .3f; MakeAudio();
            LoadRecords(); muted = PlayerPrefs.GetInt("ZeroHero.Muted",0) == 1;
            currentMap = ZeroContent.CreateOffers(1,random)[0]; BuildMap(); player = new Vector2(-10,Floor + HeroHalf);
            smokeMode = Array.IndexOf(Environment.GetCommandLineArgs(),"--zero-smoke") >= 0;
            if (smokeMode) StartCoroutine(SmokeTest());
        }

        static Color C(string hex) => ZeroHeroArt.Hex(hex);
        float Range(float min, float max) => min + (float)random.NextDouble() * (max-min);
        float StatValue(int index)
        {
            float value = stats[index] + runStatBonuses[index] + metaLevels[index] * .1f;
            return activeInvert == index ? -value : value;
        }
        int CurrentDamage => Mathf.RoundToInt(Weapon.power * StatValue(0) / 3f);
        int CurrentDamageTaken(int incoming) => Mathf.Max(1,Mathf.RoundToInt(incoming-StatValue(1)*2));
        int CurrentHealing(int amount) => amount <= 0 ? 0 : Mathf.Max(0,Mathf.RoundToInt(amount+Mathf.Max(0,-StatValue(1))*2));
        float CurrentMoveSpeed => StatValue(2)*1.6f;
        float ReloadRate => StatValue(2) < 0 ? 0 : Mathf.Clamp(Mathf.Max(1,StatValue(2))/3f,1/3f,2.22f);
        float ReloadSecondsLeft(int id) => gunReload[id] <= 0 ? 0 : ReloadRate <= 0 ? float.PositiveInfinity : gunReload[id]/ReloadRate;
        int CurrentCriticalDamage(int damage,bool inverted)
        { return Mathf.RoundToInt(damage*Mathf.Max(.1f,1+StatValue(4)*(inverted?-.2f:.2f))); }
        SpriteRenderer Sprite(string label, Sprite sprite, Color color, Vector2 pos, Vector2 scale, int order, Transform parent = null)
        {
            var go = new GameObject(label); go.transform.SetParent(parent == null ? world : parent,false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order; sr.sharedMaterial = spriteMaterial; return sr;
        }

        void StartRun()
        {
            ClearActors(); stats = new ZeroStats(); hp = 100; wallet = ZeroContent.StartingGold; kills = completedRooms = 0; room = 1; shields = 0;
            seed = Environment.TickCount & int.MaxValue; random = new System.Random(seed); runTime = 0;
            owned.Clear(); owned.Add(0); owned.Add(10); gunSlot = 0; swordSlot = activeWeapon = 10; shopTab = 1;
            Array.Clear(runStatBonuses,0,runStatBonuses.Length); for (int i = 0; i < bagItems.Length; i++) bagItems[i] = -1;
            for (int i = 0; i < gunAmmo.Length; i++) { gunAmmo[i] = ZeroContent.Weapons[i].magazine; gunReload[i] = 0; }
            Array.Clear(swordCombo,0,swordCombo.Length); swordAnimTime = swordAnimDuration = 0; swordMotion = SwordMotion.None;
            pendingTrade = false; routes = null; activeInvert = pendingInvert = -1; deathReward = 0;
            if (!smokeMode) { totalRuns++; SaveRecords(); }
            lastUp = lastDown = -1; paused = help = campHealed = campShield = shopOpen = false; phase = Phase.Camp;
            currentMap = ZeroContent.CreateOffers(1,random)[0]; BuildMap(); player = new Vector2(-10,Floor + HeroHalf); velocity = Vector2.zero;
            petrified = dropTimer = attackTimer = dashTimer = dashTime = bombTimer = invulnerable = 0;
            selected = routeSelected = -1; jumps = 0; grounded = true; aim = Vector2.right; Notice(""); PlaySound(4);
        }

        void NextRoom()
        {
            if (phase != Phase.Camp || shopOpen) return;
            room = completedRooms + 1; routes = ZeroContent.CreateOffers(room,random); routeSelected = -1; phase = Phase.Route;
        }
        void ChooseRoute()
        {
            if (phase != Phase.Route || shopOpen || routes == null || routeSelected < 0 || routeSelected >= routes.Length) return;
            currentMap = routes[routeSelected];
            if (pendingTrade) { PrepareTrade(); phase = Phase.Trade; } else BeginRoom();
        }
        void BeginRoom()
        {
            shopOpen = false; ClearActors(); BuildMap(); phase = Phase.Combat; player = new Vector2(-10,Floor+HeroHalf); velocity = Vector2.zero;
            jumps = 0; grounded = true; aim = Vector2.right; petrified = dropTimer = 0;
            activeInvert = pendingInvert; pendingInvert = -1;
            attackTimer = dashTimer = dashTime = bombTimer = swordAnimTime = 0; swordMotion = SwordMotion.None; invulnerable = 1.3f; roomTime = 0; roomBanner = 2;
            bossNotice = ""; spawnTimer = .5f; roomClearTime = 0; roomClearRevealed = false;
            for (int k = 0; k < currentMap.counts.Length; k++) for (int n = 0; n < currentMap.counts[k]; n++)
            {
                if (ZeroContent.Enemies[k].Boss) SpawnEnemy((EnemyKind)k,new Vector2(8,0));
                else spawnQueue.Enqueue((EnemyKind)k);
            }
            Notice(room == 1 ? "WASD 공격 방향 · A/D 이동 · Space 2단 점프 · 클릭 공격" : activeInvert >= 0 ? ZeroStats.Name(activeInvert)+" ×−1 적용 중" : "");
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime,.035f); visualTime += dt;
            cam.orthographicSize = Mathf.Max(9,16 / Mathf.Max(.4f,cam.aspect));
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.mKey.wasPressedThisFrame) ToggleSound();
                if (kb.f1Key.wasPressedThisFrame) { help = !help; if (phase == Phase.Combat) paused = help; }
                if (kb.escapeKey.wasPressedThisFrame) { if (help) { help = false; paused = false; } else if (shopOpen) shopOpen=false; else if (phase == Phase.Combat) paused = !paused; }
                if (!help && !paused && !shopOpen)
                {
                    if ((phase == Phase.Title || phase == Phase.Dead || phase == Phase.Victory) && kb.enterKey.wasPressedThisFrame) StartRun();
                    else if (phase == Phase.Camp && kb.enterKey.wasPressedThisFrame) NextRoom();
                    else if (phase == Phase.Route)
                    {
                        if (kb.digit1Key.wasPressedThisFrame && routes != null && routes.Length > 0) routeSelected = 0;
                        if (kb.digit2Key.wasPressedThisFrame && routes != null && routes.Length > 1) routeSelected = 1;
                        if (kb.digit3Key.wasPressedThisFrame && routes != null && routes.Length > 2) routeSelected = 2;
                        if (kb.enterKey.wasPressedThisFrame) ChooseRoute();
                    }
                    else if (phase == Phase.Trade)
                    {
                        if (kb.digit1Key.wasPressedThisFrame) selected = 0;
                        if (kb.digit2Key.wasPressedThisFrame) selected = 1;
                        if (kb.digit3Key.wasPressedThisFrame) selected = 2;
                        if (kb.enterKey.wasPressedThisFrame) ApplyTrade();
                    }
                }
            }
            if (!paused && !help)
            {
                TickEffects(dt); noticeTime -= dt; roomBanner -= dt;
                if (phase == Phase.Combat) TickCombat(dt);
                else if (phase == Phase.RoomClear) TickRoomClear(dt);
            }
            shake = Mathf.MoveTowards(shake,0,dt*2);
            cam.transform.position = new Vector3(Mathf.Sin(visualTime*117)*shake,Mathf.Cos(visualTime*133)*shake,-10);
            SetHeroVisual();
        }

        void TickCombat(float dt)
        {
            runTime += dt; roomTime += dt; invulnerable -= dt; attackTimer -= dt; dashTimer -= dt; bombTimer -= dt; petrified -= dt; dropTimer -= dt; swordAnimTime -= dt;
            TickPlatforms(dt);
            TickReloads(dt);
            var kb = Keyboard.current; var mouse = Mouse.current;
            float input = kb == null ? 0 : (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Equip(gunSlot);
                if (kb.digit2Key.wasPressedThisFrame) Equip(swordSlot);
                if (kb.tabKey.wasPressedThisFrame) Equip(Weapon.gun ? swordSlot : gunSlot);
                Vector2 attackInput = new Vector2(input,(kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0)-(kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0));
                if (attackInput.sqrMagnitude > .01f) aim = attackInput.normalized;
            }
            cursorRing.enabled = false;
            if (petrified <= 0)
            {
                if (kb != null && kb.spaceKey.wasPressedThisFrame) TryJump(kb.sKey.isPressed || kb.downArrowKey.isPressed);
                if (kb != null && kb.rKey.wasPressedThisFrame) BeginReload(activeWeapon,true);
                if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) && dashTimer <= 0)
                { dashDirection = (input != 0 ? input : Mathf.Sign(aim.x)) * (StatValue(2) < 0 ? -1 : 1); dashTime = .16f; dashTimer = 1.35f; invulnerable = .28f; PlaySound(3); }
                velocity.x = dashTime > 0 ? dashDirection * 18 : input * CurrentMoveSpeed;
                if (attackTimer <= 0 && ((mouse != null && mouse.leftButton.isPressed) || (kb != null && kb.jKey.isPressed))) Fire();
                if (kb != null && kb.eKey.wasPressedThisFrame && bombTimer <= 0) Bomb();
            }
            else velocity.x = 0;
            if (dashTime > 0) { dashTime -= dt; AddEffect(art.hero,player,ZeroHeroArt.Gain,.18f,1.2f,Vector2.zero,0); }
            grounded = MoveBody(ref player,ref velocity,HeroHalf,.36f,dt,dropTimer > 0,true);
            if (grounded) jumps = 0;
            TickEnemies(dt); if (phase != Phase.Combat) return;
            TickShots(dt); TickHazards(dt); if (phase != Phase.Combat) return;
            TickCoins(dt); spawnTimer -= dt;
            if (spawnQueue.Count > 0 && spawnTimer <= 0)
            {
                EnemyKind kind = spawnQueue.Peek(); int group = kind == EnemyKind.Orc ? 4 : kind == EnemyKind.Goblin ? 6 : 1;
                float side = random.Next(2) == 0 ? -1 : 1;
                for (int i = 0; i < group && spawnQueue.Count > 0 && spawnQueue.Peek() == kind; i++)
                { spawnQueue.Dequeue(); SpawnEnemy(kind,new Vector2(side * (13 - i*.7f),Floor+1)); }
                spawnTimer = group > 1 ? 3.2f : 1.4f;
            }
            if (spawnQueue.Count == 0 && enemies.Count == 0) BeginRoomClear();
        }

        void TryJump(bool drop)
        {
            if (petrified > 0) return;
            if (drop && player.y-HeroHalf > Floor+.15f) { dropTimer = .24f; velocity.y = -3; grounded = false; return; }
            if (jumps >= 2) return;
            velocity.y = 11.8f; jumps++; grounded = false; PlaySound(3);
        }
        bool MoveBody(ref Vector2 pos, ref Vector2 vel, float halfHeight, float radius, float dt, bool drop = false, bool playerControlled = false)
        {
            float oldFeet = pos.y-halfHeight; vel.y -= 28*dt;
            pos.x = Mathf.Clamp(pos.x+vel.x*dt,Left+radius,Right-radius); pos.y += vel.y*dt;
            bool landed = false;
            if (vel.y <= 0 && !drop)
                foreach (var ledge in platforms) if (ledge.active && pos.x+radius > ledge.rect.xMin && pos.x-radius < ledge.rect.xMax && oldFeet >= ledge.rect.yMax-.025f && pos.y-halfHeight <= ledge.rect.yMax)
                { pos.y = ledge.rect.yMax+halfHeight; vel.y = 0; landed = true; if(playerControlled && ledge.collapse < 0) ledge.collapse = .7f; break; }
            if (pos.y-halfHeight <= Floor) { pos.y = Floor+halfHeight; vel.y = 0; landed = true; }
            if (pos.y > 5.7f-halfHeight) { pos.y = 5.7f-halfHeight; vel.y = Mathf.Min(0,vel.y); }
            return landed;
        }

        void Equip(int id)
        {
            if (!owned.Contains(id)) return;
            activeWeapon = id; if (Weapon.gun) gunSlot = id; else swordSlot = id;
        }
        bool BuyWeapon(int id)
        {
            if (!ShopAccessible || id < 0 || id >= ZeroContent.Weapons.Length) return false;
            var w = ZeroContent.Weapons[id];
            if (!owned.Contains(id))
            {
                if (wallet < w.price) return false; wallet -= w.price; owned.Add(id); if (w.gun) { gunAmmo[id] = w.magazine; gunReload[id] = 0; }
            }
            Equip(id); PlaySound(6); return true;
        }
        bool BuySupply(bool shield)
        {
            if (!ShopAccessible) return false;
            int price = shield ? 350 : 500;
            if (wallet < price || (shield ? campShield : campHealed || hp >= 100)) return false;
            wallet -= price;
            if (shield) { shields++; campShield = true; } else { HealPlayer(40); campHealed = true; }
            PlaySound(4); return true;
        }

        bool ShopAccessible => phase == Phase.Camp || shopOpen && (phase == Phase.Route || phase == Phase.Trade);
        const int StatItemPrice = 250;
        bool BuyStatItem(int stat)
        {
            if (!ShopAccessible || stat < 0 || stat >= ZeroStats.Count || wallet < StatItemPrice) return false;
            int slot = Array.IndexOf(bagItems,-1); if (slot < 0) return false;
            wallet -= StatItemPrice; bagItems[slot] = stat; PlaySound(6); return true;
        }
        bool UseBagItem(int slot)
        {
            if (slot < 0 || slot >= bagItems.Length || bagItems[slot] < 0 || phase == Phase.Title || phase == Phase.Dead || phase == Phase.Victory) return false;
            int stat = bagItems[slot]; bagItems[slot] = -1; runStatBonuses[stat]++; Notice(ZeroStats.Name(stat)+" +1"); PlaySound(4); return true;
        }

        void Fire()
        {
            if (phase != Phase.Combat || petrified > 0 || attackTimer > 0) return;
            if (Weapon.gun && Weapon.magazine > 0)
            {
                if (gunReload[activeWeapon] > 0) return;
                if (gunAmmo[activeWeapon] <= 0) { BeginReload(activeWeapon,true); attackTimer = .15f; return; }
            }
            attackTimer = 1/Weapon.rate; int damage = CurrentDamage;
            bool critical = random.NextDouble() < .2;
            if (Weapon.gun)
            {
                for (int i = 0; i < Weapon.pellets; i++)
                {
                    float a = Mathf.Atan2(aim.y,aim.x) + (i-(Weapon.pellets-1)*.5f)*.095f;
                    var direction = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    CreateShot(player+direction*.65f,direction*Weapon.bulletSpeed,damage,false,ShotKind.Bullet,Weapon.pierce,0,critical);
                }
                if (Weapon.magazine > 0)
                {
                    gunAmmo[activeWeapon]--;
                    if (gunAmmo[activeWeapon] == 0) BeginReload(activeWeapon,false);
                }
            }
            else FireSword(damage,critical);
            PlaySound(0);
        }
        bool BeginReload(int id, bool showBlocked)
        {
            if (id < 0 || id >= gunAmmo.Length) return false;
            var weapon = ZeroContent.Weapons[id];
            if (!weapon.gun || weapon.magazine < 0 || gunAmmo[id] >= weapon.magazine || gunReload[id] > 0) return false;
            if (StatValue(2) < 0)
            {
                if (showBlocked) Notice("이동 속도가 음수라 장전할 수 없습니다.");
                return false;
            }
            gunReload[id] = weapon.reloadTime;
            return true;
        }
        void TickReloads(float dt)
        {
            float rate = ReloadRate;
            if (rate <= 0) return;
            for (int id = 0; id < gunAmmo.Length; id++)
            {
                var weapon = ZeroContent.Weapons[id];
                if (!owned.Contains(id) || weapon.magazine < 0) continue;
                if (gunAmmo[id] <= 0 && gunReload[id] <= 0) gunReload[id] = weapon.reloadTime;
                if (gunReload[id] <= 0) continue;
                gunReload[id] -= dt*rate;
                if (gunReload[id] <= 0) { gunReload[id] = 0; gunAmmo[id] = weapon.magazine; }
            }
        }
        void Bomb()
        {
            if (phase != Phase.Combat || bombTimer > 0 || petrified > 0) return;
            bombTimer = 6; shake = .2f; PlaySound(2);
            AddEffect(art.ring,player,ZeroHeroArt.Gold,.6f,.7f,Vector2.zero,14); Burst(player,ZeroHeroArt.Gold,24);
            for (int i = enemies.Count-1; i >= 0; i--) if (Vector2.Distance(player,enemies[i].pos) < 4.8f+enemies[i].Radius) HitEnemy(enemies[i],42);
            for (int i = shots.Count-1; i >= 0; i--) if (shots[i].hostile && Vector2.Distance(player,shots[i].pos) < 5) RemoveShot(i);
        }

        void HitEnemy(Enemy e, int signedDamage, bool critical = false, Vector2? direction = null)
        {
            if (e.invulnerable)
            { Float(e.pos+Vector2.up*e.Radius,"무적",C("D56A78")); AddEffect(art.ring,e.pos,C("8E263B"),.18f,e.Radius*.35f,Vector2.zero,2); return; }
            if (e.kind == EnemyKind.RearGuard && direction.HasValue && direction.Value.x * e.facing <= 0)
            { Float(e.pos+Vector2.up,"방어",ZeroHeroArt.Gold); return; }
            if (critical) signedDamage = CurrentCriticalDamage(signedDamage,e.kind == EnemyKind.Inverter);
            int damage = ZeroContent.ResolveDamage(e.kind,signedDamage);
            float nextHp = ZeroStats.ApplyAttack(e.hp,e.maxHp,damage);
            float heartFloor = HeartDamageFloor(e);
            bool heartWave = damage > 0 && heartFloor > 0 && nextHp <= heartFloor;
            e.hp = heartWave ? heartFloor : nextHp; e.flash = .12f;
            Float(e.pos+Vector2.up*e.Radius,(critical ? "치명타 " : "") + (damage < 0 ? "+"+-damage : damage.ToString()),damage < 0 ? ZeroHeroArt.Gain : e.kind == EnemyKind.Undead && signedDamage < 0 ? ZeroHeroArt.Gold : Color.white);
            Burst(e.pos,damage < 0 ? ZeroHeroArt.Gain : ZeroHeroArt.Pink,4); PlaySound(1);
            if (heartWave) { StartHeartWave(e); return; }
            if (e.hp > 0) return;
            kills++; lifetimeKills++; Burst(e.pos,e.Def.Boss ? ZeroHeroArt.Gold : ZeroHeroArt.Pink,18);
            int number = ZeroContent.CoinDropCount(e.kind);
            for (int i = 0; i < number; i++)
            {
                Vector2 p = e.pos+new Vector2(Range(-.5f,.5f),Range(-.2f,.4f));
                var sr = Sprite("Coin",art.coin,Color.white,p,Vector2.one*.7f,15);
                coins.Add(new Pickup { root = sr.transform,pos = p,value = ZeroContent.CoinValue(e.kind,room) });
            }
            Destroy(e.root.gameObject); enemies.Remove(e);
        }
        bool Hurt(int incoming)
        {
            if (invulnerable > 0 || phase != Phase.Combat) return false;
            invulnerable = .75f; shake = .15f;
            if (shields > 0) { shields--; Float(player+Vector2.up,"보호막",ZeroHeroArt.Gain); PlaySound(3); return false; }
            int damage = CurrentDamageTaken(incoming); hp = Mathf.Max(0,hp-damage); Float(player+Vector2.up,"−"+damage,ZeroHeroArt.Pink); PlaySound(5);
            if (hp <= 0) { phase = Phase.Dead; paused = false; RecordDeath(); cursorRing.enabled = false; }
            return true;
        }

        void TickCoins(float dt)
        {
            for (int i = coins.Count-1; i >= 0; i--)
            {
                var p = coins[i]; p.age += dt;
                if (Vector2.Distance(p.pos,player) < 2.2f && p.age > .15f) p.pos = Vector2.MoveTowards(p.pos,player,9*dt);
                else p.pos.y = Mathf.Max(Floor+.18f,p.pos.y-dt*2.5f);
                p.root.position = p.pos;
                if (Vector2.Distance(p.pos,player) < .65f) { Collect(p); Destroy(p.root.gameObject); coins.RemoveAt(i); }
            }
        }
        void Collect(Pickup p)
        {
            int before = wallet, change = ZeroStats.CoinChange(p.value,StatValue(3)); wallet = Mathf.Max(0,wallet+change);
            if (wallet > before) lifetimeGold += wallet-before;
            Float(p.pos,Signed(wallet-before)+" G",StatValue(3)<0 ? ZeroHeroArt.Pink : ZeroHeroArt.Gold); PlaySound(6);
        }

        void HealPlayer(int amount)
        {
            float before = hp; hp = Mathf.Min(100,hp+CurrentHealing(amount));
            if (hp > before) Float(player+Vector2.up,"회복 +"+(hp-before).ToString("0"),ZeroHeroArt.Gain);
        }

        void BeginRoomClear()
        {
            if (phase != Phase.Combat) return;
            phase = Phase.RoomClear; roomClearTime = 0; roomClearRevealed = false;
            velocity = Vector2.zero; petrified = 0; cursorRing.enabled = false; ClearProjectiles();
        }
        void TickRoomClear(float dt)
        {
            roomClearTime += dt;
            var kb = Keyboard.current;
            float input = kb == null ? 0 : (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            TickRoomClearMovement(dt,input,kb != null && kb.spaceKey.wasPressedThisFrame,kb != null && (kb.sKey.isPressed || kb.downArrowKey.isPressed),kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame));
            if (!roomClearRevealed && roomClearTime >= RoomClearRevealDelay)
            { roomClearRevealed = true; PlaySound(4); }
            if (roomClearTime >= RoomClearRevealDelay + RoomClearHoldDuration) ClearRoom();
        }
        void TickRoomClearMovement(float dt,float input,bool jumpPressed,bool dropHeld,bool dashPressed)
        {
            dashTimer -= dt; dropTimer -= dt; TickPlatforms(dt);
            if (jumpPressed) TryJump(dropHeld);
            if (dashPressed && dashTimer <= 0)
            { dashDirection = (input != 0 ? input : Mathf.Sign(aim.x)) * (StatValue(2) < 0 ? -1 : 1); dashTime = .16f; dashTimer = 1.35f; PlaySound(3); }
            velocity.x = dashTime > 0 ? dashDirection*18 : input*CurrentMoveSpeed;
            if (dashTime > 0) { dashTime -= dt; AddEffect(art.hero,player,ZeroHeroArt.Gain,.18f,1.2f,Vector2.zero,0); }
            grounded = MoveBody(ref player,ref velocity,HeroHalf,.36f,dt,dropTimer > 0,true);
            if (grounded) jumps = 0;
            TickCoins(dt);
        }
        void ClearRoom()
        {
            if (phase != Phase.Combat && phase != Phase.RoomClear) return;
            foreach (var p in coins) { Collect(p); Destroy(p.root.gameObject); } coins.Clear();
            ClearProjectiles(); completedRooms = room; activeInvert = -1; SaveBest(completedRooms); HealPlayer(12);
            pendingTrade = true; campHealed = campShield = false; selected = -1; cursorRing.enabled = false;
            if (completedRooms == LastRoom) { PrepareTrade(); phase = Phase.Trade; }
            else phase = Phase.Camp;
        }
        void PrepareTrade()
        {
            selected = -1;
            int up = random.Next(ZeroStats.Count), down;
            do down = random.Next(ZeroStats.Count); while (up == down);
            trades[0] = new ZeroTrade(up,down,2+random.Next(3));
            up = random.Next(ZeroStats.Count); do down = random.Next(ZeroStats.Count); while (up == down);
            trades[1] = random.Next(2)==0 ? new ZeroTrade(up,down,0,TradeKind.Swap) : new ZeroTrade(up,down,6+random.Next(5),TradeKind.Extreme);
            if (completedRooms < LastRoom) trades[2] = new ZeroTrade(random.Next(ZeroStats.Count),-1,0,TradeKind.InvertNext);
            else trades[2] = new ZeroTrade(up,down,8,TradeKind.Extreme);
        }
        void ApplyTrade()
        {
            if (phase != Phase.Trade || shopOpen || selected < 0 || selected >= 3) return;
            var t = trades[selected];
            if (t.kind == TradeKind.Swap) stats.Swap(t.up,t.down);
            else if (t.kind == TradeKind.InvertNext) pendingInvert = t.up;
            else stats.Exchange(t.up,t.down,t.amount);
            lastUp = t.up; lastDown = t.down; lastAmount = t.amount;
            pendingTrade = false; petrified = 0; PlaySound(4);
            if (completedRooms == LastRoom)
            { phase = Phase.Victory; if (!smokeMode) { totalVictories++; SaveRecords(); } }
            else BeginRoom();
        }
        void ReturnToTitle() { ClearActors(); SaveRecords(); phase = Phase.Title; paused = help = shopOpen = false; }
        void SaveBest(int n) { if (smokeMode) return; best = Mathf.Max(best,n); SaveRecords(); }
        void LoadRecords()
        {
            best=PlayerPrefs.GetInt("ZeroHero.Sideview.Best",0); legacyPoints=PlayerPrefs.GetInt("ZeroHero.Legacy.Points",0);
            totalRuns=PlayerPrefs.GetInt("ZeroHero.Record.Runs",0); totalDeaths=PlayerPrefs.GetInt("ZeroHero.Record.Deaths",0);
            totalVictories=PlayerPrefs.GetInt("ZeroHero.Record.Victories",0); lifetimeKills=PlayerPrefs.GetInt("ZeroHero.Record.Kills",0); lifetimeGold=PlayerPrefs.GetInt("ZeroHero.Record.Gold",0);
            for(int i=0;i<ZeroStats.Count;i++) metaLevels[i]=PlayerPrefs.GetInt("ZeroHero.Legacy.Stat."+i,0);
        }
        void SaveRecords()
        {
            if(smokeMode) return;
            PlayerPrefs.SetInt("ZeroHero.Sideview.Best",best); PlayerPrefs.SetInt("ZeroHero.Legacy.Points",legacyPoints);
            PlayerPrefs.SetInt("ZeroHero.Record.Runs",totalRuns); PlayerPrefs.SetInt("ZeroHero.Record.Deaths",totalDeaths);
            PlayerPrefs.SetInt("ZeroHero.Record.Victories",totalVictories); PlayerPrefs.SetInt("ZeroHero.Record.Kills",lifetimeKills); PlayerPrefs.SetInt("ZeroHero.Record.Gold",lifetimeGold);
            for(int i=0;i<ZeroStats.Count;i++) PlayerPrefs.SetInt("ZeroHero.Legacy.Stat."+i,metaLevels[i]);
            PlayerPrefs.Save();
        }
        void RecordDeath()
        {
            SaveBest(completedRooms); deathReward=1+completedRooms/4; legacyPoints+=deathReward; totalDeaths++; SaveRecords();
        }
        bool UpgradeMeta(int index)
        {
            if(phase!=Phase.Dead || index<0 || index>=ZeroStats.Count || legacyPoints<=0) return false;
            legacyPoints--; metaLevels[index]++; SaveRecords(); return true;
        }
        void ToggleSound() { muted = !muted; PlayerPrefs.SetInt("ZeroHero.Muted",muted?1:0); PlayerPrefs.Save(); }
        void Notice(string text) { notice = text; noticeTime = 4; }
        void Float(Vector2 pos, string text, Color color) { floating.Add(new Floating { pos=pos,text=text,color=color,life=1 }); }

        void SetHeroVisual()
        {
            heroRoot.gameObject.SetActive(phase != Phase.Title); heroRoot.position = player;
            heroBody.flipX = aim.x < 0;
            heroBody.color = petrified > 0 ? C("7E8583") : phase == Phase.Dead ? C("655E55") : invulnerable > 0 && Mathf.Sin(visualTime*40)>0 ? new Color(1,1,1,.45f) : Color.white;
            heroBody.transform.localPosition = Vector2.up*(grounded && Mathf.Abs(velocity.x)>.1f ? Mathf.Sin(visualTime*16)*.035f : 0);
            heroShadow.enabled = grounded; heroShadow.transform.localPosition = Vector2.down*HeroHalf;
            heldWeapon.sprite = Weapon.gun ? art.weaponGun : art.weaponBlade;
            bool animatingSword = swordAnimTime > 0 && !Weapon.gun;
            float attackProgress = animatingSword ? 1-Mathf.Clamp01(swordAnimTime/Mathf.Max(.001f,swordAnimDuration)) : 0;
            float weaponAngle = Weapon.gun ? 0 : !animatingSword ? -45 : swordMotion == SwordMotion.Thrust ? 0 : swordMotion == SwordMotion.Overhead ? Mathf.Lerp(95,-80,attackProgress) : -45;
            float spinAngle = animatingSword && swordMotion == SwordMotion.Spin ? attackProgress*360 : animatingSword && swordMotion == SwordMotion.Swing ? Mathf.Lerp(-65,70,attackProgress) : 0;
            float thrustOffset = animatingSword && swordMotion == SwordMotion.Thrust ? Mathf.Sin(attackProgress*Mathf.PI)*.55f : 0;
            heldWeapon.transform.localPosition = new Vector2(.55f+thrustOffset,0);
            heldWeapon.transform.localRotation = Quaternion.Euler(0,0,weaponAngle);
            aimRoot.rotation = Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg+spinAngle);
            if (phase != Phase.Combat || paused || help) cursorRing.enabled = false;
        }
        void ClearProjectiles()
        { foreach (var s in shots) Destroy(s.root.gameObject); shots.Clear(); foreach (var h in hazards) Destroy(h.sprite.gameObject); hazards.Clear(); }
        void ClearActors()
        {
            foreach (var e in enemies) Destroy(e.root.gameObject); enemies.Clear(); ClearProjectiles(); spawnQueue.Clear();
            foreach (var p in coins) Destroy(p.root.gameObject); coins.Clear(); foreach (var e in effects) Destroy(e.sprite.gameObject); effects.Clear(); floating.Clear();
        }
        void OnApplicationFocus(bool focus) { if (!focus && phase == Phase.Combat && !smokeMode) paused = true; }
        void OnApplicationQuit() { SaveRecords(); }
        void OnDestroy()
        {
            ClearActors(); if (world != null) Destroy(world.gameObject); if (scenery != null) Destroy(scenery.gameObject); art?.Dispose();
            foreach (var c in clips) Destroy(c); if (uiFont != null) Destroy(uiFont);
            if (spriteMaterial != null && spriteMaterial != Resources.Load<Material>("ZeroSprite")) Destroy(spriteMaterial);
        }
    }
}
