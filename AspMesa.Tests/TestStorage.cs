using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspMesa.Tests
{
    public class TestStorage : IStorage
    {
        public List<User> Users { get; } = new List<User>();
        public List<Email> Messages { get; } = new List<Email>();

        public ValueTask<List<User>> GetUsers()
        {
            return new(Users.ToList());
        }

        public ValueTask<List<Email>> GetMessages()
        {
            return new(Messages.ToList());
        }

        public ValueTask AddUser(User user)
        {
            Users.Add(user);
            return new ValueTask();
        }

        public ValueTask AddMessage(Email email)
        {
            Messages.Add(email);
            return new ValueTask();
        }
    }
}