using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net.Queue;
using Discord.WebSocket;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace r20esdiscordbot
{
    class BotConfig
    {
        public string Token { get; set; }
        public ulong Server { get; set; }
        public ulong IJustJoinedTheServerRole { get; set; }
        public ulong IssueChannel { get; set; }
    }
    
    class Program
    {
        private static BotConfig config;
        
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public static async Task SendMessageToIssueChannelAndPingPeople(string message, DiscordSocketClient discord, IEnumerable<SocketUser> mention)
        {
            try
            {
                //const ulong channelId = 495907473279156224UL;
                var targetChannel = (ITextChannel)discord.GetChannel(config.IssueChannel);
                
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
            config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));
            
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

            client.UserJoined += async (message) =>
            {
                if (message.Id == client.CurrentUser.Id) return;
                if (message.IsBot) return;
                if (message.IsWebhook) return;
                if (message.Guild.Id != config.Server) return;
                
                var role = message.Guild.GetRole(config.IJustJoinedTheServerRole);

                await message.AddRoleAsync(role);
            };
            
            client.MessageReceived += async (inMessage) =>
            {
                if (inMessage.Author.Id == client.CurrentUser.Id) return;
                if (inMessage.Author.IsBot) return;
                if (inMessage.Author.IsWebhook) return;

                if (!(inMessage is SocketUserMessage)) return;
                var message = (SocketUserMessage) inMessage;
                
                if(message.Channel is IDMChannel dmChannel)
                {
                    if (message.Content.ToLowerInvariant() == "y")
                    {
                        try
                        {
                            var server = client.GetGuild(config.Server);
                            var userInServer = server.GetUser(message.Author.Id);
                            var role = server.GetRole(config.IJustJoinedTheServerRole);
                            if (userInServer == null)
                            {
                                await message.Channel.SendMessageAsync(
                                    "userInServer: server is null. Please contact stormy#0427 to get access to the server.");
                            }
                            else if (role == null)
                            {
                                await message.Channel.SendMessageAsync(
                                    "role: server is null. Please contact stormy#0427 to get access to the server.");
                            }
                            else if (server == null)
                            {
                                await message.Channel.SendMessageAsync(
                                    "bug: server is null. Please contact stormy#0427 to get access to the server.");
                            }
                            else
                            {
                                await userInServer.RemoveRoleAsync(role);
                                await message.Channel.SendMessageAsync("You can now access the rest of the server!");
                            }
                        }
                        catch (Exception e)
                        {
                            await message.Channel.SendMessageAsync("There's been a problem. Please contact stormy#0427 to get access to the server.");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Nothing interesting happens.");
                    }
                }
                else if(message.Channel is IGuildChannel serverChannel)
                {
                    if (serverChannel.Guild.Id != config.Server) return;

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
BROWSER: <What browser (w/ version) do you use? What version of the extension do you use? What other extensions do you use (screenshots preferred)?>
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
                }
            };

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
