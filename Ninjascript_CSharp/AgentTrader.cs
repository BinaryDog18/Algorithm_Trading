/*
 * RED NEURONAL ARTIFICIAL (IA DEEP LEARNING) PARA TRADING AUTOMÁTICO EN NINJASCRIPT
 * 
 *	Este código fuente es usado en el StrategyTester de NINJATRADER, es compilado desde allí y configurado desde allí como una dll
 * 
 *	Para realizar pruebas, entrenamiento o producción con este código fuente, se debe configurar los siguientes parámetros desde NINJASCRIPT/STRATEGY TESTER.
 *	
 *	
 *	---- PARÁMETROS DE CONFIGURACIÓN ----
 *	
 *	LoadSavedModel: Determina cargar un nuevo modelo o el que se suministra por defecto.
 *					si desea entrenar una red desde cero, establézcalo en false y haga un respaldo del conocimiento anterior si desea
 *					
 *	TradeActive:	Modo de trading activado determina si el robot tiene permisos de realizar operaciones en entranamiento o tiempo real.
 *					Establezca en false si solo está en modo de aprendizaje.
 *					
 *	Learn:			Establece si estará aprendiendo en tiempo real o en modo de prueba. Esto modificará ligeramente la base de conocimientos
 *					en proporción al coeficiente de aprendizaje.
 *					
 *	SaveModel:		Determina si se guardará el modelo ligeramente modificado o entrenado. Si está en modo aprendizaje, esto debe ser true.
 *	
 *	MAPeriod /
 *	RSIPeriod:		La red usa indicadores técnicos como parte de los datos suministrados, además de las barras.
 *					Estos valores determinan los periodos que se usarán en los indicadores (Media Móvil y RSI).
 *					Durante el desarrollo de este código, se seguirán añadiendo indicadores y opciones para añadirlos o no.
 *					
 *	MINPeriod /
 *	MAXPeriod:		Periodos utilizados para determinar soportes y resistencias locales. Se usarán los valores de cierre
 *					y apertura por cada vela
 *					
 * 
*/

