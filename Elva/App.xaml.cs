using Elva.Core;
using Elva.MVVM.Model;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.CControl.Home;
using Elva.MVVM.ViewModel.CControl.Info;
using Elva.MVVM.ViewModel.CControl.Search;
using Elva.MVVM.ViewModel.CControl.Settings;
using Elva.MVVM.ViewModel.CControl.WebsiteMenu;
using Elva.MVVM.ViewModel.Window;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        private readonly ILogger<App> _logger;

        public App(ILoggerFactory loggerF)
        {
            _logger = loggerF.CreateLogger<App>();
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(options => options.AddSimpleConsole(c => { c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; c.UseUtcTimestamp = true; }).AddDebug());
            services.AddSingleton(provider => new MainWindow { DataContext = provider.GetRequiredService<MainWindowVM>() });
            services.AddSingleton(provider => new WebsiteManager(IOManager.LoadWebsites()));
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
            Task database = _serviceProvider.GetRequiredService<ComicDatabaseManager>().LoadDataAsync();
            SettingsManager settingsManager = _serviceProvider.GetRequiredService<SettingsManager>();
            settingsManager.LoadSettings();

            // Apply the saved theme
            ApplyTheme(settingsManager.Theme);

            ConnectionManager.ConnectionChanged += (o, s) =>
            {
                if ((!ConnectionManager.ConnectionIsSave && settingsManager.IsKillSwitchEnabled) || !ConnectionManager.ConnectionIsAvailable)
                {
                    RequestHandler.CancelMainCTS();
                    ToastNotification.Show("Downloads paused: Connection not secure", ToastType.Warning);
                }
                else if (ConnectionManager.ConnectionIsAvailable)
                {
                    RequestHandler.CreateMainCTS();
                    if (ConnectionManager.ConnectionIsSave)
                    {
                        ToastNotification.Show("Connection secure: Downloads enabled", ToastType.Success);
                    }
                }
            };
            ConnectionManager.Initialize();

            database.Wait();
            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<HomeVM>();
            _serviceProvider.GetRequiredService<MainWindow>().Show();
            Log<App>("Show MainWindow", time: true);
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
            ResourceDictionary themeDictionary = null;

            // Determine which theme to use
            if (theme == "System")
            {
                // Check if system is using dark mode
                bool systemUsesDarkTheme = IsSystemUseDarkTheme();
                theme = systemUsesDarkTheme ? "Dark" : "Light";
            }

            // Load the appropriate theme dictionary
            string packUri = $"pack://application:,,,/Style/Theme{theme}.xaml";
            themeDictionary = new ResourceDictionary() { Source = new Uri(packUri, UriKind.Absolute) };

            // Remove any existing theme dictionaries
            for (int i = Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                ResourceDictionary dict = Resources.MergedDictionaries[i];
                if (dict.Source != null && dict.Source.OriginalString.Contains("Theme"))
                {
                    Resources.MergedDictionaries.RemoveAt(i);
                }
            }

            // Add the new theme dictionary
            if (themeDictionary != null)
            {
                Resources.MergedDictionaries.Insert(0, themeDictionary);
            }
        }

        /// <summary>
        /// Checks if the system is using dark theme
        /// </summary>
        private bool IsSystemUseDarkTheme()
        {
            try
            {
                // Check Windows registry for dark mode setting
                using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                if (key != null)
                {
                    object value = key.GetValue("AppsUseLightTheme");
                    if (value != null && value is int lightThemeEnabled)
                    {
                        return lightThemeEnabled == 0; // 0 means dark theme is enabled
                    }
                }
            }
            catch
            {
                // If we can't read the registry, default to dark theme
            }

            return true; // Default to dark theme if we can't determine
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