﻿namespace ATAS.Indicators.Technical;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OFT.Attributes;
using OFT.Localization;

[DisplayName("Accumulation / Distribution Flow")]
[Display(ResourceType = typeof(Strings), Description = nameof(Strings.ADFDescription))]
[HelpLink("https://help.atas.net/en/support/solutions/articles/72000602569")]
public class ADF : Indicator
{
	#region Fields

	private readonly ValueDataSeries _adf = new("AdfValues");

	private readonly ValueDataSeries _renderSeries = new("RenderSeries", "ADF")
	{
		VisualType = VisualMode.Histogram,
		UseMinimizedModeIfEnabled = true
	};

	private readonly SMA _sma = new()
	{
		Period = 14
	};

	private bool _usePrev = true;

    #endregion

    #region Properties

    [Parameter]
    [Display(ResourceType = typeof(Strings), Name = nameof(Strings.SMAPeriod), GroupName = nameof(Strings.Settings), Description = nameof(Strings.SMAPeriodDescription), Order = 100)]
	[Range(1, 10000)]
	public int Period
	{
		get => _sma.Period;
		set
		{
			_sma.Period = value;
			RecalculateValues();
		}
	}

	[Display(ResourceType = typeof(Strings), Name = nameof(Strings.UsePreviousClose), GroupName = nameof(Strings.Settings), Description = nameof(Strings.UsePreviousCloseDescription), Order = 110)]
	public bool UsePrev
	{
		get => _usePrev;
		set
		{
			_usePrev = value;
			RecalculateValues();
		}
	}

	#endregion

	#region ctor

	public ADF()
		: base(true)
	{
		Panel = IndicatorDataProvider.NewPanel;
		DataSeries[0] = _renderSeries;
	}

	#endregion

	#region Protected methods

	protected override void OnCalculate(int bar, decimal value)
	{
		if (bar == 0)
		{
			_sma.Calculate(bar, _adf[bar]);
			return;
		}

		var candle = GetCandle(bar);

		if (candle.High - candle.Low == 0)
			_adf[bar] = _adf[bar - 1];
		else
		{
			if (_usePrev)
			{
				var prevCandle = GetCandle(bar - 1);
				_adf[bar] = _adf[bar - 1] + (candle.Close - prevCandle.Close) * candle.Volume / (candle.High - candle.Low);
			}
			else
				_adf[bar] = _adf[bar - 1] + (candle.Close - candle.Open) * candle.Volume / (candle.High - candle.Low);
		}

		_renderSeries[bar] = _sma.Calculate(bar, _adf[bar]);
	}

	#endregion
}