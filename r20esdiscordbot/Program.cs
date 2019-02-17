using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace r20esdiscordbot
{
    class BotConfig
    {
        public string Token { get; set; }
    }
    
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public static async Task SendMessageToIssueChannelAndPingPeople(string message, DiscordSocketClient discord, IEnumerable<SocketUser> mention)
        {
            try
            {
                //const ulong channelId = 327823001112412163UL;
                const ulong channelId = 495907473279156224UL;
                var targetChannel = (ITextChannel)discord.GetChannel(channelId);
                
                var builder = new StringBuilder();
                
                foreach (var user in mention)
                {
                    builder.Append(user.Mention)
                        .Append(' ');
                }
                

                builder.AppendLine(message);
                
                await targetChannel.SendMessageAsync(builder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: exception during SendMessageToIssueChannelAndPingPeople ${ex}.");
            }
        } 
        

        public async Task MainAsync()
        {
            var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));
            
            var client = new DiscordSocketClient();

            client.Log +=  (message) =>  
            {
                Console.WriteLine($"[{message.Severity}] {message.Message} ({message.Source})");
                if (message.Exception != null)
                {
                    Console.WriteLine(message.Exception);
                }

                return Task.CompletedTask;
            };

            client.MessageReceived += async (message) =>
            {
                var trimmed = message.Content.Trim();

                // !console
                // !background-page
                if (trimmed.StartsWith("!issue"))
                {
                    const string template = @"Please use the following template to file an issue:
```
ISSUE: <What is the issue? Explain as detailed as you can, this is also the perfect place for screenshots of the issue if applicable>
STEPS: <What did I do, or didn't do to get to this issue?>
NOTES: <Any extra information you think could be useful?>
BROWSER: <What browser do you use? What version of the extension do you use? What other extensions do you use (screenshots preferred)?>
```
";
                    await SendMessageToIssueChannelAndPingPeople(template, client, message.MentionedUsers);
                }

                if (trimmed.StartsWith("!console"))
                {
                    const string template = @"Accessing the web console:
:one: Press F12.
:two: In the top bar of the new window, press console.";
                    
                    await SendMessageToIssueChannelAndPingPeople(template, client, message.MentionedUsers);
                    
                }

                if (trimmed.StartsWith("!background-page"))
                {
                    const string template = @"Accessing the background page:
**Chrome**
    :one: Navigate to `chrome://extensions` in the URL bar.
    :two: Turn on developer mode in the top right.
    :three: Click `background page` in the R20ES square.
    :four: In the top bar of the new window, press console.

**Firefox**
    :one: Navigate to `about:debugging` in the URL bar.
    :two: Turn on Debugging (`Enable add-on debugging`)
    :three: Click `Debug` in the R20ES square.
    :four: Accept the debugging connection (nobody's taking over your browser, no worries.)
    :five: In the top bar of the new window, press console.
";
                    
                    await SendMessageToIssueChannelAndPingPeople(template, client, message.MentionedUsers);
                    
                }
            };

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}