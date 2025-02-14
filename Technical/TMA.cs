﻿namespace ATAS.Indicators.Technical
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Triangular Moving Average")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.TMADescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602233")]
	public class TMA : Indicator
	{
		#region Fields

		private readonly ValueDataSeries _sma1 = new("Sma1Id", "Sma1");
		private readonly ValueDataSeries _sma2 = new("Sma2Id", "Sma2");
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

        #region Protected methods

        protected override void OnCalculate(int bar, decimal value)
		{
			var n1 = (int)Math.Ceiling(_period / 2m);
			var n2 = _period % 2 == 1 ? n1 : n1 + 1;

			_sma1[bar] = DynamicSma(bar, n1, SourceDataSeries);
			_sma2[bar] = DynamicSma(bar, n2, _sma1);
			this[bar] = _sma2[bar];
		}

		#endregion

		#region Private methods

		private decimal DynamicSma(int bar, int period, IDataSeries series)
		{
			var sum = 0m;

			for (var i = Math.Max(0, bar - period); i < bar; i++)
				sum += (decimal)series[i];

			return sum / period;
		}

		#endregion
	}
}