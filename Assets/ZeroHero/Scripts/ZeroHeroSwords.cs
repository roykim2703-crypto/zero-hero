using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        void FireSword(int damage, bool critical)
        {
            Color color = damage < 0 ? ZeroHeroArt.Gain : ZeroHeroArt.Gold;
            switch (activeWeapon)
            {
                case 11:
                    StartSwordMotion(SwordMotion.Thrust,.10f);
                    SwordThrustEffect(player,aim,Weapon.reach,color,9,.045f);
                    HitSwordThrust(damage,critical,aim,Weapon.reach,.16f);
                    break;
                case 13:
                    StartSwordMotion(SwordMotion.Thrust,.18f);
                    SwordThrustEffect(player,aim,Weapon.reach,color,18,.055f);
                    HitSwordThrust(damage,critical,aim,Weapon.reach,.13f);
                    break;
                case 14:
                    FireCutlass(damage,critical,color);
                    break;
                case 16:
                    FireGreatsword(damage,critical,color);
                    break;
                case 17:
                    FireCurvedSword(damage,critical,color);
                    break;
                case 19:
                    FireBlackIronSword(damage,critical,color);
                    break;
                default:
                    StartSwordMotion(SwordMotion.Swing,.22f);
                    SwordArcEffect(player,aim,Weapon.reach,-70,70,color,17);
                    HitSwordArc(damage,critical,aim,Weapon.reach,.15f);
                    break;
            }
        }

        void FireCutlass(int damage, bool critical, Color color)
        {
            int combo = NextSwordCombo(4);
            if (combo == 0)
            {
                StartSwordMotion(SwordMotion.Overhead,.26f);
                SwordArcEffect(player,aim,Weapon.reach,95,-45,color,20);
                HitSwordArc(damage,critical,aim,Weapon.reach,.25f);
            }
            else if (combo == 1)
            {
                StartSwordMotion(SwordMotion.Swing,.24f);
                SwordArcEffect(player,aim,Weapon.reach,-105,105,color,24);
                HitSwordArc(damage,critical,aim,Weapon.reach,.02f);
            }
            else if (combo == 2)
            {
                StartSwordMotion(SwordMotion.Swing,.25f);
                SwordArcEffect(player,aim,Weapon.reach,70,-70,color,20);
                SwordThrustEffect(player,Rotated(aim,-32),Weapon.reach*.9f,color,10,.04f);
                HitSwordArc(damage,critical,aim,Weapon.reach,.18f);
            }
            else
            {
                StartSwordMotion(SwordMotion.Spin,.32f);
                SwordCircleEffect(player,Weapon.reach,color,32);
                HitSwordCircle(damage,critical,player,Weapon.reach);
            }
        }

        void FireGreatsword(int damage, bool critical, Color color)
        {
            int combo = NextSwordCombo(3);
            if (combo == 0)
            {
                StartSwordMotion(SwordMotion.Overhead,.42f);
                SwordArcEffect(player,aim,Weapon.reach,105,-35,color,25);
                Vector2 impact = player+aim*Weapon.reach*.82f;
                AddEffect(art.ring,impact,color,.3f,.18f,Vector2.zero,4.5f);
                Burst(impact,color,8);
                HitSwordArc(damage,critical,aim,Weapon.reach,.28f);
                shake = Mathf.Max(shake,.1f);
            }
            else if (combo == 1)
            {
                StartSwordMotion(SwordMotion.Thrust,.34f);
                SwordThrustEffect(player,aim,Weapon.reach,color,22,.09f);
                HitSwordThrust(damage,critical,aim,Weapon.reach,.28f);
            }
            else
            {
                StartSwordMotion(SwordMotion.Spin,.46f);
                Vector2 start = player;
                float forward = Mathf.Abs(aim.x) > .15f ? Mathf.Sign(aim.x) : heroBody.flipX ? -1 : 1;
                player.x = Mathf.Clamp(player.x+forward*1.35f,Left+.36f,Right-.36f);
                SwordCircleEffect(start,Weapon.reach*.72f,color,26);
                SwordCircleEffect(player,Weapon.reach*.72f,color,26);
                HitSwordMovingCircle(damage,critical,start,player,Weapon.reach*.72f);
                velocity.x = forward*5.5f;
            }
        }

        void FireCurvedSword(int damage, bool critical, Color color)
        {
            int combo = NextSwordCombo(2);
            StartSwordMotion(SwordMotion.Swing,.095f);
            float start = combo == 0 ? -95 : 95;
            float end = -start;
            SwordArcEffect(player,aim,Weapon.reach,start,end,color,18);
            SwordArcEffect(player,aim,Weapon.reach*.78f,start*.75f,end*.75f,C("E9F4D0"),14);
            SwordArcEffect(player,aim,Weapon.reach*.58f,-start*.55f,-end*.55f,C("9AA16C"),11);
            Vector2 tip = player+aim*Weapon.reach*.8f;
            AddEffect(art.ring,tip,color,.16f,.08f,aim*1.8f,3.5f);
            Burst(tip,color,5);
            HitSwordArc(damage,critical,aim,Weapon.reach,-.05f);
        }

        void FireBlackIronSword(int damage, bool critical, Color color)
        {
            StartSwordMotion(SwordMotion.Swing,.25f);
            SwordArcEffect(player,aim,Weapon.reach,-78,78,C("657078"),22);
            SwordArcEffect(player,aim,Weapon.reach*.82f,-65,65,color,16);
            HitSwordArc(damage,critical,aim,Weapon.reach,.08f);
            int shardDamage = Mathf.RoundToInt(damage*.45f);
            float angle = Mathf.Atan2(aim.y,aim.x);
            for (int i = -1; i <= 1; i++)
            {
                float a = angle+i*.18f;
                Vector2 direction = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                CreateShot(player+direction*.8f,direction*14,shardDamage,false,ShotKind.BlackIron,1,0,critical);
            }
        }

        void StartSwordMotion(SwordMotion motion, float duration)
        {
            swordMotion = motion;
            swordAnimDuration = swordAnimTime = duration;
        }

        int NextSwordCombo(int length)
        {
            int slot = Mathf.Clamp(activeWeapon-10,0,swordCombo.Length-1);
            int result = swordCombo[slot] % length;
            swordCombo[slot] = (result+1)%length;
            return result;
        }

        void HitSwordArc(int damage, bool critical, Vector2 direction, float reach, float minDot)
        {
            direction.Normalize();
            for (int i = enemies.Count-1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                Vector2 delta = enemy.pos-player;
                if (delta.magnitude <= reach+enemy.Radius && (delta.sqrMagnitude < .001f || Vector2.Dot(delta.normalized,direction) > minDot))
                    HitEnemy(enemy,damage,critical,direction);
            }
        }

        void HitSwordThrust(int damage, bool critical, Vector2 direction, float reach, float width)
        {
            direction.Normalize();
            Vector2 start = player+direction*.25f;
            Vector2 end = player+direction*reach;
            for (int i = enemies.Count-1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (SegmentDistance(enemy.pos,start,end) <= width+enemy.Radius)
                    HitEnemy(enemy,damage,critical,direction);
            }
        }

        void HitSwordCircle(int damage, bool critical, Vector2 center, float reach)
        {
            for (int i = enemies.Count-1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                Vector2 delta = enemy.pos-center;
                if (delta.magnitude <= reach+enemy.Radius)
                    HitEnemy(enemy,damage,critical,delta.sqrMagnitude < .001f ? aim : delta.normalized);
            }
        }

        void HitSwordMovingCircle(int damage, bool critical, Vector2 start, Vector2 end, float reach)
        {
            Vector2 direction = (end-start).sqrMagnitude < .001f ? aim : (end-start).normalized;
            for (int i = enemies.Count-1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (SegmentDistance(enemy.pos,start,end) <= reach+enemy.Radius)
                    HitEnemy(enemy,damage,critical,direction);
            }
        }

        void SwordThrustEffect(Vector2 origin, Vector2 direction, float reach, Color color, int count, float scale)
        {
            direction.Normalize();
            Vector2 side = new Vector2(-direction.y,direction.x);
            for (int i = 0; i < count; i++)
            {
                float t = (i+1f)/count;
                Vector2 pos = origin+direction*(.3f+reach*.86f*t)+side*Mathf.Sin(t*Mathf.PI*4)*.035f;
                AddEffect(art.square,pos,color,.11f+(.06f*t),scale,Vector2.zero,0);
            }
        }

        void SwordArcEffect(Vector2 origin, Vector2 direction, float reach, float startDegrees, float endDegrees, Color color, int count)
        {
            float angle = Mathf.Atan2(direction.y,direction.x);
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0 : i/(float)(count-1);
                float a = angle+Mathf.Lerp(startDegrees,endDegrees,t)*Mathf.Deg2Rad;
                float radius = reach*(.66f+.16f*Mathf.Sin(t*Mathf.PI));
                Vector2 pos = origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                AddEffect(art.square,pos,color,.14f+Mathf.Sin(t*Mathf.PI)*.08f,.075f+Mathf.Sin(t*Mathf.PI)*.055f,Vector2.zero,0);
            }
        }

        void SwordCircleEffect(Vector2 origin, float reach, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i*Mathf.PI*2/count;
                Vector2 direction = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                AddEffect(art.square,origin+direction*reach*.72f,color,.24f,.1f,direction*.35f,0);
            }
            AddEffect(art.ring,origin,color,.26f,reach*.18f,Vector2.zero,reach*2.1f);
        }

        static Vector2 Rotated(Vector2 direction, float degrees)
        {
            float radians = degrees*Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians), sine = Mathf.Sin(radians);
            return new Vector2(direction.x*cosine-direction.y*sine,direction.x*sine+direction.y*cosine);
        }
    }
}
