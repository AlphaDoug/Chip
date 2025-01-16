using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chips
{


    public partial class OnePriceWidget : UserControl
    {
        private string _name;
        private int _count;
        private int _price;
        private int _chipsCount;
        private int _index;
        private int _peopleCount;
        private int _mineCount;

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }
        public int PeopleCount
        {
            get
            {
                return _peopleCount;
            }
            set
            {
                _peopleCount = value;
                textBox4.Text = _peopleCount.ToString();
            }
        }
        public int MineCount
        {
            get
            {
                return _mineCount;
            }
            set
            {
                _mineCount = value;
                numericUpDown1.Value = _mineCount;
            }
        }
        [Category("配置")]
        [Description("奖品名")]
        [DisplayName("奖品名")]
        [DefaultValue("名称")]
        public string PrizeName
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
                label1.Text = value;
            }
        }

        [Category("配置")]
        [Description("奖品数量")]
        [DisplayName("奖品数量")]
        [DefaultValue("1")]
        public int Count
        {
            get
            {
                return this._count;
            }
            set
            {
                this._count = value;
                numericUpDown4.Value = value;
            }
        }


        [Category("配置")]
        [Description("奖品价值")]
        [DisplayName("奖品价值")]
        [DefaultValue("1")]
        public int PrizeValue
        {
            get
            {
                return this._price;
            }
            set
            {
                this._price = value;
                numericUpDown2.Value = value;
            }
        }


        [Category("配置")]
        [Description("下注数量")]
        [DisplayName("下注数量")]
        [DefaultValue("1")]
        public int ChipCount
        {
            get
            {
                return this._chipsCount;
            }
            set
            {
                this._chipsCount = value;
                numericUpDown3.Value = value;
            }
        }

        public OnePriceWidget()
        {
            InitializeComponent();
        }
        public void SetMineCountMax(int Max)
        {
            if(numericUpDown1.Value > Max)
            {
                numericUpDown1.Value = Max;
            }
            numericUpDown1.Maximum = Max;
        }

        private void NativeValueChanged()
        {
            Count = ((int)numericUpDown4.Value);
            PrizeValue = ((int)numericUpDown2.Value);
            ChipCount = ((int)numericUpDown3.Value);
            MineCount = ((int)numericUpDown1.Value);
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(Count, PrizeValue, ChipCount, this));
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NativeValueChanged();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            NativeValueChanged();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            NativeValueChanged();
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            NativeValueChanged();
        }
    }
}
