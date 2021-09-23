using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Entities;
using TelegramBot.Infrastructure;

namespace TelegramBot
{
    class Program
    {
        public static DbContextOptions<LocalDbContext> ContextOptions = new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=updates.db")
            .Options;

        public static string[] ValidUsernames = new string[] { "yanpitangui" };
        public const string SetTrackCommandName = "command1";
        public const string GetTracksCommandName = "command2";
        public static Regex GetTracksRegex = new($@"^\/{GetTracksCommandName}\s*$", RegexOptions.Compiled);
        public static Regex SetTrackRegex = new(@$"^\/{SetTrackCommandName}\s+", RegexOptions.Compiled);
        public static Regex SplitTrackRegex = new($@"^\/{SetTrackCommandName}\s+", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        public static Regex SplitSplittedTrackRegex = new($@",*\s+", RegexOptions.Compiled);


        public static async Task Main(string[] args)
        {

            var builder = BuildConfig(new ConfigurationBuilder());
            var configuration = builder.Build();
            Log.Logger = new LoggerConfiguration()
                         .ReadFrom.Configuration(configuration)
                         .Destructure.AsScalar<JObject>()
                         .Destructure.AsScalar<JArray>()
                         .Enrich.FromLogContext()
                         .WriteTo.Console(outputTemplate:
                             "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                         .CreateLogger();
            ITelegramBotClient bot = new TelegramBotClient(configuration.GetValue<string>("Telegram:Token"));
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            await using var context = new LocalDbContext(ContextOptions);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            Log.Logger.Information("Bot starting...");

            try
            {
                await bot.ReceiveAsync<UpdateHandler>(cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                Log.Logger.Error(exception, "Bot operation cancelled.");
            }

        }

        class UpdateHandler : IUpdateHandler
        {
            public async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
            {
                Log.Logger.Information("Handling message {updateId}, {update}", update.Message.MessageId, JObject.FromObject(update));
                await using var context = new LocalDbContext(ContextOptions);

                if (ValidUsernames.Contains(update?.Message?.From?.Username?.Trim()))
                {
                    var returnMessage = string.Empty;
                    var message = update.Message;
                    if (string.IsNullOrWhiteSpace(message.Text)) return;
                    if (!await context.MessageTrackings.AnyAsync(x => x.ChatId == message.From.Id,
                        cancellationToken))
                    {
                        context.MessageTrackings.Add(new MessageTracking(message.From.Id));
                    }

                    if (SetTrackRegex.IsMatch(message.Text))
                    {
                        var splitted = SplitTrackRegex.Split(message.Text).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                        if (string.IsNullOrWhiteSpace(splitted)) return;
                        var tracks = SplitSplittedTrackRegex.Split(splitted);
                        var tracking = await context.MessageTrackings.FindAsync(message.From.Id);
                        tracking.TrackedWords = tracks.Select(x => new Word(x)).ToList();
                        returnMessage = "Tracked words updated.";
                    }

                    else if (GetTracksRegex.IsMatch(message.Text))
                    {
                        var messageTracking = await context.MessageTrackings.FindAsync(message.From.Id);

                        var currentTrackedWords = messageTracking.TrackedWords;
                        if (currentTrackedWords is not null && currentTrackedWords.Any())
                        {
                            returnMessage = $"Current tracked words: {string.Join(", ", currentTrackedWords.Select(x => $"'{x.Value}'"))}";
                        }
                        else
                        {
                            returnMessage = $"Currently, there are zero words being tracked.";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(returnMessage))
                    {
                        await bot.SendTextMessageAsync(message.From.Id,
                            returnMessage, cancellationToken: cancellationToken);
                    }

                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            public Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                Log.Logger.Error(exception, "Error while handling update.");
                return Task.CompletedTask;
            }

            public UpdateType[]? AllowedUpdates { get; }
        }

        static IConfigurationBuilder BuildConfig(IConfigurationBuilder builder)
        {
            return builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();
        }
    }
}
