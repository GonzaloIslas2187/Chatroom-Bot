using System;

namespace Chatroom_Bot.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public string Content { get; set; }

        public string UserName { get; set; }
    }
}
