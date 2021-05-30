using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AspMesa
{
    /// <summary>
    ///     Реализация IStorage, которая хранит данные в двух json файлах.
    ///     Причём список пользователей хранится отсортированно по Email.
    /// </summary>
    public class JsonStorage : IStorage, IDisposable
    {
        private readonly FileStream _users;
        private readonly FileStream _messages;

        /// <summary>
        ///     Конструктор.
        /// </summary>
        public JsonStorage()
        {
            _users = File.Open("users.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _messages = File.Open("users.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _users?.Dispose();
            _messages?.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask<List<User>> GetUsers()
        {
            List<User> result = null;
            try
            {
                _users.Seek(0, SeekOrigin.Begin);
                result = await JsonSerializer.DeserializeAsync<List<User>>(_users);
            }
            catch
            {
                // ignored
            }

            return result ?? new List<User>();
        }

        /// <inheritdoc />
        public async ValueTask<List<Email>> GetMessages()
        {
            List<Email> result = null;
            try
            {
                _messages.Seek(0, SeekOrigin.Begin);
                result = await JsonSerializer.DeserializeAsync<List<Email>>(_messages);
            }
            catch
            {
                // ignored
            }

            return result ?? new List<Email>();
        }

        /// <inheritdoc />
        public async ValueTask AddUser(User user)
        {
            var list = await GetUsers();
            list.Add(user);
            _users.Seek(0, SeekOrigin.Begin);
            await JsonSerializer.SerializeAsync(_users, list);
        }

        /// <inheritdoc />
        public async ValueTask AddMessage(Email email)
        {
            var list = await GetMessages();
            list.Add(email);
            _messages.Seek(0, SeekOrigin.Begin);
            await JsonSerializer.SerializeAsync(_messages, list);
        }
    }
}