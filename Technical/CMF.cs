namespace ATAS.Indicators.Technical
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using ATAS.Indicators.Drawing;
    using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Chaikin Money Flow")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.CMFDescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602540")]
	public class CMF : Indicator
	{
		#region Fields

		private readonly ValueDataSeries _adSeries = new("ADSeries");
		private readonly ValueDataSeries _cmf = new("Cmf", "CMFline");
		private readonly RangeDataSeries _cmfHigh = new("CmfHigh", "CMFHigh")
		{ 
			DescriptionKey = nameof(Strings.PositiveValueColorDescription) 
		};

		private readonly RangeDataSeries _cmfLow = new("CmfLow", "CMFLow")
		{
			DescriptionKey = nameof(Strings.NegativeValueColorDescription)
		};

		private decimal _ad;
		private decimal _dailyHigh;
		private decimal _dailyLow;
		private DateTime _lastSessionTime;

		private int _period = 21;

        #endregion

        #region Properties

        [Parameter]
        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.Period), Description = nameof(Strings.PeriodDescription))]
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

		public CMF()
			: base(true)
		{
			Panel = IndicatorDataProvider.NewPanel;
			
			_cmfHigh.RangeColor = DefaultColors.Green.Convert();
			_cmfLow.RangeColor = DefaultColors.Red.Convert();
			_cmf.Color = DefaultColors.Gray.Convert();

			DataSeries[0] = _cmfHigh;
            DataSeries.Add(_cmfLow);
			DataSeries.Add(_cmf);
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
			{
				_dailyHigh = _dailyLow = 0;
				return;
			}

			var candle = GetCandle(bar);

			if (IsNewSession(bar))
			{
				if (_lastSessionTime != candle.Time)
				{
					_lastSessionTime = candle.Time;
					_dailyHigh = _dailyLow = 0;
				}
			}

			if (candle.High > _dailyHigh || _dailyHigh == 0)
				_dailyHigh = candle.High;

			if (candle.Low < _dailyLow || _dailyLow == 0)
				_dailyLow = candle.Low;

			if (_dailyHigh != _dailyLow)
				_ad = (candle.Close - _dailyLow - (_dailyHigh - candle.Close)) / (_dailyHigh - _dailyLow) * candle.Volume;
			else
				_ad = _adSeries[bar - 1];

			_adSeries[bar] = _ad;

			if (bar > _period)
			{
				decimal adSum = 0;
				decimal volumeSum = 0;

				for (var i = 0; i <= _period; i++)
				{
					adSum += _adSeries[bar - i];
					volumeSum += GetCandle(bar - i).Volume;
				}

				var result = adSum / volumeSum;
				_cmf[bar] = result;

				if (result >= 0)
				{
					_cmfHigh[bar].Upper = result;
					_cmfHigh[bar].Lower = 0;
				}
				else
				{
					_cmfLow[bar].Upper = 0;
					_cmfLow[bar].Lower = result;
				}
			}
		}

		#endregion
	}
}