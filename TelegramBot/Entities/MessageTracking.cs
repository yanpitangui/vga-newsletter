using System;
using System.Collections;
using System.Collections.Generic;

namespace TelegramBot.Entities
{
    public class MessageTracking
    {
        public MessageTracking(int chatId)
        {
            ChatId = chatId;
        }
        public int ChatId { get; set; }
        public IEnumerable<Word> TrackedWords = new List<Word>();
    }

    public class Word
    {
        public Word(string value)
        {
            Value = value;
        }

        public Guid Id { get; set; }
        public string Value { get; set; }
    }
}