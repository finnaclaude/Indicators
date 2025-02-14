using System.ComponentModel;
using System.Drawing;
using System.Timers;
using ATAS.DataFeedsCore;
using ATAS.Indicators;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;
using Utils.Common.Logging;
using MarketDataType = ATAS.DataFeedsCore.MarketDataType;
using Timer = System.Timers.Timer;

namespace DomV10;

using System;
using System.Collections.Generic;

using OFT.Attributes;

[DisplayName("MBO DOM")]
[HelpLink("https://help.atas.net/en/support/solutions/articles/72000633231")]
[Category(IndicatorCategories.OrderBook)]
public partial class MainIndicator : Indicator
{
    private MboGridController _gridController = new();
    private Timer _timer = new();
    private readonly object _renderLock = new();
    private MarketDataArg? _lastAsk = null;
    private MarketDataArg? _lastBid = null;
    private decimal _lastPrice = 0;

    public MainIndicator() : base(true)
    {
	    ((ValueDataSeries)DataSeries[0]).IsHidden = true;
	    EnableCustomDrawing = true;
        SubscribeToDrawingEvents(DrawingLayouts.Final);
        DenyToChangePanel = true;
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        try
        {
            _timer.Enabled = false;
            _timer.Stop();
            _timer.Dispose();
        }
        catch
        {
            //ignored
        }
    }

    private void TickTok(object? sender, ElapsedEventArgs e)
    {
        _timer?.Stop();
        _gridController?.Tick();
        _timer?.Start();
    }

    protected override async void OnInitialize()
    {
        await SubscribeMarketByOrderData();
        _gridController = new();
        _timer = new Timer();
        _timer.Elapsed += TickTok;
        _timer.Interval = 1000;
        _timer.Enabled = true;
        _timer.Start();

        OrderSizeFilter.PropertyChanged += UpdateUi;
        MinBlockSize.PropertyChanged += UpdateUi;
        RowOrderVolume.PropertyChanged += UpdateUi;
        RowOrderCount.PropertyChanged += UpdateUi;
    }

    public override void Dispose()
    {
        base.Dispose();
        OrderSizeFilter.PropertyChanged -= UpdateUi;
        MinBlockSize.PropertyChanged -= UpdateUi;
        RowOrderVolume.PropertyChanged -= UpdateUi;
        RowOrderCount.PropertyChanged -= UpdateUi;
    }

    private void UpdateUi(object? sender, PropertyChangedEventArgs e) => RedrawChart(_emptyRedrawArg);

    protected override void OnApplyDefaultColors()
    {
        if (ChartInfo == null) 
	        return;

        _bidColor = ChartInfo.ColorsStore.UpCandleColor;
        _askColor = ChartInfo.ColorsStore.DownCandleColor;
        _textColor = ChartInfo.ColorsStore.FootprintMaximumVolumeTextColor;

        RedrawChart();
    }

    protected override void OnMarketByOrdersChanged(IEnumerable<MarketByOrder> orders)
    {
        if (!_gridController.Update(orders)) _gridController.Load(MarketByOrders);
    }

    protected override void MarketDepthChanged(MarketDataArg depth)
    {
        if (MarketDepthInfo == null) return;
        if (!_gridController.Update(depth)) _gridController.Load(MarketDepthInfo.GetMarketDepthSnapshot());
    }

    protected override void OnNewTrade(MarketDataArg trade)
    {
        _gridController.UpdateTrade(trade);
    }

    protected override void OnBestBidAskChanged(MarketDataArg depth)
    {
        base.OnBestBidAskChanged(depth);
        if (depth.IsAsk) _lastAsk = depth;
        if (depth.IsBid) _lastBid = depth;
    }

    protected override void OnCalculate(int bar, decimal value)
    {
        _lastPrice = GetCandle(bar).Close;
    }

