using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame : MonoBehaviour
    {
        enum Phase { Title, Camp, Route, Combat, Trade, Dead, Victory }
        enum ShotKind { Bullet, Arrow, Snake, Blood, Flame, Fly, Meteor }
        sealed class Enemy
        {
            public Transform root;
            public SpriteRenderer body, health;
            public readonly List<Transform> wings = new List<Transform>();
            public readonly List<SpriteRenderer> flies = new List<SpriteRenderer>();
            public EnemyKind kind;
            public Vector2 pos, velocity, target;
            public float hp, maxHp, timer, windup, flash, flyTime, jumpTimer;
            public int pattern, nextPattern;
            public bool casting;
            public EnemyDef Def => ZeroContent.Enemies[(int)kind];
            public float Radius => Def.size * .43f;
            public bool Flying => kind == EnemyKind.Angel || kind == EnemyKind.Beelzebub || kind == EnemyKind.Heart;
        }
        sealed class Shot
        {
            public Transform root;
            public Vector2 pos, velocity;
            public float life, gravity;
            public int damage, pierce;
            public bool hostile;
            public ShotKind kind;
            public readonly HashSet<Enemy> hit = new HashSet<Enemy>();
        }
        sealed class Hazard
        {
            public SpriteRenderer sprite;
            public Vector2 pos, size, direction;
            public float warning, life;
            public int damage;
            public bool beam, stone;
            public Color color;
        }
        sealed class Pickup { public Transform root; public Vector2 pos; public int value; public float age; }
        sealed class Effect { public SpriteRenderer sprite; public Vector2 velocity; public float life, maxLife, scale, grow; }
        sealed class Floating { public Vector2 pos; public string text; public Color color; public float life; }

        const int LastRoom = ZeroContent.TotalRooms;
        const float Left = -14.4f, Right = 14.4f, Floor = -4.8f, HeroHalf = .67f;
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly List<Pickup> coins = new List<Pickup>();
        readonly List<Effect> effects = new List<Effect>();
        readonly List<Floating> floating = new List<Floating>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly List<Rect> platforms = new List<Rect>();
        readonly Queue<EnemyKind> spawnQueue = new Queue<EnemyKind>();
        readonly HashSet<int> owned = new HashSet<int>();
        readonly ZeroTrade[] trades = new ZeroTrade[3];
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
        float noticeTime, roomBanner, spawnTimer, petrified, dropTimer;
        string notice = "", bossNotice = "";
        int room = 1, completedRooms, wallet, kills, seed, best, selected = -1, routeSelected = -1, jumps;
        int lastUp = -1, lastDown = -1, lastAmount, shields, activeWeapon, gunSlot, swordSlot = 10, shopTab;
        bool paused, muted, help, campHealed, campShield, autoAim, smokeMode, grounded;
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
            best = PlayerPrefs.GetInt("ZeroHero.Sideview.Best",0); muted = PlayerPrefs.GetInt("ZeroHero.Muted",0) == 1;
            currentMap = ZeroContent.CreateOffers(1,random)[0]; BuildMap(); player = new Vector2(-10,Floor + HeroHalf);
            smokeMode = Array.IndexOf(Environment.GetCommandLineArgs(),"--zero-smoke") >= 0;
            if (smokeMode) StartCoroutine(SmokeTest());
        }

        static Color C(string hex) => ZeroHeroArt.Hex(hex);
        float Range(float min, float max) => min + (float)random.NextDouble() * (max-min);
        SpriteRenderer Sprite(string label, Sprite sprite, Color color, Vector2 pos, Vector2 scale, int order, Transform parent = null)
        {
            var go = new GameObject(label); go.transform.SetParent(parent == null ? world : parent,false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order; sr.sharedMaterial = spriteMaterial; return sr;
        }

        void StartRun()
        {
            ClearActors(); stats = new ZeroStats(); hp = 100; wallet = 90; kills = completedRooms = 0; room = 1; shields = 0;
            seed = Environment.TickCount & int.MaxValue; random = new System.Random(seed); runTime = 0;
            owned.Clear(); owned.Add(0); owned.Add(10); gunSlot = activeWeapon = 0; swordSlot = 10; shopTab = 0;
            lastUp = lastDown = -1; paused = help = campHealed = campShield = false; phase = Phase.Camp;
            currentMap = ZeroContent.CreateOffers(1,random)[0]; BuildMap(); player = new Vector2(-10,Floor + HeroHalf); velocity = Vector2.zero;
            petrified = dropTimer = attackTimer = dashTimer = dashTime = bombTimer = invulnerable = 0;
            selected = routeSelected = -1; jumps = 0; grounded = true; aim = Vector2.right; Notice(""); PlaySound(4);
        }

        void NextRoom()
        {
            if (phase != Phase.Camp) return;
            room = completedRooms + 1; routes = ZeroContent.CreateOffers(room,random); routeSelected = -1; phase = Phase.Route;
        }
        void ChooseRoute()
        {
            if (phase != Phase.Route || routeSelected < 0 || routeSelected >= 3) return;
            currentMap = routes[routeSelected]; BeginRoom();
        }
        void BeginRoom()
        {
            ClearActors(); BuildMap(); phase = Phase.Combat; player = new Vector2(-10,Floor+HeroHalf); velocity = Vector2.zero;
            jumps = 0; grounded = true; aim = Vector2.right; petrified = dropTimer = 0;
            attackTimer = dashTimer = dashTime = bombTimer = 0; invulnerable = 1.3f; roomTime = 0; roomBanner = 2;
            bossNotice = ""; spawnTimer = .5f;
            for (int k = 0; k < currentMap.counts.Length; k++) for (int n = 0; n < currentMap.counts[k]; n++)
            {
                if (k >= (int)EnemyKind.Medusa) SpawnEnemy((EnemyKind)k,new Vector2(8,0));
                else spawnQueue.Enqueue((EnemyKind)k);
            }
            Notice(room == 1 ? "A/D 이동 · Space 2단 점프 · 클릭 공격 · 1 총 / 2 칼" : "");
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
                if (kb.escapeKey.wasPressedThisFrame) { if (help) { help = false; paused = false; } else if (phase == Phase.Combat) paused = !paused; }
                if (!help && !paused)
                {
                    if ((phase == Phase.Title || phase == Phase.Dead || phase == Phase.Victory) && kb.enterKey.wasPressedThisFrame) StartRun();
                    else if (phase == Phase.Camp && kb.enterKey.wasPressedThisFrame) NextRoom();
                    else if (phase == Phase.Route)
                    {
                        if (kb.digit1Key.wasPressedThisFrame) routeSelected = 0;
                        if (kb.digit2Key.wasPressedThisFrame) routeSelected = 1;
                        if (kb.digit3Key.wasPressedThisFrame) routeSelected = 2;
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
            }
            shake = Mathf.MoveTowards(shake,0,dt*2);
            cam.transform.position = new Vector3(Mathf.Sin(visualTime*117)*shake,Mathf.Cos(visualTime*133)*shake,-10);
            SetHeroVisual();
        }

        void TickCombat(float dt)
        {
            runTime += dt; roomTime += dt; invulnerable -= dt; attackTimer -= dt; dashTimer -= dt; bombTimer -= dt; petrified -= dt; dropTimer -= dt;
            var kb = Keyboard.current; var mouse = Mouse.current;
            float input = kb == null ? 0 : (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            if (kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) autoAim = !autoAim;
                if (kb.digit1Key.wasPressedThisFrame) Equip(gunSlot);
                if (kb.digit2Key.wasPressedThisFrame) Equip(swordSlot);
                if (kb.tabKey.wasPressedThisFrame) Equip(Weapon.gun ? swordSlot : gunSlot);
            }
            Vector2 target = mouse != null ? (Vector2)cam.ScreenToWorldPoint(mouse.position.ReadValue()) : player + aim;
            if (autoAim && enemies.Count > 0)
            {
                float closest = float.MaxValue;
                foreach (var e in enemies) if ((e.pos-player).sqrMagnitude < closest) { closest = (e.pos-player).sqrMagnitude; target = e.pos; }
            }
            if ((target-player).sqrMagnitude > .02f) aim = (target-player).normalized;
            cursorRing.transform.position = target; cursorRing.enabled = !autoAim;
            if (petrified <= 0)
            {
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) TryJump(kb.sKey.isPressed || kb.downArrowKey.isPressed);
                if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) && dashTimer <= 0)
                { dashDirection = (input != 0 ? input : Mathf.Sign(aim.x)) * (stats[2] < 0 ? -1 : 1); dashTime = .16f; dashTimer = 1.35f; invulnerable = .28f; PlaySound(3); }
                velocity.x = dashTime > 0 ? dashDirection * 18 : input * stats.MoveSpeed;
                if (attackTimer <= 0 && ((mouse != null && mouse.leftButton.isPressed) || (kb != null && kb.jKey.isPressed))) Fire();
                if (kb != null && kb.eKey.wasPressedThisFrame && bombTimer <= 0) Bomb();
            }
            else velocity.x = 0;
            if (dashTime > 0) { dashTime -= dt; AddEffect(art.hero,player,ZeroHeroArt.Gain,.18f,1.2f,Vector2.zero,0); }
            grounded = MoveBody(ref player,ref velocity,HeroHalf,.36f,dt,dropTimer > 0);
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
            if (spawnQueue.Count == 0 && enemies.Count == 0) ClearRoom();
        }

        void TryJump(bool drop)
        {
            if (petrified > 0) return;
            if (drop && player.y-HeroHalf > Floor+.15f) { dropTimer = .24f; velocity.y = -3; grounded = false; return; }
            if (jumps >= 2) return;
            velocity.y = 11.8f; jumps++; grounded = false; PlaySound(3);
        }
        bool MoveBody(ref Vector2 pos, ref Vector2 vel, float halfHeight, float radius, float dt, bool drop = false)
        {
            float oldFeet = pos.y-halfHeight; vel.y -= 28*dt;
            pos.x = Mathf.Clamp(pos.x+vel.x*dt,Left+radius,Right-radius); pos.y += vel.y*dt;
            bool landed = false;
            if (vel.y <= 0 && !drop)
                foreach (var p in platforms) if (pos.x+radius > p.xMin && pos.x-radius < p.xMax && oldFeet >= p.yMax-.025f && pos.y-halfHeight <= p.yMax)
                { pos.y = p.yMax+halfHeight; vel.y = 0; landed = true; break; }
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
            if (phase != Phase.Camp || id < 0 || id >= ZeroContent.Weapons.Length) return false;
            var w = ZeroContent.Weapons[id];
            if (!owned.Contains(id)) { if (wallet < w.price) return false; wallet -= w.price; owned.Add(id); }
            Equip(id); PlaySound(6); return true;
        }
        bool BuySupply(bool shield)
        {
            if (phase != Phase.Camp) return false;
            int price = shield ? 18 : 24;
            if (wallet < price || (shield ? campShield : campHealed || hp >= 100)) return false;
            wallet -= price;
            if (shield) { shields++; campShield = true; } else { hp = Mathf.Min(100,hp+40); campHealed = true; }
            PlaySound(4); return true;
        }

        void Fire()
        {
            if (phase != Phase.Combat || petrified > 0 || attackTimer > 0) return;
            attackTimer = 1/Weapon.rate; int damage = Weapon.Damage(stats);
            if (Weapon.gun)
            {
                for (int i = 0; i < Weapon.pellets; i++)
                {
                    float a = Mathf.Atan2(aim.y,aim.x) + (i-(Weapon.pellets-1)*.5f)*.095f;
                    var direction = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    CreateShot(player+direction*.65f,direction*Weapon.bulletSpeed,damage,false,ShotKind.Bullet,Weapon.pierce);
                }
            }
            else
            {
                float angle = Mathf.Atan2(aim.y,aim.x);
                for (int i = -5; i <= 5; i++)
                {
                    float a = angle+i*.14f;
                    AddEffect(art.square,player+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Weapon.reach*.72f,damage < 0 ? ZeroHeroArt.Gain : ZeroHeroArt.Gold,.15f,.15f,Vector2.zero,0);
                }
                for (int i = enemies.Count-1; i >= 0; i--)
                { var e = enemies[i]; Vector2 delta = e.pos-player; if (delta.magnitude <= Weapon.reach+e.Radius && Vector2.Dot(delta.normalized,aim) > .15f) HitEnemy(e,damage); }
            }
            PlaySound(0);
        }
        void Bomb()
        {
            if (phase != Phase.Combat || bombTimer > 0 || petrified > 0) return;
            bombTimer = 6; shake = .2f; PlaySound(2);
            AddEffect(art.ring,player,ZeroHeroArt.Gold,.6f,.7f,Vector2.zero,14); Burst(player,ZeroHeroArt.Gold,24);
            for (int i = enemies.Count-1; i >= 0; i--) if (Vector2.Distance(player,enemies[i].pos) < 4.8f+enemies[i].Radius) HitEnemy(enemies[i],42);
            for (int i = shots.Count-1; i >= 0; i--) if (shots[i].hostile && Vector2.Distance(player,shots[i].pos) < 5) RemoveShot(i);
        }

        void HitEnemy(Enemy e, int signedDamage)
        {
            int damage = ZeroContent.ResolveDamage(e.kind,signedDamage);
            e.hp = ZeroStats.ApplyAttack(e.hp,e.maxHp,damage); e.flash = .12f;
            Float(e.pos+Vector2.up*e.Radius,damage < 0 ? "+"+-damage : damage.ToString(),damage < 0 ? ZeroHeroArt.Gain : e.kind == EnemyKind.Undead && signedDamage < 0 ? ZeroHeroArt.Gold : Color.white);
            Burst(e.pos,damage < 0 ? ZeroHeroArt.Gain : ZeroHeroArt.Pink,4); PlaySound(1);
            if (e.hp > 0) return;
            kills++; Burst(e.pos,e.Def.Boss ? ZeroHeroArt.Gold : ZeroHeroArt.Pink,18);
            int number = e.Def.Boss ? 12 : e.kind == EnemyKind.Giant ? 5 : 2;
            for (int i = 0; i < number; i++)
            {
                Vector2 p = e.pos+new Vector2(Range(-.5f,.5f),Range(-.2f,.4f));
                var sr = Sprite("Coin",art.coin,Color.white,p,Vector2.one*.7f,15);
                coins.Add(new Pickup { root = sr.transform,pos = p,value = e.Def.Boss ? 5 : 3 });
            }
            Destroy(e.root.gameObject); enemies.Remove(e);
        }
        bool Hurt(int incoming)
        {
            if (invulnerable > 0 || phase != Phase.Combat) return false;
            invulnerable = .75f; shake = .15f;
            if (shields > 0) { shields--; Float(player+Vector2.up,"보호막",ZeroHeroArt.Gain); PlaySound(3); return false; }
            int damage = stats.DamageTaken(incoming); hp = Mathf.Max(0,hp-damage); Float(player+Vector2.up,"−"+damage,ZeroHeroArt.Pink); PlaySound(5);
            if (hp <= 0) { phase = Phase.Dead; paused = false; SaveBest(completedRooms); cursorRing.enabled = false; }
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
        { int before = wallet; wallet = ZeroStats.CollectCoins(wallet,p.value,stats); Float(p.pos,Signed(wallet-before)+" G",stats[3]<0 ? ZeroHeroArt.Pink : ZeroHeroArt.Gold); PlaySound(6); }

        void ClearRoom()
        {
            if (phase != Phase.Combat) return;
            foreach (var p in coins) { Collect(p); Destroy(p.root.gameObject); } coins.Clear();
            ClearProjectiles(); completedRooms = room; SaveBest(completedRooms); hp = Mathf.Min(100,hp+12);
            phase = Phase.Trade; selected = -1; cursorRing.enabled = false; PlaySound(4);
            var pairs = new HashSet<int>();
            for (int i = 0; i < 3; i++) { int up,down; do { up = random.Next(4); down = random.Next(4); } while (up == down || !pairs.Add(up*4+down)); trades[i] = new ZeroTrade(up,down,2+random.Next(3)); }
        }
        void ApplyTrade()
        {
            if (phase != Phase.Trade || selected < 0 || selected >= 3) return;
            var t = trades[selected]; stats.Exchange(t.up,t.down,t.amount); lastUp = t.up; lastDown = t.down; lastAmount = t.amount;
            phase = completedRooms == LastRoom ? Phase.Victory : Phase.Camp; campHealed = campShield = false; petrified = 0; PlaySound(4);
        }
        void ReturnToTitle() { ClearActors(); phase = Phase.Title; paused = help = false; }
        void SaveBest(int n) { if (smokeMode) return; best = Mathf.Max(best,n); PlayerPrefs.SetInt("ZeroHero.Sideview.Best",best); PlayerPrefs.Save(); }
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
            heldWeapon.transform.localPosition = new Vector2(.55f,0);
            heldWeapon.transform.localRotation = Quaternion.Euler(0,0,Weapon.gun ? 0 : -45);
            aimRoot.rotation = Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg);
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
        void OnDestroy()
        {
            ClearActors(); if (world != null) Destroy(world.gameObject); if (scenery != null) Destroy(scenery.gameObject); art?.Dispose();
            foreach (var c in clips) Destroy(c); if (uiFont != null) Destroy(uiFont);
            if (spriteMaterial != null && spriteMaterial != Resources.Load<Material>("ZeroSprite")) Destroy(spriteMaterial);
        }
    }
}
