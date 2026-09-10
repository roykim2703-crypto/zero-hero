using System;
using System.Collections.Generic;

namespace ZeroHero
{
    public enum EnemyKind { Human, Undead, Orc, Goblin, Elf, Giant, Medusa, Angel, Heart, Beelzebub, DemonKing, RearGuard, Inverter }
    public enum MapTheme { Outpost, Graveyard, OrcCamp, GoblinMine, ElfForest, GiantPass, Temple, Heaven, Viscera, Swamp, Citadel }

    public sealed class WeaponDef
    {
        public readonly int id, power, price, pellets, pierce, magazine;
        public readonly string name, description;
        public readonly bool gun;
        public readonly float rate, bulletSpeed, reach, reloadTime;
        public WeaponDef(int id, string name, bool gun, int power, float rate, float speed, float reach, int price, string description, int pellets = 1, int pierce = 1, int magazine = 0, float reloadTime = 0)
        { this.id = id; this.name = name; this.gun = gun; this.power = power; this.rate = rate; bulletSpeed = speed; this.reach = reach; this.price = price; this.description = description; this.pellets = pellets; this.pierce = pierce; this.magazine = magazine; this.reloadTime = reloadTime; }
        public int Damage(ZeroStats stats) => (int)Math.Round(power * stats[0] / 3f);
    }

    public sealed class EnemyDef
    {
        public readonly EnemyKind kind;
        public readonly string name, detail;
        public readonly float health, speed, size;
        public readonly int damage;
        public bool Boss => kind >= EnemyKind.Medusa && kind <= EnemyKind.DemonKing;
        public EnemyDef(EnemyKind kind, string name, string detail, float health, float speed, float size, int damage)
        { this.kind = kind; this.name = name; this.detail = detail; this.health = health; this.speed = speed; this.size = size; this.damage = damage; }
    }

    public sealed class MapOffer
    {
        public string name, detail, terrain;
        public MapTheme theme;
        public int layout;
        public readonly int[] counts = new int[13];
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
        public const int RoomsPerAct = 4, TotalRooms = 20, StartingGold = 0;
        public static readonly WeaponDef[] Weapons = {
            new WeaponDef(0,"낡은 권총",true,16,2.5f,20,0,0,"기본 장비",1,1,8,1.35f),
            new WeaponDef(1,"리볼버",true,34,1.6f,25,0,200,"느리고 강한 한 발",1,1,6,1.80f),
            new WeaponDef(2,"기관 단총",true,9,7.5f,18,0,250,"짧은 간격으로 연사",1,1,30,2.15f),
            new WeaponDef(3,"산탄총",true,12,1.05f,16,0,350,"한 번에 5발",5,1,10,2.45f),
            new WeaponDef(4,"사냥 소총",true,48,1.15f,32,0,500,"빠른 탄환, 긴 사거리",1,1,12,2.10f),
            new WeaponDef(5,"돌격 소총",true,19,4.5f,27,0,650,"공격 속도와 피해 균형",1,1,25,2.35f),
            new WeaponDef(6,"중 기관총",true,26,3.4f,23,0,850,"묵직한 연속 사격",1,1,100,4.80f),
            new WeaponDef(7,"저격총",true,100,.7f,44,0,1050,"2개체 관통",1,2,5,3.80f),
            new WeaponDef(8,"쌍열 산탄총",true,18,.9f,21,0,1300,"한 번에 7발",7,1,2,.75f),
            new WeaponDef(9,"전자포",true,67,1.8f,38,0,2000,"3개체 관통 · 무한 탄창",1,3,-1,0),
            new WeaponDef(10,"녹슨 칼",false,30,2.0f,0,1.9f,0,"묵직한 기본 베기"),
            new WeaponDef(11,"단검",false,17,8.4f,0,1.2f,200,"초고속 단거리 찌르기"),
            new WeaponDef(12,"군용검",false,45,2.0f,0,2.1f,250,"강하고 안정적인 베기"),
            new WeaponDef(13,"레이피어",false,27,3.5f,0,4.0f,350,"초장거리 찌르기"),
            new WeaponDef(14,"커틀러스",false,42,2.2f,0,2.4f,500,"4연속 궤적 콤보"),
            new WeaponDef(15,"장검",false,54,1.7f,0,3.5f,650,"긴 범위 베기"),
            new WeaponDef(16,"대검",false,85,1.0f,0,4.0f,850,"강타·찌르기·회전 돌진"),
            new WeaponDef(17,"곡도",false,40,10.0f,0,1.8f,1050,"화려한 초고속 연속 베기"),
            new WeaponDef(18,"처형검",false,130,.7f,0,3.6f,1300,"매우 느리고 강한 공격"),
            new WeaponDef(19,"흑철검",false,74,2.1f,0,3.1f,2000,"베기와 흑철 파편 3개")
        };