    protected override void OnRender(RenderContext context, DrawingLayouts layout)
    {
	    try
	    {
		    lock (_renderLock)
		    {
			    if (ChartInfo == null)
				    return;

			    if (Container == null)
				    return;

			    if (InstrumentInfo == null)
				    return;

			    if (_lastAsk is null || _lastBid is null)
				    return;

			    var tickSize = InstrumentInfo.TickSize;
			    var fixHigh = GetFixPrice(ChartInfo.PriceChartContainer.High, true);
			    var fixLow = GetFixPrice(ChartInfo.PriceChartContainer.Low, false);

			    if (fixLow >= fixHigh)
				    return;

			    var yy1 = ChartInfo.GetYByPrice(ChartInfo.PriceChartContainer.High);
			    var yy2 = ChartInfo.GetYByPrice(ChartInfo.PriceChartContainer.Low);

			    var height = Math.Abs(yy2 - yy1) / ((ChartInfo.PriceChartContainer.High - ChartInfo.PriceChartContainer.Low) / InstrumentInfo.TickSize);

                var (fontSize, fontWidth) =
				    SetFontSize(context, ChartInfo.PriceChartContainer.PriceRowHeight);

			    var showSeparately = height >= 4.5m;

				var showText = fontSize >= 5;

                var maxScreenSize = Container.RelativeRegion.Width * 0.5m;
			    var (maxVol, maxCount) = _gridController.MaxInView(fixHigh, fixLow, tickSize, true);

			    var aggregationBaseRow = new Rectangle() { X = Container.RelativeRegion.Right - 1, Width = 0, };

			    if (showText && (ShowSum || ShowCount))
			    {
				    var maxWidth = (int)(((ShowSum ? 1 : 0) + (ShowCount ? 1 : 0)) * 6 * fontWidth);

				    if (maxWidth > 0)
				    {
					    aggregationBaseRow.X -= maxWidth;
					    aggregationBaseRow.Width = maxWidth;
				    }
			    }

			    var prevY = 0;
			    Dictionary<decimal, int> tempSize = new();
			    Rectangle selectedRectangle = new();
			    string tooltip = "";
				
                for (var price = fixHigh; price >= fixLow; price -= tickSize)
			    {
				    var y1 = 0;

				    if (price == fixHigh)
					    y1 = ChartInfo.GetYByPrice(price);
				    else
					    y1 = prevY;

				    var y2 = ChartInfo.GetYByPrice(price - tickSize);
				    
				    if (showSeparately)
				    {
					    y1 += 1;
                    }

				    var realHeight = y2 - y1;

				    prevY = y2;

                    var blockInRow = _gridController.GetItemInRow(price, _lastAsk, _lastBid, !showSeparately&& !showText);

                    var (rowVol, dataType) = _gridController.Volume(price, _lastAsk, _lastBid, _lastPrice);

                    if (dataType == DataType.Lvl2)
				    {
					    aggregationBaseRow.X += aggregationBaseRow.Width;
					    aggregationBaseRow.Width = 0;
				    }

				    if (blockInRow.Orders.Length == 0 && rowVol == 0)
					    continue;

				    if (rowVol > 0 && blockInRow.Orders.Length == 0)
					    rowVol = 0;

				    var pen = RenderPens.Transparent;

				    if (blockInRow.Type is MarketDataType.Ask)
				    {
					    pen = new RenderPen(AskBlockColor);
				    }

				    if (blockInRow.Type is MarketDataType.Bid)
				    {
					    pen = new RenderPen(BidBlockColor);
				    }

				    if (blockInRow.Type is MarketDataType.Trade)
					    pen = new RenderPen(TextColor);

				    var aggregationRow = aggregationBaseRow with { Y = y1, Height = realHeight };

				    if (aggregationRow.Height < 1)
				    {
					    aggregationRow.Height = 1;
				    }

				    if (aggregationRow.Width > 0)
				    {
					    context.DrawRectangle(pen, aggregationRow);

					    var pw = 0;

					    if (ShowSum)
					    {
						    var text = $"V {ChartInfo.TryGetMinimizedVolumeString(rowVol)}";

						    var aggVolBox = aggregationRow with
						    {
							    Width = ShowCount ? (aggregationRow.Width / 2) : aggregationRow.Width
						    };
						    pw = aggVolBox.Width;

						    if (RowOrderVolume.Enabled && blockInRow.Type != MarketDataType.Trade)
						    {
							    if (RowOrderVolume.Value <= rowVol)
								    context.FillRectangle(pen.Color, aggVolBox);
						    }

						    context.DrawString(text, _font, TextColor, aggVolBox, _stringCenterFormat);
					    }

					    if (ShowCount)
					    {
						    var text = $"C {blockInRow.Orders.Length}";

						    var aggCountBox = aggregationRow with
						    {
							    X = aggregationRow.X + pw, Width = aggregationRow.Width - pw
						    };

						    if (RowOrderCount.Enabled && blockInRow.Type != MarketDataType.Trade)
						    {
							    if (RowOrderCount.Value <= blockInRow.Orders.Length)
								    context.FillRectangle(pen.Color, aggCountBox);
						    }

						    context.DrawString(text, _font, TextColor, aggCountBox, _stringCenterFormat);
					    }
				    }

				    var availableArea = maxScreenSize - aggregationRow.Width;

				    var availableForThisRow =
					    maxCount == 0 ? 0 : (int)((availableArea / maxCount) * blockInRow.Orders.Length);

				    if (blockInRow.Type is not MarketDataType.Trade && blockInRow.Orders.Length > 0)
				    {
					    var minW = (int)Math.Max(height, 6 );
					    var lastX = aggregationRow.X - 2;

					    foreach (var order in blockInRow.Orders)
					    {
						    var vol = order.Order.Volume;

						    var needToFilterBlockSize = (MinBlockSize.Enabled && MinBlockSize.Value > vol &&
							    blockInRow.Type != MarketDataType.Trade);

						    var needToFillBox = (OrderSizeFilter.Enabled && OrderSizeFilter.Value <= vol &&
							    blockInRow.Type != MarketDataType.Trade);

						    if (needToFilterBlockSize)
							    continue;

						    var width = ItemWidthCalculation(vol, maxVol, (int)maxScreenSize/2, maxCount, 0, minW);

						    tempSize[vol] = width;
						    
						    var ww = tempSize[vol];

						    if (needToFilterBlockSize)
							    ww = (int)fontSize;

						    var orderBlockRow = aggregationRow with
						    {
							    X = lastX - ww, Width = ww, Height = realHeight
                            };

						    if (showSeparately)
						    {
							    if (!needToFilterBlockSize)
							    {
								    var textColor = TextColor;

                                    if (IsPointInRectangle(orderBlockRow, MouseLocationInfo.LastPosition))
                                    {
	                                    selectedRectangle = orderBlockRow;
                                        context.FillRectangle(ChartInfo.ColorsStore.MouseBackground, orderBlockRow);
									    textColor = ChartInfo.ColorsStore.MouseTextColor;

									    if (ChartInfo.KeyboardInfo.PressedKey != null && ChartInfo.KeyboardInfo.PressedKey.Key == CrossKey.LeftCtrl)
									    {
										    var text = $"Price\t\t{ChartInfo.GetPriceString(order.Order.Price)}{Environment.NewLine}";
										    text += $"Volume\t{order.Order.Volume}{Environment.NewLine}";
                                            text += $"Time\t\t{order.Order.Time:HH:mm:ss.fff}{Environment.NewLine}";
										    text += $"Id\t\t{order.Order.ExchangeOrderId}{Environment.NewLine}";
										    text += $"Priority\t{order.Order.Priority}{Environment.NewLine}";

                                            tooltip = text;
                                        }
								    }
								    else if (needToFillBox)
									    context.FillRectangle(pen.Color, orderBlockRow);
								    else
									    context.DrawRectangle(pen, orderBlockRow);

								    if (showText)
								    {
									    context.DrawString(ChartInfo.TryGetMinimizedVolumeString(vol), _font,
										    textColor, orderBlockRow,
										    dataType is DataType.Lvl3 ? _stringCenterFormat : _stringRightFormat);
                                    }
							    }
						    }

						    lastX = orderBlockRow.X - 1;
					    }

					    if (!showSeparately)
					    {
						    var end = aggregationRow.X - 1;

						    var orderBlockRow = aggregationRow with
						    {
							    X = lastX, Width = end - lastX
						    };

						    context.FillRectangle(pen.Color, orderBlockRow);
					    }
				    }

				    y1 = y2;
			    }

                if (!string.IsNullOrWhiteSpace(tooltip))
                {
	                var tooltipFont = new RenderFont("Arial", 8);
                    var size = context.MeasureString(tooltip, tooltipFont);

                    size = new Size(size.Width + 20, size.Height);
	                var rectangle = new Rectangle(new Point(selectedRectangle.X, selectedRectangle.Bottom + 1), size);

                    if (rectangle.Right > Container.RelativeRegion.Right)
	                {
		                rectangle.X += (Container.RelativeRegion.Right - rectangle.Right);
	                }
	                if (rectangle.Bottom > Container.RelativeRegion.Bottom)
	                {
		                rectangle.Y = selectedRectangle.Y - rectangle.Height - 1;
	                }

                    context.FillRectangle(ChartInfo.ColorsStore.MouseBackground, rectangle, 10);
	                rectangle.X += 10;
	                rectangle.Y += 9;
                    context.DrawString(tooltip, tooltipFont, ChartInfo.ColorsStore.MouseTextColor, rectangle, _stringLeftFormat);
                }
		    }
	    }
	    catch (Exception es)
	    {
		    this.LogWarn(es.ToString());
	    }
    }

    private bool IsPointInRectangle(Rectangle rectangle, Point e)
    {
	    if (rectangle.Width == 0 || rectangle.Height == 0)
		    return false;
	    if (e.X >= rectangle.X && e.Y >= rectangle.Y && e.X <= rectangle.X + rectangle.Width && e.Y <= rectangle.Y + rectangle.Height)
		    return true;
	    return false;
    }
}