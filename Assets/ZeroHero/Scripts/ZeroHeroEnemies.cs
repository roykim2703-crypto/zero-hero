using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        Enemy SpawnEnemy(EnemyKind kind,Vector2 pos)
        {
            var d=ZeroContent.Enemies[(int)kind];
            var e=new Enemy { kind=kind,pos=pos,maxHp=d.health*(d.Boss?1:1+Act*.22f),timer=Range(.6f,1.7f),jumpTimer=1 };
            e.hp=e.maxHp; e.pos.y=e.Flying?(kind==EnemyKind.Angel?1.3f:.3f):Floor+d.size*.55f;
            e.root=new GameObject(d.name).transform; e.root.SetParent(world); e.root.position=e.pos;
            Sprite("Shadow",art.disc,new Color(0,0,0,.3f),new Vector2(0,-d.size*.5f),new Vector2(d.size,.16f),10,e.root);
            e.body=Sprite(d.name,art.creatures[(int)kind],Color.white,Vector2.zero,Vector2.one*d.size,20,e.root);
            Sprite("Health track",art.square,C("191E19"),new Vector2(0,d.size*.67f),new Vector2(d.size,.075f),24,e.root);
            e.health=Sprite("Health",art.square,d.Boss?ZeroHeroArt.Gold:ZeroHeroArt.Pink,new Vector2(0,d.size*.67f),new Vector2(d.size,.075f),25,e.root);
            if(kind==EnemyKind.Elf)
            {
                Sprite("Bow",art.ring,C("B39760"),new Vector2(.5f,.1f),new Vector2(.4f,.9f),22,e.root);
                Sprite("Bow string",art.square,C("D8D0B5"),new Vector2(.5f,.1f),new Vector2(.035f,.8f),23,e.root);
            }
            if(kind==EnemyKind.Angel)
                for(int side=-1;side<=1;side+=2) for(int j=0;j<3;j++)
                {
                    var w=Sprite("Eye-covered wing",art.wing,C("E5D7AE"),new Vector2(side*1.9f,1.2f-j*1.1f),new Vector2(side*2.2f,1.5f),18,e.root);
                    e.wings.Add(w.transform);
                    for(int n=0;n<3;n++)
                    {
                        var eye=Sprite("Wing eye",art.disc,C("F0E6C8"),new Vector2(-.22f+n*.22f,.02f),new Vector2(.08f,.11f),21,w.transform);
                        Sprite("Pupil",art.disc,C("4A3E24"),Vector2.zero,Vector2.one*.42f,22,eye.transform);
                    }
                }
            if(kind==EnemyKind.Beelzebub)
                for(int i=0;i<9;i++) { var f=Sprite("Swarm fly",art.fly,Color.white,Vector2.zero,Vector2.one*.95f,23,e.root); f.enabled=false; e.flies.Add(f); }
            enemies.Add(e); AddEffect(art.ring,e.pos,ZeroHeroArt.Pink,.5f,.2f,Vector2.zero,2); return e;
        }

        void TickEnemies(float dt)
        {
            for(int i=enemies.Count-1;i>=0;i--)
            {
                var e=enemies[i]; e.timer-=dt; e.flash-=dt; e.jumpTimer-=dt;
                if(e.Def.Boss) TickBoss(e,dt); else TickNormal(e,dt);
                e.root.position=e.pos;
                e.body.flipX=player.x<e.pos.x;
                e.body.color=e.flash>0?ZeroHeroArt.Gain:e.casting?Color.Lerp(Color.white,ZeroHeroArt.Gold,.38f):Color.white;
                float pulse=e.kind==EnemyKind.Heart?1+Mathf.Sin(visualTime*5.8f)*.08f:1;
                e.body.transform.localScale=Vector3.one*(e.flyTime>0?.6f:e.Def.size*pulse);
                e.health.transform.localScale=new Vector3(e.Def.size*e.hp/e.maxHp,.075f,1);
                for(int j=0;j<e.wings.Count;j++) e.wings[j].localRotation=Quaternion.Euler(0,0,Mathf.Sin(visualTime*3+j*.4f)*9);
                if(phase!=Phase.Combat) break;
            }
        }
        void TickNormal(Enemy e,float dt)
        {
            float dx=player.x-e.pos.x, distance=Mathf.Abs(dx); float speed=e.Def.speed;
            e.velocity.x=Mathf.Sign(dx)*speed;
            if(e.kind==EnemyKind.Elf) e.velocity.x=distance<5?-Mathf.Sign(dx)*speed:distance>8?Mathf.Sign(dx)*speed:0;
            if(e.casting)
            {
                e.velocity.x=0; e.windup-=dt;
                if(e.windup<=0)
                {
                    if(e.kind==EnemyKind.Elf)
                    {
                        Vector2 delta=player-e.pos; float flight=Mathf.Max(.2f,delta.magnitude/10);
                        CreateShot(e.pos,delta/flight+Vector2.up*2*flight,e.Def.damage,true,ShotKind.Arrow,1,4);
                    }
                    else if(e.kind==EnemyKind.Giant)
                    {
                        shake=.18f;
                        AddHazard(new Vector2(e.pos.x+Mathf.Sign(dx)*1.5f,Floor+.6f),new Vector2(3.4f,1.2f),0,.25f,e.Def.damage,ZeroHeroArt.Pink);
                        CreateShot(new Vector2(e.pos.x,Floor+.35f),new Vector2(Mathf.Sign(dx)*5,0),e.Def.damage/2,true,ShotKind.Blood);
                    }
                    else if(Vector2.Distance(e.pos,player)<1.65f) Hurt(e.Def.damage);
                    e.casting=false; e.timer=e.kind==EnemyKind.Elf?1.9f:e.kind==EnemyKind.Giant?2.6f:1.15f;
                }
            }
            else if(e.timer<=0 && (e.kind==EnemyKind.Elf || distance<(e.kind==EnemyKind.Giant?3.1f:1.4f)) && Mathf.Abs(player.y-e.pos.y)<(e.kind==EnemyKind.Elf?12:3))
            {
                e.casting=true; e.windup=e.kind==EnemyKind.Giant?1.05f:e.kind==EnemyKind.Elf?.55f:.38f;
                if(e.kind==EnemyKind.Giant) AddHazard(new Vector2(e.pos.x+Mathf.Sign(dx)*1.5f,Floor+.1f),new Vector2(3.4f,.12f),1.05f,.01f,0,ZeroHeroArt.Gold);
            }
            if(e.kind!=EnemyKind.Giant && e.jumpTimer<=0 && player.y>e.pos.y+1.2f && distance<7)
            { e.velocity.y=13; e.jumpTimer=1.7f; }
            MoveBody(ref e.pos,ref e.velocity,e.Def.size*.55f,e.Radius*.75f,dt);
        }

        static readonly string[][] BossPatterns = {
            new[] { "석화 광선 — 빛줄기에서 벗어나세요", "뱀 투척 — 점프로 피하세요", "독 웅덩이 — 표시된 바닥에서 이동", "낙석 — 낙하 위치를 확인하세요" },
            new[] { "심판 기둥 — 기둥 사이로 이동", "눈의 탄막 — 빈틈을 통과하세요", "날개 돌진 — 높이를 바꾸세요", "깃털 비 — 발판 아래도 안전하지 않습니다" },
            new[] { "박동 — 지면 충격파를 넘으세요", "피 분사 — 퍼지는 탄환에 주의", "혈전 소환 — 언데드 3마리", "출혈 지대 — 붉은 바닥에서 벗어나세요" },
            new[] { "파리 무리 — 변신 중에도 공격 가능합니다", "부패탄 — 추적하는 파리", "독액 — 착탄 위치를 피하세요", "파리 돌진 — 위아래로 회피" },
            new[] { "순간이동 베기 — 뒤쪽을 확인하세요", "불기둥 — 표시된 자리를 피하세요", "군단 소환 — 오크와 고블린", "운석 — 이동을 멈추지 마세요", "화염파 — 점프 높이를 조절하세요" }
        };

        void TickBoss(Enemy e,float dt)
        {
            if(e.flyTime>0)
            {
                e.flyTime-=dt;
                Vector2 target=player+Vector2.up*.4f;
                e.pos=Vector2.MoveTowards(e.pos,target,3.5f*dt);
                e.body.sprite=art.fly;
                for(int i=0;i<e.flies.Count;i++)
                {
                    float angle=visualTime*7+i*Mathf.PI*2/e.flies.Count;
                    Vector2 offset=new Vector2(Mathf.Cos(angle)*2.0f,Mathf.Sin(angle)*1.4f);
                    e.flies[i].enabled=true; e.flies[i].transform.localPosition=offset;
                    if(Vector2.Distance(player,e.pos+offset)<.7f) Hurt(18);
                }
            }
            else
            {
                e.body.sprite=art.creatures[(int)e.kind]; foreach(var f in e.flies) f.enabled=false;
                if(e.Flying && !e.casting && e.kind!=EnemyKind.Heart)
                    e.pos.y=Mathf.MoveTowards(e.pos.y,(e.kind==EnemyKind.Angel?1.3f:.5f)+Mathf.Sin(roomTime*.8f)*.7f,dt);
                if(!e.Flying && !e.casting)
                {
                    e.velocity.x=Mathf.Abs(player.x-e.pos.x)>5?Mathf.Sign(player.x-e.pos.x)*e.Def.speed:0;
                    MoveBody(ref e.pos,ref e.velocity,e.Def.size*.55f,e.Radius*.7f,dt);
                }
                if(e.casting)
                {
                    e.windup-=dt;
                    if(e.windup<=0) { ExecuteBossPattern(e); e.casting=false; e.timer=e.hp<e.maxHp*.5f?.8f:1.45f; }
                }
                else if(e.timer<=0) StartBossPattern(e);
            }
            e.pos.x=Mathf.Clamp(e.pos.x,Left+2,Right-2); e.pos.y=Mathf.Clamp(e.pos.y,Floor+e.Radius,4);
        }

        void StartBossPattern(Enemy e)
        {
            int boss=(int)e.kind-(int)EnemyKind.Medusa;
            e.pattern=e.nextPattern++%BossPatterns[boss].Length; e.casting=true; e.windup=1.05f; e.target=player;
            bossNotice=BossPatterns[boss][e.pattern];
            switch(e.kind)
            {
                case EnemyKind.Medusa:
                    if(e.pattern==0) AddBeam(e.pos+Vector2.up*.45f,(player-e.pos).normalized,1.05f,10,true);
                    if(e.pattern==2) GroundPools(e.target.x,3,1.05f,4,13,C("8CA66E"));
                    if(e.pattern==3) MarkMeteors(e.target.x,3,1.05f,24);
                    break;
                case EnemyKind.Angel:
                    if(e.pattern==0) Pillars(e.target.x,5,1.05f,22,ZeroHeroArt.Gold);
                    if(e.pattern==2) AddBeam(e.pos,(player-e.pos).normalized,1.05f,26);
                    break;
                case EnemyKind.Heart:
                    if(e.pattern==0) AddHazard(new Vector2(e.pos.x,Floor+.1f),new Vector2(5,.12f),1.05f,.01f,0,ZeroHeroArt.Pink);
                    if(e.pattern==3) GroundPools(e.target.x,4,1.05f,3.5f,15,ZeroHeroArt.Pink);
                    break;
                case EnemyKind.Beelzebub:
                    if(e.pattern==2) GroundPools(e.target.x,3,1.05f,4,15,C("8A9C54"));
                    if(e.pattern==3) AddHazard(new Vector2(0,e.target.y),new Vector2(29,.2f),1.05f,.02f,0,ZeroHeroArt.Gold);
                    break;
                case EnemyKind.DemonKing:
                    if(e.pattern==0)
                    {
                        e.pos=new Vector2(Mathf.Clamp(player.x+(player.x<0?3.8f:-3.8f),Left+2,Right-2),Floor+e.Def.size*.55f);
                        AddEffect(art.ring,e.pos,ZeroHeroArt.Pink,.6f,.5f,Vector2.zero,6);
                        float direction=Mathf.Sign(player.x-e.pos.x);
                        AddHazard(e.pos+new Vector2(direction*2,0),new Vector2(4.2f,3.1f),1.05f,.32f,34,ZeroHeroArt.Pink);
                    }
                    if(e.pattern==1) Pillars(e.target.x,4,1.05f,28,ZeroHeroArt.Pink);
                    if(e.pattern==3) MarkMeteors(e.target.x,5,1.05f,29);
                    break;
            }
        }

        void ExecuteBossPattern(Enemy e)
        {
            switch(e.kind)
            {
                case EnemyKind.Medusa:
                    if(e.pattern==1)
                        for(int i=0;i<8;i++) { float a=i*Mathf.PI/7; CreateShot(e.pos,new Vector2(Mathf.Cos(a)*7,Mathf.Sin(a)*5+1),17,true,ShotKind.Snake,1,9); }
                    break;
                case EnemyKind.Angel:
                    if(e.pattern==1) RingShots(e.pos,18,4.5f,18,ShotKind.Bullet);
                    if(e.pattern==2) e.pos=new Vector2(Mathf.Clamp(e.target.x+(e.target.x<0?4:-4),Left+2,Right-2),Mathf.Clamp(e.target.y+1,-2,3));
                    if(e.pattern==3)
                        for(int i=0;i<11;i++) CreateShot(new Vector2(-13+i*2.6f,5.4f),new Vector2((i%2==0?1:-1)*1.1f,-4.8f),19,true,ShotKind.Arrow);
                    break;
                case EnemyKind.Heart:
                    if(e.pattern==0)
                        for(int sign=-1;sign<=1;sign+=2) { CreateShot(new Vector2(e.pos.x,Floor+.35f),new Vector2(sign*7,0),25,true,ShotKind.Blood); CreateShot(new Vector2(e.pos.x,Floor+.6f),new Vector2(sign*5,0),21,true,ShotKind.Blood); }
                    if(e.pattern==1) FanShots(e,9,6,18,ShotKind.Blood,.16f);
                    if(e.pattern==2)
                    {
                        for(int i=0;i<3;i++) SpawnEnemy(EnemyKind.Undead,new Vector2(e.pos.x-2+i*2,Floor));
                        e.hp=Mathf.Min(e.maxHp,e.hp+18); Float(e.pos,"+18",ZeroHeroArt.Gain);
                    }
                    break;
                case EnemyKind.Beelzebub:
                    if(e.pattern==0) { e.flyTime=3.2f; AddEffect(art.ring,e.pos,C("9AA16C"),.5f,1,Vector2.zero,6); }
                    if(e.pattern==1) FanShots(e,7,4.5f,18,ShotKind.Fly,.24f);
                    if(e.pattern==3)
                        for(int i=0;i<5;i++) { CreateShot(new Vector2(Left,e.target.y+i*.28f),Vector2.right*9,18,true,ShotKind.Fly); CreateShot(new Vector2(Right,e.target.y+1.7f+i*.28f),Vector2.left*9,18,true,ShotKind.Fly); }
                    break;
                case EnemyKind.DemonKing:
                    if(e.pattern==2) { for(int i=0;i<4;i++) SpawnEnemy(i<2?EnemyKind.Orc:EnemyKind.Goblin,new Vector2(i%2==0?-12:12,Floor)); }
                    if(e.pattern==4)
                        for(int side=-1;side<=1;side+=2) for(int i=0;i<3;i++) CreateShot(e.pos+Vector2.up*(i*.85f-1),new Vector2(side*(6+i),0),23,true,ShotKind.Flame);
                    break;
            }
            PlaySound(2);
        }

        void RingShots(Vector2 p,int count,float speed,int damage,ShotKind kind)
        { for(int i=0;i<count;i++) { float a=roomTime*.35f+i*Mathf.PI*2/count; CreateShot(p,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed,damage,true,kind); } }
        void FanShots(Enemy e,int count,float speed,int damage,ShotKind kind,float spread)
        {
            float angle=Mathf.Atan2(e.target.y-e.pos.y,e.target.x-e.pos.x);
            for(int i=0;i<count;i++) { float a=angle+(i-(count-1)*.5f)*spread; CreateShot(e.pos,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed,damage,true,kind); }
        }
        void GroundPools(float x,int count,float warning,float life,int damage,Color color)
        { for(int i=0;i<count;i++) AddHazard(new Vector2(Mathf.Clamp(x+(i-(count-1)*.5f)*3,Left+1,Right-1),Floor+.12f),new Vector2(2.0f,.3f),warning,life,damage,color); }
        void Pillars(float x,int count,float warning,int damage,Color color)
        { for(int i=0;i<count;i++) AddHazard(new Vector2(Mathf.Clamp(x+(i-(count-1)*.5f)*4.0f,Left+1,Right-1),.1f),new Vector2(1.1f,10.5f),warning,.55f,damage,color); }
        void MarkMeteors(float x,int count,float warning,int damage)
        {
            for(int i=0;i<count;i++)
            {
                float targetX=Mathf.Clamp(x+(i-(count-1)*.5f)*3.0f,Left+1,Right-1);
                AddHazard(new Vector2(targetX,Floor+.04f),new Vector2(1.4f,.1f),warning,.05f,0,ZeroHeroArt.Gold);
                // Falling rocks take the warning interval to reach the playable space.
                CreateShot(new Vector2(targetX,6.5f),new Vector2(0,-3),damage,true,ShotKind.Meteor,1,4);
            }
        }
    }
}
