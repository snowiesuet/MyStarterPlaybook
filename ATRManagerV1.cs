#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

// git inited

namespace NinjaTrader.NinjaScript.Strategies
{
	public class ATRManagerV1 : Strategy
	{
		private EMA emaFast;
		private EMA emaSlow;

		private ATR atr;

		private double entryPrice;
		private double initialStop;
		private double trailActivation;
		private double trailingStop;
		private bool stopPlaced;

		private bool isTrailing;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "ATRManagerV1";
				Description = "Stage 1 - EMA Cross";

				Calculate = Calculate.OnBarClose;

				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;

				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;

				ATRPeriod = 14;
				StopATR = 2.0;
				TrailActivationATR = 1.0;
				TrailATR = 2.5;

				BarsRequiredToTrade = 25;

				Fast = 10;
				Slow = 25;
			}

			else if (State == State.DataLoaded)
			{
				emaFast = EMA(Fast);
				emaSlow = EMA(Slow);
				atr = ATR(ATRPeriod);

				emaFast.Plots[0].Brush = Brushes.Gold;
				emaSlow.Plots[0].Brush = Brushes.DeepSkyBlue;
				atr.Plots[0].Brush = Brushes.DeepPink;


				AddChartIndicator(emaFast);
				AddChartIndicator(emaSlow);
				AddChartIndicator(atr);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				stopPlaced = false;
				if (CrossAbove(emaFast, emaSlow, 1))
				{
					Print(Time[0] + " LONG");
					EnterLong("Long");
				}

				else if (CrossBelow(emaFast, emaSlow, 1))
				{
					Print(Time[0] + " SHORT");
					EnterShort("Short");
				}

			}

			//==============================
			// Initial ATR Stop - Long
			//==============================
			if (Position.MarketPosition == MarketPosition.Long && !stopPlaced)
			{
				Print("Submitting Long Stop");
				Print("Entry Price : " + entryPrice);
				Print("Initial Stop: " + initialStop);
				Print("Distance    : " + (entryPrice - initialStop));


				ExitLongStopMarket("Long", CalculationMode.Price, initialStop, false);

				stopPlaced = true;

				Print(Time[0] + "  Placed Long Stop @ " + initialStop);
			}

			//==============================
			// Initial ATR Stop - Short
			//==============================
			if (Position.MarketPosition == MarketPosition.Short && !stopPlaced)
			{
				Print("Submitting Short Sto0p");
				Print("Entry Price : " + entryPrice);
				Print("Initial Stop: " + initialStop);
				Print("Distance    : " + (initialStop - entryPrice));



				ExitShortStopMarket("Short", CalculationMode.Price, initialStop, false);

				stopPlaced = true;

				Print(Time[0] + "  Placed Short Stop @ " + initialStop);
			}
		}

		protected override void OnExecutionUpdate(
			Execution execution,
			string executionId,
			double price,
			int quantity,
			MarketPosition marketPosition,
			string orderId,
			DateTime time)
		{
			if (execution.Order == null)
				return;

			if (execution.Order.OrderState != OrderState.Filled)
				return;


			Print("");
			Print("==================================");
			Print("Execution");
			Print("Name     : " + execution.Order.Name);
			Print("Action   : " + execution.Order.OrderAction);
			Print("Price    : " + price);
			Print("Time     : " + time);
			Print("==================================");

			if (execution.Order.Name == "Long")
			{
				entryPrice = price;

				initialStop = entryPrice - atr[0] * StopATR;

				trailActivation = entryPrice + atr[0] * TrailActivationATR;

				trailingStop = initialStop;

				isTrailing = false;
				stopPlaced = false;

				Print("");
				Print("========== LONG ==========");
				Print("Entry Price      : " + entryPrice);
				Print("ATR              : " + atr[0]);
				Print("Initial Stop     : " + initialStop);
				Print("Trail Activation : " + trailActivation);
				Print("Current Bar : " + CurrentBar);
				Print("Time        : " + Time[0]);
				Print("==========================");
			}

			if (execution.Order.Name == "Short")
			{
				entryPrice = price;

				initialStop = entryPrice + atr[0] * StopATR;

				trailActivation = entryPrice - atr[0] * TrailActivationATR;

				trailingStop = initialStop;

				isTrailing = false;

				Print("");
				Print("========= SHORT ==========");
				Print("Entry Price      : " + entryPrice);
				Print("ATR              : " + atr[0]);
				Print("Initial Stop     : " + initialStop);
				Print("Trail Activation : " + trailActivation);
				Print("Current Bar : " + CurrentBar);
				Print("Time        : " + Time[0]);
				Print("==========================");
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Fast EMA", Order = 1, GroupName = "Parameters")]
		public int Fast
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Slow EMA", Order = 2, GroupName = "Parameters")]
		public int Slow
		{ get; set; }

		[Range(1, 100)]
		[Display(Name = "ATR Period", Order = 3, GroupName = "Parameters")]
		public int ATRPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "Stop ATR", Order = 4, GroupName = "Parameters")]
		public double StopATR
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "Trail Activation ATR", Order = 5, GroupName = "Parameters")]
		public double TrailActivationATR
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "Trail ATR", Order = 6, GroupName = "Parameters")]
		public double TrailATR
		{ get; set; }

		#endregion
	}
}