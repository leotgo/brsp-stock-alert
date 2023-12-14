public interface IStockData
{
    bool IsValidData { get; }
    string StockCode { get; }
    string ShortName { get; }
    string LongName { get; }
    double CurrentPrice { get; }
    public DateTime Timestamp { get; }
}

public class InvalidStockData : IStockData
{
    public bool IsValidData => false;
    public string StockCode => string.Empty;
    public string ShortName => string.Empty;
    public string LongName => string.Empty;
    public double CurrentPrice => -1;

    public DateTime Timestamp => DateTime.MinValue;
}