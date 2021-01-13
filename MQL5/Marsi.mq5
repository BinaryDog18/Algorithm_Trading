//+------------------------------------------------------------------+
//|                                                        Marsi.mq5 |
//|                                                        BinaryDog |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "BinaryDog"
#property link      ""
#property version   "1.00"

#include <Trade\Trade.mqh>

input int MagicNumber = 100001;
input int Slippage = 3;
input int MA_PERIOD = 660;
input ENUM_MA_METHOD MA_MODE = MODE_EMA;
input int RSI_PERIOD = 14;
input double Lots = 0.1;

input bool CalculateInNewBar = true;
input bool LotsAuto = true;
input double FactorLoss = 0.5; // Factor Loss:  Loss/Profit. 

//input double Expect = 0.25;
//input int Cobertura = 260;

double FactorRange = 0.55;
int MaxMinPeriod = 60;

double lotss;

datetime Old_Time;
datetime New_Time;

double currentMax;
double currentMin;

int MainMA_Handle;
int RSI_Handle;
//---
double ExpectPIPSWin = 0.0040;
double ExpectPIPSLose = ExpectPIPSWin/2;
//---
CTrade trade;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   MainMA_Handle = iMA(Symbol(),PERIOD_CURRENT, MA_PERIOD,0,MA_MODE,PRICE_CLOSE);
   RSI_Handle = iRSI(Symbol(), PERIOD_CURRENT, RSI_PERIOD, PRICE_CLOSE);
   
   if(MainMA_Handle < 0 || RSI_Handle < 0) 
      return(INIT_FAILED);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---


   bool newbar = isNewBar();
   
   if(PositionsTotal() > 0 && newbar) verificarOperacionesAbiertas(Symbol());
   
   
   if(!newbar && CalculateInNewBar) return;
   
   if(LotsAuto) lotss = AccountInfoDouble(ACCOUNT_BALANCE) / 10000;
   else lotss = Lots; 
   
   lotss = NormalizeDouble(lotss,2);


   bool ErrorIndicator = false;
   
   // Refresh Indicators
   
   //--- Buffers ---
   double _MABuffer[],
          _RSIBuffer[],
          _HighsBuffer[],
          _LowsBuffer[],
          _ClosesBuffer[];
   
   ArraySetAsSeries(_MABuffer, true);
   ArraySetAsSeries(_RSIBuffer, true);       
   ArraySetAsSeries(_HighsBuffer, true);     
   ArraySetAsSeries(_LowsBuffer, true);     
   ArraySetAsSeries(_ClosesBuffer, true);
   
   // Set Buffers
   if (CopyBuffer(MainMA_Handle,0,0,4,_MABuffer) < 0)  {Print("CopyBuffer MainMA_Handle Error = ",GetLastError());ErrorIndicator = true;}
   //
   if (CopyBuffer(RSI_Handle, 0,0,3,_RSIBuffer) < 0)   {Print("CopyBuffer RSI_Handle Error = ",GetLastError());   ErrorIndicator = true;}
   //
   if (CopyHigh(Symbol(),PERIOD_CURRENT, 1,600, _HighsBuffer) < 0){Print("CopyHigh Historical Data Error = ",GetLastError());ErrorIndicator = true;}
   //
   if (CopyLow(Symbol(), PERIOD_CURRENT, 1,600, _LowsBuffer) < 0) {Print("CopyLow Historical Data Error = ",GetLastError());ErrorIndicator = true;}
   //
   if (CopyClose(Symbol(),PERIOD_CURRENT,0, 200, _ClosesBuffer) < 0){Print("CopyClose Historical Data Error = ",GetLastError());ErrorIndicator = true;}
   
   // MA[] { 0,   1,    2}
   //        ^ Current bar -> Position 0
   //             ^ Previous bar -> Position 1
   
   if(ErrorIndicator){ Print("Error Indicator: ", GetLastError()); return;}
   
   
   currentMax = calcMax(_ClosesBuffer);
   currentMin = calcMin(_ClosesBuffer);
   double Price = _ClosesBuffer[0];
   double range = currentMax - currentMin;
   
   ExpectPIPSWin = range * FactorRange;
   ExpectPIPSLose = ExpectPIPSWin*FactorLoss;
   
   
   
   //---
   long ord = 0;
   //
   if(VerificarCondicionesCompra(_MABuffer,_RSIBuffer, _ClosesBuffer)
      && PositionsTotal() == 0)
      {
         
         //ord = enviarOrden(Symbol(), 0, NormalizeDouble(ExpectationLoss,_Digits)*1000000, NormalizeDouble(ExpectationProfit,_Digits)*1000000);
         ord = enviarOrden(Symbol(), 0, 0, 0);
      }
      
   //
   else if(VerificarCondicionesVenta(_MABuffer,_RSIBuffer,_ClosesBuffer)
      && PositionsTotal() == 0)  
      {
         
         //ord = enviarOrden(Symbol(), 1, NormalizeDouble(ExpectationLoss,_Digits)*1000000, NormalizeDouble(ExpectationProfit,_Digits)*1000000);
         ord = enviarOrden(Symbol(), 1, 0, 0);
      }
      
   //---
      
   if(ord < 0) Print("Error in OrderSend: ", GetLastError());   
   
  }
