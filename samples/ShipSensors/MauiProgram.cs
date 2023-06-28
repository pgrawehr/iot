using Microsoft.Extensions.Logging;

namespace ShipSensors
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            
            builder.Logging.AddDebug();

            return builder.Build();
        }

#if !WINDOWS
        public static int Main(string[] args)
        {
            return 0;
        }
#endif
    }
}
