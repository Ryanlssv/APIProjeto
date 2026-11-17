using System.Text.Json.Nodes;

namespace APIProjeto
{
    public partial class FotosPage : ContentPage
    {
        private static readonly HttpClient ClienteHttp = new();
        private const string UnsplashUrl = "https://api.unsplash.com/search/photos?query={0}&per_page=10&client_id=n20Yinxil6wasi5-XvJDa5weHCEuhQwpcOOjIitpE1o";

        public FotosPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarFotosAsync();
        }

        private async Task CarregarFotosAsync()
        {
            var cidade = string.IsNullOrWhiteSpace(App.UltimaCidade) ? "São Paulo" : App.UltimaCidade;
            TituloFotos.Text = $"Fotos de: {cidade}";
            ListaFotos.ItemsSource = null;

            try
            {
                var url = string.Format(UnsplashUrl, Uri.EscapeDataString(cidade));
                var resposta = await ClienteHttp.GetStringAsync(url);
                var resultados = JsonNode.Parse(resposta)?["results"]?.AsArray();
                var fotos = resultados?
                    .Select(item => item?["urls"]?["regular"]?.GetValue<string>())
                    .Where(urlImagem => !string.IsNullOrWhiteSpace(urlImagem))
                    .Cast<string>()
                    .ToList() ?? new List<string>();

                ListaFotos.ItemsSource = fotos;
            }
            catch (Exception ex)
            {
                TituloFotos.Text = $"Fotos de: {cidade} (erro ao carregar)";
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar fotos: {ex.Message}");
            }
        }
    }
}
