using Customer.ViewModel;       


namespace Customer
 
{
    public partial class MainPage : ContentPage
    {


        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;    
        }

        
    }

}
