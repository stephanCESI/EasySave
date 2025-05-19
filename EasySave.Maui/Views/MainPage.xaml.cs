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
            var localizationService = new LocalizationService();
            string languageCode = e.Value ? "fr" : "en";
            localizationService.SetLanguage(languageCode);

            string languageName = localizationService.GetLocalizedString(e.Value ? "French" : "English");

            string labelText = localizationService.GetLocalizedString("LanguageLabel", languageName);
            LanguageLabel.Text = labelText;
        }



    }


}
