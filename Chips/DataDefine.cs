using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chips
{
    public class ValueChangedEventArgs : EventArgs
    {
        public int Count { get; }
        public int Prize { get; }
        public int ChipsCount { get; }
        public OnePriceWidget Widget { get; }
        public ValueChangedEventArgs(int newCount, int newPrize, int newChipsCount, OnePriceWidget priceWidget)
        {
            Count = newCount;
            Prize = newPrize;
            ChipsCount = newChipsCount;
            Widget = priceWidget;
        }
    }
    internal class Chip
    {
        public Player BelongPlayer;
        public string UUID;
        /// <summary>
        /// 此筹码当前所在的奖池，下注之后将不会是null
        /// </summary>
        public Prize? CurrentPrize;
        public Chip() { }
        public Chip(Player player) 
        {
            BelongPlayer = player;
            UUID = Guid.NewGuid().ToString();
            CurrentPrize = null;
        }

        public void BetToPool(Prize? prize)
        {
            //if (CurrentPrize != null) return;
            CurrentPrize = prize;
        }

        public bool HasBet()
        {
            return CurrentPrize != null; 
        }
    }

    internal class Player
    {
        private string UUID;
        private bool IsMine;
        public Player(bool isMine = false)
        {
            UUID = Guid.NewGuid().ToString();
            IsMine = isMine;
            if(isMine)
            {
                UUID = "0";
            }
        }

        public bool GetIsMine() { return IsMine; }
        public string GetUUID() { return UUID; }
    }

    internal class Prize
    {
        public int Cost;
        public int Count;
        public int TotalChipCount;
        public int Index;
        public int CurrentChipCount = 0;
        public int LeftCount;

        public int MineChipCount = 0;
        public Dictionary<Player, Dictionary<string, Chip>> BettingData = new Dictionary<Player, Dictionary<string, Chip>>();
        public Dictionary<string, Dictionary<string, Chip>> MineBettingData = new Dictionary<string, Dictionary<string, Chip>>();
        public Prize()
        {

        }
        public Prize(int cost, int count, int total, int index)
        {
            Cost = cost;
            Count = count;
            LeftCount = count;
            TotalChipCount = total;
            Index = index;
        }
        /// <summary>
        /// 向此奖池中下注
        /// </summary>
        /// <param name="chip"></param>
        public bool AddChip(Chip chip)
        {
            if (CurrentChipCount >= TotalChipCount) return false;
            if(BettingData.TryGetValue(chip.BelongPlayer, out Dictionary<string, Chip>? value))
            {
                value.Add(chip.UUID, chip);
            }
            else
            {
                BettingData.Add(chip.BelongPlayer, new Dictionary<string, Chip>());
                BettingData[chip.BelongPlayer].Add(chip.UUID, chip);
            }
            chip.BetToPool(this);
            CurrentChipCount++;
            return true;
        }
        public void ResetCount()
        {
            LeftCount = Count;
        }
        public bool ExtractOne(ref bool isZero)
        {
            if(LeftCount <= 0) return false;
            LeftCount--;
            isZero = LeftCount <= 0;
            return true;
        }
        public bool RemoveChip(Chip chip)
        {
            if(CurrentChipCount <= 0) return false;
            if (BettingData.TryGetValue(chip.BelongPlayer, out Dictionary<string, Chip>? value))
            {
                value.Remove(chip.UUID);
                chip.BetToPool(null);
                CurrentChipCount--;
                return true;
            }
            return false;
        }

        public int GetChipCountLeft()
        {
            return TotalChipCount - CurrentChipCount;
        }

        public List<Chip> AddMyChip(int Count, string uuid)
        {
            List<Chip> chips = new List<Chip>();
            MineChipCount = Count;
            if(Count <= 0)
            {
                return chips;
            }
            MineBettingData.Add(uuid, new Dictionary<string, Chip>());
            for (int i = 0; i < MineChipCount; i++)
            {
                Chip chip = new Chip(DataHandler.Mine);
                MineBettingData[uuid].Add(chip.UUID, chip);
                chip.BetToPool(this);
                chips.Add(chip);
            }
            return chips;
        }

        public void RemoveMyChip(string uuid)
        {
            MineBettingData.Remove(uuid);
        }

        // Fisher-Yates shuffle
        static void Shuffle(List<string> list, int seed)
        {
            Random random = new Random(seed); // 使用种子以确保每个任务都独立
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}
