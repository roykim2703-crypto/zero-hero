using System;

namespace ZeroHero
{
    public enum HeroStat { Attack, Defense, Speed, Fortune }

    [Serializable]
    public sealed class ZeroStats
    {
        public const int InitialTotal = 10;
        readonly int[] values = { 3, 2, 3, 2 };
        public int this[int index] => values[index];
        public int Total => values[0] + values[1] + values[2] + values[3];
        public int AttackDamage => values[0] * 6;
        public float MoveSpeed => values[2] * 1.6f;
        public int DamageTaken(int incoming) => Math.Max(1, incoming - values[1] * 2);
        public int CoinChange(int value) => value * values[3];
        public static string Name(int index) => new[] { "공격력", "방어력", "이동 속도", "자금 획득" }[index];

        public void Exchange(int increase, int decrease, int amount)
        {
            if (increase < 0 || increase > 3 || decrease < 0 || decrease > 3 || increase == decrease || amount <= 0)
                throw new ArgumentException("서로 다른 두 능력치를 같은 양만큼 교환해야 합니다.");
            checked { values[increase] += amount; values[decrease] -= amount; }
            if (Total != InitialTotal) throw new InvalidOperationException("제로 합계가 깨졌습니다.");
        }

        public static int CollectCoins(int current, int value, ZeroStats stats)
            => Math.Max(0, current + stats.CoinChange(value));

        public static float ApplyAttack(float health, float maxHealth, int signedDamage)
            => Math.Max(0, Math.Min(maxHealth, health - signedDamage));
    }

    public struct ZeroTrade
    {
        public int up, down, amount;
        public ZeroTrade(int up, int down, int amount) { this.up = up; this.down = down; this.amount = amount; }
    }
}
