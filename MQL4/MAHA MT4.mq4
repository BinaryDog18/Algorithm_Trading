//+------------------------------------------------------------------+
//|                                                     MAHA MT4.mq4 |
//|                                                        BinaryDog |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "BinaryDog"
#property link      ""
#property version   "1.00"
#property strict

extern int MagicNumber = 111110;
extern int Slippage = 3;
extern int MA_PERIOD_LONG = 100;
extern int MA_PERIOD_SHORT = 100;
extern ENUM_MA_METHOD MA_MODE_LONG = 0;
extern ENUM_MA_METHOD MA_MODE_SHORT = 0;
extern double Lots = 0.1;


double ExpectationProfit;
double ExpectationLoss;


#define HAOPEN      2
#define HALOW      1
#define HAHIGH       0
#define HACLOSE     3

//#define HAHIGH       0
//#define HALOW        1
//#define HAOPEN       2
//#define HACLOSE      3

#define Expect 0.25




color color1 = Red;
color color2 = Blue;
color color3 = Red;
color color4 = Blue;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   
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
   
   //Print("High: ", HA(1, HAHIGH),".    Low: ", HA(1, HALOW), ".    Open: ", HA(1, HAOPEN), ".     Close: ", HA(1, HACLOSE));
   
   // Comprobar velas 
   //Print("Ma anterior anterior ", MA(MA_PERIOD_LONG, MA_MODE_LONG, 2), ".    MA anterior: ", MA(MA_PERIOD_LONG, MA_MODE_LONG, 1));
   //Print("HA Open: ", HA(1, HAOPEN),".    HA Close: ", HA(1, HACLOSE), ".    HA Low:",HA(1, HALOW), ".    HA High:",HA(1, HAHIGH));
   
   if(!hayTradesAbiertos())
   {
      if(VerificarCondicionesCompra() &&
         VerificarCondicionesHorarias())
         {
            // Enviar orden de compra
            
            ExpectationProfit = Expect*(VolatilidadMax(150));
            ExpectationLoss = ExpectationProfit;
            
            bool op = enviarOrden(Symbol(), 0, ExpectationLoss*100000, ExpectationProfit*100000);// == true ? return : Print("Error al enviar orden de compra. ERROR: ", GetLastError());
            
            if(op) return;
            else Print("Error al enviar orden de compra. ERROR: ", GetLastError());
         }
      else if(VerificarCondicionesVenta() &&
         VerificarCondicionesHorarias())  
         {
            ExpectationProfit = Expect*(VolatilidadMax(150));
            ExpectationLoss = ExpectationProfit;
            
            bool op = enviarOrden(Symbol(), 1, ExpectationLoss*100000, ExpectationProfit*100000);// == true ? return : Print("Error al enviar orden de compra. ERROR: ", GetLastError());
            
            if(op) return;
            else{
             Print("Error al enviar orden de venta. ERROR: ", GetLastError());
             }
         } 
      else{
       Comment("Volatility: ", VolatilidadMax(150),"\nExpect: ", ExpectationProfit*10000);   
       //Comment("Expect: ", ExpectationProfit);
       }
   }
  }
//+------------------------------------------------------------------+
bool VerificarCondicionesCompra()
{
   if(MA(MA_PERIOD_LONG, MA_MODE_LONG, 2) < MA(MA_PERIOD_LONG, MA_MODE_LONG, 1) &&//MABuffer[2] < MABuffer[1] &&   
                                      
      HA(1, HAOPEN) < HA(1, HACLOSE) &&                                             //HAOpen[1] < HAClose[1] && 
                                   
      HA(1, HALOW)  < MA(MA_PERIOD_LONG,MA_MODE_LONG, 1) &&                        //HALowBuffer[1] < MABuffer[1] && 

      HA(1, HACLOSE)> MA(MA_PERIOD_LONG,MA_MODE_LONG, 1))                          //HAClose[1] > MABuffer[1])
         return true;
         
   else return false;                           // MA[] { 0,   1,    2}
                                                //        ^ Current bar -> Position 0
                                                //             ^ Previous bar -> Position 1
}
//---
bool VerificarCondicionesVenta()
{
   if(MA(MA_PERIOD_SHORT, MA_MODE_SHORT, 2) > MA(MA_PERIOD_SHORT, MA_MODE_SHORT, 1) &&//MABuffer[2] > MABuffer[1] &&
   
      HA(1, HAOPEN) > HA(1, HACLOSE) &&                                              //HAOpen[1] > HAClose[1] &&
      
      HA(1, HAHIGH) > MA(MA_PERIOD_SHORT,MA_MODE_SHORT, 1) &&                       //HAHighBuffer[1] > MABuffer[1] &&
      
      HA(1, HACLOSE)< MA(MA_PERIOD_SHORT,MA_MODE_SHORT, 1))                        //HAClose[1] < MABuffer[1])
         return true;
         
   else return false;      
}
//+------------------------------------------------------------------+
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
bool enviarOrden(string symbol, int OrderTypee, double SL, double TP)   // TP & SL in cents. Example(SL = 1500, TP = 1500)
{
   // BUY = 0;
   // SELL = 1
   
   int ord = -1;
   
   if(OrderTypee == 0)
   {
      double SLLevel = Bid-(SL*_Point);
      double TPLevel = Bid+(TP*_Point);
      
      ord = OrderSend(symbol, OP_BUY, Lots, Ask, Slippage, SLLevel, TPLevel, NULL, MagicNumber, 0, Blue);
   }
   else if(OrderTypee == 1)
   {
      double SLLevel = Ask+(SL*_Point);
      double TPLevel = Ask-(TP*_Point);
      
      ord = OrderSend(symbol, OP_SELL,Lots, Bid, Slippage, SLLevel, TPLevel, NULL, MagicNumber, 0, Red);
   }
   
   return ord > 0 ? true : false;
}
//---
bool hayTradesAbiertos()
{

   for(int i=0; i<OrdersTotal(); i++)
   {
      int ord = OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber)
         return true;
   
   }
     
     return false;
}

//+------------------------------------------------------------------+
double VolatilidadMax(int cobertura)
{
   
   double temp = MaximoEnGrafico(cobertura) - MinimoEnGrafico(cobertura);
   return temp;
}

double MaximoEnGrafico(int cobertura){
   
   double max = 0;
   
   for(int i = 0; i < cobertura; i++)
      if(HA(i,HAHIGH) > max) max = HA(i,HAHIGH);
   
   return max;
      
}

double MinimoEnGrafico(int cobertura){

   double min = 99999;
   
   for(int i = 0; i < cobertura; i++)
      if(HA(i,HALOW) < min) min = HA(i,HALOW);
   
   return min; 
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
double MA(int period, int mode, int position)   // Current Symbol and Current TimeFrame
{
   return iMA(NULL, 0, period, 0, mode,PRICE_CLOSE, position);
}
//---
double HA(int position, int mode)   // Current Symbol and Current TimeFrame
{
   return iCustom(NULL, 0, "Heiken Ashi", color1,color2,color3,color4, mode, position);
}
//+------------------------------------------------------------------+

