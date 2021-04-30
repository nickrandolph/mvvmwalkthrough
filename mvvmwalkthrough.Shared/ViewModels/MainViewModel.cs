using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using Uno.Extensions.Specialized;

namespace mvvmwalkthrough.ViewModels
{
    public class ChatViewModel : ObservableObject, IRecipient<ChatMessage>
    {
        public string Channel { get; set; }
        public string Sender { get; set; }

        public ICommand SendCommand { get; }
        public string MessageToSend { get => messageToSend; set => SetProperty(ref messageToSend, value); }

        public ObservableCollection<MessageWrapper> Messages { get; } = new ObservableCollection<MessageWrapper>();

        private IMessenger Messenger;
        private string messageToSend;

        public ChatViewModel(IWeakMessenger weakMessenger)
        {
            Messenger = weakMessenger.Instance;
            SendCommand = new RelayCommand(() =>
            {
                if (Channel is not null)
                {
                Messenger.Send(new ChatMessage(Sender, MessageToSend), Channel); 
                }
                else
                {
                    Messenger.Send(new ChatMessage(Sender, MessageToSend));

                }
                
                MessageToSend = string.Empty;
            });
        }

        public void Receive(ChatMessage message)
        {
            Messages.Add(new MessageWrapper(message, message.Sender == Sender));
        }

        public void Init()

        {
            if (Channel is not null)
            {
                Messenger.RegisterAll(this, Channel);
            }
            else
            {
                Messenger.RegisterAll(this);

            }
        }
    }

    public record MessageWrapper(ChatMessage Message, bool IsSender) { }

    public record ChatMessage(string Sender, string Message)
    {        }


    public class MainViewModel : ObservableObject, IRecipient<WelcomeTextMessage>, IRecipient<GoodbyeTextMessage>

    {
        private string welcomeText = "Hello from my ViewModel";
        private string goodbyeText = "Goodbye from my ViewModel";

        public ICommand AddChatCommand { get; }
        public ICommand DeleteChatCommand { get; }
        public ICommand BroadcastCommand { get; }
        public string ChannelName { get; set; }
        public string SenderName { get; set; }
        public string BroadcastMessage { get; set; }

        public ICommand RegisterCommand { get; }
        public ICommand UnregisterCommand { get; }
        public ICommand RegisterNonDefaultCommand { get; }
        public ICommand RegisterAnotherCommand { get; }
        public ICommand RegisterAsyncCommand { get; }

        public ICommand ChangeWelcomeCommand { get; }
        public ICommand BackgroundChangeWelcomeCommand { get; }
        public ICommand AsyncChangeWelcomeCommand { get; }

        private IMessenger StrongMessenger { get; }
        private IMessenger WeakMessenger { get; }

        public ObservableCollection<ChatViewModel> Chats { get; } = new ObservableCollection<ChatViewModel>();

