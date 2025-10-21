using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImportXml
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void aprifile_Click(object sender, EventArgs e)
        {
            var filePath = string.Empty;
            string stato = "";
            openxml.InitialDirectory = @"D:\xml_file\scaricati";
            openxml.Filter = "xml files (*.xml)|(*.xml)|All files (*.*)|*.*";
            openxml.FilterIndex = 2;
            openxml.RestoreDirectory = true;

            if (openxml.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                filePath = openxml.FileName;



                ResidentFE.import app_res = new ResidentFE.import();
                stato=app_res.xmlfile(filePath);
                if (stato == "OK")
                    MessageBox.Show("Import fattura avvenuta con successo");
                else
                    MessageBox.Show("Si è verificato un errore: " + stato);
            }

        }

        private void importanag_Click(object sender, EventArgs e)
        {
            int numcondomini = 0, numcaricati = 0, numassegnati=0;
            string login_OK = "", carica_OK ="", utente_OK="";
            ResidentFE.Amministrazione app_res = new ResidentFE.Amministrazione();
            ResidentFE.APISQL wapi = new ResidentFE.APISQL();
            IList<ResidentFE.Reobj> Condomini = wapi.Datiammnistrativi(); //FROM: Resident_Dati: zz_Condomini WHERE (import = N'0')
            numcondomini = Condomini.Count();

            login_OK = app_res.Login();
            MessageBox.Show(login_OK);
            if (login_OK== "SUCCESS")
            {
                foreach (ResidentFE.Reobj wCond in Condomini)
                {
                   carica_OK = app_res.CaricaAnagrafica(wCond); //TO: push to "https://prod-satanetfe.resident.it/admin-api/save-company" passa i dati anagrafici del condominio
                    MessageBox.Show(carica_OK);
                    if (carica_OK == "SUCCESS")
                    {  
                        numcaricati++;
                        utente_OK = app_res.AssegnaUtente(wCond.GetNotNullString("codicefiscale"), "amministrazione@resident.it"); //TO: push to "https://prod-satanetfe.resident.it//enable-or-disable-user-company" enable=true
                        if (utente_OK == "SUCCESS")
                            numassegnati++;     

                    }
                        
                }
            }
            
            MessageBox.Show("Import anagrafiche condomini avvenute con successo " + numcaricati + " su " + numcondomini + ", assegnati all'utente amministrazione "+ numassegnati);
            
        }
    }
}
