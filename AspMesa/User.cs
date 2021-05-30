namespace AspMesa
{
    /// <summary>
    ///     Информация о пользователя.
    /// </summary>
    public class User
    {
        /// <summary>
        ///     Имя человека.
        /// </summary>
        public string UserName { get; init; }
        
        /// <summary>
        ///     Адрес почты.
        /// </summary>
        public string Email { get; set; }
    }
}