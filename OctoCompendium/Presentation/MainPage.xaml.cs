namespace OctoCompendium.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (DataContext is CollectionViewModel vm)
        {
            await vm.LoadCollectionAsync();
        }
    }
}
