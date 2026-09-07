using System;
using System.Collections.Generic;

namespace ZeroHero
{
    public enum EnemyKind { Human, Undead, Orc, Goblin, Elf, Giant, Medusa, Angel, Heart, Beelzebub, DemonKing }
    public enum MapTheme { Outpost, Graveyard, OrcCamp, GoblinMine, ElfForest, GiantPass, Temple, Heaven, Viscera, Swamp, Citadel }

    public sealed class WeaponDef
    {
        public readonly int id, power, price, pellets, pierce;
        public readonly string name, description;
        public readonly bool gun;
        public readonly float rate, bulletSpeed, reach;
        public WeaponDef(int id, string name, bool gun, int power, float rate, float speed, float reach, int price, string description, int pellets = 1, int pierce = 1)
        { this.id = id; this.name = name; this.gun = gun; this.power = power; this.rate = rate; bulletSpeed = speed; this.reach = reach; this.price = price; this.description = description; this.pellets = pellets; this.pierce = pierce; }
        public int Damage(ZeroStats stats) => (int)Math.Round(power * stats[0] / 3f);
    }

    public sealed class EnemyDef
    {
        public readonly EnemyKind kind;
        public readonly string name, detail;
        public readonly float health, speed, size;
        public readonly int damage;
        public bool Boss => kind >= EnemyKind.Medusa;
        public EnemyDef(EnemyKind kind, string name, string detail, float health, float speed, float size, int damage)
        { this.kind = kind; this.name = name; this.detail = detail; this.health = health; this.speed = speed; this.size = size; this.damage = damage; }
    }

    public sealed class MapOffer
    {
        public string name, detail, terrain;
        public MapTheme theme;
        public int layout;
        public readonly int[] counts = new int[11];
        public bool Boss;
        public int Total { get { int n = 0; foreach (int c in counts) n += c; return n; } }
        public string Roster
        {
            get
            {
                var names = new List<string>();
                for (int i = 0; i < counts.Length; i++) if (counts[i] > 0) names.Add(ZeroContent.Enemies[i].name + " " + counts[i]);
                return string.Join(" · ", names);
            }
        }
    }

    public static class ZeroContent
    {
        public const int RoomsPerAct = 4, TotalRooms = 20;
        public static readonly WeaponDef[] Weapons = {
            new WeaponDef(0,"낡은 권총",true,16,2.5f,20,0,0,"기본 장비"),
            new WeaponDef(1,"리볼버",true,34,1.6f,25,0,45,"느리고 강한 한 발"),
            new WeaponDef(2,"기관권총",true,9,7.5f,18,0,60,"짧은 간격으로 연사"),
            new WeaponDef(3,"산탄총",true,12,1.05f,16,0,75,"한 번에 5발",5),
            new WeaponDef(4,"사냥 소총",true,48,1.15f,32,0,90,"빠른 탄환, 긴 사거리"),
            new WeaponDef(5,"돌격소총",true,19,4.5f,27,0,115,"공격 속도와 피해 균형"),
            new WeaponDef(6,"중기관총",true,26,3.4f,23,0,145,"묵직한 연속 사격"),
            new WeaponDef(7,"저격총",true,100,.7f,44,0,175,"2개체 관통",1,2),
            new WeaponDef(8,"쌍열 산탄총",true,18,.9f,21,0,195,"한 번에 7발",7),
            new WeaponDef(9,"전자포",true,67,1.8f,38,0,250,"3개체 관통",1,3),
            new WeaponDef(10,"녹슨 칼",false,23,2.3f,0,1.9f,0,"기본 장비"),
            new WeaponDef(11,"단검",false,17,4.2f,0,1.6f,30,"짧고 빠른 연속 베기"),
            new WeaponDef(12,"군용검",false,32,2.6f,0,2.1f,45,"가까운 적을 안정적으로 벰"),
            new WeaponDef(13,"레이피어",false,27,3.5f,0,2.5f,65,"긴 사거리와 빠른 찌르기"),
            new WeaponDef(14,"커틀러스",false,42,2.2f,0,2.4f,80,"빠른 중형 검"),
            new WeaponDef(15,"장검",false,54,1.7f,0,2.9f,105,"넓은 범위의 베기"),
            new WeaponDef(16,"대검",false,85,1.0f,0,3.3f,130,"여러 적을 한 번에 벰"),
            new WeaponDef(17,"곡도",false,40,3.2f,0,2.6f,155,"속도를 유지한 연속 공격"),
            new WeaponDef(18,"처형검",false,130,.7f,0,3.6f,195,"매우 느리고 강한 공격"),
            new WeaponDef(19,"흑철검",false,74,2.1f,0,3.1f,240,"긴 칼날과 높은 피해")
        };

