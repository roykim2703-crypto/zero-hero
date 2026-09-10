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
        // Standalone verification only. Normal runs never enter this coroutine.
        IEnumerator SmokeTest()
        {
            var checks = new List<string>(); var errors = new List<string>();
            string folder = Path.Combine(Application.persistentDataPath,"Verification");
            foreach(string arg in Environment.GetCommandLineArgs()) if(arg.StartsWith("--zero-output=")) folder = arg.Substring(14);
            Directory.CreateDirectory(folder);
            Application.LogCallback captureError = (message,stack,type) => { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(message+"\n"+stack); };
            Application.logMessageReceived += captureError;
            Action<bool,string> check = (condition,name) => { checks.Add((condition?"PASS ":"FAIL ")+name); if(!condition) errors.Add(name); };
            muted = true;
            for(int i=0;i<ZeroStats.Count;i++) metaLevels[i]=0;
            legacyPoints=totalRuns=totalDeaths=totalVictories=lifetimeKills=lifetimeGold=0;
            yield return new WaitForSeconds(.8f);
            yield return CaptureSmoke(folder,"01-title");
            var keyboard = Keyboard.current;
            if(keyboard != null)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                check(phase==Phase.Camp,"Enter opens the starting shop through the Input System");
            }
            else { StartRun(); check(false,"Keyboard device exists"); }
            check(wallet==ZeroContent.StartingGold && owned.Count==2 && activeWeapon==10 && gunSlot==0 && swordSlot==10,"Starting money, both basic weapons, and the rusty sword equipped");
            int[] expectedMagazines={8,6,30,10,12,25,100,5,2,-1};
            for(int i=0;i<expectedMagazines.Length;i++) check(gunAmmo[i]==expectedMagazines[i],"Starting magazine: "+ZeroContent.Weapons[i].name);
            check(!BuyWeapon(11) && wallet==ZeroContent.StartingGold,"Paid weapons require enough gold");
            int firstMapGold=Mathf.RoundToInt(ZeroContent.RawCoinReward(ZeroContent.CreateOffers(1,new System.Random(12))[0],1)*ZeroStats.CoinRate(new ZeroStats()[3]));
            wallet=firstMapGold; check(!BuyWeapon(11) && wallet==firstMapGold,"First-map baseline income cannot buy the cheapest paid weapon");
            wallet=ZeroContent.Weapons[11].price; check(BuyWeapon(11) && wallet==0 && swordSlot==11,"Gold and price alone control a paid purchase");
            check(BuyWeapon(11) && wallet==0,"Equipping an owned weapon does not charge again");
            check(!BuyWeapon(9) && wallet==0,"Insufficient funds reject a purchase");
            wallet=2000; check(BuyWeapon(9) && wallet==0 && gunSlot==9,"A saved-up player can skip directly to the final weapon");
            wallet=100000;
            foreach(var w in ZeroContent.Weapons) check(BuyWeapon(w.id),"Weapon can be purchased: "+w.name);
            check(owned.Count==20 && stats.Total==10,"All twenty weapons are owned without changing stat total");
            StartRun(); shopTab=0; yield return CaptureSmoke(folder,"02-shop-guns");
            shopTab=1; yield return CaptureSmoke(folder,"03-shop-swords");
            wallet=1000; hp=40; check(BuySupply(false) && hp==80 && wallet==500 && !BuySupply(false),"Potion costs 500 and heals once per shop");
            check(BuySupply(true) && wallet==150 && shields==1 && !BuySupply(true),"Shield costs 350 and is limited to once per shop");
            StartRun(); wallet=1000; float attackBeforeItem=StatValue(0);
            check(BuyStatItem(0) && BuyStatItem(1) && BuyStatItem(2) && wallet==250,"Three stat items fill the three-slot bag");
            check(!BuyStatItem(3) && wallet==250,"A full bag rejects another stat item without charging");
            check(UseBagItem(0) && Mathf.Abs(StatValue(0)-attackBeforeItem-1)<.001f && bagItems[0]<0,"Using a bag slot grants its run-long stat bonus and empties the slot");
            StartRun(); NextRoom();
            check(phase==Phase.Route && routes.Length==3,"Shop opens exactly three route cards");
            ChooseRoute(); check(phase==Phase.Route,"A route must be selected before entering");
            shopOpen=true; wallet=StatItemPrice; check(BuyStatItem(4) && wallet==0,"The shop remains usable from the route screen"); shopOpen=false;
            yield return CaptureSmoke(folder,"04-first-route");
            routeSelected=1; ChooseRoute();
            check(currentMap==routes[1] && spawnQueue.Count==6,"Chosen map controls the actual encounter roster");
            check(!BuyWeapon(1),"Weapons cannot be bought during combat");

            var target=SmokeArena(EnemyKind.Human);
            Vector2 before=player;
            if(keyboard!=null)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D)); yield return new WaitForSeconds(.16f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                check(player.x>before.x && aim.x>.9f,"D moves and aims right at positive speed");
                stats.Exchange(0,2,5); before=player;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D)); yield return new WaitForSeconds(.16f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                check(player.x<before.x && aim.x>.9f,"Negative speed reverses movement but keeps D attack direction right");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); yield return null; yield return null;
                check(aim.y>.9f,"W sets attack direction upward without the mouse");
                stats=new ZeroStats(); before=player;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                check(player.y>before.y && jumps==1,"Space jumps through the Input System");
            }
            TryJump(false); check(jumps==2 && velocity.y>0,"Second airborne jump is available");
            velocity.y=2; TryJump(false); check(jumps==2 && velocity.y==2,"Third airborne jump is rejected");
            var body=new Vector2(12,2); var motion=Vector2.zero;
            for(int i=0;i<200;i++) MoveBody(ref body,ref motion,HeroHalf,.36f,.02f);
            check(Mathf.Abs(body.y-(Floor+HeroHalf))<.001f && motion.y==0,"Gravity lands on the floor");
            var platform=platforms[0]; body=new Vector2(platform.rect.center.x,platform.rect.yMax+2); motion=Vector2.zero;
            for(int i=0;i<100;i++) MoveBody(ref body,ref motion,HeroHalf,.36f,.02f);
            check(Mathf.Abs(body.y-(platform.rect.yMax+HeroHalf))<.001f,"One-way platform catches falling bodies");
            player=body; velocity=Vector2.zero; MoveBody(ref player,ref velocity,HeroHalf,.36f,.02f,false,true);
            check(platform.collapse>0,"A platform starts its collapse countdown when the player stands on it");
            TickPlatforms(.71f); check(!platform.active && platform.visuals.TrueForAll(v=>!v.enabled),"A stepped-on platform disappears after its warning");
            TickPlatforms(2.76f); check(platform.active && platform.visuals.TrueForAll(v=>v.enabled),"A collapsed platform returns and becomes solid again");
            player=body; velocity=Vector2.zero; TryJump(true); MoveBody(ref player,ref velocity,HeroHalf,.36f,.1f,dropTimer>0);
            check(player.y<body.y,"S plus jump drops through a platform");

            target=SmokeArena(EnemyKind.Human); target.hp=10; HitEnemy(target,-12);
            check(target.hp==22,"Negative attack heals an ordinary enemy");
            HitEnemy(target,-999); check(target.hp==target.maxHp,"Ordinary healing is capped at maximum health");
            target=SmokeArena(EnemyKind.Undead); float initial=target.hp; HitEnemy(target,-12);
            check(target.hp==initial-24,"Undead take twice the negative attack as damage");
            stats.Exchange(0,1,5); hp=100; invulnerable=0; Hurt(10);
            check(hp==84,"Negative defense increases incoming damage");
            stats=new ZeroStats(); stats.Exchange(0,3,5); wallet=10;
            Collect(new Pickup { pos=player,value=2 }); check(wallet==7,"Negative fortune removes money at the reduced rate");
            Collect(new Pickup { pos=player,value=5 }); check(wallet==0,"Wallet cannot become negative");

            target=SmokeArena(EnemyKind.Human); target.hp=42; player=new Vector2(0,Floor+HeroHalf); target.pos=new Vector2(2,player.y); target.root.position=target.pos;
            stats.Exchange(1,0,5); Bomb(); check(enemies.Count==0 && bombTimer==6,"Bomb can kill with negative attack and starts its cooldown");
            target=SmokeArena(EnemyKind.Human); owned.Add(3); Equip(3); aim=Vector2.right; attackTimer=0; Fire();
            check(shots.Count==5 && gunAmmo[3]==9 && Mathf.Abs(shots[0].velocity.magnitude-16)<.01f,"Shotgun fires five pellets while spending one shell");
            int fired=shots.Count; Fire(); check(shots.Count==fired && Mathf.Abs(attackTimer-1/Weapon.rate)<.001f,"Attack interval prevents early repeat fire");
            ClearProjectiles(); owned.Add(7); Equip(7); attackTimer=0; Fire();
            check(shots.Count==1 && gunAmmo[7]==4 && shots[0].pierce==2 && Mathf.Abs(shots[0].velocity.magnitude-44)<.01f,"Sniper spends one of five rounds and retains its speed and penetration");
            ClearProjectiles(); owned.Add(8); Equip(8); gunAmmo[8]=1; gunReload[8]=0; attackTimer=0; Fire();
            check(gunAmmo[8]==0 && Mathf.Abs(gunReload[8]-.75f)<.001f,"Empty double-barrel begins its fast reload automatically");
            TickReloads(.8f); check(gunAmmo[8]==2 && gunReload[8]==0,"Double-barrel reload refills both shells");
            owned.Add(0); Equip(0); gunAmmo[0]=4; gunReload[0]=0;
            if(keyboard!=null)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                check(gunReload[0]>0,"R starts a manual reload through the Input System");
            }
            yield return CaptureSmoke(folder,"13-reloading");
            stats=new ZeroStats(); stats.Exchange(0,2,5); gunAmmo[0]=0; gunReload[0]=ZeroContent.Weapons[0].reloadTime; float pausedReload=gunReload[0];
            TickReloads(1); check(gunReload[0]==pausedReload && gunAmmo[0]==0,"Negative movement speed freezes an active reload");
            gunReload[0]=0; check(!BeginReload(0,false),"Negative movement speed cannot start a reload");
            yield return CaptureSmoke(folder,"14-reload-blocked");
            stats=new ZeroStats(); stats.Exchange(0,2,3); check(BeginReload(0,false),"Zero movement speed can start a reload");
            float zeroSeconds=ReloadSecondsLeft(0); check(zeroSeconds>ZeroContent.Weapons[0].reloadTime*2.9f,"Zero movement speed makes reload three times slower");
            gunReload[0]=ZeroContent.Weapons[0].reloadTime; stats=new ZeroStats(); stats.Exchange(2,1,3); TickReloads(.5f);
            check(Mathf.Abs(gunReload[0]-(ZeroContent.Weapons[0].reloadTime-1f))<.01f,"Higher movement speed accelerates reload progress");
            ClearActors(); SpawnEnemy(EnemyKind.Human,new Vector2(-4,0)); SpawnEnemy(EnemyKind.Human,new Vector2(-2,0));
            CreateShot(new Vector2(-6,Floor+.64f),Vector2.right*44,99,false,ShotKind.Bullet,2);
            for(int i=0;i<15 && enemies.Count>0;i++) TickShots(.02f);
            check(enemies.Count==0,"Swept piercing bullet hits two separate targets");
            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); target.pos=new Vector2(1.2f,player.y); target.hp=10;
            var second=SpawnEnemy(EnemyKind.Goblin,new Vector2(1.6f,0)); second.hp=10; Equip(10); aim=Vector2.right; attackTimer=0; Fire();
            check(enemies.Count==0,"Sword cuts several enemies in its forward arc");

            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); aim=Vector2.right; owned.Add(11); Equip(11);
            target.pos=player+new Vector2(.8f,1); target.root.position=target.pos; float swordHealth=target.hp; Fire();
            check(target.hp==swordHealth,"Dagger thrust does not hit targets outside its narrow line");
            target.pos=player+Vector2.right*1.1f; target.root.position=target.pos; attackTimer=0; Fire();
            check(target.hp<swordHealth,"Dagger thrust hits a close target directly ahead");

            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); target.pos=player+Vector2.right*3.7f; target.root.position=target.pos;
            owned.Add(13); Equip(13); aim=Vector2.right; swordHealth=target.hp; Fire();
            check(target.hp<swordHealth,"Rapier thrust reaches enemies four metres away");

            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); target.pos=player+Vector2.left; target.root.position=target.pos; target.hp=target.maxHp=500;
            owned.Add(14); Equip(14); aim=Vector2.right; swordCombo[4]=0; swordHealth=target.hp;
            for(int i=0;i<3;i++) { attackTimer=0; Fire(); }
            check(target.hp==swordHealth,"Cutlass first three combo attacks stay directed forward");
            attackTimer=0; Fire(); check(target.hp<swordHealth && swordCombo[4]==0,"Cutlass fourth combo attack circles behind and resets the chain");

            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); owned.Add(16); Equip(16); aim=Vector2.right; swordCombo[6]=0;
            attackTimer=0; Fire(); attackTimer=0; Fire(); float greatswordStart=player.x; attackTimer=0; Fire();
            check(player.x>greatswordStart && swordCombo[6]==0,"Greatsword third combo attack spins and advances");

            SmokeArena(EnemyKind.Human); owned.Add(17); Equip(17); aim=Vector2.right; int effectCount=effects.Count; Fire();
            check(effects.Count-effectCount>=45,"Curved sword creates layered arcs, a ring, and sparks");

            SmokeArena(EnemyKind.Human); owned.Add(19); Equip(19); aim=Vector2.right; Fire();
            check(shots.FindAll(s=>s.kind==ShotKind.BlackIron&&!s.hostile).Count==3,"Black iron sword launches three ranged shards with its swing");

            SmokeArena(EnemyKind.Human); ClearActors(); for(int i=0;i<12;i++) spawnQueue.Enqueue(EnemyKind.Orc); spawnTimer=0; TickCombat(.01f);
            check(enemies.Count==4 && spawnQueue.Count==8,"Orcs enter in simultaneous groups of four");
            ClearActors(); for(int i=0;i<18;i++) spawnQueue.Enqueue(EnemyKind.Goblin); spawnTimer=0; TickCombat(.01f);
            check(enemies.Count==6 && spawnQueue.Count==12,"Goblins enter in simultaneous groups of six");
            target=SmokeArena(EnemyKind.Elf); target.timer=0; TickNormal(target,.01f); TickNormal(target,.6f);
            check(shots.Exists(s=>s.hostile && s.kind==ShotKind.Arrow),"Elf actually shoots an arcing hostile arrow");
            player=target.pos+Vector2.left*2; target.casting=false; target.timer=10; TickNormal(target,.01f);
            check(target.velocity.x>0,"Elf retreats when the player is too close");

            SmokeArena(EnemyKind.Medusa,4); player=new Vector2(-4,-2); hp=100; invulnerable=0;
            AddBeam(player-Vector2.right*5,Vector2.right,.25f,10,true); TickHazards(.1f);
            check(hp==100 && petrified<=0,"Beam warning causes no damage or petrification");
            TickHazards(.2f); check(hp==94 && petrified>1,"Active Medusa beam damages and petrifies");
            attackTimer=0; jumps=0; velocity.y=0; int shotCount=shots.Count; Fire(); TryJump(false);
            check(shots.Count==shotCount && velocity.y==0,"Petrification blocks attacks and jumps");
            ClearProjectiles(); invulnerable=999; TickCombat(1.4f); check(petrified<=0,"Petrification expires without requiring input");
            hp=100; shields=1; invulnerable=0; AddBeam(player-Vector2.right*5,Vector2.right,0,10,true); TickHazards(.01f);
            check(hp==100 && petrified<=0 && shields==0,"Shield prevents both beam damage and petrification");

            target=SmokeArena(EnemyKind.Heart,12);
            check(target.invulnerable && target.heartSummons==1 && enemies.FindAll(e=>e.summoner==target).Count==3,"Heart starts at full health with an invulnerable three-minion wave");
            initial=target.hp; HitEnemy(target,100); check(target.hp==initial,"Heart ignores damage while summoned minions live");
            int bloodShots=shots.Count; TickHeartShield(target,.31f);
            check(shots.Count>bloodShots && shots.Exists(s=>s.kind==ShotKind.Blood&&s.gravity>0),"Shielded heart sprays a falling blood bullet barrage");
            for(int i=enemies.Count-1;i>=0;i--) if(enemies[i].summoner==target) HitEnemy(enemies[i],100000);
            TickBoss(target,.01f); check(!target.invulnerable,"Heart loses invulnerability when its summoned wave dies");
            HitEnemy(target,100000);
            var heartMinions=enemies.FindAll(e=>e.summoner==target);
            check(target.invulnerable && target.heartSummons==2 && Mathf.Abs(target.hp-target.maxHp*2/3f)<.01f && heartMinions.Count==5 && heartMinions.TrueForAll(e=>e.strength>=1.35f),"Heart clamps at two-thirds health and summons a larger stronger wave");
            for(int i=enemies.Count-1;i>=0;i--) if(enemies[i].summoner==target) HitEnemy(enemies[i],100000);
            TickBoss(target,.01f); HitEnemy(target,100000); heartMinions=enemies.FindAll(e=>e.summoner==target);
            check(target.invulnerable && target.heartSummons==3 && Mathf.Abs(target.hp-target.maxHp/3f)<.01f && heartMinions.Count==7 && heartMinions.TrueForAll(e=>e.strength>=1.7f),"Heart clamps at one-third health and summons its largest strongest wave");
            for(int i=enemies.Count-1;i>=0;i--) if(enemies[i].summoner==target) HitEnemy(enemies[i],100000);
            TickBoss(target,.01f); HitEnemy(target,100000); check(!enemies.Contains(target),"Heart can die after all three summon waves are defeated");

            target=SmokeArena(EnemyKind.Heart,12); ClearProjectiles(); target.invulnerable=false; target.nextPattern=0; StartBossPattern(target);
            check(hazards.Exists(h=>h.size.x>=29 && h.damage>0),"Heart warns a damaging full-screen horizontal attack");
            ExecuteBossPattern(target); check(shots.FindAll(s=>s.kind==ShotKind.Blood&&s.gravity>0).Count>=12,"Heart horizontal attack throws falling blood from both sides");
            ClearProjectiles(); target.nextPattern=2; StartBossPattern(target); ExecuteBossPattern(target);
            check(hazards.FindAll(h=>h.size.y>10).Count==7 && shots.FindAll(s=>s.kind==ShotKind.Blood&&s.velocity.y<0).Count==7,"Heart vertical attack marks seven columns and rains blood");
            ClearProjectiles(); AddHeartBloodPool(0,12); float poolWidth=hazards.Find(h=>h.blood).size.x; AddHeartBloodPool(.2f,14);
            check(hazards.Exists(h=>h.blood&&h.size.x>poolWidth&&h.life>=45),"Repeated heart attacks accumulate wider long-lived damaging blood pools");

            for(int boss=0;boss<5;boss++)
            {
                var kind=(EnemyKind)((int)EnemyKind.Medusa+boss);
                for(int pattern=0;pattern<BossPatterns[boss].Length;pattern++)
                {
                    target=SmokeArena(kind,(boss+1)*4); target.nextPattern=pattern; StartBossPattern(target); ExecuteBossPattern(target); target.casting=false;
                    check(shots.Count>0 || hazards.Count>0 || enemies.Count>1 || target.flyTime>0,kind+" pattern "+pattern+" creates a gameplay action");
                    if(kind==EnemyKind.Beelzebub && pattern==0)
                    {
                        TickBoss(target,.02f);
                        check(target.body.sprite==art.fly && target.flies.TrueForAll(f=>f.enabled),"Beelzebub transforms into an attacking fly swarm");
                        initial=target.hp; HitEnemy(target,20); check(target.hp==initial-20,"Fly transformation remains damageable");
                        TickBoss(target,3.5f); TickBoss(target,.02f);
                        check(target.body.sprite==art.creatures[(int)kind] && target.flies.TrueForAll(f=>!f.enabled),"Beelzebub returns from the fly swarm");
                    }
                }
                target=SmokeArena(kind,(boss+1)*4); target.nextPattern=0; StartBossPattern(target);
                if(kind==EnemyKind.Angel) check(target.wings.Count==6,"Angel has six eye-covered wings");
                if(kind==EnemyKind.Beelzebub) { ExecuteBossPattern(target); target.casting=false; TickBoss(target,.01f); }
                yield return CaptureSmoke(folder,"boss-"+(boss+1)+"-"+kind);
            }

            for(int kind=0;kind<6;kind++)
            {
                target=SmokeArena((EnemyKind)kind,5); currentMap.theme=(MapTheme)kind; currentMap.name=ZeroContent.Enemies[kind].name+"의 맵"; BuildMap();
                player=new Vector2(-5,Floor+HeroHalf); invulnerable=999;
                if(kind==2 || kind==3) for(int i=0;i<(kind==2?3:5);i++) SpawnEnemy((EnemyKind)kind,new Vector2(3+i*1.2f,0));
                yield return CaptureSmoke(folder,"enemy-"+kind);
            }
            paused=true; Vector2 frozen=player; float time=runTime; yield return new WaitForSeconds(.12f);
            check(player==frozen && runTime==time,"Pause freezes combat and the run timer"); paused=false;

            // The design document adds negative-defense healing and two inversion enemies.
            target=SmokeArena(EnemyKind.Human); stats.Exchange(0,1,5); hp=20;
            HealPlayer(12); check(hp==38,"Defense -3 adds six to received healing");
            hp=98; HealPlayer(40); check(hp==100,"Amplified healing respects maximum health");
            hp=60; HealPlayer(0); check(hp==60,"Zero healing cannot create free recovery");
            stats=new ZeroStats(); stats.Exchange(0,4,3);
            target.hp=target.maxHp; float healthBefore=target.hp; HitEnemy(target,10,true);
            check(target.hp==healthBefore-4,"Negative critical stat reduces ordinary critical damage");
            target=SmokeArena(EnemyKind.Inverter,5); stats.Exchange(0,4,3); healthBefore=target.hp; HitEnemy(target,10,true);
            check(target.hp==healthBefore-16,"Inverter takes increased damage from a negative critical stat");
            stats=new ZeroStats(); stats.Exchange(4,0,3); healthBefore=target.hp; HitEnemy(target,10,true);
            check(target.hp==healthBefore-4,"Inverter weakens positive critical damage");
            target=SmokeArena(EnemyKind.RearGuard,5); target.facing=-1; healthBefore=target.hp;
            HitEnemy(target,10,false,Vector2.right); check(target.hp==healthBefore,"Rear guard blocks frontal weapon hits");
            HitEnemy(target,10,false,Vector2.left); check(target.hp==healthBefore-10,"Rear guard takes damage from behind");
            player=target.pos+Vector2.left; target.timer=0; TickNormal(target,.01f); target.turnTimer=0; float facingBefore=target.facing; player=target.pos+Vector2.right;
            TickNormal(target,.1f); check(target.facing==facingBefore,"Rear guard commits its facing during attack windup");
            target.casting=false; TickNormal(target,.01f); check(target.facing==1,"Rear guard turns after its commitment");
            yield return CaptureSmoke(folder,"11-rear-guard");
            target=SmokeArena(EnemyKind.Inverter,5); yield return CaptureSmoke(folder,"12-inverter");

            stats=new ZeroStats(); stats.Exchange(0,1,3); int sum=stats.Total; stats.Swap(0,1);
            check(stats[0]==-1 && stats[1]==6 && stats.Total==sum,"Swap card exchanges exact values and preserves the sum");
            stats=new ZeroStats(); stats.Exchange(0,1,9); check(stats.Total==10 && stats[0]==12 && stats[1]==-7,"Extreme exchange supports much larger zero-sum changes");
            target=SmokeArena(EnemyKind.Human,2); pendingInvert=2; BeginRoom();
            check(activeInvert==2 && CurrentMoveSpeed<0,"Invert card negates the chosen stat for the next combat");
            ClearActors(); BeginRoomClear();
            check(phase==Phase.RoomClear && !roomClearRevealed,"Clearing an encounter keeps the combat scene before the result appears");
            Vector2 clearStart=player; TickRoomClearMovement(.1f,1,false,false,false);
            check(Mathf.Abs(player.x-clearStart.x)>.01f,"The player can keep moving during the clear countdown");
            int clearWallet=wallet; var clearCoinSprite=Sprite("Clear Coin",art.coin,Color.white,player,Vector2.one*.7f,15);
            coins.Add(new Pickup { root=clearCoinSprite.transform,pos=player,value=5 }); TickRoomClearMovement(.01f,0,false,false,false);
            check(coins.Count==0 && wallet==clearWallet+5,"A final-enemy coin remains collectible during the clear countdown");
            TickRoomClear(RoomClearRevealDelay*.5f); check(!roomClearRevealed && phase==Phase.RoomClear,"Clear result waits before appearing");
            TickRoomClear(RoomClearRevealDelay*.6f); check(roomClearRevealed && phase==Phase.RoomClear,"Clear result appears while the combat scene remains");
            TickRoomClear(RoomClearHoldDuration); check(phase==Phase.Camp && activeInvert==-1,"Clear result advances after its hold and expires round inversion");

            StartRun(); int bosses=0;
            for(int n=1;n<=LastRoom;n++)
            {
                NextRoom(); check(phase==Phase.Route && routes.Length==(ZeroContent.IsBossRoom(n)?1:3),"Map "+n+" presents the correct route count");
                if(n==6 || n==8) yield return CaptureSmoke(folder,n==6?"05-mixed-route":"06-boss-route");
                routeSelected=n%routes.Length; ChooseRoute();
                if(n>1)
                {
                    check(phase==Phase.Trade && pendingTrade,"Map "+n+" reveals enemies before requiring its exchange");
                    MapOffer lockedMap=currentMap; NextRoom(); ChooseRoute(); ApplyTrade();
                    check(phase==Phase.Trade && currentMap==lockedMap,"Unselected exchange cannot be skipped or reroll the route");
                    if(n==2) { shopOpen=true; wallet+=StatItemPrice; check(BuyStatItem(3),"The shop remains usable from the stat-exchange screen"); shopOpen=false; }
                    if(n==2) yield return CaptureSmoke(folder,"07-trade");
                    selected=0; int total=stats.Total; ApplyTrade();
                    check(stats.Total==total && !pendingTrade && phase==Phase.Combat,"Exchange preserves all five stats and enters the selected map");
                }
                int expected=currentMap.Total;
                bool heartRoom=ZeroContent.IsBossRoom(n) && ZeroContent.BossForRoom(n)==EnemyKind.Heart;
                check(enemies.Count+spawnQueue.Count==expected+(heartRoom?3:0),"Map "+n+" matches its card and opening summon count");
                if(ZeroContent.IsBossRoom(n)) { bosses++; check(enemies.FindAll(e=>e.kind==ZeroContent.BossForRoom(n)).Count==1,"Correct boss for map "+n); }
                while(spawnQueue.Count>0) SpawnEnemy(spawnQueue.Dequeue(),new Vector2(7,0));
                if(heartRoom)
                {
                    var heart=enemies.Find(e=>e.kind==EnemyKind.Heart);
                    for(int wave=0;wave<3;wave++)
                    {
                        for(int i=enemies.Count-1;i>=0;i--) if(enemies[i].summoner==heart) HitEnemy(enemies[i],100000);
                        TickBoss(heart,.01f); HitEnemy(heart,100000);
                    }
                }
                else for(int i=enemies.Count-1;i>=0;i--) HitEnemy(enemies[i],100000);
                if(n==1) { stats.Exchange(0,3,4); wallet=1000; }
                int beforeWallet=wallet, coinValue=0; foreach(var c in coins) coinValue+=c.value;
                ClearRoom();
                check(completedRooms==n && pendingTrade,"Cleared map "+n+" owes exactly one exchange");
                if(n==1) check(wallet==Mathf.Max(0,beforeWallet+Mathf.RoundToInt(coinValue*ZeroStats.CoinRate(stats[3]))),"Clear auto-collection applies reduced negative fortune to all remaining coins");
                if(n<LastRoom)
                {
                    check(phase==Phase.Camp,"Cleared map "+n+" opens the shelter before the next route");
                    int total=stats.Total; selected=0; ApplyTrade();
                    check(phase==Phase.Camp && stats.Total==total,"No stat exchange before seeing the next encounter");
                }
                else
                {
                    check(phase==Phase.Trade,"Final clear still requires its last exchange");
                    selected=0; int total=stats.Total; ApplyTrade();
                    check(phase==Phase.Victory && stats.Total==total && !pendingTrade,"Final exchange returns from the dungeon");
                }
            }
            check(bosses==5 && completedRooms==20 && phase==Phase.Victory,"Twenty-map campaign contains five bosses and ends after the final exchange");
            yield return CaptureSmoke(folder,"08-victory");
            SmokeArena(EnemyKind.Human); completedRooms=8; hp=1; invulnerable=0; Hurt(50); check(phase==Phase.Dead,"Lethal damage opens the death screen");
            check(deathReward==3 && legacyPoints==3 && totalDeaths==1,"Death grants persistent legacy currency from progress");
            float beforeMeta=StatValue(0); check(UpgradeMeta(0) && legacyPoints==2 && Mathf.Abs(StatValue(0)-beforeMeta-.1f)<.001f,"One legacy point permanently raises a base stat by 0.1");
            yield return CaptureSmoke(folder,"09-death");
            ReturnToTitle(); check(phase==Phase.Title && Mathf.Abs(StatValue(0)-beforeMeta-.1f)<.001f,"Returning home retains permanent growth");
            StartRun(); check(hp==100 && wallet==ZeroContent.StartingGold && stats.Total==10 && Mathf.Abs(StatValue(0)-3.1f)<.001f && owned.Count==2 && activeWeapon==10 && completedRooms==0 && petrified==0 && System.Array.TrueForAll(bagItems,item=>item<0),"Restart resets equipment, bag, and run bonuses while retaining permanent growth");
            help=true; yield return CaptureSmoke(folder,"10-controls");
            Application.logMessageReceived -= captureError;
            File.WriteAllLines(Path.Combine(folder,"runtime-checks.txt"),checks);
            File.WriteAllText(Path.Combine(folder,"runtime-result.txt"),errors.Count==0?"PASS: "+checks.Count+" runtime checks; no Unity errors.":"FAIL\n"+string.Join("\n",errors));
            Application.Quit(errors.Count==0?0:1);
        }

        Enemy SmokeArena(EnemyKind kind,int map=1)
        {
            ClearActors(); pendingTrade=false; shopOpen=false; phase=Phase.Combat; paused=help=false; room=map; stats=new ZeroStats(); hp=100; shields=0;
            Array.Clear(runStatBonuses,0,runStatBonuses.Length); for(int i=0;i<bagItems.Length;i++) bagItems[i]=-1;
            currentMap=ZeroContent.CreateOffers(map,new System.Random(12))[0]; BuildMap();
            player=new Vector2(-8,Floor+HeroHalf); velocity=Vector2.zero; aim=Vector2.right; jumps=0; grounded=true;
            petrified=dashTime=dropTimer=attackTimer=bombTimer=0; invulnerable=999; spawnTimer=99; roomBanner=0; noticeTime=0;
            activeWeapon=gunSlot=0; swordSlot=10; owned.Add(0); owned.Add(10); bossNotice="";
            Array.Clear(swordCombo,0,swordCombo.Length); swordAnimTime=swordAnimDuration=0; swordMotion=SwordMotion.None;
            for(int i=0;i<gunAmmo.Length;i++) { gunAmmo[i]=ZeroContent.Weapons[i].magazine; gunReload[i]=0; }
            var target=SpawnEnemy(kind,new Vector2(6,0)); target.timer=99; return target;
        }
        IEnumerator CaptureSmoke(string folder,string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
            yield return new WaitForSeconds(.25f);
        }
    }
}
