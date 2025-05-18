using EasySave.Maui.ViewModels;

namespace EasySave.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(
                MauiProgram.CreateMauiApp().Services.GetRequiredService<MainPage>()
            );
        }
    }
}
