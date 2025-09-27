//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//NinjaScript generated code. DO NOT EDIT.

namespace NinjaTrader.NinjaScript.Indicators
{
	public class ElliottWave3Catcher : Indicator
	{
		#region Variables
		private Series<double> averageSmas;
		private Series<double> percentageChange;
		private Series<double> macdLineScaled;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"[blackcat] L1 Elliott Wave3 Catcher";
				Name										= "ElliottWave3Catcher";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				AddPlot(Brushes.DarkGray, "EmaPercentageChange");
				AddLine(Brushes.Gray, 0, "Zero Line");

				YellowBrush 	= Brushes.Yellow;
				GreenBrush 		= Brushes.Green;
				RedBrush 		= Brushes.Red;
				FuchsiaBrush 	= Brushes.Fuchsia;

				LimeBrush 		= new SolidColorBrush(Colors.Lime) { Opacity = 0.6 };
				LimeBrush.Freeze();
				AquaBrush 		= new SolidColorBrush(Colors.Aqua) { Opacity = 0.4 };
				AquaBrush.Freeze();
				BlueBrush 		= new SolidColorBrush(Colors.Blue) { Opacity = 0.4 };
				BlueBrush.Freeze();
				FuchsiaBgBrush 	= new SolidColorBrush(Colors.Fuchsia) { Opacity = 0.4 };
				FuchsiaBgBrush.Freeze();
			}
			else if (State == State.Configure)
			{
				averageSmas 		= new Series<double>(this, MaximumBarsLookBack.Infinite);
				percentageChange 	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				macdLineScaled 		= new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 50)
			{
				if (CurrentBar > 0)
				{
					averageSmas[0] = double.NaN;
					percentageChange[0] = double.NaN;
					macdLineScaled[0] = double.NaN;
				}
				Values[0][0] = double.NaN;
				return;
			}

			// --- Calculations ---
			double sma5 = SMA(5)[0];
			double sma10 = SMA(10)[0];
			double sma20 = SMA(20)[0];
			double sma30 = SMA(30)[0];
			averageSmas[0] = (sma5 + sma10 + sma20 + sma30) / 4;

			double previous15dAvg = GetValueFromPast(averageSmas, 15);
			if (!double.IsNaN(previous15dAvg) && previous15dAvg.ApproxCompare(0) != 0)
				percentageChange[0] = (averageSmas[0] - previous15dAvg) / previous15dAvg * 100;
			else
				percentageChange[0] = double.NaN;

			double emaPercentageChange = EMA(percentageChange, 8)[0];
			Values[0][0] = emaPercentageChange;

			// --- Manual Candle Drawing Logic ---
			string rectTag = "Candle" + CurrentBar;
			if (IsFirstTickOfBar)
			{
				// Highest priority: Fuchsia
				if (!double.IsNaN(percentageChange[0]) && !double.IsNaN(percentageChange[1]) && !double.IsNaN(emaPercentageChange) && !double.IsNaN(Values[0][1]) && percentageChange[0] < percentageChange[1] && emaPercentageChange > Values[0][1])
				{
					Draw.Rectangle(this, rectTag, true, 0, emaPercentageChange, 0, percentageChange[0], FuchsiaBrush, FuchsiaBrush, 100);
				}
				// Red
				else if (!double.IsNaN(percentageChange[0]) && !double.IsNaN(percentageChange[1]) && !double.IsNaN(emaPercentageChange) && percentageChange[0] < percentageChange[1])
				{
					Draw.Rectangle(this, rectTag, true, 0, emaPercentageChange, 0, percentageChange[0], RedBrush, RedBrush, 100);
				}
				// Green
				else if (!double.IsNaN(percentageChange[0]) && !double.IsNaN(percentageChange[1]) && !double.IsNaN(emaPercentageChange) && percentageChange[0] > percentageChange[1])
				{
					Draw.Rectangle(this, rectTag, true, 0, emaPercentageChange, 0, percentageChange[0], GreenBrush, GreenBrush, 100);
				}
				// Yellow
				else if (!double.IsNaN(percentageChange[0]) && !double.IsNaN(emaPercentageChange) && !double.IsNaN(Values[0][1]) && emaPercentageChange > Values[0][1])
				{
					Draw.Rectangle(this, rectTag, true, 0, 0, 0, percentageChange[0], YellowBrush, YellowBrush, 100);
				}
				// No condition met
				else
				{
					RemoveDrawObject(rectTag);
				}
			}

			// --- Background Coloring and other calculations ---
			double close2dAgoScaled = GetValueFromPast(Close, 2) * 0.865;
			double close13dAgoScaled = GetValueFromPast(Close, 13) * 0.772;
			double minCloseScaled = Math.Min(close2dAgoScaled, close13dAgoScaled);

			int highest50dHighPosition = HighestBar(High, 50);
			double openAtHighestHigh = GetValueFromPast(Open, highest50dHighPosition);
			double priceChangeFromHighestHighOpen = double.NaN;
			if(!double.IsNaN(openAtHighestHigh) && openAtHighestHigh.ApproxCompare(0) != 0)
				priceChangeFromHighestHighOpen = (Close[0] - openAtHighestHigh) / openAtHighestHigh * 100;

			double waveBottomCatcherSignal = (!double.IsNaN(priceChangeFromHighestHighOpen) && Close[0].ApproxCompare(0) != 0 && ((Close[0] - minCloseScaled) / Close[0] < 0.03 && priceChangeFromHighestHighOpen < -35)) ? 10 : 0;

