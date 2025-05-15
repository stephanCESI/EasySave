namespace EasySave.Maui
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

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
        }

        private void ClickAddButton(object sender, EventArgs e)
        {

        }
    }

}
