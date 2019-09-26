/*
 * Falta ver porque no se puede setear en el picker de tipo de local
 * 
 */

using Newtonsoft.Json;
using Relevamiento.Clases;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace Relevamiento.Vistas
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Comercio : ContentPage
    {
        private bool hasConnectivity;
        //public Distribuidora distribuidorseleccionado;
        public _COMERCIO ComercioSeleccionado;
        public ERP_LOCALIDADES LocalidadSeleccionada;
        public _TIP_COM TipoSeleccionado;
        public List<ERP_LOCALIDADES> ListaLocalidades = new List<ERP_LOCALIDADES>();
        public Comercio(ERP_EMPRESAS Distribuidor)
        {
            InitializeComponent();
            List<_TIP_COM> lista_locales = new List<_TIP_COM>();
            lista_locales = TraerLocales();
            ListaLocalidades = TraerLocalidades();
            pickerTipoLocal.ItemsSource = lista_locales.ToList();
            PickerProvincia.SelectedItem = Distribuidor.Z_FK_ERP_PROVINCIAS;
            App.distribuidorseleccionado = Distribuidor;
        }
            public bool ValidarDatos()
        {
            bool validar = true;
            if (string.IsNullOrEmpty(entryCalleLocal.Text))
            {
                LabelCalleLocal.IsVisible = true;
                validar = false;
            }
            if (string.IsNullOrEmpty(EntryLocalidad.Text))
            {
                LabelLocalidad.IsVisible = true;
                validar = false;
            }
            if (string.IsNullOrEmpty(entryNombreLocal.Text))
            {
                LabelNombreLocal.IsVisible = true;
                validar = false;
            }
            if (string.IsNullOrEmpty(entryNumeroLocal.Text))
            {
                LabelNumero.IsVisible = true;
                validar = false;
            }
            if (pickerTipoLocal.SelectedIndex == -1)
            {
                LabelTipoLocal.IsVisible = true;
              validar = false;
            }
            return validar;
        }

        public List<ERP_LOCALIDADES> TraerLocalidades()
        {
            List<ERP_LOCALIDADES> ListaLocalidades = new List<ERP_LOCALIDADES>();
            ERP_LOCALIDADES l1 = new ERP_LOCALIDADES()
            {

                ID = 3058,
                DESCRIPCION = "CIUDAD AUTONOMA BUENOS AIRES",
                FK_ERP_PARTIDOS = 7,
                Z_FK_ERP_PARTIDOS = "CAPITAL FEDERAL",
                FK_ERP_PROVINCIAS = 0,
                Z_FK_ERP_PROVINCIAS = "Capital Federal"
            };
            ListaLocalidades.Add(l1);
            l1 = new ERP_LOCALIDADES()
            {

                ID = 3065,
                DESCRIPCION = "CIUDADELA",
                FK_ERP_PARTIDOS = 140,
                Z_FK_ERP_PARTIDOS = "TRES DE FEBRERO",
                FK_ERP_PROVINCIAS = 1,
                Z_FK_ERP_PROVINCIAS = "Buenos Aires"
            };
            ListaLocalidades.Add(l1);
            return ListaLocalidades;
        }

        public List<_TIP_COM> TraerLocales()
        {
            List<_TIP_COM> ListaComercios = new List<_TIP_COM>();

            _TIP_COM d3 = new _TIP_COM()
            {
                ID = 1,
                DESCRIPCION = "Almacen"
            };
            ListaComercios.Add(d3);

            d3 = new _TIP_COM()
            {
                ID = 2,
                DESCRIPCION = "Chino"
            };
            ListaComercios.Add(d3);
            d3 = new _TIP_COM()
            {
                ID = 3,
                DESCRIPCION = "Despensa",
            };
            ListaComercios.Add(d3);
            d3 = new _TIP_COM()
            {
                ID = 4,
                DESCRIPCION = "Kiosco",
            };
            ListaComercios.Add(d3);

            return ListaComercios;
        }

        private async void btnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void btnFinalizarClicked(object sender, EventArgs e)
        {
            App.releva.FK_ERP_EMPRESAS = App.distribuidorseleccionado.ID.ToString();
            App.releva.FK_ERP_ASESORES = App.distribuidorseleccionado.FK_ERP_ASESORES;
            App.releva.FECHA = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            App.releva.CODIGO = "ASD123ADSASD";
            ItrisPlanillaEntity relevamientos = new ItrisPlanillaEntity();
            relevamientos.relevamiento = App.releva;
            relevamientos.comercios = App.comercios;

			//Obtengo el imei del equipo para el request
			string phoneImei = Task.Run(async () => await this.GetImei()).GetAwaiter().GetResult().ToString();
			//Seteo el IMEI y el maximo identificador de la tabla tbRequest local SQLite
			relevamientos.codigoRequest = string.Format("{0}-{1}", phoneImei, GetMaxIdTbRequest());
            var post = relevamientos;

            string jsonRelevamiento = JsonConvert.SerializeObject(relevamientos);

            //String content que serealiza la clase a string
            StringContent stringContent =
				new StringContent(jsonRelevamiento, Encoding.UTF8, "application/json");

            TbRequest tbRequests = new TbRequest()
            {
                req_codigo = phoneImei,
                req_json = jsonRelevamiento,
                req_estado = false
            };

            //INSERT
            using (SQLite.SQLiteConnection conexion = new SQLiteConnection(App.RutaBD))
            {
                int result = conexion.Insert(tbRequests);
                EditorResponseDebug.Text = "INSERT: "+ result.ToString();
            }

            if (hasConnectivity)
            {
                await SendPostRelevamiento(jsonRelevamiento, tbRequests);
            }
        }

        private async Task SendPostRelevamiento(string jsonRelevamiento, TbRequest tbRequestToUpdate)
        {
            StringContent stringContent =
               new StringContent(jsonRelevamiento, Encoding.UTF8, "application/json");

            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30);

            //URL para hacer el post
            string urlPost = "http://iserver.itris.com.ar:7101/DACServicesTest/api/Relevamiento";

            //variable que se utiliza para tomar la respuesta
            HttpResponseMessage httpResponseMessage;

            //Se ejecuta el post y se lo asigna a la variable que contiene la respuesta
            httpResponseMessage = await httpClient.PostAsync(new Uri(urlPost), stringContent);

            //Obtengo el mensaje de respuesta del server
            var stringResponse = httpResponseMessage.Content.ReadAsStringAsync().Result;

            //Serializo la repsuesta que viene en formato json al tipo de clase
            //ACA TENES QUE TENER LA RESPUESTA DEL SERVICIO DACServiceTest
            ItrisPlanillaEntity respuesta = JsonConvert.DeserializeObject<ItrisPlanillaEntity>(stringResponse);

            //Dato a guardar en tabla tbRequest
            string requestBody = JsonConvert.SerializeObject(respuesta);

            using (SQLite.SQLiteConnection conexion = new SQLiteConnection(App.RutaBD))
            {
                tbRequestToUpdate.req_json = requestBody;
                tbRequestToUpdate.req_estado = true;
                int result = conexion.Update(tbRequestToUpdate);
               
                EditorResponseDebug.Text = result.ToString();
            }
        }

        private void CheckPendingsTbRquests()
        {
            List<TbRequest> lstTbRequest = new List<TbRequest>();
            using (SQLite.SQLiteConnection conexion = new SQLiteConnection(App.RutaBD))
            {
                lstTbRequest = conexion.Query<TbRequest>("SELECT * FROM TbRequest WHERE req_estado = false").ToList();
            }

            EditorResponseDebug.Text = lstTbRequest.Count.ToString();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            hasConnectivity = false;

            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                hasConnectivity = true;
                CheckPendingsTbRquests();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        private async void btnSiguienteClicked(object sender, EventArgs e)
        {
            if (ValidarDatos())
            {

                _COMERCIO nuevoLocal = new _COMERCIO()
                {
                    FK_ERP_PROVINCIAS = 1,
                    FK_TIP_COM = TipoSeleccionado.ID,
                    NOMBRE = entryNombreLocal.Text,
                    CALLE = entryCalleLocal.Text,
                    NUMERO = entryNumeroLocal.Text,
                    FK_ERP_LOCALIDADES = EntryLocalidad.Text,
                };
                await Navigation.PushAsync(new Tabbed(nuevoLocal));
            }
        }

        private void PickerTipoLocal_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;

            var selectedTipo = picker.ItemsSource[picker.SelectedIndex] as _TIP_COM;
            TipoSeleccionado = selectedTipo;
        }

        public void LocalidadSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LocalidadSearch.Text))
            {
                List<ERP_LOCALIDADES> temp = new List<ERP_LOCALIDADES>();

                temp = ListaLocalidades.Where(c => c.DESCRIPCION.ToString().ToLower().Contains(LocalidadSearch.Text)).ToList();
                if (temp.Count != 0)
                {
                    LocalidadList.IsVisible = true;
                    LocalidadList.ItemsSource = temp;
                }

            }
            else LocalidadList.IsVisible = false;
        }

        public void LocalidadList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            LocalidadList.IsVisible = false;
            LocalidadSeleccionada = e.Item as ERP_LOCALIDADES;
            PickerProvincia.SelectedItem = LocalidadSeleccionada.Z_FK_ERP_PROVINCIAS;
        }

        private void PickerProvincia_SelectedIndexChanged(object sender, EventArgs e)
        {
            //var picker = sender as Picker;
            //var provincia = picker.SelectedItem.ToString();
            //LocalidadSeleccionada.Z_FK_ERP_PROVINCIAS = provincia;
        }

		#region Metodos para generar codigo del POST para el relevamiento

		/// <summary>
		/// Metodo de obtencion de IMEI
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetImei()
		{
			//Verifico permisos en el equipo
			var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Phone);
			if (status != PermissionStatus.Granted)
			{
				var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Phone);
				//como buena practica siempre es bueno chequear tener los permisos
				if (results.ContainsKey(Permission.Phone))
					status = results[Permission.Phone];
			}

			return DependencyService.Get<IServiceImei>().GetImei();
		}

		private string GetMaxIdTbRequest()
		{
			int maxId = 1;
			return maxId.ToString();
		}

		#endregion

	}
}
 