			double prevClose = GetValueFromPast(Close, 1);
			double dailyPriceChangePercent = double.NaN;
			if (!double.IsNaN(prevClose) && prevClose.ApproxCompare(0) != 0)
				dailyPriceChangePercent = (Close[0] - prevClose) / prevClose * 100;

			macdLineScaled[0] = (EMA(12)[0] - EMA(26)[0]) * 100;
			double signalLineScaled = EMA(macdLineScaled, 9)[0];

			bool isBgFuchsia = !double.IsNaN(dailyPriceChangePercent) && !double.IsNaN(macdLineScaled[0]) && !double.IsNaN(signalLineScaled) && macdLineScaled[0] < -50 && dailyPriceChangePercent > 7 && macdLineScaled[0] >= signalLineScaled;
			bool isBgBlue = !double.IsNaN(dailyPriceChangePercent) && !double.IsNaN(macdLineScaled[0]) && !double.IsNaN(signalLineScaled) && macdLineScaled[0] < -50 && dailyPriceChangePercent > 7 && macdLineScaled[0] < signalLineScaled;
			bool isBgAqua = !double.IsNaN(dailyPriceChangePercent) && !double.IsNaN(macdLineScaled[0]) && macdLineScaled[0] < -50 && dailyPriceChangePercent > 7;
			bool isBgLime = waveBottomCatcherSignal == 1;

			if(isBgFuchsia) BackBrush = FuchsiaBgBrush;
			else if (isBgBlue) BackBrush = BlueBrush;
			else if (isBgAqua) BackBrush = AquaBrush;
			else if (isBgLime) BackBrush = LimeBrush;
			else BackBrush = null;
		}

		#region Helpers
		private double GetValueFromPast(ISeries<double> series, int lookback)
		{
			for (int i = lookback; i >= 0; i--)
			{
				if (i <= CurrentBar)
				{
					double val = series[i];
					if (!double.IsNaN(val))
						return val;
				}
			}
			return double.NaN;
		}
		#endregion

		#region Properties
		[XmlIgnore]
		[Display(Name = "Yellow", GroupName = "Colors", Order = 1)]
		public Brush YellowBrush { get; set; }
		[Browsable(false)]
		public string YellowBrushSerializable { get { return Serialize.BrushToString(YellowBrush); } set { YellowBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Green", GroupName = "Colors", Order = 2)]
		public Brush GreenBrush { get; set; }
		[Browsable(false)]
		public string GreenBrushSerializable { get { return Serialize.BrushToString(GreenBrush); } set { GreenBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Red", GroupName = "Colors", Order = 3)]
		public Brush RedBrush { get; set; }
		[Browsable(false)]
		public string RedBrushSerializable { get { return Serialize.BrushToString(RedBrush); } set { RedBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Fuchsia (Plot)", GroupName = "Colors", Order = 4)]
		public Brush FuchsiaBrush { get; set; }
		[Browsable(false)]
		public string FuchsiaBrushSerializable { get { return Serialize.BrushToString(FuchsiaBrush); } set { FuchsiaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Lime (BG)", GroupName = "Colors", Order = 5)]
		public Brush LimeBrush { get; set; }
		[Browsable(false)]
		public string LimeBrushSerializable { get { return Serialize.BrushToString(LimeBrush); } set { LimeBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Aqua (BG)", GroupName = "Colors", Order = 6)]
		public Brush AquaBrush { get; set; }
		[Browsable(false)]
		public string AquaBrushSerializable { get { return Serialize.BrushToString(AquaBrush); } set { AquaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Blue (BG)", GroupName = "Colors", Order = 7)]
		public Brush BlueBrush { get; set; }
		[Browsable(false)]
		public string BlueBrushSerializable { get { return Serialize.BrushToString(BlueBrush); } set { BlueBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Fuchsia (BG)", GroupName = "Colors", Order = 8)]
		public Brush FuchsiaBgBrush { get; set; }
		[Browsable(false)]
		public string FuchsiaBgBrushSerializable { get { return Serialize.BrushToString(FuchsiaBgBrush); } set { FuchsiaBgBrush = Serialize.StringToBrush(value); } }
		#endregion
	}
}

#region NinjaScript generated code. DO NOT EDIT.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private ElliottWave3Catcher[] cacheElliottWave3Catcher;
        public ElliottWave3Catcher ElliottWave3Catcher()
        {
            return ElliottWave3Catcher(Input);
        }

        public ElliottWave3Catcher ElliottWave3Catcher(ISeries<double> input)
        {
            if (cacheElliottWave3Catcher != null)
                for (int idx = 0; idx < cacheElliottWave3Catcher.Length; idx++)
                    if (cacheElliottWave3Catcher[idx] != null &&  cacheElliottWave3Catcher[idx].EqualsInput(input))
                        return cacheElliottWave3Catcher[idx];
            return CacheIndicator<ElliottWave3Catcher>(new ElliottWave3Catcher(), input, ref cacheElliottWave3Catcher);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.ElliottWave3Catcher ElliottWave3Catcher()
        {
            return indicator.ElliottWave3Catcher(Input);
        }

        public Indicators.ElliottWave3Catcher ElliottWave3Catcher(ISeries<double> input)
        {
            return indicator.ElliottWave3Catcher(input);
        }
    }
}
#endregion