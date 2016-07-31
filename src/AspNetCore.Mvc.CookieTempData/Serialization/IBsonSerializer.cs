namespace AspNetCore.Mvc.CookieTempData.Serialization
{
    /// <summary>
    /// A generic serializer that uses the BSON format.
    /// </summary>
    public interface IBsonSerializer
    {
        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The resulting bytes</returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// Deserializes an object.
        /// </summary>
        /// <typeparam name="T">The expected type after deserialization.</typeparam>
        /// <param name="bytes">The serialized data.</param>
        /// <returns>The resulting object</returns>
        T Deserialize<T>(byte[] bytes);
    }
}