#region Using declarations
using System;
using System.IO;
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	
	
	public class AgentTrader : Strategy
	{
		bool configured;
		//
		private SMA iMA;
		private RSI iRSI;
		private MAX iMAX;
		private MIN iMIN;
		//
		private Agent agent;
		private TElement[] TElements;
		const int barsResult = 50;
		const int rangeBarsInput = 250;
		const int rangeMA = 5;
		const int rangeMAXMIN = 10;
		ulong operations;
		decimal SumErrorsT1;
		decimal SumErrorsT2;
		//const int NumbInputs = 900;
		//const int NumbOutputs = 2;
		//
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				#region defaults
				Description									= @"Escriba la descripción de su nuevo Estrategia personalizado aquí.";
				Name										= "AgentTrader";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 300;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				LoadSavedModel				= true;
				TradeActive					= false;
				Learn						= true;
				SaveModel					= true;
				FakeNumber					= 123;
				MAPeriod					= 20;
				RSIPeriod					= 14;
				MINPeriod					= 14;
				MAXPeriod					= 14;
				#endregion
				
			}
			
			//Print("- State: " + State);
			
			
			if(State == State.DataLoaded)
			{
				//	Indicators Load
				#region Indicators
				iMA = SMA(MAPeriod);
				iRSI = RSI(RSIPeriod,1);
				iMAX = MAX(MAXPeriod);
				iMIN = MIN(MINPeriod);
				
				Bars.GetClose(0);
				#endregion
				
				Print("Initialized.");
			}
			
			else if (State == State.Configure)
			{
				
				#region configure
				
				if(!configured)
				{
					configured = true;
					SumErrorsT1 = 0;
					SumErrorsT2 = 0;
					
					
					Random rnd = new Random();
					
					//	New agent
					agent = new Agent(LoadSavedModel, SaveModel, Learn, TradeActive, rnd);
					Print(agent.getState());
					Print("Configured neurons: " + agent.Number_Neurons + ". Parameters: " + agent.Number_Parameters + ".");
					TElements = new TElement[barsResult];
					for(int i = 0; i < barsResult; i++) TElements[i] = new TElement(null);
					
				}
				
				#endregion
				
				
			}
			
			else if(State == State.Terminated){
				if(configured)
				{ 
					configured = false;
					agent.SaveModel("C:\\Users\\Binary\\Desktop\\data.dat");
					Print(agent.getState());
					
					decimal errorAvBuy = (SumErrorsT1 / operations);
					decimal errorAvSell = (SumErrorsT2 / operations);
					
					Print("Average Error T[0]: " + Math.Round(errorAvBuy,6));
					Print("Average Error T[1]: " + Math.Round(errorAvSell,6));
					Print("Operations: " + operations);
					Print(Process.SaveAndLoadStats((double)errorAvBuy, (double) errorAvSell));
					Print("Finishing...\n-----------------------------------");
					
				}
			}
			
		}

		protected override void OnBarUpdate()
		{
			//Print("current: " + CurrentBar);
			if(CurrentBar < 260) return;
			
			#region logicalProcess
			
			for(int i = 0; i < barsResult; i++) if(TElements[i].isActive) TElements[i].bars++;
			
			double[] inputs = new double[Agent.Number_inputs];
			
			Process.TransformDataAndSetInputs(ref inputs, Agent.Number_inputs, High, Low, Close, iMA, iRSI, iMAX, iMIN, rangeBarsInput,rangeMA, rangeMAXMIN);
			//Process.TransformDataAndSetInputs(ref inputs, agent.getInputsNumber(), High, Low, Close, iMA, iRSI, iMAX, iMIN, 300, 5, 10);
			
			//	1.- Backpropagation
			for(int i = 0; i < barsResult; i++)
			{
				if(TElements[i].bars == barsResult)
				{
			    	double[] T = new double[Agent.Number_outputs];
					double[] Outs = new double[Agent.Number_outputs];
					double[] tempInputs = new double[Agent.Number_inputs];
					//---
					double[] AxisH1 = new double[Agent.Number_Hidden1];
					double[] AxisH2 = new double[Agent.Number_Hidden2];
					
					
					Process.CalculateTArray(barsResult, rangeBarsInput, ref T, High, Low, Close);
					
					tempInputs = TElements[i].inputs;

					agent.ForwardPropagation(tempInputs, ref Outs, ref AxisH1, ref AxisH2);
					agent.Backpropafagation(tempInputs, ref Outs, T, ref AxisH1, ref AxisH2);
					
					TElements[i] = new TElement(null);
					
					Print("For T[0]: " + Math.Round(T[0],4) + " -> " + Math.Round(Outs[0],4));
					
					Print("For T[1]: " + Math.Round(T[1],4) + " -> " + Math.Round(Outs[1],4));
					
					SumErrorsT1 += (decimal)Math.Pow((double)Math.Abs(T[0] - Outs[0]),2);
					SumErrorsT2 += (decimal)Math.Pow((double)Math.Abs(T[1] - Outs[1]),2);
					operations++;
					//TbarsElementT[i] = 0;
					//TisActiveElementT[i] = false;
					//Aux++;
					//if(Aux == 1000) { dataProcessed++; Aux = 0; }
			     
				}
			}
			
			
			double[] outputs = new double[Agent.Number_outputs]; //	Result of neural network
			
			//	2.- ForwardPropagation
			for(int i = 0; i < barsResult; i++)
			{
				if(!TElements[i].isActive)
				{
					double[] AxisH1 = new double[Agent.Number_Hidden1];
					double[] AxisH2 = new double[Agent.Number_Hidden2];
					
			    	TElements[i] = new TElement(inputs);
			     	agent.ForwardPropagation(inputs, ref outputs, ref AxisH1, ref AxisH2);
			     	break;
			  	}
			}
			
			#endregion
			
			//Print("Buy: " + Math.Round(outputs[0], 4) + "\nSell: "+ Math.Round(outputs[0], 4));
		}
		
		
		//---
		public static class Process
		{
			//Ex: Process.TransformDataAndSetInputs(ref inputs, agent.getInputsNumber(), High, Low, Close, iMA, iRSI, iMAX, iMIN, 300, 5, 10);
			public static void TransformDataAndSetInputs(ref double[] inputs, int Number_inputs, ISeries<double> Highs, ISeries<double> Lows, ISeries<double> Closes,
												  ISeries<double> iMA, ISeries<double> iRSI, ISeries<double> iMAX,
												  ISeries<double> iMIN, int rangeBars, int rangeMA, int rangeiMAXMIN)
			{
				double max;
				double min;
				double dif;
				
				int indexInput = 0;
				
				max = FindValueMaxInSeries(Highs, 1, rangeBars);
				min = FindValueMinInSeries(Lows, 1, rangeBars);
				dif = max - min;
				
				
				// Transform bars and set
				for(int i = 0; i < rangeBars; i++)
				{
					inputs[indexInput] = Math.Abs(((max - Highs[i]) / dif) - 1);
					indexInput++;
					inputs[indexInput] = Math.Abs(((max - Lows[i]) / dif)  - 1);
					indexInput++;
					inputs[indexInput] = Math.Abs(((max - Closes[i]) / dif) - 1);
					indexInput++;
				}
				
				// Transform and Reg iMA data
				for(int i = 0; i < rangeMA; i++)
				{
					inputs[indexInput] = Math.Abs(((max - iMA[i]) / dif) - 1);
					indexInput++;
				}
				
				// Transform and Reg iMAX & iMIN data
				for(int i = 0; i < rangeiMAXMIN; i++)
				{
					inputs[indexInput] = Math.Abs(((max - iMAX[i]) / dif) - 1);
					indexInput++;
					inputs[indexInput] = Math.Abs(((max - iMIN[i]) / dif) - 1);
					indexInput++;
				}
				
				inputs[indexInput] = iRSI[0];
				indexInput++;
				inputs[indexInput] = iRSI[1];
				indexInput++;
				
				
			}
			
			
			public static void CalculateTArray(int rangeBarsPL, int rangeBarsAnalized, ref double[] T, ISeries<double> Highs, ISeries<double> Lows, ISeries<double> Closes)
			{
				double maxPre = FindValueMaxInSeries(Highs, rangeBarsPL, rangeBarsAnalized);
				double minPre = FindValueMinInSeries(Lows, rangeBarsPL, rangeBarsAnalized);
				double difPre  = maxPre - minPre;
				
				double maxPost = FindValueMaxInSeries(Highs, 1, rangeBarsPL);
				double minPost = FindValueMinInSeries(Lows, 1, rangeBarsPL);
				double difPost  = maxPre - minPre;
				
				double open = Closes[rangeBarsPL];
				
				
				// Buy
				T[0] = (Math.Abs(maxPost - open)) / difPre;
				// Sell
				T[1] = (Math.Abs(minPost - open)) / difPre;
				
			}
			
			public static string SaveAndLoadStats(double error1, double error2)
			{
				string path = "C:\\Users\\Binary\\Desktop\\Stats.dat";
				string ret = "";
				double[] errors = new double[2];
				//byte[] dataBytes;
				
				if(!File.Exists(path))
				{
					ret+="Creating File \"Stats.dat\" for de next time.";
					
					errors[0] = error1;
					errors[1] = error2;
					
					//File.Create(path);
					File.WriteAllBytes(path, GetBytes(errors));
					
					ret+="\nFile created.";
				}
				else{
					byte[] dataImport;
					dataImport = File.ReadAllBytes(path);
					errors = GetDoubles(dataImport);
					
					ret+="\nCurrent error compared to the previous test Error:";
					ret+="\nError Buy  Variation: " + Math.Round((error1-errors[0])*100,6) + "%";
					ret+="\nError Sell Variation: " + Math.Round((error2-errors[1])*100,6) + "%";
					
					errors[0] = error1;
					errors[1] = error2;
					File.WriteAllBytes(path, GetBytes(errors));
				}
				
				return ret;
				
				
			}
			
			private static byte[] GetBytes(double[] values)
	        {
	            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
	        }

	        private static double[] GetDoubles(byte[] bytes)
	        {
	            return Enumerable.Range(0, bytes.Length / sizeof(double))
	                .Select(offset => BitConverter.ToDouble(bytes, offset * sizeof(double)))
	                .ToArray();
	        }
			
			//	Return value Max in a ISeries
			private static double FindValueMaxInSeries(ISeries<double> Vector, int From, int range)
			{
				double max = -1;
				for(int i = From; i < From + range; i++)
				{
					if(Vector[i] > max) max = Vector[i];
				}
				
				if(max < 0) throw new Exception();
				else return max;
			}
			//	Return value Min in a ISeries
			private static double FindValueMinInSeries(ISeries<double> Vector, int From, int range)
			{
				double min = int.MaxValue-1;
				
				for(int i = From; i < From + range; i++)
				{
					if(Vector[i] < min) min = Vector[i];
				}
				
				if(min == int.MaxValue-1) throw new Exception();
				else return min;
			}
		}

		#region Properties
		
		[NinjaScriptProperty]
		[Display(Name="LoadSavedModel", Order=1, GroupName="Parameters")]
		public bool LoadSavedModel
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="TradeActive", Order=1, GroupName="Parameters")]
		public bool TradeActive
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Learn", Order=1, GroupName="Parameters")]
		public bool Learn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="SaveModel", Order=1, GroupName="Parameters")]
		public bool SaveModel
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="FakeNumber", Description="A number", Order=1, GroupName="Parameters")]
		public int FakeNumber
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MA Period", Description="Period of MA", Order=1, GroupName="Parameters")]
		public int MAPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI Period", Description="Period of RSI", Order=1, GroupName="Parameters")]
		public int RSIPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MIN Period", Description="Period of MIN indicator", Order=1, GroupName="Parameters")]
		public int MINPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MAX Period", Description="Period of MAX indicator", Order=1, GroupName="Parameters")]
		public int MAXPeriod
		{ get; set; }
		#endregion

	}
}
