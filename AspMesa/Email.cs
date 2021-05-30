namespace AspMesa
{
    /// <summary>
    /// Информация о сообщении.
    /// </summary>
    public class Email
    {
        /// <summary>
        ///     Тема письма.
        /// </summary>
        public string Subject { get; init; }
        
        /// <summary>
        ///     Содержимое письма.
        /// </summary>
        public string Message { get; init; }
        
        /// <summary>
        ///     Email отправителя.
        /// </summary>
        public string SenderId { get; init; }
        
        /// <summary>
        ///     Email получателя.
        /// </summary>
        public string ReceiverId { get; init; }
    }
}