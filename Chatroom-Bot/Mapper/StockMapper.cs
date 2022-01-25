using Chatroom_Bot.Entities;
using TinyCsvParser.Mapping;

namespace Chatroom_Bot.Mapper
{
    public class StockMapper : CsvMapping<Stock>
    {
        public StockMapper()
                : base()
        {
            MapProperty(0, x => x.Volume);
            MapProperty(1, x => x.High);
            MapProperty(2, x => x.Low);
            MapProperty(3, x => x.Open);
            MapProperty(4, x => x.Close);
            MapProperty(5, x => x.Date);
            MapProperty(6, x => x.Symbol);
            MapProperty(7, x => x.Time);
        }
    }
}
