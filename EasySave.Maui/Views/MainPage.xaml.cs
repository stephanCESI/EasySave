using EasySave.Maui.Localizations;
using EasySave.Maui.Models;
using EasySave.Maui.ViewModels;

namespace EasySave.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BindingContext is MainViewModel viewModel)
            {
                viewModel.SelectedJobs.Clear();
                foreach (var item in e.CurrentSelection)
                {
                    if (item is BackupJob job)
                    {
                        viewModel.SelectedJobs.Add(job);
                    }
                }
            }
        }

        private void OnLanguageToggled(object sender, ToggledEventArgs e)
        {
            string language = e.Value ? "Français" : "Anglais";
            LanguageLabel.Text = $"Langue actuelle : {language}";

            // Appel à ton service pour changer la langue
            var localizationService = new LocalizationService();
            localizationService.SetLanguage(e.Value ? "fr" : "en");
        }


    }


}
