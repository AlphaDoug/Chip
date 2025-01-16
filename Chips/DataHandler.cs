using AutoMapper;
using FastDeepCloner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static ILGPU.IR.Analyses.Uniforms;

namespace Chips
{
    internal class DataHandler
    {
        List<Prize> prizeList = new List<Prize>();
        public static Player Mine = new Player(true);
        /// <summary>
        /// 当前所有其他玩家的下注数量信息
        /// </summary>
        Dictionary<Player, Dictionary<string, Chip>> totalChips = new Dictionary<Player, Dictionary<string, Chip>>();
        public DataHandler(int onePlayerHasChipCount, List<int> totalChipCount, List<int> prePrizeValue, List<int> prePrizeCount)
        {
            var totalPlayerCount = DataPreprocessing(ref totalChipCount, onePlayerHasChipCount);
            for (int i = 0; i < totalPlayerCount; i++)
            {
                var PlayerIns = new Player();
                totalChips.Add(PlayerIns, new Dictionary<string, Chip>());
                for (int j = 0; j < onePlayerHasChipCount; j++)
                {
                    var ChipIns = new Chip(PlayerIns);
                    totalChips[PlayerIns].Add(ChipIns.UUID, ChipIns);
                }
            }

            if (totalChipCount.Count != prePrizeValue.Count || prePrizeValue.Count != prePrizeCount.Count) return;
            for (int i = 0; i < totalChipCount.Count; i++)
            {
                var prize = new Prize(prePrizeValue[i], prePrizeCount[i], totalChipCount[i], i);
                prizeList.Add(prize);
            }

        }

        public int GetPeopleCountByIndex(int index)
        {
            return prizeList[index].BettingData.Count;
        }

        public void SimulateAllBet()
        {
            foreach (KeyValuePair<Player, Dictionary<string, Chip>> Pair in totalChips)
            {
                foreach (KeyValuePair<string, Chip> ChipPair in Pair.Value)
                {
                    ChipPair.Value.CurrentPrize?.RemoveChip(ChipPair.Value);
                }
            }
            foreach (KeyValuePair<Player, Dictionary<string, Chip>> Pair in totalChips)
            {
                foreach(KeyValuePair<string, Chip> ChipPair in Pair.Value)
                {
                    int index = GetRandomPrizeByWeight();
                    Prize prize = prizeList[index];
                    prize.AddChip(ChipPair.Value);      
                }
            }
        }

        public double GetSimulatePrize(int[] param, int count, ref List<string> logRes)
        {
            int? prize = 0;
            object lockObj = new object();
            // ThreadLocal accumulator to prevent lock contention
            var localPrizeAccumulator = new ThreadLocal<int?>(() => 0);
            List<string> log = new List<string>();
            try
            {
                Parallel.For(0, count, () => 0, (i, state, localAcc) =>
                {
                    int simulateCount = 1;
                    int? result = GetSimulateOneTimePrize(param, ref simulateCount);
                    log.Add($"此次模拟抽取中奖价值结果为：{result}，在第{simulateCount}次时候中奖。");
                    return (int)(localAcc + result);
                },
                // Final aggregation of all local contributions
                localTotal =>
                {
                    lock (lockObj)
                    {
                        prize += localTotal;
                    }
                });
            }
            finally
            {
                localPrizeAccumulator.Dispose();
            }
            logRes = log;
            return (double)prize / count;
        }
        private int GetSimulateOneTimePrize(int[] param, ref int simulateCount)
        {
            int index = SimulateOneTime(param, ref simulateCount);
            if(index == -1)
            {
                return 0;
            }
            else
            {
                return prizeList[index].Cost;
            }
        }
        /// <summary>
        /// 模拟抽奖一次
        /// </summary>
        /// <returns>-1表示未中奖，其他索引表示中奖的奖品索引</returns>
        public int SimulateOneTime(int[] mineChip, ref int simulateCount)
        {
            Dictionary<Player, Dictionary<string, Chip>> totalChipsCache = new Dictionary<Player, Dictionary<string, Chip>>();
            simulateCount = 1;
            if (mineChip.Sum() > 0)
            {
                totalChipsCache[Mine] = new Dictionary<string, Chip>();
            }
            else
            {
                return -1;
            }

            // 深拷贝totalChips
            foreach (var item in totalChips)
            {
                totalChipsCache[item.Key] = new Dictionary<string, Chip>(item.Value);
            }

            // 批量添加自己下注的数据到缓存中
            var mineCache = totalChipsCache[Mine];
            for (int i = 0; i < mineChip.Length; i++)
            {
                if (mineChip[i] <= 0) continue;
                for (int j = 0; j < mineChip[i]; j++)
                {
                    Chip chip = new Chip(DataHandler.Mine);
                    chip.BetToPool(prizeList[i]);
                    mineCache[chip.UUID] = chip;
                }
            }

            int finalIndex = -1;
            while (true)
            {
                Chip randomChip = GetRandomChip(totalChipsCache);
                if (randomChip.BelongPlayer.GetIsMine())
                {
                    finalIndex = randomChip.CurrentPrize.Index;
                    break;
                }

                // 不是自己的筹码
                var winnerPlayer = randomChip.BelongPlayer;
                totalChipsCache.Remove(winnerPlayer);

                bool isDepleted = false;
                randomChip.CurrentPrize.ExtractOne(ref isDepleted);
                if (isDepleted)
                {
                    var prizeToRemove = randomChip.CurrentPrize;
                    // 清除奖池中奖品为0的筹码
                    foreach (var playerChips in totalChipsCache.Values)
                    {
                        playerChips.Keys
                            .Where(key => playerChips[key].CurrentPrize == prizeToRemove)
                            .ToList()
                            .ForEach(key => playerChips.Remove(key));
                    }
                    // 移除没有筹码的玩家
                    totalChipsCache.Keys
                        .Where(key => totalChipsCache[key].Count == 0)
                        .ToList()
                        .ForEach(key => totalChipsCache.Remove(key));
                }

                if (totalChipsCache.Count == 0) break;
                simulateCount++;
            }
            return finalIndex;
        }