        public MainViewModel(ViewModelLocator viewModelLocator, IStrongMessenger strongMessenger, IWeakMessenger weakMessenger)
        {
            StrongMessenger = strongMessenger.Instance;
            WeakMessenger = weakMessenger.Instance;

            AddChatCommand = new RelayCommand(() =>
            {
                var vm = viewModelLocator.NewChat;
                vm.Sender = SenderName;
                vm.Channel = ChannelName;
                vm.Init();

                Chats.Add(vm);
            });

            DeleteChatCommand = new RelayCommand(() =>
            {
                if (Chats.Any())
                {
                    Chats.RemoveAt(Chats.Count - 1);
                    GC.Collect();
                }
            });

            BroadcastCommand = new RelayCommand(() =>
            {
                var channels = (from c in Chats
                                select c.Channel).Distinct();
                foreach (var ch in channels)
                {
                    if (ch is not null)
                    {
                        WeakMessenger.Send(new ChatMessage("Broadcast", BroadcastMessage),ch);
                    }
                    else
                    {
                        WeakMessenger.Send(new ChatMessage("Broadcast", BroadcastMessage));

                    }

                }
            });


            RegisterCommand = new RelayCommand(() => StrongMessenger.RegisterAll(this));
            UnregisterCommand = new RelayCommand(() => StrongMessenger.Unregister<WelcomeTextMessage>(this));
            RegisterNonDefaultCommand = new RelayCommand(() => StrongMessenger.Register<MainViewModel,WelcomeTextMessage>(this, NotDefaultMessageHandler));
            RegisterAnotherCommand = new RelayCommand(() => StrongMessenger.RegisterHandler< WelcomeTextMessage>(AnotherMessageHandler));
            RegisterAsyncCommand = new RelayCommand(() => StrongMessenger.RegisterHandler<WelcomeTextMessage>(SampleAsyncHandler));

            ChangeWelcomeCommand = new RelayCommand(() => StrongMessenger.Send(new WelcomeTextMessage ( "Test" )));
            BackgroundChangeWelcomeCommand = new RelayCommand(() => Task.Run(()=>StrongMessenger.Send(new WelcomeTextMessage("Test"))).ConfigureAwait(false));
            AsyncChangeWelcomeCommand = new RelayCommand(async () =>
            {
                Debug.WriteLine("Test1");
                await StrongMessenger.SendAsync(new WelcomeTextMessage("Test"));
                Debug.WriteLine("Test2");
            });

        }

        public string WelcomeText
        {
            get => welcomeText; set => SetProperty(ref welcomeText, value);
        }

        public string GoodbyeText
        {
            get => goodbyeText; set => SetProperty(ref goodbyeText, value);
        }

        void IRecipient<WelcomeTextMessage>.Receive(WelcomeTextMessage message)
        {
            WelcomeText = message?.WelcomeText;
        }

        void IRecipient<GoodbyeTextMessage>.Receive(GoodbyeTextMessage message)
        {
            GoodbyeText = message?.GoodbyeText;
        }

        private void NotDefaultMessageHandler(object sender, WelcomeTextMessage message)
        {
            WelcomeText = "Not default: " + message.WelcomeText;
        }
        private void AnotherMessageHandler(WelcomeTextMessage message)
        {
            WelcomeText = "Another: " + message.WelcomeText;
        }

        public async Task SampleAsyncHandler(WelcomeTextMessage message)
        {
            Debug.WriteLine("Test A");
            await Task.Delay(5000);
            Debug.WriteLine("Test B");
            WelcomeText = "Delayed: " + message.WelcomeText;
            await Task.Delay(5000);
            Debug.WriteLine("Test C");
            WelcomeText = "Delayed (again) : " + message.WelcomeText;
        }
    }

    public class AsyncMessage<TMessage> : AsyncRequestMessage<object> 
        where TMessage: class
    {
        public TMessage Message { get; }
        public AsyncMessage(TMessage message)
        {
            Message = message;
        }
    }

    public static class IMessengerExtensions
    {
        public static object RegisterHandler<TMessage>(this IMessenger messenger, Func<TMessage, Task> handler) where TMessage : class
        {
            var taskHandler = (Func<TMessage,Task<object>>)(async (mess) =>
            {
                await handler(mess);
                return new object();
            });


            var recipient = new object();
            messenger.Register<AsyncMessage<TMessage>>(recipient, (rec, msg) => {
                msg.Reply(taskHandler(msg.Message));
                //msg.Reply((Task<object>)(async ()=>
                //{
                //    await handler(msg.Message);
                //    return new object();
                //}));
            //    handler(msg); 
            });
            return recipient;
        }

        public static  async Task<TMessage> SendAsync<TMessage>(this IMessenger messenger, TMessage message) where TMessage : class
        {
            await messenger.Send(new AsyncMessage<TMessage>(message));
            return message;
        }

        public static object RegisterHandler<TMessage>(this IMessenger messenger, Action<TMessage> handler) where TMessage : class
        {
            var recipient = new object();
            messenger.Register<TMessage>(recipient, (rec, msg) => handler(msg));
            return recipient;
        }
    }
}
