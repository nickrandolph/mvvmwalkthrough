using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.Devices.PointOfService;

namespace mvvmwalkthrough.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            Ioc.Default.ConfigureServices(
                   new ServiceCollection()
                   .AddSingleton<IStrongMessenger>(sp => new MessengerWrapper(new StrongReferenceMessenger()))
                   .AddSingleton<IWeakMessenger>(sp => new MessengerWrapper(new WeakReferenceMessenger()))
                   .AddSingleton<ViewModelLocator>(this)
                   .AddTransient<MainViewModel>()
                   .AddTransient<ChatViewModel>()
                   .BuildServiceProvider());
        }

        public MainViewModel Main => Ioc.Default.GetService<MainViewModel>();
        public ChatViewModel NewChat=> Ioc.Default.GetService<ChatViewModel>();
    }

    public interface IMessengerWrapper
    {
        IMessenger Instance { get; }
    }

    public interface IStrongMessenger : IMessengerWrapper { }

    public interface IWeakMessenger : IMessengerWrapper { }

    public record MessengerWrapper(IMessenger Instance) : IStrongMessenger, IWeakMessenger;

    public record WelcomeTextMessage(string WelcomeText);

    public record GoodbyeTextMessage(string GoodbyeText);

}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