        public static readonly EnemyDef[] Enemies = {
            new EnemyDef(EnemyKind.Human,"인간","특수 능력 없음. 접근해서 공격.",36,1.9f,1.15f,12),
            new EnemyDef(EnemyKind.Undead,"언데드","회복 공격을 받으면 회복량의 2배 피해.",54,1.35f,1.18f,15),
            new EnemyDef(EnemyKind.Orc,"오크","개체는 약함. 4마리씩 함께 등장.",25,2.05f,1.1f,10),
            new EnemyDef(EnemyKind.Goblin,"고블린","체력이 낮고 빠름. 6마리씩 등장.",16,3.0f,.8f,7),
            new EnemyDef(EnemyKind.Elf,"엘프","거리를 벌리며 활로 조준 사격.",40,2.2f,1.2f,16),
            new EnemyDef(EnemyKind.Giant,"거인","매우 느림. 높은 체력과 강한 내려찍기.",240,.52f,2.8f,42),
            new EnemyDef(EnemyKind.Medusa,"메두사","석화 광선 · 뱀 투척 · 독 웅덩이 · 낙석",440,.8f,2.5f,24),
            new EnemyDef(EnemyKind.Angel,"천사","심판 기둥 · 눈의 탄막 · 날개 돌진 · 깃털 비",560,1.0f,2.8f,25),
            new EnemyDef(EnemyKind.Heart,"심장","박동 충격파 · 피 분사 · 혈전 소환 · 출혈 지대",650,0,2.7f,27),
            new EnemyDef(EnemyKind.Beelzebub,"바알제붑","파리 무리 변신 · 부패탄 · 독액 · 파리 돌진",760,1.3f,2.5f,28),
            new EnemyDef(EnemyKind.DemonKing,"마왕","순간이동 베기 · 불기둥 · 군단 소환 · 운석 · 화염파",950,1.0f,3.0f,34)
        };

        public static int ResolveDamage(EnemyKind kind, int signedDamage)
            => kind == EnemyKind.Undead && signedDamage < 0 ? -signedDamage * 2 : signedDamage;
        public static bool IsBossRoom(int room) => room > 0 && room % RoomsPerAct == 0;
        public static EnemyKind BossForRoom(int room) => (EnemyKind)((int)EnemyKind.Medusa + (room - 1) / RoomsPerAct);
        public static MapTheme BossTheme(EnemyKind kind) => (MapTheme)((int)MapTheme.Temple + (int)kind - (int)EnemyKind.Medusa);

        public static MapOffer[] CreateOffers(int room, Random random)
        {
            var offers = new MapOffer[3]; int act = (room - 1) / 4;
            if (IsBossRoom(room))
            {
                EnemyKind boss = BossForRoom(room);
                string[] terrain = { "중앙 발판", "양쪽 높은 발판", "낮은 발판 세 개" };
                for (int i = 0; i < 3; i++)
                {
                    var m = new MapOffer { name = Enemies[(int)boss].name, detail = Enemies[(int)boss].detail, theme = BossTheme(boss), layout = i, terrain = terrain[i], Boss = true };
                    m.counts[(int)boss] = 1;
                    offers[i] = m;
                }
                return offers;
            }
            var pool = new List<int>();
            for (int i = 0; i < 6; i++) if (act > 0 || i != 5) pool.Add(i);
            string[] names = { "버려진 초소", "공동묘지", "오크 야영지", "고블린 광산", "엘프의 숲", "거인의 고개" };
            string[] terrains = { "무너진 성벽과 발판", "묘비와 납골당", "나무 망루", "갱도와 작업 발판", "나무와 가지 발판", "절벽과 돌기둥" };
            for (int i = 0; i < 3; i++)
            {
                int pick = random.Next(pool.Count), type = pool[pick]; pool.RemoveAt(pick);
                if (room == 1) type = 0;
                var m = new MapOffer { name = room == 1 ? new[] { "외곽 초소", "성문 앞길", "무너진 병영" }[i] : names[type], theme = (MapTheme)type, layout = i, terrain = terrains[type], detail = Enemies[type].detail };
                m.counts[type] = type == 2 ? 12 + act * 4 : type == 3 ? 18 + act * 6 : type == 5 ? 2 + act / 2 : 6 + act * 2;
                if (room > 1 && act > 0 && type != 5) m.counts[type == 0 ? 4 : 0] = 2 + act;
                offers[i] = m;
            }
            return offers;
        }
    }
}
