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
            check(wallet==90 && owned.Count==2 && activeWeapon==0,"Starting money and both basic weapons");
            check(BuyWeapon(1) && wallet==45 && gunSlot==1,"Purchase deducts the price and equips the gun");
            check(BuyWeapon(1) && wallet==45,"Equipping an owned weapon does not charge again");
            check(!BuyWeapon(9) && wallet==45,"Insufficient funds reject a purchase");
            wallet=10000;
            foreach(var w in ZeroContent.Weapons) check(BuyWeapon(w.id),"Weapon can be purchased: "+w.name);
            check(owned.Count==20 && stats.Total==10,"All twenty weapons are owned without changing stat total");
            StartRun(); yield return CaptureSmoke(folder,"02-shop-guns");
            shopTab=1; yield return CaptureSmoke(folder,"03-shop-swords");
            hp=40; check(BuySupply(false) && hp==80 && !BuySupply(false),"Potion heals once per shop");
            check(BuySupply(true) && shields==1 && !BuySupply(true),"Shield is limited to once per shop");
            StartRun(); NextRoom();
            check(phase==Phase.Route && routes.Length==3,"Shop opens exactly three route cards");
            ChooseRoute(); check(phase==Phase.Route,"A route must be selected before entering");
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
                check(player.x>before.x,"D moves right at positive speed");
                stats.Exchange(0,2,5); before=player;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D)); yield return new WaitForSeconds(.16f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                check(player.x<before.x,"D moves left at negative speed");
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
            var platform=platforms[0]; body=new Vector2(platform.center.x,platform.yMax+2); motion=Vector2.zero;
            for(int i=0;i<100;i++) MoveBody(ref body,ref motion,HeroHalf,.36f,.02f);
            check(Mathf.Abs(body.y-(platform.yMax+HeroHalf))<.001f,"One-way platform catches falling bodies");
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
            Collect(new Pickup { pos=player,value=2 }); check(wallet==4,"Negative fortune removes money");
            Collect(new Pickup { pos=player,value=5 }); check(wallet==0,"Wallet cannot become negative");

            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); target.pos=new Vector2(2,player.y); target.root.position=target.pos;
            stats.Exchange(1,0,5); Bomb(); check(enemies.Count==0 && bombTimer==6,"Bomb can kill with negative attack and starts its cooldown");
            target=SmokeArena(EnemyKind.Human); owned.Add(3); Equip(3); aim=Vector2.right; attackTimer=0; Fire();
            check(shots.Count==5 && Mathf.Abs(shots[0].velocity.magnitude-16)<.01f,"Shotgun fires five pellets at its own bullet speed");
            int fired=shots.Count; Fire(); check(shots.Count==fired && Mathf.Abs(attackTimer-1/Weapon.rate)<.001f,"Attack interval prevents early repeat fire");
            ClearProjectiles(); owned.Add(7); Equip(7); attackTimer=0; Fire();
            check(shots.Count==1 && shots[0].pierce==2 && Mathf.Abs(shots[0].velocity.magnitude-44)<.01f,"Sniper has its own speed and penetration");
            ClearActors(); SpawnEnemy(EnemyKind.Human,new Vector2(-4,0)); SpawnEnemy(EnemyKind.Human,new Vector2(-2,0));
            CreateShot(new Vector2(-6,Floor+.64f),Vector2.right*44,99,false,ShotKind.Bullet,2);
            for(int i=0;i<15 && enemies.Count>0;i++) TickShots(.02f);
            check(enemies.Count==0,"Swept piercing bullet hits two separate targets");
            target=SmokeArena(EnemyKind.Human); player=new Vector2(0,Floor+HeroHalf); target.pos=new Vector2(1.2f,player.y); target.hp=10;
            var second=SpawnEnemy(EnemyKind.Goblin,new Vector2(1.6f,0)); second.hp=10; Equip(10); aim=Vector2.right; attackTimer=0; Fire();
            check(enemies.Count==0,"Sword cuts several enemies in its forward arc");

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
                    if(kind==EnemyKind.Heart && pattern==2) check(enemies.FindAll(e=>e.kind==EnemyKind.Undead).Count==3,"Heart summons three undead");
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

            StartRun(); int bosses=0;
            for(int n=1;n<=LastRoom;n++)
            {
                NextRoom(); check(phase==Phase.Route && routes.Length==3,"Map "+n+" presents three routes");
                if(n==6 || n==8) yield return CaptureSmoke(folder,n==6?"05-mixed-route":"06-boss-route");
                routeSelected=n%3; ChooseRoute(); int expected=currentMap.Total;
                check(enemies.Count+spawnQueue.Count==expected,"Map "+n+" matches its card's enemy count");
                if(ZeroContent.IsBossRoom(n)) { bosses++; check(enemies.Count==1 && enemies[0].kind==ZeroContent.BossForRoom(n),"Correct boss for map "+n); }
                while(spawnQueue.Count>0) SpawnEnemy(spawnQueue.Dequeue(),new Vector2(7,0));
                for(int i=enemies.Count-1;i>=0;i--) HitEnemy(enemies[i],100000);
                if(n==1) { stats.Exchange(0,3,4); wallet=1000; }
                int beforeWallet=wallet, coinValue=0; foreach(var c in coins) coinValue+=c.value;
                ClearRoom(); check(phase==Phase.Trade && completedRooms==n,"Map "+n+" requires an exchange after clearing");
                if(n==1) check(wallet==Mathf.Max(0,beforeWallet+coinValue*stats[3]),"Clear auto-collection applies negative fortune to all remaining coins");
                NextRoom(); check(phase==Phase.Trade,"Exchange cannot be skipped on map "+n);
                if(n==1) yield return CaptureSmoke(folder,"07-trade");
                selected=0; int total=stats.Total; ApplyTrade();
                check(stats.Total==total && phase==(n==LastRoom?Phase.Victory:Phase.Camp),"Map "+n+" preserves stat total and advances correctly");
            }
            check(bosses==5 && completedRooms==20 && phase==Phase.Victory,"Twenty-map campaign contains five bosses and ends after the final exchange");
            yield return CaptureSmoke(folder,"08-victory");
            SmokeArena(EnemyKind.Human); hp=1; invulnerable=0; Hurt(50); check(phase==Phase.Dead,"Lethal damage opens the death screen");
            yield return CaptureSmoke(folder,"09-death");
            StartRun(); check(hp==100 && wallet==90 && stats.Total==10 && owned.Count==2 && completedRooms==0 && petrified==0,"Restart resets health, stats, inventory, money and progress");
            help=true; yield return CaptureSmoke(folder,"10-controls");
            Application.logMessageReceived -= captureError;
            File.WriteAllLines(Path.Combine(folder,"runtime-checks.txt"),checks);
            File.WriteAllText(Path.Combine(folder,"runtime-result.txt"),errors.Count==0?"PASS: "+checks.Count+" runtime checks; no Unity errors.":"FAIL\n"+string.Join("\n",errors));
            Application.Quit(errors.Count==0?0:1);
        }

        Enemy SmokeArena(EnemyKind kind,int map=1)
        {
            ClearActors(); phase=Phase.Combat; paused=help=false; room=map; stats=new ZeroStats(); hp=100; shields=0;
            currentMap=ZeroContent.CreateOffers(map,new System.Random(12))[0]; BuildMap();
            player=new Vector2(-8,Floor+HeroHalf); velocity=Vector2.zero; aim=Vector2.right; jumps=0; grounded=true;
            petrified=dashTime=dropTimer=attackTimer=bombTimer=0; invulnerable=999; spawnTimer=99; roomBanner=0; noticeTime=0;
            activeWeapon=gunSlot=0; swordSlot=10; owned.Add(0); owned.Add(10); bossNotice="";
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
