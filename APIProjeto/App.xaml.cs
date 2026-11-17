namespace APIProjeto
{
    public partial class App : Application
    {
        public static string UltimaCidade { get; set; } = "São Paulo";

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
