#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.IO;
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
	public class Agent
	{
		#region Agent declarations
		public const int Number_inputs = 782;//782;
		public const int Number_outputs = 2;
		public const int Number_Hidden1 = 1200;//1200
		public const int Number_Hidden2 = 1200;
		public const int Number_BarsResult = 50;
		public const int NumberBarsBuffer = 250;
		public const double learningCoefficient = 0.01;
		public int Number_Neurons;
		public int Number_Parameters;
		private const double e = 2.71828182;
		
		
		private bool learn;
		private bool saveModel;
		
		private bool trade;
		private ulong Processes;
		private string stateAgent;
		
		//	Data Neurons
		private double[,] dataLayer1;
		private double[,] dataLayer2;
		private double[,] dataLayer3;
		
		//	Axis Hidden's Neurons
		//private double[] Axis1;
		//private double[] Axis2;
		
		#endregion
		
		public string getState() { return stateAgent;}
		public ulong getProcesses() { return Processes;}
		public int getInputsNumber() { return Number_inputs;}
		public bool isTrading() { return trade;}
		
		public Agent(bool LoadModelSaved, bool saveModel, bool Learn, bool Trade, Random rand)
		{
			this.saveModel = saveModel;
			this.learn = Learn;
			this.trade = Trade;
			
			stateAgent = "";
			
			dataLayer1 = new double[Number_Hidden1,Number_inputs +1];
			dataLayer2 = new double[Number_Hidden2,Number_Hidden1+1];
			dataLayer3 = new double[Number_outputs,Number_Hidden2+1];
			
			//Axis1 = new double[Number_Hidden1];
			//Axis2 = new double[Number_Hidden2];
			
			
			Number_Neurons = Number_outputs+Number_Hidden1+Number_Hidden2;
			Number_Parameters = Number_inputs*Number_Hidden1+Number_Hidden1*Number_Hidden2+Number_Hidden2*Number_outputs + Number_Neurons;
			
			if(!LoadModelSaved) NewModel(rand);
			else LoadModel("C:\\Users\\Binary\\Desktop\\data.dat");	//	TODO
		}
		
		public void ForwardPropagation(double[] inputs, ref double[] outputs, ref double[] AxisH1, ref double[] AxisH2)
		{
			double A;
			
			// Layer 1
			for(int i = 0; i < Number_Hidden1; i++)
			{
				A = 0;
				for(int j = 0; j < Number_inputs; j++)
				{
			     	A += inputs[j]*dataLayer1[i,j];
			  	}
			  	AxisH1[i] = Sigmoid(A+dataLayer1[i,Number_inputs]);
			}

			// Layer 2
			for(int i = 0; i < Number_Hidden2; i++)
			{
			  	A = 0;
			  	for(int j = 0; j < Number_Hidden1; j++)
			  	{
			     	A += AxisH1[j]*dataLayer2[i,j];
			  	}
			  	AxisH2[i] = Sigmoid(A+dataLayer2[i,Number_Hidden1]);
			}

			// Layer 3
			for(int i = 0; i < Number_outputs; i++)
			{
			  	A = 0;
			  	for(int j = 0; j < Number_Hidden2; j++)
			  	{
			     	A += AxisH2[j]*dataLayer3[i,j];
			  	}
			  	outputs[i] = Sigmoid(A+dataLayer3[i,Number_Hidden2]);
			}
		}
		
		public void Backpropafagation(double[] Tinputs, ref double[] Toutputs, double[] T, ref double[] TAxis1, ref double[] TAxis2)
		{
			if(!learn) return; 
			
			double[] deltaI = new double[Number_Hidden1];
			double[] deltaJ = new double[Number_Hidden2];
			double[] deltaK = new double[Number_outputs];
			
			double[] prodTempI = new double[Number_Hidden1];
			double[] prodTempJ = new double[Number_Hidden2];
			
			double[] Error = new double[Number_outputs];
			
			double sumj = 0, sumi = 0;
			
			//	Processes
			for (int i = 0; i < Number_Hidden1; i++) prodTempI[i] = 0;
		   	for (int i = 0; i < Number_Hidden2; i++) prodTempJ[i] = 0;
		   
			
		   	for (int i = 0; i < Number_outputs; i++) Error[i] = T[i] - Toutputs[i];	//	Error normal
			//for (int i = 0; i < Number_outputs; i++) Error[i] = Math.Pow(T[i] - Toutputs[i],2);	//	Error Cuadrático medio
		   
		   	for (int i = 0; i < Number_outputs; i++) deltaK[i] = (Toutputs[i] * (1 - Toutputs[i])) * Error[i];
   
			
			//---
			for (int j = 0; j < Number_Hidden2; j++)
			{
				for (int k = 0; k < Number_outputs; k++)
				{
			    	prodTempJ[j] += dataLayer3[k,j] * deltaK[k];
				}
			}
			for (int i = 0; i < Number_Hidden2; i++) sumj += prodTempJ[i];
			for (int i = 0; i < Number_Hidden2; i++) deltaJ[i] = sumj * (TAxis2[i] * (1 - TAxis2[i]));
			
			//---
			for (int i = 0; i < Number_Hidden1; i++) 
			{
				for (int j = 0; j < Number_Hidden2; j++) 
				{
					prodTempI[i] += dataLayer2[j,i] * deltaJ[j];
				}
			}

			for (int i = 0; i < Number_Hidden2; i++) sumi += prodTempI[i];
			for (int i = 0; i < Number_Hidden2; i++) deltaI[i] = sumi * (TAxis1[i] * (1 - TAxis1[i]));
			
			//---
			//	Update Knowledge
			//	Layer Hidden 1
			for (int i = 0; i < Number_Hidden1; i++) 
			{
				double AlphaDelta = learningCoefficient * deltaI[i];
				for (int j = 0; j < Number_inputs; j++) 
				{
					dataLayer1[i,j] += AlphaDelta * Tinputs[j];
				}
				dataLayer1[i,Number_inputs] += AlphaDelta;
			}

			//	Layer Hidden (2)
			for (int i = 0; i < Number_Hidden2; i++) 
			{
				double AlphaDelta = learningCoefficient * deltaJ[i];
				for (int j = 0; j < Number_Hidden1; j++) 
				{
					dataLayer2[i,j] += AlphaDelta * TAxis1[j];
				}
				dataLayer2[i,Number_Hidden1] += AlphaDelta;
			}

			//	Layer Output (3)
			for (int i = 0; i < Number_outputs; i++) 
			{
				double AlphaDelta = learningCoefficient * deltaK[i];
				for (int j = 0; j < Number_Hidden2; j++) 
				{
					dataLayer3[i,j] += AlphaDelta * TAxis2[j];
				}
				dataLayer3[i,Number_Hidden2] += AlphaDelta;
			}
			
		}
		
		private void NewModel(Random rnd)
		{
			
			stateAgent = "Creating a new model...\nParam: " + Number_Parameters;
			
			
			//	Layer 1 reset:
			for(int i = 0; i < Number_Hidden1; i++)
		   	{
		    	for(int j = 0; j < Number_inputs; j++)
		    	{
		        	dataLayer1[i,j] = rnd.Next(-200,200) / 100.0;
		      	}
		      	dataLayer1[i,Number_inputs] = rnd.Next(-200,200) / 100.0;
		   	}
			
			//	Layer 2 reset:
			for(int i = 0; i < Number_Hidden2; i++)
		   	{
		    	for(int j = 0; j < Number_Hidden1; j++)
		    	{
		        	dataLayer2[i,j] = rnd.Next(-200,200) / 100.0;
		      	}
		      	dataLayer2[i,Number_Hidden1] = rnd.Next(-200,200) / 100.0;
		   	}
			
			//	Layer 3 reset:
			for(int i = 0; i < Number_outputs; i++)
		   	{
		    	for(int j = 0; j < Number_Hidden2; j++)
		    	{
		        	dataLayer3[i,j] = rnd.Next(-200,200) / 100.0;
		      	}
		      	dataLayer3[i,Number_Hidden2] = rnd.Next(-200,200) / 100.0;
		   	}
			
			stateAgent += "\nModel Created.";
		}
		
		//	Load from a binary File
		public void LoadModel(string path)
		{
			//string path = "C:\\Users\\Binary\\Desktop\\data.dat";
			bool error = false;
			
			stateAgent = ("Loading Model...");
			
			if(!File.Exists(path)) { stateAgent+=("\n(!) Error: Error Load NN Model. (The file does not exists)."); error = true; throw new ArgumentNullException(); }
			
			
			byte[] AllDataBytes = File.ReadAllBytes(path);
			
			double[] AllData = GetDoubles(AllDataBytes);
			
			uint indexParameter = 0;
			
			//	Layer 1
			for(int i = 0; i < Number_Hidden1; i++)
			{
				for(int j = 0; j < Number_inputs; j++)
				{
					dataLayer1[i,j] = AllData[indexParameter]; 
					indexParameter++;
				}
				dataLayer1[i,Number_inputs] = AllData[indexParameter]; 
				indexParameter++;
			}
			
			//	Layer 2
			for(int i = 0; i < Number_Hidden2; i++)
			{
				for(int j = 0; j < Number_Hidden1; j++)
				{
					dataLayer2[i,j] = AllData[indexParameter]; 
					indexParameter++;
				}
				dataLayer2[i,Number_Hidden1] = AllData[indexParameter]; 
				indexParameter++;
			}
			
			//	Layer 3
			for(int i = 0; i < Number_outputs; i++)
			{
				for(int j = 0; j < Number_Hidden2; j++)
				{
					dataLayer3[i,j] = AllData[indexParameter]; 
					indexParameter++;
				}
				dataLayer3[i,Number_Hidden2] = AllData[indexParameter]; 
				indexParameter++;
			}
			
			if(AllData.Length != Number_Parameters) 
			{
				stateAgent+=("\n(!) Warning: Number of loaded parameters do not match the number of configured parameters: Detected param = " + AllData.Length);
				error = true;	
			}
			
			if(!error) stateAgent+=("\nModel successfully loaded.");
			
		}
		
		/*
		//	Load from a CSV File
		public void LoadModel(string path)
		{
			stateAgent = ("Loading Model...");
			
			bool error = false;
			
			//string path = @"C:\Users\" + Environment.UserName + @"\Desktop\data." + ".txt";
			//string path = "data." + ".txt";
			
			if(!File.Exists(path)) { stateAgent+=("\n(!) Error: Error Load NN Model. (The file does not exists)."); error = true; throw new ArgumentNullException(); }
			
			string data = File.ReadAllText(path);
			
			double[] parametersVector = new double[Number_Parameters];
			
			
			int a = 0;
			
			for(int i = 0; i < Number_Parameters; i++)
			{
				string chunk = "";
				
				while(data[a] != ',' && data[a] != ';'){	
					chunk+=data[a];
					a++;
				}
				
				
				
				//stateAgent+=("\nchunk " + chunk);
				parametersVector[i] = Convert.ToDouble(chunk);
				//stateAgent+=("\nparam " + parametersVector[i]);
				
				if(data[a] == ';') break;
				else a++;
			}
			
			int ipa = 0;
			
			//	Layer 1
			for(int j = 0; j < Number_Hidden1;j++)
			{
				for(int k = 0; k < Number_inputs; k++)
				{
					dataLayer1[j,k] = parametersVector[ipa];
					ipa++;
				}
				dataLayer1[j,Number_inputs] = parametersVector[ipa];
				ipa++;
			}
			//	Layer 2
			for(int j = 0; j < Number_Hidden2;j++)
			{
				for(int k = 0; k < Number_Hidden1; k++)
				{
					dataLayer2[j,k] = parametersVector[ipa];
					ipa++;
				}
				dataLayer2[j,Number_Hidden1] = parametersVector[ipa];
				ipa++;
			}
			//	Layer 3
			for(int j = 0; j < Number_outputs;j++)
			{
				for(int k = 0; k < Number_Hidden2; k++)
				{
					dataLayer3[j,k] = parametersVector[ipa];
					ipa++;
				}
				dataLayer3[j,Number_Hidden2] = parametersVector[ipa];
				ipa++;
			}
			
			if(ipa != Number_Parameters) 
			{
				stateAgent+=("\n(!) Warning: Number of loaded parameters do not match the number of configured parameters: Detected param = " + ipa);
				error = true;	
			}
			
			if(!error) stateAgent+=("\nModel successfully loaded.");
			
		}
		
		//	Save in a CSV File
		public void SaveModel(string path)
		{
			if(!learn) return;
			
			stateAgent = "Saving model...";
			//string path = @"C:\Users\" + Environment.UserName + @"\Desktop\data." + ".txt";
			//string path = "data." + ".txt";
			
			string data = "";
			
			//	Layer 1
			for(int i = 0; i < Number_Hidden1; i++)
			{
				for(int j = 0; j < Number_inputs;j++)
				{
					if(i == 0 && j == 0) data+=Math.Round(dataLayer1[i,j],5).ToString();
					else data+= "," + Math.Round(dataLayer1[i,j], 5).ToString();
				}
				data+= "," + Math.Round(dataLayer1[i,Number_inputs], 5).ToString();
			}
			
			//	Layer 2
			for(int i = 0; i < Number_Hidden2; i++)
			{
				for(int j = 0; j < Number_Hidden1;j++)
				{
					data+= "," + Math.Round(dataLayer2[i,j],5).ToString();
				}
				data+= "," + Math.Round(dataLayer2[i,Number_Hidden1],5).ToString();
			}
			
			//	Layer 3
			for(int i = 0; i < Number_outputs; i++)
			{
				for(int j = 0; j < Number_Hidden2;j++)
				{
					data+= "," + Math.Round(dataLayer3[i,j],5).ToString();
				}
				data+= "," + Math.Round(dataLayer3[i,Number_Hidden2],5).ToString();
			}
			
			data+= ";";
			
			
			File.WriteAllText(path, data);
			
			stateAgent+="\ndata successfully saved.";
			
		}*/
		
		//	Save in a binary file
		public void SaveModel(string path)
		{
			//string path = "C:\\Users\\Binary\\Desktop\\data.dat";
			stateAgent = "Saving model...";
			
			double[] AllData = new double[Number_Parameters];
			uint indexParameter = 0;
			
			//	Layer 1
			for(int i = 0; i < Number_Hidden1; i++)
			{
				for(int j = 0; j < Number_inputs;j++)
				{
					AllData[indexParameter] = dataLayer1[i,j]; 
					indexParameter++;
				}
				AllData[indexParameter] = dataLayer1[i,Number_inputs];
				indexParameter++;
			}
			
			//	Layer 2
			for(int i = 0; i < Number_Hidden2; i++)
			{
				for(int j = 0; j < Number_Hidden1;j++)
				{
					AllData[indexParameter] = dataLayer2[i,j]; 
					indexParameter++;
				}
				AllData[indexParameter] = dataLayer2[i,Number_Hidden1];
				indexParameter++;
			}
			
			//	Layer 3
			for(int i = 0; i < Number_outputs; i++)
			{
				for(int j = 0; j < Number_Hidden2;j++)
				{
					AllData[indexParameter] = dataLayer3[i,j]; 
					indexParameter++;
				}
				AllData[indexParameter] = dataLayer3[i,Number_Hidden2];
				indexParameter++;
			}
			
			byte[] AllDataBytes = GetBytes(AllData);
			
			File.WriteAllBytes(path, AllDataBytes);
			
			stateAgent+="\ndata successfully saved.";
		}
				
		private double Sigmoid(double x)
		{
			return 1 / (1 + Math.Pow(e, -x));
		}
		
		private byte[] GetBytes(double[] values)
        {
            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        }

        private double[] GetDoubles(byte[] bytes)
        {
            return Enumerable.Range(0, bytes.Length / sizeof(double))
                .Select(offset => BitConverter.ToDouble(bytes, offset * sizeof(double)))
                .ToArray();
        }
		
	}
	
	public class TElement
	{
		public int bars;
		public bool isActive;
		public double[] inputs;
		
		public TElement(double[] inputsArray)
		{
			
			if(inputsArray == null) isActive = false;
			else isActive = true;
			bars = 0;
			
			inputs = inputsArray;
		}
	}
}
