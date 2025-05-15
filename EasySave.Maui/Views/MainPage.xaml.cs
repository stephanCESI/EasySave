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

        private void ClickAddJobButton(object sender, EventArgs e)
        {
            MainContent.Opacity = 0.5;
            MainContent.BackgroundColor = Colors.Gray;
            PopUpAddJob.IsVisible = true;
        }

        private void ClickCancelButton(object sender, EventArgs e)
        {
            MainContent.Opacity = 1;
            MainContent.BackgroundColor = Colors.White;
            PopUpAddJob.IsVisible = false;
            PopUpCreateSelection.IsVisible = false;
            PopUpParameters.IsVisible = false;

        }

        private void ClickAddButton(object sender, EventArgs e)
        {

        }

        private void ClickParametersButton(object sender, EventArgs e)
        {
            MainContent.Opacity = 0.5;
            MainContent.BackgroundColor = Colors.Gray;
            PopUpParameters.IsVisible = true;
        }

        private void ClickConfirmParametersButton(object send, EventArgs e) { 
        

        }

        private void ClickLaunchButton(object sender, EventArgs e)
        {
            
        }

        private void ClickPopUpCreateSelection(object sender, EventArgs e)
        {

            MainContent.Opacity = 0.5;
            MainContent.BackgroundColor = Colors.Gray;
            PopUpCreateSelection.IsVisible = true;
        }
    }

}
