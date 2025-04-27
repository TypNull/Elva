using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Home.ViewModels;
using Elva.Pages.Info.ViewModels;
using Elva.Pages.Search.ViewModels;
using Elva.Pages.Settings.ViewModels;
using Elva.Pages.Shared.Models;
using Elva.Pages.WebsiteMenu.ViewModels;
using Elva.Pages.Windows.View;
using Elva.Pages.Windows.ViewModel;
using Elva.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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

        private readonly ILogger<App> _logger;

        public event EventHandler<string>? OnThemeChanged;
        private MainWindow? _mainWindow;

        public App(ILoggerFactory loggerF)
        {
            _logger = loggerF.CreateLogger<App>();
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(options => options.AddSimpleConsole(c => { c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; c.UseUtcTimestamp = true; }).AddDebug());
            services.AddSingleton(provider => new MainWindow { DataContext = provider.GetRequiredService<MainWindowVM>() });
            services.AddSingleton(_ => new WebsiteManager(IOManager.LoadWebsites()));
            services.AddSingleton<ComicDatabaseManager>();
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<SearchManager>();
            services.AddSingleton<FavoriteManager>();

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
            services.AddSingleton<WebsiteMenuVM>();
            services.AddSingleton<SettingsVM>();
            services.AddSingleton<LicenseInfoVM>();
            services.AddSingleton<SearchBarVM>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log<App>("Start Application", time: true);
            base.OnStartup(e);
            _ = _serviceProvider.GetRequiredService<ComicDatabaseManager>().LoadDataAsync();

            SettingsManager settingsManager = _serviceProvider.GetRequiredService<SettingsManager>();
            settingsManager.LoadSettings();
            SetupConnectionManager(settingsManager);
            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<HomeVM>();
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            _mainWindow.Show();
            ApplyTheme(settingsManager.Theme);

            Log<App>("Show MainWindow", time: true);
        }

        private void SetupConnectionManager(SettingsManager settingsManager)
        {
            ConnectionManager.ConnectionChanged += (o, s) =>
            {
                if ((!ConnectionManager.ConnectionIsSave && settingsManager.IsKillSwitchEnabled) || !ConnectionManager.ConnectionIsAvailable)
                {
                    RequestHandler.CancelMainCTS();
                    Dispatcher.Invoke(() =>
                    {
                        ToastNotification.Show("Downloads paused: Connection not secure", ToastType.Warning);
                    });
                }
                else if (ConnectionManager.ConnectionIsAvailable)
                {
                    RequestHandler.CreateMainCTS();
                    if (ConnectionManager.ConnectionIsSave)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ToastNotification.Show("Connection secure: Downloads enabled", ToastType.Success);
                        });
                    }
                }
            };

            Task.Run(ConnectionManager.Initialize);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            OwnRequest? request = _serviceProvider.GetRequiredService<SettingsVM>().UpdateRequest;
            _serviceProvider.GetRequiredService<SettingsManager>().SaveSettingsDirect();
            request?.Task.Wait(600000);
        }

        /// <summary>
        /// Applies the specified theme to the application
        /// </summary>
        public void ApplyTheme(string theme)
        {
            if (theme == "System")
            {
                bool systemUsesDarkTheme = IsSystemUseDarkTheme();
                theme = systemUsesDarkTheme ? "Dark" : "Light";
            }

            string packUri = $"pack://application:,,,/Resources/Styles/Theme{theme}.xaml";
            ResourceDictionary themeDictionary = new() { Source = new Uri(packUri, UriKind.Absolute) };

            Dispatcher.Invoke(() =>
            {
                for (int i = Resources.MergedDictionaries.Count - 1; i >= 0; i--)
                {
                    ResourceDictionary dict = Resources.MergedDictionaries[i];
                    if (dict.Source?.OriginalString.Contains("Theme") == true)
                        Resources.MergedDictionaries.RemoveAt(i);
                }

                if (themeDictionary != null)
                    Resources.MergedDictionaries.Insert(0, themeDictionary);
            });
            OnThemeChanged?.Invoke(this, theme);
        }

        /// <summary>
        /// Checks if the system is using dark theme
        /// </summary>
        private static bool IsSystemUseDarkTheme()
        {
            try
            {
                using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                if (key != null)
                {
                    object? value = key.GetValue("AppsUseLightTheme");
                    if (value != null && value is int lightThemeEnabled)
                        return lightThemeEnabled == 0;
                }
            }
            catch { }
            return true;
        }

        public static void Log(string information, LogLevel logLevel = LogLevel.Information) => Current._logger.Log(logLevel, information);
        public static void Log<T>(string information, LogLevel logLevel = LogLevel.Information, bool time = false, [CallerMemberName] string callerName = "", [CallerLineNumber] long callerLineNumber = 0) =>
            Current._logger.Log(logLevel, $"Time: {(time ? DateTime.Now.TimeOfDay : "")} Class: {typeof(T).FullName} Method:{callerName} Line: {callerLineNumber} »»  {information} ");

        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.0.0")]
        public static void Main()
        {
            IHost host = Host.CreateDefaultBuilder().ConfigureServices(services => services.AddSingleton<App>()).Build();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                ILoggerFactory? loggerFactory = host.Services.GetService<ILoggerFactory>();
                if (loggerFactory == null)
                    return;
                ILogger<App> logger = loggerFactory.CreateLogger<App>();
                logger.LogError(args.ExceptionObject as Exception, "Application exited because of an unhandled exception");
            };
            App? app = host.Services.GetService<App>();
            app?.InitializeComponent();
            app?.Run();
        }
    }
}