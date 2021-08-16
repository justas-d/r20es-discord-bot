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

namespace r20esdiscordbot2 {
  public class BotConfig {
    public string Token { get; set; }
    public ulong Server { get; set; }
    public ulong IJustJoinedTheServerRole { get; set; }
    public ulong IssueChannel { get; set; }
    public ulong JustasAccountId { get; set; }
  }

  public class Program {
    public static BotConfig config;
    public static DiscordSocketClient client;

    public static void Main(string[] args)
      => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task report_error(
      string message,
      IMessageChannel channel
    ) {
      try {
        await channel.SendMessageAsync($"Something went wrong.\nPlease contact Justas#0427 to get access to the server.\nDetail: {message}\n");
          
        var my_acc = client.GetUser(config.JustasAccountId);
        var my_dm = await my_acc.GetOrCreateDMChannelAsync();

        await my_dm.SendMessageAsync($"report_error: Message: {message}. Channel: {channel}\n");
      }
      catch(Exception ex) {
        // NOTE(justasd): ignored
      }
    }

    public async Task MainAsync() {
      config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));

      client = new DiscordSocketClient();

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
              if (userInServer == null) {
                await report_error("bug: userInServer is null", message.Channel);
              }
              else if (role == null) {
                await report_error("bug: role is null", message.Channel);
              }
              else if (server == null) {
                await report_error("bug: server is null", message.Channel);
              }
              else {
                await userInServer.RemoveRoleAsync(role);
                await message.Channel.SendMessageAsync("You can now access the issue discussion channel!");
              }
            }
            catch (Exception e) {
              await report_error(e.Message, message.Channel);
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
