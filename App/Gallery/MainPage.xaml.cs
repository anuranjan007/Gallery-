namespace Gallery
{
    public partial class MainPage : ContentPage
    {
        private bool isRegistration;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnSwitchAuthClicked(object? sender, EventArgs e)
        {
            isRegistration = !isRegistration;
            AuthTitle.Text = isRegistration ? "Start your cloud." : "Your files, everywhere.";
            AuthSubtitle.Text = isRegistration ? "Create a secure home for all your files." : "Sign in to your private cloud gallery.";
            FormTitle.Text = isRegistration ? "Create an account" : "Welcome back";
            AuthButton.Text = isRegistration ? "Create account" : "Sign in";
            SwitchButton.Text = isRegistration ? "I already have an account" : "Create a new account";
            NameLabel.IsVisible = isRegistration;
            NameBorder.IsVisible = isRegistration;
        }

        private void OnAuthClicked(object? sender, EventArgs e)
        {
            OtpSubtitle.Text = $"We sent a 6-digit code to {EmailEntry.Text ?? "your email"}.";
            AuthScroll.IsVisible = false;
            OtpScroll.IsVisible = true;
        }

        private void OnVerifyClicked(object? sender, EventArgs e)
        {
            OtpScroll.IsVisible = false;
            Dashboard.IsVisible = true;
        }

        private void OnGridViewClicked(object? sender, EventArgs e)
        {
            GridFiles.IsVisible = true;
            ListFiles.IsVisible = false;
            GridViewButton.BackgroundColor = Color.FromArgb("#E9E7FF");
            ListViewButton.BackgroundColor = Colors.White;
        }

        private void OnListViewClicked(object? sender, EventArgs e)
        {
            GridFiles.IsVisible = false;
            ListFiles.IsVisible = true;
            GridViewButton.BackgroundColor = Colors.White;
            ListViewButton.BackgroundColor = Color.FromArgb("#E9E7FF");
        }

        private async void OnUploadClicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Upload", "Connect your file picker here to add images, videos, and files.", "OK");
        }
    }
}
