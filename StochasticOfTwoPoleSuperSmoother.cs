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
	public class StochasticOfTwoPoleSuperSmoother : Indicator
	{
		private Series<double> filt;
		private Series<double> ssm;
		private Series<double> upperDsl;
		private Series<double> lowerDsl;
		private EMA emaForDsl;
		private Brush redColorBrush = Brushes.Red;
		private Brush greenColorBrush = Brushes.Green;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Stochastic of Two-Pole SuperSmoother";
				Name										= "StochasticOfTwoPoleSuperSmoother";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				StochPeriod 	= 32;
				SmootherPeriod 	= 25;
				SignalPeriod 	= 9;
				SourceType 		= SimplifiedSourceType.Median;
				ColorBars 		= true;
				ShowSignals 	= true;

				redColorBrush = new SolidColorBrush(Color.FromRgb(0xD2, 0x04, 0x2D));
				redColorBrush.Freeze();
			greenColorBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0xD2, 0x04));
				greenColorBrush.Freeze();
			}
			else if (State == State.Configure)
			{
				AddPlot(new Stroke(Brushes.Transparent, 2), PlotStyle.Line, "StochSuperSmoother");
				AddPlot(Brushes.Gray, "UpperDSL");
				AddPlot(Brushes.Gray, "LowerDSL");

				filt = new Series<double>(this, MaximumBarsLookBack.Infinite);
				ssm = new Series<double>(this, MaximumBarsLookBack.Infinite);
				upperDsl = new Series<double>(this, MaximumBarsLookBack.Infinite);
				lowerDsl = new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			else if (State == State.DataLoaded)
			{
				emaForDsl = EMA(Values[0], SignalPeriod);
			}
		}

		protected override void OnBarUpdate()
		{
			// --- Get Input Source ---
			double src;
			switch (SourceType)
			{
				case SimplifiedSourceType.Open: src = Open[0]; break;
				case SimplifiedSourceType.High: src = High[0]; break;
				case SimplifiedSourceType.Low: src = Low[0]; break;
				case SimplifiedSourceType.Median: src = Median[0]; break;
				case SimplifiedSourceType.Typical: src = Typical[0]; break;
				case SimplifiedSourceType.Weighted: src = Weighted[0]; break;
				default: src = Close[0]; break;
			}

			// --- Two-Pole SuperSmoother Filter Calculation ---
			if (CurrentBar < 3)
			{
				filt[0] = src;
			}
			else
			{
				double a1 		= Math.Exp(-1.414 * Math.PI / SmootherPeriod);
				double b1 		= 2 * a1 * Math.Cos(1.414 * Math.PI / SmootherPeriod);
				double coef2 	= b1;
				double coef3 	= -a1 * a1;
				double coef1 	= 1 - coef2 - coef3;
				filt[0] 		= coef1 * src + coef2 * filt[1] + coef3 * filt[2];
			}
			ssm[0] = filt[0];

			if (CurrentBar < StochPeriod)
			{
				Values[0][0] = 50;
				Values[1][0] = 50;
				Values[2][0] = 50;
				return;
			}

			// --- Stochastic Calculation ---
			double fmin = MIN(ssm, StochPeriod)[0];
			double fmax = MAX(ssm, StochPeriod)[0];
			double stochValue = 50;

			double range = fmax - fmin;
			if (range.ApproxCompare(0) != 0)
			{
				stochValue = 100 * (ssm[0] - fmin) / range;
			}
			Values[0][0] = stochValue;

			// --- Signal Line (DSL) Logic ---
			double prevStochValue = Values[0][1];

			if (stochValue > prevStochValue)
			{
				upperDsl[0] = emaForDsl[0];
				lowerDsl[0] = lowerDsl[1];
			}
			else if (stochValue < prevStochValue)
			{
				lowerDsl[0] = emaForDsl[0];
				upperDsl[0] = upperDsl[1];
			}
			else
			{
				upperDsl[0] = upperDsl[1];
				lowerDsl[0] = lowerDsl[1];
			}

			Values[1][0] = upperDsl[0];
			Values[2][0] = lowerDsl[0];

			// --- Visual Elements ---
			Brush colorBuffer = GetGradientColor(stochValue, 0, 100, redColorBrush, greenColorBrush);
			PlotBrushes[0][0] = colorBuffer;

			if (ColorBars)
			{
				BarBrush = colorBuffer;
			}

			if (ShowSignals)
			{
				if (CrossAbove(Values[0], Values[1], 1))
					Draw.TriangleUp(this, "GoLong" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Yellow);

				if (CrossBelow(Values[0], Values[2], 1))
					Draw.TriangleDown(this, "GoShort" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Fuchsia);
			}
		}

		#region Helpers
		private Brush GetGradientColor(double value, double min, double max, Brush startColor, Brush endColor)
		{
			if (!(startColor is SolidColorBrush) || !(endColor is SolidColorBrush))
				return Brushes.Gray;

			var start = ((SolidColorBrush)startColor).Color;
			var end = ((SolidColorBrush)endColor).Color;

			double percent = (value - min) / (max - min);
			percent = Math.Max(0, Math.Min(1, percent)); // Clamp between 0 and 1

			byte a = (byte)(start.A + (end.A - start.A) * percent);
			byte r = (byte)(start.R + (end.R - start.R) * percent);
			byte g = (byte)(start.G + (end.G - start.G) * percent);
			byte b = (byte)(start.B + (end.B - start.B) * percent);

			SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
			brush.Freeze();
			return brush;
		}
		#endregion

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name="Stochastic Period", Description="Period for Stochastic calculation", Order=1, GroupName="Parameters")]
		public int StochPeriod { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name="SuperSmoother Period", Description="Period for the 2-Pole SuperSmoother filter", Order=2, GroupName="Parameters")]
		public int SmootherPeriod { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name="Signal Period", Description="Period for the DSL signal line", Order=3, GroupName="Parameters")]
		public int SignalPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Source", Description="Price source for calculations", Order=4, GroupName="Parameters")]
		public SimplifiedSourceType SourceType { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Color Bars?", Description="Enable/disable bar coloring", Order=5, GroupName="UI Options")]
		public bool ColorBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Signals?", Description="Enable/disable signal shapes", Order=6, GroupName="UI Options")]
		public bool ShowSignals { get; set; }
		#endregion
	}

	#region Enums
	public enum SimplifiedSourceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		// Note: More complex, library-dependent sources from the original script are omitted.
	}
	#endregion
}

