﻿namespace ATAS.Indicators.Technical
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Moving Median")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.MMedDescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602433")]
	public class MMed : Indicator
	{
		#region Fields

		private readonly ValueDataSeries _renderSeries = new("RenderSeries", Strings.Visualization);
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

		public MMed()
		{
			DataSeries[0] = _renderSeries;
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			var startBar = Math.Max(0, bar - Period);
			var orderedValues = new List<decimal>();

			for (var i = startBar; i <= bar; i++)
				orderedValues.Add((decimal)SourceDataSeries[i]);

			orderedValues = orderedValues
				.OrderBy(x => x)
				.ToList();

			if (bar < Period)
			{
				var targetBar = bar - (bar + 1) / 2;

				if ((bar + 1) % 2 == 1)
					_renderSeries[bar] = orderedValues[bar - targetBar];
				else
					_renderSeries[bar] = (orderedValues[bar - targetBar] + orderedValues[bar - (targetBar + 1)]) / 2;
			}
			else
			{
				var targetBar = bar - Period / 2;

				if (Period % 2 == 1)
					_renderSeries[bar] = orderedValues[bar - targetBar];
				else
					_renderSeries[bar] = (orderedValues[bar - targetBar] + orderedValues[bar - (targetBar + 1)]) / 2;
			}
		}

		#endregion
	}
}