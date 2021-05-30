using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspMesa
{
    /// <summary>
    /// Интерфейс, обспечивающий сохранение данных между перезапусками.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Получает список пользователей.
        /// </summary>
        public ValueTask<List<User>> GetUsers();

        /// <summary>
        /// Получает список сообщений.
        /// </summary>
        public ValueTask<List<Email>> GetMessages();

        /// <summary>
        /// Добавляет в хранилище пользователя.
        /// </summary>
        public ValueTask AddUser(User user);

        /// <summary>
        /// Добавляет в хранилище сообщение.
        /// </summary>
        public ValueTask AddMessage(Email user);
    }
}