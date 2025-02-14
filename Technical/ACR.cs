﻿namespace ATAS.Indicators.Technical
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Average Candle Range")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.ACRDescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602323")]
    public class ACR : Indicator
	{
		#region Fields
		
		private readonly ValueDataSeries _renderSeries = new("RenderSeries", Strings.Visualization);
		private readonly ValueDataSeries _rangeSeries = new("Ranges");
		private bool _ignoreWicks;
		private int _lastSession;

		#endregion

		[Display(ResourceType = typeof(Strings), Name = nameof(Strings.IgnoreWicks), GroupName = nameof(Strings.Settings), Description = nameof(Strings.IgnoreWicksDescription), Order = 100)]
		public bool IgnoreWicks
		{
			get => _ignoreWicks;
			set
			{
				_ignoreWicks = value;
				RecalculateValues();
			}
		}

		#region ctor

		public ACR()
			: base(true)
		{
			Panel = IndicatorDataProvider.NewPanel;
			DenyToChangePanel = true;

			_renderSeries.VisualType = VisualMode.Histogram;

			DataSeries[0] = _renderSeries;
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
			{
				_renderSeries.Clear();
				_rangeSeries.Clear();
				_lastSession = 0;
			}

			if (IsNewSession(bar) && bar > 0)
				_lastSession = bar;

			var candle = GetCandle(bar);

			var range = IgnoreWicks
				? Math.Abs(candle.Open - candle.Close)
				: candle.High - candle.Low;

			_rangeSeries[bar] = range;

			_renderSeries[bar] = _rangeSeries.CalcAverage(bar - _lastSession + 1, bar);
		}

		#endregion
	}
}