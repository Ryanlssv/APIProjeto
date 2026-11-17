using System.Text.Json.Nodes;

namespace APIProjeto
{
    public partial class MainPage : ContentPage
    {
        private static readonly HttpClient ClienteHttp = new();
        private const string OpenWeatherMapUrl = "https://api.openweathermap.org/data/2.5/weather?q={0}&appid=f315cceccdb28080c8a21764b014876e&units=metric&lang=pt_br";
        private const string UnsplashUrl = "https://api.unsplash.com/search/photos?query={0}&per_page=1&client_id=n20Yinxil6wasi5-XvJDa5weHCEuhQwpcOOjIitpE1o";
        private const string IbgeEstadosUrl = "https://servicodados.ibge.gov.br/api/v1/localidades/estados?orderBy=nome";
        private const string IbgeCidadesUrl = "https://servicodados.ibge.gov.br/api/v1/localidades/estados/{0}/municipios?orderBy=nome";
        private bool estadosCarregados;
        private List<Estado> estados = new();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!estadosCarregados)
                await CarregarEstadosAsync();
        }

        private async Task CarregarEstadosAsync()
        {
            try
            {
                var resposta = await ClienteHttp.GetStringAsync(IbgeEstadosUrl);
                var dados = JsonNode.Parse(resposta)?.AsArray() ?? new JsonArray();
                estados = dados
                    .Select(item => new Estado(
                        item?["id"]?.GetValue<int>() ?? 0,
                        item?["sigla"]?.GetValue<string>() ?? "",
                        item?["nome"]?.GetValue<string>() ?? ""))
                    .Where(estado => estado.Id > 0)
                    .ToList();

                PickerEstado.ItemsSource = estados.Select(estado => $"{estado.Sigla} - {estado.Nome}").ToList();
                estadosCarregados = true;
                var indiceSaoPaulo = estados.FindIndex(estado => estado.Sigla == "SP");
                if (indiceSaoPaulo >= 0)
                    PickerEstado.SelectedIndex = indiceSaoPaulo;
            }
            catch (Exception ex)
            {
                MostrarErro($"Não foi possível carregar os estados: {ex.Message}");
            }
        }

        private async void OnEstadoSelecionado(object? sender, EventArgs e)
        {
            if (PickerEstado.SelectedIndex < 0 || PickerEstado.SelectedIndex >= estados.Count)
                return;

            PickerCidade.IsEnabled = false;
            PickerCidade.ItemsSource = null;
            PickerCidade.SelectedIndex = -1;
            try
            {
                var estado = estados[PickerEstado.SelectedIndex];
                var resposta = await ClienteHttp.GetStringAsync(string.Format(IbgeCidadesUrl, estado.Id));
                var dados = JsonNode.Parse(resposta)?.AsArray() ?? new JsonArray();
                var cidades = dados
                    .Select(item => item?["nome"]?.GetValue<string>())
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Cast<string>()
                    .ToList();

                PickerCidade.ItemsSource = cidades;
                PickerCidade.IsEnabled = cidades.Count > 0;
                var indiceSaoPaulo = cidades.FindIndex(cidade => cidade.Equals("São Paulo", StringComparison.OrdinalIgnoreCase));
                if (estado.Sigla == "SP" && indiceSaoPaulo >= 0)
                    PickerCidade.SelectedIndex = indiceSaoPaulo;
            }
            catch (Exception ex)
            {
                MostrarErro($"Não foi possível carregar as cidades: {ex.Message}");
            }
        }

        private async void OnBuscarClimaClicked(object? sender, EventArgs e)
        {
            var cidade = PickerCidade.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(cidade))
            {
                MostrarErro("Selecione primeiro um estado e uma cidade.");
                return;
            }

            Indicador.IsVisible = true;
            Indicador.IsRunning = true;
            LblErro.IsVisible = false;

            try
            {
                var cidadeCodificada = Uri.EscapeDataString(cidade);
                var climaResposta = await ClienteHttp.GetStringAsync(string.Format(OpenWeatherMapUrl, cidadeCodificada));
                var clima = JsonNode.Parse(climaResposta);
                var nomeCidade = clima?["name"]?.GetValue<string>() ?? cidade;
                var temperatura = clima?["main"]?["temp"]?.GetValue<double>() ?? 0;
                var umidade = clima?["main"]?["humidity"]?.GetValue<int>() ?? 0;
                var descricao = clima?["weather"]?[0]?["description"]?.GetValue<string>() ?? "Sem descrição";

                App.UltimaCidade = nomeCidade;
                LblCidade.Text = nomeCidade;
                LblTemperatura.Text = $"{temperatura:0}°C";
                LblUmidade.Text = $"Umidade: {umidade}%";
                LblDescricao.Text = descricao;
                await AtualizarImagemFundoAsync(cidadeCodificada);
            }
            catch (Exception ex)
            {
                MostrarErro($"Não foi possível carregar o clima: {ex.Message}");
            }
            finally
            {
                Indicador.IsRunning = false;
                Indicador.IsVisible = false;
            }
        }

        private async Task AtualizarImagemFundoAsync(string cidadeCodificada)
        {
            try
            {
                var resposta = await ClienteHttp.GetStringAsync(string.Format(UnsplashUrl, cidadeCodificada));
                var imagem = JsonNode.Parse(resposta)?["results"]?[0]?["urls"]?["regular"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(imagem))
                    ImgFundo.Source = ImageSource.FromUri(new Uri(imagem));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar imagem: {ex.Message}");
            }
        }

        private void MostrarErro(string mensagem)
        {
            LblErro.Text = mensagem;
            LblErro.IsVisible = true;
        }

        private sealed record Estado(int Id, string Sigla, string Nome);
    }
}
