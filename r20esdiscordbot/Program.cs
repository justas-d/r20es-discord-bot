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

namespace r20esdiscordbot {
  class BotConfig {
    public string Token { get; set; }
    public ulong Server { get; set; }
    public ulong IJustJoinedTheServerRole { get; set; }
    public ulong IssueChannel { get; set; }
  }

  class Program {
    private static BotConfig config;

    public static void Main(string[] args)
      => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task MainAsync() {
      config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));

      var client = new DiscordSocketClient();

      client.Log += (message) => {
        Console.WriteLine($"[{message.Severity}] {message.Message} ({message.Source})");
        if (message.Exception != null)
        {
          Console.WriteLine(message.Exception);
        }

        return Task.CompletedTask;
      };

      client.UserJoined += async (message) => {
        if (message.Id == client.CurrentUser.Id) return;
        if (message.IsBot) return;
        if (message.IsWebhook) return;
        if (message.Guild.Id != config.Server) return;

        var role = message.Guild.GetRole(config.IJustJoinedTheServerRole);

        await message.AddRoleAsync(role);
      };

      client.MessageReceived += async (inMessage) => {
        if (inMessage.Author.Id == client.CurrentUser.Id) return;
        if (inMessage.Author.IsBot) return;
        if (inMessage.Author.IsWebhook) return;

        if (!(inMessage is SocketUserMessage)) return;
        var message = (SocketUserMessage) inMessage;

        if(message.Channel is IDMChannel dmChannel) {
          if (message.Content.ToLowerInvariant() == "issues") {
            try {
              var server = client.GetGuild(config.Server);
              var userInServer = server.GetUser(message.Author.Id);
              var role = server.GetRole(config.IJustJoinedTheServerRole);
              var contact_info = "Please contact Justas#0427 to get access to the server.";
              if (userInServer == null) {
                await message.Channel.SendMessageAsync(
                  $"bug: userInServer is null. {contact_info}"
                );
              }
              else if (role == null) {
                await message.Channel.SendMessageAsync(
                  $"bug: role is null. {contact_info}"
                );
              }
              else if (server == null) {
                await message.Channel.SendMessageAsync(
                  $"bug: server is null. {contact_info}"
                );
              }
              else {
                await userInServer.RemoveRoleAsync(role);
                await message.Channel.SendMessageAsync("You can now access the rest of the server!");
              }
            }
            catch (Exception e) {
              await message.Channel.SendMessageAsync($"There's been a problem: '{e.Message}'. {contact_info}");
            }
          }
          else {
            await message.Channel.SendMessageAsync("Nothing interesting happens.");
          }
        }
      };

      await client.LoginAsync(TokenType.Bot, config.Token);
      await client.StartAsync();

      await Task.Delay(-1);
    }
  }
}
