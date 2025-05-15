namespace EasySave.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
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
            PopUpRunSelection.IsVisible = false;

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

        private void ClickCancelParametersButton(object sender, EventArgs e)
        {
            MainContent.Opacity = 1;
            MainContent.BackgroundColor = Colors.White;
            PopUpParameters.IsVisible = false;
        }

        private void ClickConfirmParametersButton(object send, EventArgs e) { 
        

        }

        private void ClickLaunchButton(object sender, EventArgs e)
        {
            
        }

        private void ClickLaunchSelectionButton(object sender, EventArgs e)
        {
            MainContent.Opacity = 0.5;
            MainContent.BackgroundColor = Colors.Gray;
            PopUpRunSelection.IsVisible = true;
        }
    }

}
