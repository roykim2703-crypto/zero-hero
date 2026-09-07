using System;

namespace ZeroHero
{
    public enum HeroStat { Attack, Defense, Speed, Fortune, Critical }

    [Serializable]
    public sealed class ZeroStats
    {
        public const int InitialTotal = 10, Count = 5;
        readonly int[] values = { 3, 2, 3, 2, 0 };
        public int this[int index] => values[index];
        public void Set(int index, int value)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            values[index] = value;
        }
        public int Total => values[0] + values[1] + values[2] + values[3] + values[4];
        public int AttackDamage => values[0] * 6;
        public float MoveSpeed => values[2] * 1.6f;
        public int DamageTaken(int incoming) => Math.Max(1, incoming - values[1] * 2);
        public int HealingReceived(int amount) => Math.Max(0, amount) + (amount > 0 ? Math.Max(0, -values[1]) * 2 : 0);
        public float CriticalMultiplier(bool inverted = false) => Math.Max(.1f, 1 + values[4] * (inverted ? -.2f : .2f));
        public int CriticalDamage(int damage, bool inverted = false) => (int)Math.Round(damage * CriticalMultiplier(inverted));
        public int CoinChange(int value) => value * values[3];
        public static string Name(int index) => new[] { "공격력", "방어력", "이동 속도", "자금 획득", "치명타 피해" }[index];

        public void Exchange(int increase, int decrease, int amount)
        {
            if (increase < 0 || increase >= Count || decrease < 0 || decrease >= Count || increase == decrease || amount <= 0)
                throw new ArgumentException("서로 다른 두 능력치를 같은 양만큼 교환해야 합니다.");
            checked { values[increase] += amount; values[decrease] -= amount; }
            if (Total != InitialTotal) throw new InvalidOperationException("제로 합계가 깨졌습니다.");
        }

        public void Swap(int first, int second)
        {
            if (first < 0 || first >= Count || second < 0 || second >= Count || first == second)
                throw new ArgumentException("서로 다른 두 능력치를 골라야 합니다.");
            int value = values[first]; values[first] = values[second]; values[second] = value;
            if (Total != InitialTotal) throw new InvalidOperationException("제로 합계가 깨졌습니다.");
        }

        public static int CollectCoins(int current, int value, ZeroStats stats)
            => Math.Max(0, current + stats.CoinChange(value));

        public static float ApplyAttack(float health, float maxHealth, int signedDamage)
            => Math.Max(0, Math.Min(maxHealth, health - signedDamage));
    }

    public enum TradeKind { Exchange, Swap, Extreme, InvertNext }

    public struct ZeroTrade
    {
        public TradeKind kind;
        public int up, down, amount;
        public ZeroTrade(int up, int down, int amount, TradeKind kind = TradeKind.Exchange)
        { this.kind = kind; this.up = up; this.down = down; this.amount = amount; }
    }
}
