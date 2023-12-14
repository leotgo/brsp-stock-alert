namespace BRSP
{
    public interface IStockDataProvider
    {
        Task<IStockData?> TryGetStockDataAsync(string stockCode);
    }
}