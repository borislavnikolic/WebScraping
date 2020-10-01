using System;

namespace WebScraping.Model
{
    public class CurrencyData
    {
        public String CurrencyName { get; set; }
        public String BuyingRate { get; set; }
        public String CashBuyingRate { get; set; }
        public String SellingRate { get; set; }
        public String CashSellingRate { get; set; }
        public String MiddleRate { get; set; }
        public String PubTime { get; set; }

        public int PageNumber { get; set; }

        public override string ToString()
        {
            return CurrencyName + "," + BuyingRate + ","
                +  CashBuyingRate + "," + SellingRate + ","
                +  CashSellingRate + "," + MiddleRate + ","
                +  PubTime+","+PageNumber;
        }
    }
}