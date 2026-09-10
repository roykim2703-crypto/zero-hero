using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        void CreateShot(Vector2 pos,Vector2 velocity,int damage,bool hostile,ShotKind kind=ShotKind.Bullet,int pierce=1,float gravity=0,bool critical=false)
        {
            if(shots.Count>500) return;
            Color color=kind==ShotKind.BlackIron ? C("4C5961") : hostile ? ZeroHeroArt.Pink : damage<0 ? ZeroHeroArt.Gain : ZeroHeroArt.Gold;
            Sprite sprite=kind==ShotKind.Arrow?art.arrow:kind==ShotKind.Snake?art.snake:kind==ShotKind.Fly?art.fly:kind==ShotKind.Meteor?art.disc:art.square;
            Vector2 scale=kind==ShotKind.Arrow?Vector2.one*.8f:kind==ShotKind.Snake?Vector2.one*.9f:kind==ShotKind.Fly?Vector2.one*.8f:kind==ShotKind.Meteor?Vector2.one*.65f:kind==ShotKind.BlackIron?new Vector2(.46f,.2f):new Vector2(.34f,.13f);
            var sr=Sprite(hostile?"Enemy attack":kind==ShotKind.BlackIron?"Black iron shard":"Bullet",sprite,(kind==ShotKind.Snake||kind==ShotKind.Fly)?Color.white:color,pos,scale,30);
            sr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
            shots.Add(new Shot { root=sr.transform,pos=pos,velocity=velocity,damage=damage,hostile=hostile,life=kind==ShotKind.BlackIron?1.4f:6,kind=kind,pierce=pierce,gravity=gravity,critical=critical });
        }
        void TickShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i]; Vector2 start=s.pos; s.life-=dt;
                if(s.kind==ShotKind.Fly && s.hostile) s.velocity=Vector2.Lerp(s.velocity,(player-s.pos).normalized*5,dt*.8f);
                s.velocity.y-=s.gravity*dt; s.pos+=s.velocity*dt;
                if(s.kind==ShotKind.Snake && s.pos.y<Floor+.2f) { s.pos.y=Floor+.2f; s.velocity.y=0; s.gravity=0; }
                if(s.kind==ShotKind.BlackIron) AddEffect(art.square,s.pos,C("77838A"),.09f,.045f,Vector2.zero,0);
                s.root.position=s.pos; s.root.rotation=Quaternion.Euler(0,0,Mathf.Atan2(s.velocity.y,s.velocity.x)*Mathf.Rad2Deg);
                bool remove=s.life<=0||s.pos.x<Left-1||s.pos.x>Right+1||s.pos.y>7||s.pos.y<Floor-.25f;
                if(s.kind==ShotKind.Blood && s.hostile && s.gravity>0 && s.pos.y<=Floor+.18f)
                { AddHeartBloodPool(s.pos.x,Mathf.Max(8,s.damage/2),1.15f); remove=true; }
                if(s.kind==ShotKind.Meteor && s.pos.y<Floor+.2f)
                { AddHazard(new Vector2(s.pos.x,Floor+.15f),new Vector2(2,.35f),.1f,1.2f,s.damage,ZeroHeroArt.Pink); remove=true; }
                if(!remove && s.hostile && SegmentDistance(player,start,s.pos)<.57f) { Hurt(s.damage); remove=true; }
                if(!remove && !s.hostile)
                    for(int j=enemies.Count-1;j>=0;j--)
                    {
                        var e=enemies[j];
                        if(!s.hit.Contains(e)&&SegmentDistance(e.pos,start,s.pos)<e.Radius+.12f)
                        { s.hit.Add(e); HitEnemy(e,s.damage,s.critical,s.velocity); s.pierce--; if(s.pierce<=0) { remove=true; break; } }
                    }
                if(remove) RemoveShot(i);
            }
        }
        static float SegmentDistance(Vector2 point,Vector2 a,Vector2 b)
        { Vector2 v=b-a; float t=v.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(point-a,v)/v.sqrMagnitude):0; return Vector2.Distance(point,a+v*t); }
        void RemoveShot(int i) { Destroy(shots[i].root.gameObject); shots.RemoveAt(i); }
        void AddHazard(Vector2 center,Vector2 size,float warning,float life,int damage,Color color,bool stone=false,bool blood=false)
        {
            var sr=Sprite("Attack warning",art.square,new Color(color.r,color.g,color.b,.18f),center,size,12);
            hazards.Add(new Hazard { sprite=sr,pos=center,size=size,warning=warning,life=life,damage=damage,color=color,stone=stone,blood=blood });
        }
        void AddBeam(Vector2 origin,Vector2 direction,float warning,int damage,bool stone=false)
        {
            direction.Normalize(); float length=32;
            var sr=Sprite("Beam warning",art.square,new Color(1,.8f,.4f,.25f),origin+direction*length*.5f,new Vector2(length,.1f),29);
            sr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            hazards.Add(new Hazard { sprite=sr,pos=origin,direction=direction,size=new Vector2(length,.34f),warning=warning,life=.55f,damage=damage,stone=stone,beam=true,color=stone?C("C0D1A8"):ZeroHeroArt.Gold });
        }
        void TickHazards(float dt)
        {
            for(int i=hazards.Count-1;i>=0;i--)
            {
                var h=hazards[i]; h.warning-=dt;
                bool active=h.warning<=0;
                if(active) h.life-=dt;
                Color c=h.color; c.a=active?.7f:.14f+Mathf.Abs(Mathf.Sin(visualTime*11))*.14f; h.sprite.color=c;
                if(h.beam) h.sprite.transform.localScale=new Vector3(h.size.x,active?h.size.y:.06f,1);
                bool hit=h.beam?SegmentDistance(player,h.pos,h.pos+h.direction*h.size.x)<HeroHalf*.7f+h.size.y*.5f:
                    Mathf.Abs(player.x-h.pos.x)<h.size.x*.5f+.3f&&Mathf.Abs(player.y-h.pos.y)<h.size.y*.5f+HeroHalf*.9f;
                if(active&&hit&&h.damage>0&&Hurt(h.damage)&&h.stone) { petrified=1.35f; velocity.x=0; dashTime=0; Float(player+Vector2.up,"석화",C("B4BDB3")); }
                if(h.life<=0) { Destroy(h.sprite.gameObject); hazards.RemoveAt(i); }
            }
        }
    }
}
