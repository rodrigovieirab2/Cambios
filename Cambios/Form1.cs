using Cambios.Modelos;
using Cambios.Servicos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cambios
{
    public partial class Form1 : Form
    {
        #region Atributos

        private List<Rate> Rates;

        private NetworkService networkService;

        private ApiService apiService;

        private DialogService dialogService;

        private DataService dataService;

        #endregion

        public Form1()
        {
            InitializeComponent();
            networkService = new NetworkService();
            apiService = new ApiService();
            dialogService = new DialogService();
            dataService = new DataService();
            LoadRates();
        }

        private async void LoadRates()
        {
            bool load;
            
            lblStatus.Text = "A atualizar taxas...";

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
                LoadLocalRates();
                load = false;
            }
            else
            {
                await LoadApiRates();
                load = true;
            }

            if(Rates.Count == 0)
            {
                lblResultado.Text = "Não há ligação à Internet" + Environment.NewLine +
                    "e não foram préviamente carregadas as taxas." + Environment.NewLine +
                    "Tente mais tarde!";
                lblStatus.Text = "Primeira inicialização deverá ter ligação à Internet";

                return;
            }

            cBoxOrigem.DataSource = Rates;
            cBoxOrigem.DisplayMember = "Name";

            //Corrige bug da microsoft
            cBoxDestino.BindingContext = new BindingContext();

            cBoxDestino.DataSource = Rates;
            cBoxDestino.DisplayMember = "Name";

            btnConverter.Enabled = true;
            btnTroca.Enabled = true;

            lblStatus.Text = "Taxas atualizadas...";

            if (load)
            {
                lblStatus.Text = string.Format("Taxas carregadas da Internet em {0:F}", DateTime.Now);
            }
            else
            {
                lblStatus.Text = string.Format("Taxas carregadas da Base de Dados.");
            }

            progressBar1.Value = 100;
        }

        private void LoadLocalRates()
        {
            Rates = dataService.GetData();
        }

        private async Task LoadApiRates()
        {
            progressBar1.Value = 0;

            var response = await apiService.GetRates("http://cambios.somee.com", "/api/rates");

            Rates = (List<Rate>) response.Result;

            dataService.DeleteData();

            dataService.SaveData(Rates);
        }

        private void btnConverter_Click(object sender, EventArgs e)
        {
            Converter();
        }

        private void Converter()
        {
            if (string.IsNullOrEmpty(txtValor.Text))
            {
                dialogService.ShowMessage("Erro", "Insira um valor a converter");
                return;
            }

            decimal valor;
            if(!decimal.TryParse(txtValor.Text, out valor))
            {
                dialogService.ShowMessage("Erro de conversão", "Valor terá que ser numérico");
            }

            if(cBoxOrigem.SelectedItem == null)
            {
                dialogService.ShowMessage("Erro", "Tem que escolher uma moeda a converter");
                return;
            }

            if (cBoxDestino.SelectedItem == null)
            {
                dialogService.ShowMessage("Erro", "Tem que escolher uma moeda de destino para converter");
                return;
            }

            var taxaOrigem = (Rate)cBoxOrigem.SelectedItem;
            var taxaDestino = (Rate)cBoxDestino.SelectedItem;
            var valorConvertido = valor / (decimal)taxaOrigem.TaxRate * (decimal)taxaDestino.TaxRate;
            lblResultado.Text = string.Format("{0} {1:C2} = {2} {3:C2}", taxaOrigem.Code, valor, taxaDestino.Code, valorConvertido);
        }

        private void btnTroca_Click(object sender, EventArgs e)
        {
            Troca();
        }

        private void Troca()
        {
            var aux = cBoxOrigem.SelectedItem;
            cBoxOrigem.SelectedItem = cBoxDestino.SelectedItem;
            cBoxDestino.SelectedItem = aux;
            Converter();
        }
    }
}
