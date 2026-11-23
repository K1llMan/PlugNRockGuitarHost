using System.Net.WebSockets;
using System.Text.Json;

using MessagePack;

using PlugNRockGuitarHost.Processor;
using PlugNRockGuitarHost.Processor.Effects;
using PlugNRockGuitarHost.Processor.Effects.NoteDetector;
using PlugNRockGuitarHost.Server.Commands;
using PlugNRockGuitarHost.Server.Models;

namespace PlugNRockGuitarHost.Server
{
    public class CommandServer
    {
        #region Поля

        private StaDispatcher staDispatcher;

        private WebApplication server;
        private List<IBasicCommand> commands;

        #endregion Поля

        #region Свойства

        public CommandServerOptions Options { get; }

        #endregion Свойства

        #region Вспомогательные функции

        #region Сервер

        #region Работа с сообщениями

        private async Task ExtractAndExecute(WebSocket webSocket, WebSocketReceiveResult receiveResult, byte[] message)
        {
            CommandData? response = null;

            try
            {
                CommandData commandData = MessagePackSerializer.Deserialize<CommandData>(message);
                IBasicCommand? command = commands.FirstOrDefault(c => c.Type.Equals(commandData.Type));
                if (command == null)
                    return;

                Type parametersType = command.GetType().BaseType.GenericTypeArguments.FirstOrDefault();

                object? parameters = parametersType != null && commandData.Parameters != null
                    ? JsonSerializer.Deserialize(commandData.Parameters, parametersType)
                    : null;

                object? data = command.Execute(parameters);
                if (data != null) 
                    response = new() {
                        Type = commandData.Type,
                        Parameters = JsonSerializer.Serialize(data)
                    };
            }
            catch (Exception ex)
            {
                response = CommandData.Error(ex);
            }

            if (response != null)
                await SendMessageAsync(webSocket, response);
        }

        private async Task SendMessageAsync(WebSocket webSocket, CommandData? message)
        {
            if (message != null)
                await webSocket.SendAsync(
                    new ArraySegment<byte>(MessagePackSerializer.Serialize(message)),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);
        }

        private async Task GetMessageAsync(WebSocket webSocket)
        {
            byte[] buffer = new byte[1024 * 4];

            // Receive initial message
            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                await staDispatcher.InvokeAsync(() => ExtractAndExecute(webSocket, receiveResult, buffer));

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            // Complete the close handshake
            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }

        #endregion Работа с сообщениями

        private WebApplication InitServer()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            WebApplication app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets(new() {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });

            app.Map("/ws", async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                using GuitarProcessor processor = InitProcessor(webSocket);
                await GetMessageAsync(webSocket);
            });

            return app;
        }

        #endregion Сервер

        #region Процессор

        private IProcessorEffect InitNoteDetector(WebSocket webSocket)
        {
            NWavesNoteDetector detector = new();

            detector.NotesChanged += async (sender, e) => {
                await SendMessageAsync(webSocket, new CommandData {
                    Type = "notes",
                    Parameters = JsonSerializer.Serialize(e.MidiNotes)
                });
            };

            return detector;
        }

        private IProcessorEffect InitPitchShifter(GuitarProcessorOptions options)
        {
            PitchShifter shifter = new();
            shifter.SetPitch(options.Pitch);

            commands.Add(new SetPitchCommand(pitch => {
                if (pitch != null)
                    shifter.SetPitch((int)pitch);

                return null;
            }));

            return shifter;
        }

        private IProcessorEffect InitNAM()
        {
            NAM nam = new();

            commands.AddRange(
                new InitNAMCommand(tones => {
                    if (tones != null)
                        nam.Init(tones);
                    return null;
                }),
                new SetNAMEffectCommand(effect => {
                    if (effect != null)
                        nam.SetEffect(effect);
                    return null;
                })
            );

            return nam;
        }

        private IProcessorEffect InitVolumeControl(GuitarProcessorOptions options)
        {
            VolumeControl control = new() {
                Balance = options.Balance,
                Volume = options.Volume
            };

            commands.AddRange(
                new SetVolumeCommand(volume => {
                    if (volume != null)
                        control.Volume = (float)volume;
                    return null;
                }),
                new SetVolumeBalanceCommand(balance => {
                    if (balance != null)
                        control.Balance = (float)balance;
                    return null;
                })
            );

            return control;
        }

        private GuitarProcessor InitProcessor(WebSocket webSocket)
        {
            GuitarProcessorOptions options = new();
            GuitarProcessor guitarProcessor = new();

            commands.AddRange(
                new GetDriversCommand(_ => guitarProcessor.GetDrivers()),
                new InitProcessorCommand(driver => {
                    if (driver != null)
                        guitarProcessor.Init(driver);
                    return null;
                }),
                new StartProcessorCommand(_ => {
                    guitarProcessor.Start();
                    return null;
                }),
                new StopProcessorCommand(_ => {
                    guitarProcessor.Stop();
                    return null;
                })
            );

            guitarProcessor.AddEffect(InitNoteDetector(webSocket));
            guitarProcessor.AddEffect(InitPitchShifter(options));
            guitarProcessor.AddEffect(InitNAM());
            guitarProcessor.AddEffect(InitVolumeControl(options));

            return guitarProcessor;
        }

        #endregion Процессор

        #endregion Вспомогательные функции

        #region Основные функции

        public CommandServer(CommandServerOptions options)
        {
            // Для запуска в основном потоке приложения
            staDispatcher = new StaDispatcher();

            Options = options;
            commands = new();

            server = InitServer();
        }

        public void Run()
        {
            server.Run(Options.Address);
        }

        #endregion Основные функции
    }
}