#region NinjaScript generated code. DO NOT EDIT.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private StochasticOfTwoPoleSuperSmoother[] cacheStochasticOfTwoPoleSuperSmoother;
        public StochasticOfTwoPoleSuperSmoother StochasticOfTwoPoleSuperSmoother()
        {
            return StochasticOfTwoPoleSuperSmoother(Input);
        }

        public StochasticOfTwoPoleSuperSmoother StochasticOfTwoPoleSuperSmoother(ISeries<double> input)
        {
            if (cacheStochasticOfTwoPoleSuperSmoother != null)
                for (int idx = 0; idx < cacheStochasticOfTwoPoleSuperSmoother.Length; idx++)
                    if (cacheStochasticOfTwoPoleSuperSmoother[idx] != null &&  cacheStochasticOfTwoPoleSuperSmoother[idx].EqualsInput(input))
                        return cacheStochasticOfTwoPoleSuperSmoother[idx];
            return CacheIndicator<StochasticOfTwoPoleSuperSmoother>(new StochasticOfTwoPoleSuperSmoother(), input, ref cacheStochasticOfTwoPoleSuperSmoother);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.StochasticOfTwoPoleSuperSmoother StochasticOfTwoPoleSuperSmoother()
        {
            return indicator.StochasticOfTwoPoleSuperSmoother(Input);
        }

        public Indicators.StochasticOfTwoPoleSuperSmoother StochasticOfTwoPoleSuperSmoother(ISeries<double> input)
        {
            return indicator.StochasticOfTwoPoleSuperSmoother(input);
        }
    }
}
#endregion