﻿namespace ATAS.Indicators.Technical
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Windows.Media;

	using ATAS.Indicators.Technical.Properties;

	[DisplayName("MACD Leader")]
	public class MacdLeader : Indicator
	{
		#region Fields
		
		private readonly ValueDataSeries _renderSeries = new ValueDataSeries(Resources.Indicator);
		private readonly EMA _longEma = new EMA();
		private readonly EMA _longEmaSmooth = new EMA();
		private readonly MACD _macd = new MACD();
		private readonly EMA _shortEma = new EMA();
		private readonly EMA _shortEmaSmooth = new EMA();

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Resources), Name = "Period", GroupName = "Settings", Order = 100)]
		public int MacdPeriod
		{
			get => _macd.SignalPeriod;
			set
			{
				if (value <= 0)
					return;

				_macd.SignalPeriod = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "ShortPeriod", GroupName = "Settings", Order = 110)]
		public int MacdShortPeriod
		{
			get => _macd.ShortPeriod;
			set
			{
				if (value <= 0)
					return;

				_macd.ShortPeriod = _shortEma.Period = _shortEmaSmooth.Period = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "LongPeriod", GroupName = "Settings", Order = 120)]
		public int MacdLongPeriod
		{
			get => _macd.LongPeriod;
			set
			{
				if (value <= 0)
					return;

				_macd.LongPeriod = _longEma.Period = _longEmaSmooth.Period = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public MacdLeader()
		{
			Panel = IndicatorDataProvider.NewPanel;

			_renderSeries.Color = Colors.Blue;

			_macd.LongPeriod = _longEma.Period = _longEmaSmooth.Period = 26;
			_macd.ShortPeriod = _shortEma.Period = _shortEmaSmooth.Period = 12;
			_macd.SignalPeriod = 9;

			DataSeries[0] = _renderSeries;
			DataSeries.AddRange(_macd.DataSeries);
		}

		#endregion

		#region Protected methods

		#region Overrides of BaseIndicator

		protected override void OnRecalculate()
		{
			DataSeries.ForEach(x => x.Clear());
		}

		#endregion

		protected override void OnCalculate(int bar, decimal value)
		{
			var macd = _macd.Calculate(bar, value);

			_shortEma.Calculate(bar, value);
			_shortEmaSmooth.Calculate(bar, value - _shortEma[bar]);
			_longEma.Calculate(bar, value);
			_longEmaSmooth.Calculate(bar, value - _longEma[bar]);

			_renderSeries[bar] = _macd[bar] + _shortEmaSmooth[bar] - _longEmaSmooth[bar];
		}

		#endregion

		#region Overrides of BaseIndicator

		#endregion
	}
}