        public int GetRandomPrizeByWeight()
        {
            int index = 0;
            List<int> weightList = new List<int>();
            for (int i = 0; i < prizeList.Count; i++ )
            {
                weightList.Add(prizeList[i].GetChipCountLeft());
            }
            index = GetRandomWeightedIndex(weightList);
            return index;
        }

        /// <summary>
        /// 处理所有奖品下注数据，保证每个下注的玩家将自己的所有筹码都下注了
        /// </summary>
        /// <param name="currentChipCounts">当前每个奖池中的下注数量信息</param>
        /// <param name="onePlayerHasChipCount">一个玩家起始拥有的筹码数量</param>
        /// <returns>玩家总数</returns>
        public static int DataPreprocessing(ref List<int> currentChipCounts, int onePlayerHasChipCount)
        {
            int totalSum = currentChipCounts.Sum();
            int remainder = totalSum % onePlayerHasChipCount;

            // 如果当前总和不是one_player_total_chips的整数倍
            if (remainder != 0)
            {
                // 计算需要增加的量
                int neededIncrease = onePlayerHasChipCount - remainder;
                int index = 0;
                int length = currentChipCounts.Count;

                // 均匀分布所需增加的量，增加到sum刚好成为倍数
                while (neededIncrease > 0)
                {
                    currentChipCounts[index % length] += 1;
                    neededIncrease -= 1;
                    index += 1;
                }
            }

            int newTotalSum = currentChipCounts.Sum();
            int divisionResult = newTotalSum / onePlayerHasChipCount;
            return divisionResult;
        }

        static int GetRandomWeightedIndex(List<int> weights)
        {
            Random random = new Random();
            int totalWeight = 0;

            // 计算总权重
            foreach (int weight in weights)
            {
                totalWeight += weight;
            }

            // 在 [0, totalWeight) 范围内选择一个随机数
            int randomNumber = random.Next(totalWeight);

            // 确定随机数落在哪个权重区间内
            int cumulativeSum = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulativeSum += weights[i];
                if (randomNumber < cumulativeSum)
                {
                    return i;
                }
            }

            // 理论上不该到这里，如果到这里说明逻辑有错误
            throw new InvalidOperationException("未找到有效索引，这通常是由于权重不足或逻辑错误导致的。");
        }

        public static void DistributeChips(int chips, int pools, int[] distribution, int current, List<int[]> results)
        {
            if (current == pools - 1)
            {
                // 最后一个奖池得到剩余的所有筹码
                distribution[current] = chips;
                results.Add((int[])distribution.Clone());
                return;
            }

            for (int i = 0; i <= chips; i++)
            {
                distribution[current] = i;
                DistributeChips(chips - i, pools, distribution, current + 1, results);
            }
        }

        T DeepCopy<T>(T source)
        {
            T newPrize = ThirdPartyByAutomapper<T>(source);
            return newPrize;
        }

        public static T ThirdPartyByFastDeepCloner<T>(T original)
        {
            return (T)DeepCloner.Clone(original);
        }

        public static T ThirdPartyByAutomapper<T>(T original)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<T, T>();
            });
            var mapper = config.CreateMapper();
            T clone = mapper.Map<T, T>(original);
            return clone;
        }
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            Random rng = new Random();
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        public static T GetRandomElement<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("The list cannot be null or empty.");
            }

            Random random = new Random();
            int index = random.Next(list.Count);

            return list[index];
        }
        static Random rng = new Random();
        public static Chip GetRandomChip(Dictionary<Player, Dictionary<string, Chip>> totalChips)
        {

            if (totalChips == null || totalChips.Count == 0)
            {
                throw new ArgumentException("The totalChips dictionary cannot be null or empty.");
            }

            // Step 1: Get a random player
            List<Player> players = new List<Player>(totalChips.Keys);
            Player randomPlayer = players[rng.Next(players.Count)];

            // Step 2: Get a random chip from the selected player's dictionary
            Dictionary<string, Chip> chips = totalChips[randomPlayer];
            if (chips == null || chips.Count == 0)
            {
                throw new InvalidOperationException("The selected player's chip dictionary is empty.");
            }

            List<Chip> chipList = new List<Chip>(chips.Values);
            Chip randomChip = chipList[rng.Next(chipList.Count)];

            return randomChip;
        }
    }


}
