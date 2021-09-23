using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBot.Entities;

namespace TelegramBot.Infrastructure
{
    public class MessageTrackingConfiguration : IEntityTypeConfiguration<MessageTracking>
    {
        public void Configure(EntityTypeBuilder<MessageTracking> builder)
        {
            builder.HasKey(x => x.ChatId);
            builder.OwnsMany(x => x.TrackedWords, tw =>
            {
                tw.HasKey(x => x.Id);
                tw.Property(x => x.Id).ValueGeneratedOnAdd();
                tw.HasIndex(x => x.Value);
            });
        }
    }
}
