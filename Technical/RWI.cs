﻿namespace ATAS.Indicators.Technical
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using ATAS.Indicators.Drawing;
    using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Random Walk Indicator")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.RWIDescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602453")]
	public class RWI : Indicator
	{
		#region Fields

		private readonly ValueDataSeries _highSeries = new("HighSeries", Strings.Highest) 
		{ 
			Color = DefaultColors.Green.Convert(),
            DescriptionKey = nameof(Strings.UpTrendSettingsDescription)
        };

		private readonly ValueDataSeries _lowSeries = new("LowSeries", Strings.Lowest)
		{
			DescriptionKey = nameof(Strings.DownTrendSettingsDescription)
		};

		private readonly TrueRange _trueRange = new();
		private int _period = 10;

        #endregion

        #region Properties

        [Parameter]
        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.Period), GroupName = nameof(Strings.Settings), Description = nameof(Strings.PeriodDescription), Order = 100)]
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

		#endregion

		#region ctor

		public RWI()
			: base(true)
		{
			Panel = IndicatorDataProvider.NewPanel;
			Add(_trueRange);

			DataSeries[0] = _lowSeries;
			DataSeries.Add(_highSeries);
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
			{
				DataSeries.ForEach(x => x.Clear());
				return;
			}

			if (bar < _period)
				return;

			var maxHigh = 0m;
			var maxLow = 0m;
			var candle = GetCandle(bar);

			for (var i = 1; i <= _period; i++)
			{
				var stepCandle = GetCandle(bar - i);
				var atr = Atr(bar - 1, i);
				var high = atr == 0 ? 0 : (candle.High - stepCandle.Low) / (atr * (decimal)Math.Sqrt(i));
				var low = atr == 0 ? 0 : (stepCandle.High - candle.Low) / (atr * (decimal)Math.Sqrt(i));

				if (high > maxHigh)
					maxHigh = high;

				if (low > maxLow)
					maxLow = low;
			}

			_highSeries[bar] = maxHigh;
			_lowSeries[bar] = maxLow;
		}

		#endregion

		#region Private methods

		private decimal Atr(int bar, int period)
		{
			return ((ValueDataSeries)_trueRange.DataSeries[0]).CalcAverage(period, bar);
		}

		#endregion
	}
}