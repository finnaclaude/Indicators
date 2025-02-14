﻿namespace ATAS.Indicators.Technical
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using OFT.Attributes;
    using OFT.Localization;

    [DisplayName("Bands/Envelope")]
    [Display(ResourceType = typeof(Strings), Description = nameof(Strings.BandsEnvelopeDescription))]
    [HelpLink("https://help.atas.net/en/support/solutions/articles/72000602522")]
	public class BandsEnvelope : Indicator
	{
		#region Nested types

		public enum Mode
		{
			[Display(ResourceType = typeof(Strings), Name = nameof(Strings.Percent))]
			Percentage,

			[Display(ResourceType = typeof(Strings), Name = nameof(Strings.PriceChange))]
			Value,

			[Display(ResourceType = typeof(Strings), Name = nameof(Strings.Ticks))]
			Ticks
		}

		#endregion

		#region Fields

		private readonly RangeDataSeries _renderSeries = new("RenderSeries", Strings.Visualization)
		{
			DrawAbovePrice = false,
			DescriptionKey = nameof(Strings.RangeAreaDescription)
		};
		private readonly ValueDataSeries _topSeries = new("TopSeries", Strings.TopBand) 
		{
			DescriptionKey = nameof(Strings.TopBandDscription) 
		};

        private readonly ValueDataSeries _botSeries = new("BotSeries", Strings.BottomBand)
		{ 
			DescriptionKey = nameof(Strings.BottomBandDscription) 
		};

        private Mode _calcMode = Mode.Percentage;
        private decimal _rangeFilter = 1;

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Strings), Name = nameof(Strings.Mode), GroupName = nameof(Strings.Settings), Description = nameof(Strings.CalculationModeDescription), Order = 100)]
		public Mode CalcMode
		{
			get => _calcMode;
			set
			{
				_calcMode = value;
				RecalculateValues();
			}
		}

        [Parameter]
        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.Range), GroupName = nameof(Strings.Settings), Description = nameof(Strings.DeviationRangeDescription), Order = 110)]
		[Range(0, 100)]
		public decimal RangeFilter
		{
			get => _rangeFilter;
			set
			{
				if (_calcMode == Mode.Percentage)
					return;

				_rangeFilter = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public BandsEnvelope()
		{
			DataSeries[0] = _renderSeries;
			DataSeries.Add(_topSeries);
			DataSeries.Add(_botSeries);
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			switch (_calcMode)
			{
				case Mode.Percentage:
					var percValue = value * _rangeFilter * 0.01m;
					_renderSeries[bar].Upper = value + percValue;
					_renderSeries[bar].Lower = value - percValue;
					break;
				case Mode.Value:
					_renderSeries[bar].Upper = value + _rangeFilter;
					_renderSeries[bar].Lower = value - _rangeFilter;
					break;
				case Mode.Ticks:
					var tickValue = _rangeFilter * InstrumentInfo.TickSize;
					_renderSeries[bar].Upper = value + tickValue;
					_renderSeries[bar].Lower = value - tickValue;
					break;
			}

			_topSeries[bar] = _renderSeries[bar].Upper;
			_botSeries[bar] = _renderSeries[bar].Lower;
		}

		#endregion
	}
}