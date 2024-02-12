using Elva.Core;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.CControl.Home;
using Elva.MVVM.ViewModel.CControl.Info;
using Elva.MVVM.ViewModel.CControl.Search;
using Elva.MVVM.ViewModel.CControl.Settings;
using Elva.MVVM.ViewModel.Window;
using Microsoft.Extensions.DependencyInjection;
using Requests;
using System;
using System.Windows;

namespace Elva
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        public ServiceProvider ServiceProvider => _serviceProvider;
        internal static new App Current => (App)Application.Current;

        public App()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(provider => new MainWindow { DataContext = provider.GetRequiredService<MainWindowVM>() });
            services.AddSingleton(provider => IOManager.LoadWebsites());
            services.AddSingleton<ComicDatabaseManager>();
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<SearchManager>();

            AddViewModels(services);

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Func<Type, ViewModelObject>>(provider => viewModelType => (ViewModelObject)provider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();

        }

        private static void AddViewModels(IServiceCollection services)
        {
            services.AddSingleton<MainWindowVM>();
            services.AddSingleton<HomeVM>();
            services.AddSingleton<SearchVM>();
            services.AddSingleton<InfoVM>();
            services.AddSingleton<SettingsVM>();
            services.AddSingleton<SearchBarVM>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _serviceProvider.GetRequiredService<ComicDatabaseManager>().LoadData();
            _serviceProvider.GetRequiredService<SettingsManager>().LoadSettings();
            ConnectionManager.Initialize();
            ConnectionManager.ConnectionChanged += (o, s) => { if (ConnectionManager.ConnectionIsSave) RequestHandler.CreateMainCTS(); else RequestHandler.CancelMainCTS(); };

            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<HomeVM>();
            _serviceProvider.GetRequiredService<MainWindow>().Show();
        }

    }
}