        public static readonly EnemyDef[] Enemies = {
            new EnemyDef(EnemyKind.Human,"인간","특수 능력 없음. 접근해서 공격.",60,1.9f,1.15f,12),
            new EnemyDef(EnemyKind.Undead,"언데드","회복 공격을 받으면 회복량의 2배 피해.",90,1.35f,1.18f,15),
            new EnemyDef(EnemyKind.Orc,"오크","개체는 약함. 4마리씩 함께 등장.",42,2.05f,1.1f,10),
            new EnemyDef(EnemyKind.Goblin,"고블린","체력이 낮고 빠름. 6마리씩 등장.",28,3.0f,.8f,7),
            new EnemyDef(EnemyKind.Elf,"엘프","거리를 벌리며 활로 조준 사격.",68,2.2f,1.2f,16),
            new EnemyDef(EnemyKind.Giant,"거인","매우 느림. 높은 체력과 강한 내려찍기.",400,.52f,2.8f,42),
            new EnemyDef(EnemyKind.Medusa,"메두사","석화 광선 · 뱀 투척 · 독 웅덩이 · 낙석",720,.8f,2.5f,24),
            new EnemyDef(EnemyKind.Angel,"천사","심판 기둥 · 눈의 탄막 · 날개 돌진 · 깃털 비",900,1.0f,2.8f,25),
            new EnemyDef(EnemyKind.Heart,"심장","체력 구간 소환 · 소환 중 무적 · 피 탄막과 피 웅덩이",1050,0,2.7f,27),
            new EnemyDef(EnemyKind.Beelzebub,"바알제붑","파리 무리 변신 · 부패탄 · 독액 · 파리 돌진",1250,1.3f,2.5f,28),
            new EnemyDef(EnemyKind.DemonKing,"마왕","순간이동 베기 · 불기둥 · 군단 소환 · 운석 · 화염파",1600,1.0f,3.0f,34),
            new EnemyDef(EnemyKind.RearGuard,"방패병","정면 방어. 등을 돌아 공격하세요.",105,1.6f,1.3f,16),
            new EnemyDef(EnemyKind.Inverter,"반전술사","치명타 배율 반전. 음수 치명타에 약함.",115,1.8f,1.25f,17)
        };

        public static int ResolveDamage(EnemyKind kind, int signedDamage)
            => kind == EnemyKind.Undead && signedDamage < 0 ? -signedDamage * 2 : signedDamage;
        public static bool IsBossRoom(int room) => room > 0 && room % RoomsPerAct == 0;
        public static EnemyKind BossForRoom(int room) => (EnemyKind)((int)EnemyKind.Medusa + (room - 1) / RoomsPerAct);
        public static MapTheme BossTheme(EnemyKind kind) => (MapTheme)((int)MapTheme.Temple + (int)kind - (int)EnemyKind.Medusa);
        public static int CoinDropCount(EnemyKind kind) => Enemies[(int)kind].Boss ? 12 : kind == EnemyKind.Giant ? 5 : 2;
        public static int BaseCoinValue(EnemyKind kind) => Enemies[(int)kind].Boss ? 20 : kind == EnemyKind.Giant ? 8 : 5;
        public static int CoinMultiplier(int room) => Math.Max(1, (room - 1) / RoomsPerAct + 1);
        public static int CoinValue(EnemyKind kind, int room) => BaseCoinValue(kind) * CoinMultiplier(room);
        public static int RawCoinReward(MapOffer offer, int room)
        {
            int reward = 0;
            for (int kind = 0; kind < offer.counts.Length; kind++)
                reward += offer.counts[kind] * CoinDropCount((EnemyKind)kind) * CoinValue((EnemyKind)kind, room);
            return reward;
        }

        public static MapOffer[] CreateOffers(int room, Random random)
        {
            int act = (room - 1) / 4;
            if (IsBossRoom(room))
            {
                EnemyKind boss = BossForRoom(room);
                string[] terrain = { "중앙 발판", "양쪽 높은 발판", "낮은 발판 세 개" };
                int layout = random.Next(terrain.Length);
                var m = new MapOffer { name = Enemies[(int)boss].name, detail = Enemies[(int)boss].detail, theme = BossTheme(boss), layout = layout, terrain = terrain[layout], Boss = true };
                m.counts[(int)boss] = 1;
                return new[] { m };
            }
            var offers = new MapOffer[3];
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
                if (room > 1 && act > 0 && type != 5) m.counts[i == 0 ? (int)EnemyKind.RearGuard : i == 1 ? (int)EnemyKind.Inverter : type == 0 ? 4 : 0] = 2 + act;
                offers[i] = m;
            }
            return offers;
        }
    }
}
