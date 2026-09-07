using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame : MonoBehaviour
    {
        enum Phase { Title, Combat, Trade, Camp, Dead, Victory }
        sealed class Enemy
        {
            public Transform root;
            public SpriteRenderer body, health;
            public Vector2 pos, charge;
            public float hp, maxHp, timer, windup, charging, flash;
            public int kind;
        }
        sealed class Shot
        {
            public Transform root;
            public Vector2 pos, velocity;
            public float life;
            public int damage;
            public bool hostile;
        }
        sealed class Pickup { public Transform root; public Vector2 pos; public int value; public float age; }
        sealed class Effect { public SpriteRenderer sprite; public Vector2 velocity; public float life, maxLife, scale, grow; }
        sealed class Floating { public Vector2 pos; public string text; public Color color; public float life; }

        const int LastRoom = 8;
        const float Left = -14.4f, Right = 14.4f, Bottom = -5.95f, Top = 5.95f;
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Pickup> coins = new List<Pickup>();
        readonly List<Effect> effects = new List<Effect>();
        readonly List<Floating> floating = new List<Floating>();
        readonly List<Vector2> pillars = new List<Vector2>();
        readonly List<GameObject> decorations = new List<GameObject>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly ZeroTrade[] trades = new ZeroTrade[3];
        ZeroStats stats = new ZeroStats();
        ZeroHeroArt art;
        Material spriteMaterial;
        Camera cam;
        Transform world, heroRoot, aimRoot;
        SpriteRenderer heroBody, heroShadow, cursorRing;
        AudioSource audioSource;
        Phase phase = Phase.Title;
        Vector2 player, aim = Vector2.right, dashDirection;
        System.Random random;
        float hp = 100, attackTimer, dashTimer, dashTime, invulnerable, bombTimer, roomTime, runTime, visualTime, shake;
        float noticeTime, roomBanner, spawnTimer;
        string notice = "";
        int room = 1, wallet, kills, pendingSpawns, seed, best, selected = -1, lastUp = -1, lastDown = -1, lastAmount;
        bool paused, muted, help, campHealed, campShield, autoAim, smokeMode;
        int shields;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string scene = SceneManager.GetActiveScene().name;
            if ((scene == "SampleScene" || scene == "ZeroHero") && FindAnyObjectByType<ZeroHeroGame>() == null)
                new GameObject("ZERO / Game").AddComponent<ZeroHeroGame>();
        }

        void Awake()
        {
            Application.targetFrameRate = 120;
            art = new ZeroHeroArt();
            spriteMaterial = Resources.Load<Material>("ZeroSprite");
            if (spriteMaterial == null)
                spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.orthographic = true; cam.orthographicSize = 9;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = C("141613");
            world = new GameObject("The zero chambers").transform;
            random = new System.Random(120);
            BuildArena();
            heroRoot = new GameObject("Zero / Hero").transform;
            heroRoot.SetParent(world);
            heroShadow = Sprite("Shadow", art.disc, new Color(0, 0, 0, .35f), Vector2.zero, new Vector2(.95f, .35f), 10, heroRoot);
            heroBody = Sprite("Knight", art.hero, Color.white, Vector2.zero, Vector2.one * 1.25f, 20, heroRoot);
            aimRoot = new GameObject("Aim direction").transform; aimRoot.SetParent(heroRoot, false);
            Sprite("Aim", art.square, ZeroHeroArt.Gain, new Vector2(.88f, 0), new Vector2(.22f, .055f), 21, aimRoot);
            cursorRing = Sprite("Reticle", art.ring, ZeroHeroArt.Gain, Vector2.zero, Vector2.one * .36f, 60, world);
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .3f;
            MakeAudio();
            best = PlayerPrefs.GetInt("ZeroHero.BestRoom", 0);
            muted = PlayerPrefs.GetInt("ZeroHero.Muted", 0) == 1;
            player = new Vector2(6, -.25f);
            SetHeroVisual();
            smokeMode = Array.IndexOf(Environment.GetCommandLineArgs(), "--zero-smoke") >= 0;
            if (smokeMode) StartCoroutine(SmokeTest());
        }

        static Color C(string hex) => ZeroHeroArt.Hex(hex);
        float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        SpriteRenderer Sprite(string label, Sprite sprite, Color color, Vector2 pos, Vector2 scale, int order, Transform parent = null)
        {
            var go = new GameObject(label); go.transform.SetParent(parent == null ? world : parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color;
            sr.sortingOrder = order; sr.sharedMaterial = spriteMaterial; return sr;
        }

        void BuildArena()
        {
            Sprite("Foundation", art.square, C("383B30"), Vector2.zero, new Vector2(30.4f, 13.4f), -20);
            Sprite("Floor", art.square, C("252C23"), Vector2.zero, new Vector2(29.5f, 12.8f), -19);
            for (int x = -14; x < 14; x += 2) for (int y = -6; y <= 6; y++)
            {
                var color = Color.Lerp(C("2C3328"), C("3E4334"), Range(0, .7f));
                var tile = new Vector2(x + .5f + (y % 2 == 0 ? .4f : 0), y);
                Sprite("Flagstone", art.square, color, tile, new Vector2(1.92f, .95f), -18);
                Sprite("Stone lip", art.square, color * 1.14f, tile + new Vector2(0, .44f), new Vector2(1.84f, .04f), -17);
                if (random.NextDouble() < .27)
                {
                    Sprite("Crack", art.square, C("252C24"), tile + new Vector2(.23f, .26f), new Vector2(.035f, .32f), -16);
                    Sprite("Crack", art.square, C("252C24"), tile + new Vector2(.38f, .1f), new Vector2(.34f, .035f), -16);
                }
            }
            foreach (float y in new[] { -6.5f, 6.5f })
            {
                Sprite("Wall edge", art.square, C("656752"), new Vector2(0, y), new Vector2(30.2f, .08f), -10);
                for (int x = -14; x <= 14; x++)
                    Sprite("Wall brick", art.square, C("444A39"), new Vector2(x, y + .2f), new Vector2(.94f, .28f), -11);
            }
            foreach (float x in new[] { -15f, 15f }) Sprite("Side wall", art.square, C("505740"), new Vector2(x, 0), new Vector2(.12f, 13.2f), -10);
            for (int i = 0; i < 68; i++)
            {
                Vector2 pos = new Vector2(Range(-14, 14), Range(-5.9f, 5.9f));
                if (i < 40) pos.y = (i % 2 == 0 ? 1 : -1) * Range(5.2f, 6.2f);
                var size = new Vector2(Range(.06f, .24f), Range(.06f, .16f));
                Sprite("Rubble shadow", art.square, C("20271F"), pos + Vector2.down * .06f, size * 1.3f, -15);
                Sprite("Rubble", art.square, i % 3 == 0 ? C("68705A") : C("49523E"), pos, size, -14);
            }
            foreach (float x in new[] { -10f, 10f }) foreach (float y in new[] { -6.35f, 6.35f })
            {
                Sprite("Torch bracket", art.square, C("1C2019"), new Vector2(x, y), new Vector2(.38f, .4f), -8);
                Sprite("Torch ember", art.square, C("A36937"), new Vector2(x, y + .1f), new Vector2(.24f, .34f), -7);
                Sprite("Torch flame", art.square, ZeroHeroArt.Gold, new Vector2(x, y + .16f), new Vector2(.12f, .28f), -6);
            }
            foreach (float x in new[] { -4.5f, 4.5f })
                for (int i = 0; i < 5; i++) Sprite("Drain", art.square, C("1E251D"), new Vector2(x + i * .1f, 0), new Vector2(.045f, .43f), -16);
            SetPillars();
        }

        void SetPillars()
        {
            foreach (var go in decorations) Destroy(go); decorations.Clear(); pillars.Clear();
            float shift = room % 2 == 0 ? .9f : 0;
            foreach (float x in new[] { -8f - shift, 8f + shift }) foreach (float y in new[] { -2.6f, 2.6f })
            {
                var p = new Vector2(x, y); pillars.Add(p);
                var root = new GameObject("Obsidian pillar"); root.transform.SetParent(world); root.transform.position = p; decorations.Add(root);
                Sprite("Shadow", art.disc, new Color(0, 0, 0, .35f), new Vector2(.15f, -.45f), new Vector2(1.9f, .55f), -3, root.transform);
                Sprite("Foot", art.square, C("20271E"), new Vector2(0, -.12f), new Vector2(1.1f, 1.25f), -2, root.transform);
                Sprite("Stone", art.square, C("555E48"), new Vector2(0, .12f), new Vector2(.94f, 1.25f), -1, root.transform);
                Sprite("Top", art.square, C("7C8064"), new Vector2(0, .64f), new Vector2(1.05f, .2f), 0, root.transform);
                Sprite("Chip", art.square, C("343E2E"), new Vector2(.13f, .41f), new Vector2(.07f, .3f), 1, root.transform);
            }
        }

        void StartRun()
        {
            ClearActors(); stats = new ZeroStats(); hp = 100; wallet = 12; kills = 0; room = 1; shields = 0;
            runTime = 0; seed = Environment.TickCount & int.MaxValue; random = new System.Random(seed);
            paused = false; help = false; lastUp = lastDown = -1; player = Vector2.zero;
            BeginRoom(); PlaySound(4);
        }

        void BeginRoom()
        {
            ClearActors(); SetPillars(); phase = Phase.Combat; player = Vector2.zero; aim = Vector2.right;
            roomTime = 0; roomBanner = 2.5f; pendingSpawns = 5 + room * 2; spawnTimer = .7f;
            dashTimer = bombTimer = attackTimer = dashTime = 0; invulnerable = 1.2f;
            if (room % 4 == 0) { pendingSpawns = room == LastRoom ? 4 : 3; SpawnEnemy(3, new Vector2(9, 0)); }
            Notice(room == 1 ? "WASD 이동 · 클릭 공격 · Shift 회피 · E 폭탄" : "");
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .035f); visualTime += dt;
            cam.orthographicSize = Mathf.Max(9, 16 / Mathf.Max(.4f, cam.aspect));
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.mKey.wasPressedThisFrame) ToggleSound();
                if (keyboard.f1Key.wasPressedThisFrame) { help = !help; if (phase == Phase.Combat) paused = help; }
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (help) { help = false; paused = false; }
                    else if (phase == Phase.Combat) paused = !paused;
                }
                if (keyboard.qKey.wasPressedThisFrame && phase == Phase.Combat) { autoAim = !autoAim; Notice(autoAim ? "자동 조준 켜짐 · 클릭/Space로 발사" : "마우스 조준으로 전환"); }
                if (!help && phase == Phase.Title && keyboard.enterKey.wasPressedThisFrame) StartRun();
                else if (!help && (phase == Phase.Dead || phase == Phase.Victory) && keyboard.enterKey.wasPressedThisFrame) StartRun();
                if (phase == Phase.Trade && !help)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame) selected = 0;
                    if (keyboard.digit2Key.wasPressedThisFrame) selected = 1;
                    if (keyboard.digit3Key.wasPressedThisFrame) selected = 2;
                    if (selected >= 0 && keyboard.enterKey.wasPressedThisFrame) ApplyTrade();
                }
            }
            if (!paused && !help)
            {
                TickEffects(dt); noticeTime -= dt; roomBanner -= dt;
                if (phase == Phase.Combat) TickCombat(dt);
            }
            shake = Mathf.MoveTowards(shake, 0, dt * 2);
            cam.transform.position = new Vector3(Mathf.Sin(visualTime * 117) * shake, Mathf.Cos(visualTime * 133) * shake, -10);
            SetHeroVisual();
        }

        void TickCombat(float dt)
        {
            runTime += dt; roomTime += dt; invulnerable -= dt; attackTimer -= dt; dashTimer -= dt; bombTimer -= dt;
            var kb = Keyboard.current; var mouse = Mouse.current;
            Vector2 input = Vector2.zero;
            if (kb != null)
            {
                input.x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
                input.y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
            }
            if (input.sqrMagnitude > 1) input.Normalize();
            Vector2 pointer = mouse != null ? (Vector2)cam.ScreenToWorldPoint(mouse.position.ReadValue()) : player + aim;
            if (autoAim)
            {
                float closest = float.MaxValue;
                foreach (var e in enemies) if ((e.pos - player).sqrMagnitude < closest) { closest = (e.pos - player).sqrMagnitude; pointer = e.pos; }
            }
            if ((pointer - player).sqrMagnitude > .04f) aim = (pointer - player).normalized;
            cursorRing.transform.position = pointer;
            cursorRing.enabled = !autoAim;
            if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) && dashTimer <= 0)
            {
                dashDirection = input.sqrMagnitude > 0 ? input : aim;
                if (stats[2] < 0) dashDirection *= -1;
                dashTime = .16f; dashTimer = 1.35f; invulnerable = .28f; PlaySound(3);
            }
            if (dashTime > 0)
            {
                dashTime -= dt; player = Move(player, dashDirection * 18 * dt, .38f);
                AddEffect(art.hero, player, ZeroHeroArt.Gain, .24f, 1.2f, Vector2.zero, 0);
            }
            else player = Move(player, input * stats.MoveSpeed * dt, .38f);
            if (attackTimer <= 0 && ((mouse != null && mouse.leftButton.isPressed) || (kb != null && kb.spaceKey.isPressed))) Fire();
            if (kb != null && kb.eKey.wasPressedThisFrame && bombTimer <= 0) Bomb();
            TickEnemies(dt); if (phase != Phase.Combat) return;
            TickShots(dt); if (phase != Phase.Combat) return;
            TickCoins(dt);
            spawnTimer -= dt;
            if (pendingSpawns > 0 && spawnTimer <= 0)
            {
                var point = new Vector2(random.Next(2) == 0 ? Left + .8f : Right - .8f, Range(-4.4f, 4.4f));
                if (Vector2.Distance(point, player) < 5) point.x *= -1;
                int kind = room == 1 ? (random.NextDouble() < .8 ? 0 : 1) : random.Next(3);
                SpawnEnemy(kind, point); pendingSpawns--; spawnTimer = room % 4 == 0 ? 4 : 1.25f;
            }
            if (pendingSpawns == 0 && enemies.Count == 0) ClearRoom();
        }

        Vector2 Move(Vector2 origin, Vector2 delta, float radius)
        {
            // Substeps prevent a large negative/positive speed or dash from tunneling through columns.
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .22f));
            for (int i = 0; i < steps; i++)
            {
                Vector2 next = origin + delta / steps;
                next.x = Mathf.Clamp(next.x, Left + radius, Right - radius); next.y = Mathf.Clamp(next.y, Bottom + radius, Top - radius);
                foreach (var p in pillars)
                {
                    Vector2 diff = next - p; float min = .64f + radius;
                    if (diff.sqrMagnitude < min * min) next = p + (diff.sqrMagnitude > .0001f ? diff.normalized : Vector2.up) * min;
                }
                origin = next;
            }
            return origin;
        }

        void Fire()
        {
            attackTimer = .24f;
            CreateShot(player + aim * .55f, aim * 17, stats.AttackDamage, false);
            AddEffect(art.disc, player + aim * .7f, stats[0] < 0 ? ZeroHeroArt.Pink : ZeroHeroArt.Gain, .1f, .24f, Vector2.zero, 1);
            PlaySound(0);
        }

        void Bomb()
        {
            bombTimer = 7; shake = .23f; PlaySound(2);
            AddEffect(art.ring, player, ZeroHeroArt.Gold, .65f, .7f, Vector2.zero, 12);
            Burst(player, ZeroHeroArt.Gold, 26);
            for (int i = enemies.Count - 1; i >= 0; i--) if (Vector2.Distance(player, enemies[i].pos) < 4.4f) HitEnemy(enemies[i], 42);
            for (int i = shots.Count - 1; i >= 0; i--) if (shots[i].hostile && Vector2.Distance(player, shots[i].pos) < 4.4f) RemoveShot(i);
            Notice("폭탄 · 피해 42");
        }

        void SpawnEnemy(int kind, Vector2 pos)
        {
            var e = new Enemy { kind = kind, pos = pos, maxHp = kind == 3 ? (room == 8 ? 480 : 300) : 22 + room * 5 + (kind == 2 ? 22 : 0), timer = Range(.7f, 2) };
            e.hp = e.maxHp; e.root = new GameObject(kind == 3 ? "The keeper of zero" : "Hollow " + kind).transform; e.root.SetParent(world); e.root.position = pos;
            float size = kind == 3 ? 2.1f : kind == 2 ? 1.2f : 1;
            Sprite("Shadow", art.disc, new Color(0, 0, 0, .32f), new Vector2(0, -.38f), new Vector2(size, .28f), 10, e.root);
            e.body = Sprite("Enemy", kind == 3 ? art.crown : kind == 2 ? art.brute : kind == 1 ? art.eye : art.skull, Color.white, Vector2.zero, Vector2.one * size, 19, e.root);
            Sprite("Health track", art.square, C("09121B"), new Vector2(0, size * .7f), new Vector2(size, .07f), 25, e.root);
            e.health = Sprite("Health", art.square, kind == 3 ? ZeroHeroArt.Gold : ZeroHeroArt.Pink, new Vector2(0, size * .7f), new Vector2(size, .07f), 26, e.root);
            enemies.Add(e); AddEffect(art.ring, pos, ZeroHeroArt.Pink, .6f, .2f, Vector2.zero, 3);
        }

        void TickEnemies(float dt)
        {
            foreach (var e in enemies)
            {
                Vector2 diff = player - e.pos; float distance = diff.magnitude; Vector2 direction = diff.normalized;
                e.timer -= dt; e.flash -= dt;
                float speed = e.kind == 0 ? 1.5f + room * .08f : e.kind == 1 ? 1.2f : e.kind == 2 ? 1.0f : .8f;
                Vector2 velocity = direction * speed;
                if (e.kind == 1)
                {
                    velocity *= distance < 4 ? -1 : distance < 6 ? 0 : 1;
                    velocity += new Vector2(-direction.y, direction.x) * Mathf.Sin(roomTime * 1.3f) * .7f;
                    if (e.timer <= 0) { CreateShot(e.pos, direction * 5, 12 + room, true); e.timer = 2.6f - room * .07f; }
                }
                if (e.kind == 2)
                {
                    if (e.windup > 0) { e.windup -= dt; velocity = Vector2.zero; if (e.windup <= 0) e.charging = .6f; }
                    else if (e.charging > 0) { velocity = e.charge * 9; e.charging -= dt; }
                    else if (e.timer <= 0 && distance < 9)
                    { e.charge = direction; e.windup = .7f; e.timer = 3.7f; AddEffect(art.ring, e.pos, ZeroHeroArt.Gold, .7f, 2.2f, Vector2.zero, -1); }
                }
                if (e.kind == 3 && e.timer <= 0)
                {
                    int count = e.hp < e.maxHp * .5f ? 16 : 12;
                    float offset = roomTime * .4f;
                    for (int i = 0; i < count; i++)
                    { float angle = offset + i * Mathf.PI * 2 / count; CreateShot(e.pos, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4.2f, 18, true); }
                    CreateShot(e.pos, direction * 7, 20, true); e.timer = e.hp < e.maxHp * .5f ? 1.55f : 2.3f;
                    AddEffect(art.ring, e.pos, ZeroHeroArt.Gold, .5f, 1, Vector2.zero, 4);
                }
                foreach (var other in enemies)
                {
                    if (other == e) continue; var separation = e.pos - other.pos;
                    if (separation.sqrMagnitude < 1 && separation.sqrMagnitude > .0001f) velocity += separation.normalized * (1 - separation.magnitude) * 2;
                }
                e.pos = Move(e.pos, velocity * dt, e.kind == 3 ? .85f : .4f); e.root.position = e.pos;
                e.body.transform.localPosition = new Vector2(0, Mathf.Sin(visualTime * 6 + e.pos.x) * .045f);
                e.body.color = e.flash > 0 ? (e.flash > .12f ? ZeroHeroArt.Gain : Color.white) : e.windup > 0 ? ZeroHeroArt.Gold : Color.white;
                float size = e.kind == 3 ? 2.1f : e.kind == 2 ? 1.2f : 1;
                e.health.transform.localScale = new Vector3(size * e.hp / e.maxHp, .07f, 1);
                if (distance < (e.kind == 3 ? 1.1f : .78f)) Hurt(e.kind == 2 ? 22 : e.kind == 3 ? 25 : 12 + room);
                if (phase != Phase.Combat) break;
            }
        }

        void CreateShot(Vector2 pos, Vector2 velocity, int damage, bool hostile)
        {
            Color color = hostile ? ZeroHeroArt.Pink : damage < 0 ? ZeroHeroArt.Gold : ZeroHeroArt.Gain;
            var sr = Sprite(hostile ? "Enemy bolt" : "Zero bolt", hostile ? art.disc : art.square, color, pos, hostile ? Vector2.one * .24f : new Vector2(.4f, .12f), 30);
            sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            shots.Add(new Shot { root = sr.transform, pos = pos, velocity = velocity, life = 4, hostile = hostile, damage = damage });
        }

        void TickShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i]; Vector2 start = shot.pos; shot.pos += shot.velocity * dt; shot.life -= dt; shot.root.position = shot.pos;
                bool remove = shot.life <= 0 || shot.pos.x < Left || shot.pos.x > Right || shot.pos.y < Bottom || shot.pos.y > Top;
                foreach (var pillar in pillars) if (SegmentDistance(pillar, start, shot.pos) < .65f) { remove = true; break; }
                if (!remove && shot.hostile && SegmentDistance(player, start, shot.pos) < .42f) { Hurt(shot.damage); remove = true; }
                if (!remove && !shot.hostile)
                {
                    for (int j = enemies.Count - 1; j >= 0; j--)
                        if (SegmentDistance(enemies[j].pos, start, shot.pos) < (enemies[j].kind == 3 ? .9f : .52f))
                        { HitEnemy(enemies[j], shot.damage); remove = true; break; }
                }
                if (remove) RemoveShot(i);
            }
        }

        static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 v = b - a; float t = v.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector2.Dot(point - a, v) / v.sqrMagnitude) : 0;
            return Vector2.Distance(point, a + v * t);
        }
        void RemoveShot(int i) { Destroy(shots[i].root.gameObject); shots.RemoveAt(i); }

        void HitEnemy(Enemy e, int damage)
        {
            e.hp = ZeroStats.ApplyAttack(e.hp, e.maxHp, damage); e.flash = .18f;
            Float(e.pos + Vector2.up * .5f, damage < 0 ? "+" + -damage : damage.ToString(), damage < 0 ? ZeroHeroArt.Gain : Color.white);
            Burst(e.pos, damage < 0 ? ZeroHeroArt.Gain : ZeroHeroArt.Pink, 5); PlaySound(1);
            if (e.hp > 0) return;
            kills++; Burst(e.pos, e.kind == 3 ? ZeroHeroArt.Gold : ZeroHeroArt.Pink, 15);
            for (int i = 0; i < (e.kind == 3 ? 10 : 2); i++)
            {
                Vector2 pos = e.pos + new Vector2(Range(-.4f, .4f), Range(-.4f, .4f));
                var sr = Sprite("Coin", art.coin, Color.white, pos, Vector2.one * .7f, 15);
                coins.Add(new Pickup { root = sr.transform, pos = pos, value = 1 });
            }
            Destroy(e.root.gameObject); enemies.Remove(e);
        }

        void Hurt(int incoming)
        {
            if (invulnerable > 0 || phase != Phase.Combat) return;
            invulnerable = .8f; shake = .18f;
            if (shields > 0) { shields--; Float(player + Vector2.up, "보호막", ZeroHeroArt.Gain); PlaySound(3); return; }
            int damage = stats.DamageTaken(incoming); hp = Mathf.Max(0, hp - damage);
            Float(player + Vector2.up, "−" + damage, ZeroHeroArt.Pink); Burst(player, ZeroHeroArt.Pink, 12); PlaySound(5);
            if (hp <= 0) { phase = Phase.Dead; paused = false; SaveBest(room - 1); cursorRing.enabled = false; }
        }

        void TickCoins(float dt)
        {
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                var coin = coins[i]; coin.age += dt;
                if (Vector2.Distance(coin.pos, player) < 1.9f && coin.age > .2f) coin.pos = Vector2.MoveTowards(coin.pos, player, 7 * dt);
                coin.root.position = coin.pos + Vector2.up * Mathf.Sin(coin.age * 5) * .06f;
                if (Vector2.Distance(coin.pos, player) < .48f) { Collect(coin); Destroy(coin.root.gameObject); coins.RemoveAt(i); }
            }
        }

        void Collect(Pickup coin)
        {
            int before = wallet; wallet = ZeroStats.CollectCoins(wallet, coin.value, stats);
            Float(coin.pos, (wallet >= before ? "+" : "−") + Math.Abs(wallet - before) + " G", stats[3] < 0 ? ZeroHeroArt.Pink : ZeroHeroArt.Gold);
            PlaySound(6);
        }

        void ClearRoom()
        {
            // Every remaining coin uses the signed fortune rule, including room-clear collection.
            foreach (var coin in coins) { Collect(coin); Destroy(coin.root.gameObject); } coins.Clear();
            foreach (var shot in shots) Destroy(shot.root.gameObject); shots.Clear();
            phase = Phase.Trade; selected = -1; cursorRing.enabled = false; SaveBest(room); PlaySound(4);
            var pairs = new HashSet<int>();
            for (int i = 0; i < 3; i++)
            {
                int up, down;
                do { up = random.Next(4); down = random.Next(4); } while (up == down || !pairs.Add(up * 4 + down));
                trades[i] = new ZeroTrade(up, down, 2 + random.Next(3));
            }
        }

        void ApplyTrade()
        {
            if (phase != Phase.Trade || selected < 0 || selected >= 3) return;
            var t = trades[selected]; stats.Exchange(t.up, t.down, t.amount);
            lastUp = t.up; lastDown = t.down; lastAmount = t.amount; PlaySound(4);
            phase = room == LastRoom ? Phase.Victory : Phase.Camp;
            campHealed = campShield = false;
            if (phase == Phase.Victory) SaveBest(LastRoom);
        }

        void NextRoom() { if (phase != Phase.Camp) return; room++; BeginRoom(); }
        void SaveBest(int cleared) { if (smokeMode) return; best = Mathf.Max(best, cleared); PlayerPrefs.SetInt("ZeroHero.BestRoom", best); PlayerPrefs.Save(); }
        void ToggleSound() { muted = !muted; PlayerPrefs.SetInt("ZeroHero.Muted", muted ? 1 : 0); PlayerPrefs.Save(); }
        void Notice(string message) { notice = message; noticeTime = 4; }
        void Float(Vector2 pos, string text, Color color) { floating.Add(new Floating { pos = pos, text = text, color = color, life = 1 }); }

        void SetHeroVisual()
        {
            heroRoot.gameObject.SetActive(phase != Phase.Title);
            heroRoot.position = player;
            heroBody.transform.localPosition = Vector2.up * Mathf.Sin(visualTime * 7) * .04f;
            heroBody.flipX = aim.x < 0;
            heroBody.color = phase == Phase.Dead ? C("526575") : invulnerable > 0 && Mathf.Sin(visualTime * 40) > 0 ? new Color(1, 1, 1, .4f) : Color.white;
            heroShadow.transform.localPosition = new Vector2(0, -.55f);
            aimRoot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            if (phase != Phase.Combat || paused || help) cursorRing.enabled = false;
        }

        void AddEffect(Sprite sprite, Vector2 pos, Color color, float life, float scale, Vector2 velocity, float grow)
        {
            if (effects.Count > 450) return;
            var sr = Sprite("Particle", sprite, color, pos, Vector2.one * scale, 28);
            effects.Add(new Effect { sprite = sr, velocity = velocity, life = life, maxLife = life, scale = scale, grow = grow });
        }
        void Burst(Vector2 pos, Color color, int amount)
        {
            for (int i = 0; i < amount; i++) AddEffect(art.square, pos, color, Range(.2f, .6f), Range(.05f, .12f), new Vector2(Range(-3, 3), Range(-3, 3)), -.08f);
        }
        void TickEffects(float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var e = effects[i]; e.life -= dt;
                if (e.life <= 0) { Destroy(e.sprite.gameObject); effects.RemoveAt(i); continue; }
                e.sprite.transform.position += (Vector3)(e.velocity * dt); e.scale += e.grow * dt;
                e.sprite.transform.localScale = Vector3.one * Mathf.Max(.01f, e.scale);
                Color c = e.sprite.color; c.a = e.life / e.maxLife; e.sprite.color = c;
            }
            for (int i = floating.Count - 1; i >= 0; i--)
            { floating[i].life -= dt; floating[i].pos += Vector2.up * dt * .75f; if (floating[i].life <= 0) floating.RemoveAt(i); }
        }
        void ClearActors()
        {
            foreach (var e in enemies) Destroy(e.root.gameObject); enemies.Clear();
            foreach (var s in shots) Destroy(s.root.gameObject); shots.Clear();
            foreach (var p in coins) Destroy(p.root.gameObject); coins.Clear();
            foreach (var e in effects) Destroy(e.sprite.gameObject); effects.Clear(); floating.Clear();
        }
        void OnApplicationFocus(bool focus) { if (!focus && phase == Phase.Combat && !smokeMode) paused = true; }
        void OnDestroy()
        {
            ClearActors(); if (world != null) Destroy(world.gameObject); art?.Dispose();
            foreach (var clip in clips) Destroy(clip);
            if (spriteMaterial != null && spriteMaterial != Resources.Load<Material>("ZeroSprite")) Destroy(spriteMaterial);
            if (uiFont != null) Destroy(uiFont);
        }

        void MakeAudio()
        {
            int rate = 22050;
            float[] frequencies = { 660, 180, 65, 420, 880, 95, 1200 };
            for (int i = 0; i < frequencies.Length; i++)
            {
                float length = i == 2 ? .45f : i == 4 ? .4f : .11f;
                var samples = new float[(int)(rate * length)];
                for (int n = 0; n < samples.Length; n++)
                {
                    float t = (float)n / rate, envelope = Mathf.Pow(1 - t / length, 2);
                    float frequency = frequencies[i] * (i == 4 ? 1 + Mathf.Floor(t * 10) * .25f : 1 - t * 1.3f);
                    samples[n] = Mathf.Sin(t * frequency * Mathf.PI * 2) * envelope * .38f;
                    if (i == 2 || i == 5) samples[n] += Mathf.Sin(n * 73.123f) * envelope * .14f;
                }
                var clip = AudioClip.Create("Zero sound " + i, samples.Length, 1, rate, false); clip.SetData(samples, 0); clips.Add(clip);
            }
        }
        void PlaySound(int index) { if (!muted && audioSource != null) audioSource.PlayOneShot(clips[index]); }
    }
}
