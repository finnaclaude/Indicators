﻿namespace ATAS.Indicators.Technical
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    using ATAS.Indicators.Drawing;

    using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Greatest Swing Value")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.GreatestSwingDescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602635")]
	public class GreatestSwing : Indicator
	{
		#region Fields

		private readonly ValueDataSeries _buy = new("BuySwing");
		private readonly ValueDataSeries _sell = new("SellSwing");

		private readonly ValueDataSeries _buySeries = new("BuySeries", Strings.Buys)
		{
			Color = DefaultColors.Green.Convert(),
			DescriptionKey = nameof(Strings.TopChannelSettingsDescription)
		};

		private readonly ValueDataSeries _sellSeries = new("SellSeries", Strings.Sells)
		{
            DescriptionKey = nameof(Strings.BottomChannelSettingsDescription)
        };

		private decimal _multiplier = 5;
        private int _period = 10;

        #endregion

        #region Properties

        [Parameter]
        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.Period), GroupName = nameof(Strings.Settings), Description = nameof(Strings.PeriodDescription), Order = 110)]
		[Range(1, 10000)]
		public int Period
		{
			get => _period;
			set
			{
				_period = value;
				RecalculateValues();
			}
		}

        [Parameter]
        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.Multiplier), GroupName = nameof(Strings.Settings), Description = nameof(Strings.MultiplierDescription), Order = 110)]
		[Range(0.0000001, 10000000)]
        public decimal Multiplier
		{
			get => _multiplier;
			set
			{
				_multiplier = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public GreatestSwing()
			: base(true)
		{
			DenyToChangePanel = true;
			
			DataSeries[0] = _buySeries;
			DataSeries.Add(_sellSeries);
		}

		#endregion

		#region Protected methods
		
		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
			{
				_buy.Clear();
				_sell.Clear();
				return;
			}

			var candle = GetCandle(bar);

			if (candle.Close < candle.Open)
				_buy[bar] = candle.High - candle.Open;

			if (candle.Close > candle.Open)
				_sell[bar] = candle.Open - candle.Low;

			var buyMa = SkipZeroMa(bar - 1, _buy);
			var sellMa = SkipZeroMa(bar - 1, _sell);

			_buySeries[bar] = candle.Open + _multiplier * buyMa;
			_sellSeries[bar] = candle.Open - _multiplier * sellMa;
		}

		#endregion

		#region Private methods

		private decimal SkipZeroMa(int bar, ValueDataSeries series)
		{
			var nonZeroValues = 0;
			var sum = 0m;

			for (var i = Math.Max(0, bar - _period); i <= bar; i++)
			{
				if (series[i] == 0)
					continue;

				nonZeroValues++;
				sum += series[i];
			}

			if (nonZeroValues == 0)
				return 0;

			return sum / nonZeroValues;
		}

		#endregion
	}
}