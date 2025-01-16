using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using static System.Net.Mime.MediaTypeNames;

namespace Chips
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 每个奖池的总下注数
        /// </summary>
        List<int> totalChipCount = new List<int>();
        /// <summary>
        /// 每个奖池中奖品的价值
        /// </summary>
        List<int> prePrizeValue = new List<int>();
        /// <summary>
        /// 每个奖池中的奖品数量
        /// </summary>
        List<int> prePrizeCount = new List<int>();

        int chipPerHas = 12;
        long startTime;
        DataHandler? dataHandler = null;
        string filePath = @"file.txt";
        public MainForm()
        {
            InitializeComponent();
            int index = 0;
            foreach (object item in flowLayoutPanel1.Controls)
            {
                if (item.GetType() == typeof(OnePriceWidget))
                {
                    OnePriceWidget onePriceWidget = (OnePriceWidget)item;
                    onePriceWidget.Index = index;
                    onePriceWidget.ValueChanged += OnePriceWidget_ValueChanged;
                    index++;
                    totalChipCount.Add(onePriceWidget.ChipCount);
                    prePrizeValue.Add(onePriceWidget.PrizeValue);
                    prePrizeCount.Add(onePriceWidget.Count);
                }
            }
            CalButton_Click(null, null);
            textBoxChip_TextChanged(null, null);
        }

        private void OnePriceWidget_ValueChanged(object sender, ValueChangedEventArgs e)
        {

            OnePriceWidget onePriceWidget = e.Widget;
            totalChipCount[onePriceWidget.Index] = onePriceWidget.ChipCount;
            prePrizeValue[onePriceWidget.Index] = onePriceWidget.PrizeValue;
            prePrizeCount[onePriceWidget.Index] = onePriceWidget.Count;
            
        }
        /// <summary>
        /// 模拟其他玩家下注
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalButton_Click(object sender, EventArgs e)
        {
            dataHandler = dataHandler == null ? new DataHandler(chipPerHas, totalChipCount, prePrizeValue, prePrizeCount) : dataHandler;
            dataHandler.SimulateAllBet();
            foreach (object item in flowLayoutPanel1.Controls)
            {
                if (item.GetType() == typeof(OnePriceWidget))
                {
                    OnePriceWidget onePriceWidget = (OnePriceWidget)item;

                    
                    onePriceWidget.PeopleCount = dataHandler.GetPeopleCountByIndex(onePriceWidget.Index);
                }
            }
        }


        private void CalMineButton_Click(object sender, EventArgs e)
        {
            int Y = totalChipCount.Count;

            // 生成所有下注方式
            List<int[]> results = new List<int[]>();
            DataHandler.DistributeChips(chipPerHas, Y, new int[Y], 0, results);

            // 提示用户可能的长时间计算
            DialogResult res = MessageBox.Show("共有 " + results.Count + " 种下注方式。模拟计算自己最高期望下注，将会消耗很长时间，是否继续？");
            if (res != DialogResult.OK) return;

            // 只处理前100种下注方式
            results = results.Take(100000).ToList();

            double? max = -1;
            string result = "";

            // 使用并行结构进行计算
            Parallel.ForEach(results, item =>
            {
                List<string> log = new List<string>();
                double? p = dataHandler?.GetSimulatePrize(item, int.Parse(SimulateTimes.Text), ref log);
                lock (this)
                {
                    if (p > max)
                    {
                        max = p;
                        result = string.Join(',', item);
                    }
                }
                Debug.WriteLine($"下注方式{string.Join(',', item)}的价值期望为{p}");
            });

            MessageBox.Show($"最佳下注方式是{result},价值期望是{max}");
        }

        private void textBoxChip_TextChanged(object sender, EventArgs e)
        {
            chipPerHas = int.Parse(textBoxChip.Text);
            ResetMineCount(chipPerHas);
        }

        private void SimulateOneTimeButton_Click(object sender, EventArgs e)
        {
            int simulateCount = int.Parse(SimulateTimes.Text);
            List<string> log = new List<string>();
            double? prize = dataHandler?.GetSimulatePrize(GetCurrentMineCount(), simulateCount, ref log);
            textBoxLog.Text = string.Join("\r\n", log);
            MessageBox.Show($"模拟{simulateCount}次，中奖价值期望为{prize}");
        }

        void ResetMineCount(int Max)
        {
            foreach (OnePriceWidget item in GetOnePriceWidgets())
            {
                item.MineCount = 0;
                item.SetMineCountMax(Max);
            }
        }
        int[] GetCurrentMineCount()
        {
            int[] res = new int[chipPerHas];
            var s = GetOnePriceWidgets();
            for (int i = 0; i < s.Count; i++)
            {
                res[i] = s[i].MineCount;
            }
            return res;
        }
        List<OnePriceWidget> GetOnePriceWidgets()
        {
            List<OnePriceWidget> res = new List<OnePriceWidget>();
            foreach (object item in flowLayoutPanel1.Controls)
            {
                if (item.GetType() == typeof(OnePriceWidget))
                {
                    OnePriceWidget onePriceWidget = (OnePriceWidget)item;
                    res.Add(onePriceWidget);
                }
            }
            return res;
        }
    }
}
