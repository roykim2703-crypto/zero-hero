using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        void StartHeartWave(Enemy heart)
        {
            if(heart==null || heart.kind!=EnemyKind.Heart || heart.heartSummons>=3) return;
            int wave=heart.heartSummons;
            int count=3+wave*2;
            float strength=1+wave*.35f;
            heart.heartSummons++;
            heart.invulnerable=true; heart.casting=false; heart.timer=99; heart.heartBarrageTimer=.3f;
            bossNotice="혈육 소환 "+heart.heartSummons+"/3 — 모두 처치해야 심장을 공격할 수 있습니다";
            AddEffect(art.ring,heart.pos,ZeroHeroArt.Pink,.8f,.7f,Vector2.zero,8);
            for(int i=0;i<count;i++)
            {
                EnemyKind kind;
                if(wave==0) kind=EnemyKind.Undead;
                else if(wave==1) kind=i%3==0?EnemyKind.Orc:i%3==1?EnemyKind.Goblin:EnemyKind.Undead;
                else
                {
                    EnemyKind[] kinds={ EnemyKind.Undead,EnemyKind.Orc,EnemyKind.Goblin,EnemyKind.RearGuard,EnemyKind.Inverter };
                    kind=kinds[i%kinds.Length];
                }
                float x=Mathf.Lerp(Left+2,Right-2,(i+1f)/(count+1));
                if(Mathf.Abs(x-heart.pos.x)<1.4f) x=Mathf.Clamp(x-3,Left+1,Right-1);
                Enemy minion=SpawnEnemy(kind,new Vector2(x,Floor),strength,heart);
                minion.timer=.45f+i*.1f;
            }
            PlaySound(2);
        }

        bool TickHeartShield(Enemy heart,float dt)
        {
            if(!heart.invulnerable) return false;
            bool minionAlive=false;
            foreach(var enemy in enemies) if(enemy.summoner==heart) { minionAlive=true; break; }
            if(!minionAlive)
            {
                heart.invulnerable=false; heart.timer=.65f; heart.heartBarrageTimer=0;
                bossNotice="혈육 전멸 — 심장의 무적이 해제되었습니다";
                AddEffect(art.ring,heart.pos,C("D56A78"),.55f,1.1f,Vector2.zero,5);
                return false;
            }
            heart.casting=false; heart.velocity=Vector2.zero; heart.heartBarrageTimer-=dt;
            if(heart.heartBarrageTimer<=0)
            {
                int count=8+heart.heartSummons*2;
                float speed=4.2f+heart.heartSummons*.45f;
                int damage=13+heart.heartSummons*2;
                float phase=roomTime*.75f;
                for(int i=0;i<count;i++)
                {
                    float angle=phase+i*Mathf.PI*2/count;
                    Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    CreateShot(heart.pos+direction*.35f,direction*speed+Vector2.up*2.2f,damage,true,ShotKind.Blood,1,5.5f);
                }
                heart.heartBarrageTimer=Mathf.Max(.32f,.58f-heart.heartSummons*.07f);
                PlaySound(0);
            }
            return true;
        }

        float HeartDamageFloor(Enemy heart)
        {
            if(heart.kind!=EnemyKind.Heart) return 0;
            if(heart.heartSummons==1) return heart.maxHp*2/3f;
            if(heart.heartSummons==2) return heart.maxHp/3f;
            return 0;
        }

        void HeartCrossScreenBlood(Enemy heart)
        {
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<6;i++)
                {
                    Vector2 origin=new Vector2(side<0?Left-.4f:Right+.4f,heart.target.y+(i-2.5f)*.12f);
                    CreateShot(origin,new Vector2(-side*(9+i*.25f),2.2f+i*.12f),19,true,ShotKind.Blood,1,4.5f);
                }
        }

        void HeartBloodFan(Enemy heart,int count,float speed,int damage,float spread)
        {
            float angle=Mathf.Atan2(heart.target.y-heart.pos.y,heart.target.x-heart.pos.x);
            for(int i=0;i<count;i++)
            {
                float a=angle+(i-(count-1)*.5f)*spread;
                Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                CreateShot(heart.pos,direction*speed+Vector2.up*2.8f,damage,true,ShotKind.Blood,1,6.5f);
            }
        }

        void MarkHeartRain(Enemy heart,int count,float warning)
        {
            for(int i=0;i<count;i++)
                AddHazard(new Vector2(HeartRainX(heart,i,count),.2f),new Vector2(.72f,10.4f),warning,.45f,0,ZeroHeroArt.Pink);
        }

        void DropHeartRain(Enemy heart,int count,int damage)
        {
            for(int i=0;i<count;i++)
            {
                float x=HeartRainX(heart,i,count);
                CreateShot(new Vector2(x,5.5f),new Vector2((i%2==0?1:-1)*.35f,-7.5f),damage,true,ShotKind.Blood,1,3.5f);
            }
        }

        float HeartRainX(Enemy heart,int index,int count)
        {
            float offset=heart.heartAttacks%2==0?0:1.7f;
            return Mathf.Clamp(Mathf.Lerp(Left+1,Right-1,(index+.5f)/count)+offset,Left+1,Right-1);
        }

        void AddHeartBloodPool(float x,int damage,float width=1.4f)
        {
            x=Mathf.Clamp(x,Left+.5f,Right-.5f);
            foreach(var hazard in hazards)
                if(hazard.blood && Mathf.Abs(hazard.pos.x-x)<.85f)
                {
                    hazard.size.x=Mathf.Min(4.2f,hazard.size.x+.3f);
                    hazard.life=Mathf.Max(hazard.life,45); hazard.damage=Mathf.Max(hazard.damage,damage);
                    hazard.sprite.transform.localScale=new Vector3(hazard.size.x,hazard.size.y,1);
                    return;
                }
            int bloodCount=0; foreach(var hazard in hazards) if(hazard.blood) bloodCount++;
            if(bloodCount>=20) return;
            AddHazard(new Vector2(x,Floor+.12f),new Vector2(width,.34f),.12f,45,damage,C("8E263B"),false,true);
        }
    }
}