//+------------------------------------------------------------------+
bool VerificarCondicionesCompra(double &MABuffer[],
                                double &RSIBuffer[],
                                double &ClosesBuffer[])
{
   if(RSIBuffer[1] <  30 &&
     (MABuffer[3] <= MABuffer[2] <= MABuffer[1]) &&
      ClosesBuffer[1] > MABuffer[1])
         return true;
         
   else return false;                           // MA[] { 0,   1,    2}
                                                //        ^ Current bar -> Position 0
                                                //             ^ Previous bar -> Position 1
}
//---
bool VerificarCondicionesVenta(double &MABuffer[],
                               double &RSIBuffer[],
                               double &ClosesBuffer[])
{
   if(RSIBuffer[1] >  70 &&
      MABuffer[3] >= MABuffer[2] && MABuffer[2] >= MABuffer[1] &&
      ClosesBuffer[1] < MABuffer[1])
         return true;
         
   else return false;      
}
//---
bool VerificarCondicionesHorarias()
{
   datetime    tm=TimeCurrent();
   MqlDateTime stm;
   TimeToStruct(tm,stm);
   
   if(stm.day_of_week != 1 && 
      stm.day_of_week != 5 && 
      stm.hour != 2 &&
      stm.hour != 1) 
         return true;
         
         
   else return false;
}

//+------------------------------------------------------------------+
bool enviarOrden(string symbol, int OrderTypee, const double SL, const double TP)   
{
      
      
      bool op = false;
      
      double Ask = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
      double Bid = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
      // OrderSend : BUY = 0, SELL = 1
      if(OrderTypee == 0)
      {
         double SLLevel = Bid-(SL*_Point);
         double TPLevel = Bid+(TP*_Point);
         
         if(SL == 0 && TP == 0)
         {
            SLLevel = 0;
            TPLevel = 0;
         }
         
         op = trade.Buy(Lots,
                        NULL,
                        Ask,
                        SLLevel, //   5 DIGITS
                        TPLevel,
                        NULL);
           
      }   
      else if(OrderTypee == 1)
      {
         double SLLevel = Ask+(SL*_Point);
         double TPLevel = Ask-(TP*_Point);
         
         
         if(SL == 0 && TP == 0)
         {
            SLLevel = 0;
            TPLevel = 0;
         }
         
         op = trade.Sell(Lots,
                        NULL,
                        Bid,
                        SLLevel,
                        TPLevel,
                        NULL);
                   
         //double stoploss = NormalizeDouble(SYMBOL_ASK+Expectation*Point(),Digits());
         //double takeprofit = NormalizeDouble(SYMBOL_ASK-Expectation*Point(),Digits());
         
         //op = OrderSend(Symbol(), OrderTypee, Lots, SYMBOL_BID, Slippage, stoploss, takeprofit, NULL, MagicNumber, 0, Red);
      }   
      
      return op;     
}
//+------------------------------------------------------------------+
double VolatilidadMax(int cobertura, double &HighBuffer[], double &LowBuffer[])
{
   
   double temp = MaximoEnGrafico(cobertura,HighBuffer) - MinimoEnGrafico(cobertura,LowBuffer);
   return temp;
}
//---
double MaximoEnGrafico(int cobertura, double &High[]){
   
   int i = ArrayMaximum(High, cobertura, 1);
   
   return High[i];
      
}
//---
double MinimoEnGrafico(int cobertura, double &Low[]){

   int i = ArrayMinimum(Low, cobertura, 1);
   
   return Low[i]; 
}
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
double calcMax(double &ClosesBuffer[])
{
   double Max = 0;
   for(int i = 0; i < MaxMinPeriod; i++) if(ClosesBuffer[i] > Max) Max = ClosesBuffer[i];
   
   return Max;
}

double calcMin(double &ClosesBuffer[])
{
   double Min = 9999999;
   for(int i = 0; i < MaxMinPeriod; i++) if(ClosesBuffer[i] < Min) Min = ClosesBuffer[i];
   
   return Min;
}
//+------------------------------------------------------------------+
bool isNewBar()
{
   New_Time = iTime(Symbol(),PERIOD_CURRENT, 0);
   
   if(New_Time != Old_Time) 
   {
      Old_Time = New_Time;
      return true; 
   }
   else return false;
}
//+------------------------------------------------------------------+
void verificarOperacionesAbiertas(string symbol)
{
   bool sucess;
      sucess = PositionSelect(symbol);
      
      double Ask = NormalizeDouble(SymbolInfoDouble(symbol,SYMBOL_ASK),_Digits);
      double Bid = NormalizeDouble(SymbolInfoDouble(symbol,SYMBOL_BID),_Digits);
      
      
      
      if(sucess)
      {
         bool isPositive= true;
         double OpenPrice = PositionGetDouble(POSITION_PRICE_OPEN);
         long type = PositionGetInteger(POSITION_TYPE);
         
         double difAsk = NormalizeDouble(MathAbs(OpenPrice - Ask),_Digits);
         double difBid = NormalizeDouble(MathAbs(OpenPrice - Bid),_Digits);
         
         if(type == POSITION_TYPE_BUY  && (OpenPrice < Ask)) isPositive = true;
         if(type == POSITION_TYPE_BUY  && (OpenPrice > Ask)) isPositive = false;
         if(type == POSITION_TYPE_SELL && (OpenPrice < Bid)) isPositive = false;
         if(type == POSITION_TYPE_SELL && (OpenPrice > Bid)) isPositive = true;
         
         //Print("DifBid : ",difBid);
         if(type == POSITION_TYPE_BUY)
         {
            if((Ask >= currentMax || difAsk >= ExpectPIPSWin) && isPositive) trade.PositionClose(symbol,Slippage);
            else if((Ask >= currentMax || difAsk >= ExpectPIPSLose) && !isPositive) trade.PositionClose(symbol,Slippage);
         }
         else if(type == POSITION_TYPE_SELL)
         {
            if((Bid <= currentMin || difBid >= ExpectPIPSWin) && isPositive) trade.PositionClose(symbol,Slippage);
            else if((Bid <= currentMin || difBid >= ExpectPIPSLose) && !isPositive) trade.PositionClose(symbol,Slippage);
         }
      }
}
//+------------------------------------------------------------------+