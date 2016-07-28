namespace AspNetCore.Mvc.CookieTempData.Serialization
{
    public interface IBsonSerializer
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] bytes);
    }
}
