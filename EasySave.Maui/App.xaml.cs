using EasySave.Maui.ViewModels;

namespace EasySave.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Récupère l'instance de MainPage via le conteneur DI
            MainPage = new NavigationPage(
                MauiProgram.CreateMauiApp().Services.GetRequiredService<MainPage>()
            );
        }
    }
}
