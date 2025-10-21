using System;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.ComponentModel;


namespace ResidentFE
{


    public class import
    {
        int log_lvl = 10;
        string condominiarchivi = @"\\eva\dati\CondominiArchivi\";
        public void Run()
        {
            Console.WriteLine("Run()");
            System.IO.StreamWriter logfile = new System.IO.StreamWriter(@"D:\xml_file\log_import.txt", true);
            System.IO.StreamWriter logfile_detailed = new System.IO.StreamWriter(@"D:\xml_file\log_import_detailed.txt", true);
            logfile.AutoFlush = true;
            logfile_detailed.AutoFlush = true;

            //string debug_fatture = null; //nome file fatture, se null -> percorso normale,
            //debug
            bool debug =  System.IO.Directory.Exists(@"D:\xml_file\debug\");  //ambiente di sviluppo            
            if (debug) {
                Console.WriteLine("** DEBUG MODE **");
                XmlDocument xmlin = new XmlDocument();
                System.Xml.XmlNode n;
                //xmlin.Load(@"D:\xml_file\debug\Allegri202205270953302335.xml");
                xmlin.Load(@"D:\xml_file\debug\Maggiolino202205281300116864.xml");
                var n0 = xmlin.GetElementsByTagName("CedentePrestatore");
                var n1 = n0[0].SelectSingleNode("DatiAnagrafici");
                var n3 = n1.SelectSingleNode("IdFiscaleIVA");
                n = n3.SelectSingleNode("IdPaese");
                string Paese = n.InnerText;
                Console.WriteLine(Paese);                
                System.Environment.Exit(0);
            }
            //todo
            bool todo = System.IO.Directory.Exists(@"D:\xml_file\risposteSATA.todo\");  //ambiente di sviluppo            
            if (todo) {
                logfile.WriteLine("** TODO MODE **");
            }
            //
            string percorso_pdf = "";
            string sName = "";
            string sPdf = "";
            string sPdfRendering = "";
            string sFile = @"D:\xml_file\scaricati\";

            string Cond_partiva = "";
            string inv_ret = "";
            string stato = "";
            string errsql = "";
            string nomecond = "";
            string idcond = "";
            string checkContratto = "";
            string numfat = "";
            decimal imponibile, iva, aliq;
            decimal TOTimponibile, TOTiva, TOTfci, TOTdapagare;
            int contafatture = 0;
            DateTime datafat;
            Boolean SonoResident = false;

            //      Boolean errore = false;
            XmlNode x0, x1, x2;
            GetInvoice wInvoice = new GetInvoice();
            APISQL w_apisql = new APISQL();

            try
            {
                if (!System.IO.Directory.Exists(sFile))
                    System.IO.Directory.CreateDirectory(sFile);
                w_apisql.testconnection();
                if (w_apisql.Ok == true)
                {
                    logfile.WriteLine("inizio " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    logfile_detailed.WriteLine("inizio " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    IList<Reobj> Condomini = w_apisql.GetCondomini();
                    foreach (Reobj wCond in Condomini)
                    {
                        Cond_partiva = wCond.GetNotNullString("codiceFiscale");

                        wInvoice.Cond_partiva = Cond_partiva;
                        do
                        {
                            SonoResident = false;
                            checkContratto = "";
                            stato = wInvoice.Run();
                            nomecond = wCond.GetNotNullString("condominio");
                            logfile.WriteLine("Condominio " + Cond_partiva + " - " + nomecond);
                            logfile_detailed.WriteLine("Condominio " + Cond_partiva + " - " + nomecond);
                            if (stato == "OK")
                            {
                                inv_ret = wInvoice.Invoice;

                                idcond = Convert.ToString(wCond.GetInteger("idcondominio"));

                                sName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".xml";
                                contafatture++;

                                sName = sFile + nomecond.Substring(0, 1) + nomecond.Substring(1).ToLower() + sName;
                                if (System.IO.File.Exists(sName))
                                    System.IO.File.Delete(sName);
                                System.IO.File.WriteAllText(sName, inv_ret);
                                XmlDocument doc = new XmlDocument();
                                //ivan: genera errore su alcune fatture
                                //doc.LoadXml(inv_ret);
                                doc.Load(inv_ret);

                                using (FornitoreInfo xFornitoreInfo = new FornitoreInfo(doc))
                                {
                                    var n0 = doc.GetElementsByTagName("FatturaElettronicaBody");
                                    numfat = n0[0].SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/Numero").InnerText;
                                    datafat = Convert.ToDateTime(n0[0].SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/Data").InnerText);
                                    Reobj Operatore = w_apisql.GetOperatore(xFornitoreInfo.PartitaIva, xFornitoreInfo.PartitaIva, idcond, datafat,logfile);
                                    Reobj Invoice = new();

                                    if ((Operatore != null) && (Operatore.GetInteger("idoperatori") == 3512 | Operatore.GetInteger("IdOperatori") == 225))
                                    {
                                        SonoResident = true;
                                    }

                                    if (!SonoResident)
                                    {
                                        sPdf = "";
                                        if (Operatore.Fields.Count != 0)
                                        {
                                            sPdf = Operatore.GetNotNullString("codicetributo");
                                            if (sPdf != "")
                                            {
                                                sPdf += "_";
                                            }
                                            string s = Pulisci(Operatore.GetNotNullString("deno"));
                                            if (s.Length > 30)
                                                sPdf += s.Substring(0, 30);
                                            else
                                                sPdf += s;

                                        }
                                        sPdfRendering = sPdf;
                                        percorso_pdf = condominiarchivi + (nomecond.Substring(0, 1) + nomecond.Substring(1).ToLower()).Replace(" ","") + "=" + idcond + @"\Amministrazione\Fatture in ingresso\";
                                        if (!System.IO.Directory.Exists(percorso_pdf))
                                            System.IO.Directory.CreateDirectory(percorso_pdf);
                                        Boolean almenouno = false;
                                        foreach (XmlNode xPdf in doc.GetElementsByTagName("Allegati"))
                                        {
                                            x2 = xPdf.SelectSingleNode("NomeAttachment");
                                            if (x2 != null)
                                            {

                                                if (sPdf != "")
                                                {
                                                    sPdf += "_" + Pulisci(numfat) + "_" + datafat.ToString("yyyy_MM_dd") + System.IO.Path.GetExtension(x2.InnerText);

                                                }
                                                else
                                                {
                                                    sPdf = System.IO.Path.GetFileNameWithoutExtension(x2.InnerText) + "_" + Pulisci(numfat) + "_" + datafat.ToString("yyyy_MM_dd") + System.IO.Path.GetExtension(x2.InnerText);

                                                }
                                                almenouno = true;
                                                logfile.WriteLine("Condomino:" + nomecond + " creazione pdf (allegato) " + percorso_pdf + sPdf);
                                                logfile_detailed.WriteLine("Condomino:" + nomecond + " creazione pdf (allegato) " + percorso_pdf + sPdf);
                                                System.IO.File.WriteAllBytes(percorso_pdf + sPdf, Convert.FromBase64String(xPdf.SelectSingleNode("Attachment").InnerText));
                                            }
                                        }
                                        if (!almenouno)
                                        {
                                            sPdfRendering += "_" + Pulisci(numfat) + "_" + datafat.ToString("yyyy_MM_dd") + ".pdf";
                                            logfile.WriteLine("Condomino:" + nomecond + " creazione pdf (renderizzazione) " + percorso_pdf + sPdfRendering);
                                            logfile_detailed.WriteLine("Condomino:" + nomecond + " creazione pdf  (renderizzazione) " + percorso_pdf + sPdfRendering);
                                            System.IO.File.WriteAllBytes(percorso_pdf + sPdfRendering, wInvoice.FilePdf);

                                        }

                                        Invoice["idcondominio"] = idcond;
                                        Invoice["annobilancio"] = wCond.GetInteger("IdCdmAmministrazione");
                                        Invoice["motivo"] = "";
                                        if (Operatore.Fields.Count == 0)
                                        {
                                            Invoice["idoperatore"] = "";
                                            Invoice["ragsocoperatore"] = xFornitoreInfo.RagioneSociale;
                                            Invoice["codfiscoperatore"] = xFornitoreInfo.PartitaIva;
                                            Invoice["pivaoperatore"] = xFornitoreInfo.PartitaIva;
                                            Invoice["idpianodeiconti"] = "";
                                            Invoice["created_at"] = DateTime.Now;
                                            Invoice["motivo"] = "Operatore non trovato";
                                            Invoice["allegato"] = percorso_pdf + sPdf;
                                        }
                                        else
                                        {// deno, cFisc, pIva,pianodeiconti
                                            Invoice["idoperatore"] = Operatore.GetInteger("IdOperatori");
                                            Invoice["ragsocoperatore"] = Operatore.GetNotNullString("deno");
                                            Invoice["codfiscoperatore"] = Operatore.GetNotNullString("cFisc");
                                            Invoice["pivaoperatore"] = Operatore.GetNotNullString("pIva");
                                            Invoice["idpianodeiconti"] = Operatore.GetNotNullString("pianodeiconti");
                                            Invoice["allegato"] = percorso_pdf + sPdf;
                                        }

                                        Invoice["indirizzooperatore"] = xFornitoreInfo.Indirizzo;
                                        Invoice["capoperatore"] = xFornitoreInfo.Cap;
                                        Invoice["cittaoperatore"] = xFornitoreInfo.Comune;

                                        Invoice["numerofattura"] = numfat;
                                        Invoice["datafattura"] = datafat.ToString("dd/MM/yyyy");

                                        foreach (XmlNode xnInv in doc.GetElementsByTagName("FatturaElettronicaBody"))
                                        {
                                            TOTimponibile = 0;
                                            TOTiva = 0;
                                            TOTfci = 0;

                                            x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/TipoDocumento");
                                            switch (x0.InnerText)
                                            {
                                                case "TD04":
                                                case "TD05":
                                                case "TD08":
                                                case "TD09":
                                                    Invoice["tipodocumento"] = 2;
                                                    break;

                                                default:
                                                    Invoice["tipodocumento"] = 1;
                                                    break;
                                            }

                                            x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/ImportoTotaleDocumento");
                                            if (x0 != null)
                                                Invoice["totalefattura"] = x0.InnerText.Replace(".", ",");
                                            else
                                                Invoice["totalefattura"] = 0;
                                            x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/Causale");
                                            if (x0 != null)
                                                Invoice["descrizionesfattura"] = x0.InnerText;
                                            if (xFornitoreInfo.PartitaIva.Contains("03819031208"))  //HERA
                                            {
                                                x0 = xnInv.SelectSingleNode("DatiGenerali/DatiContratto");
                                                if (x0 != null)
                                                {
                                                    x1 = x0.SelectSingleNode("IdDocumento");
                                                    if (x1 != null)
                                                    {
                                                        checkContratto = x1.InnerText;
                                                        if (w_apisql.CheckContratto(checkContratto, Convert.ToInt32(idcond)) != "OK")
                                                            Invoice["motivo"] = "Pod/PDR HERA errato";
                                                    }

                                                }
                                            }

                                            Invoice["opzritenuta"] = 1;
                                            x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/DatiRitenuta");
                                            if (x0 != null)
                                            {
                                                x1 = x0.SelectSingleNode("ImportoRitenuta");
                                                if (x1 != null)
                                                    Invoice["ritenutaacconto"] = x1.InnerText.Replace(".", ",");

                                                x1 = x0.SelectSingleNode(" AliquotaRitenuta");

                                                if (x1 != null)
                                                    switch (x1.InnerText)
                                                    {

                                                        case "4.00":
                                                            Invoice["opzritenuta"] = 2;
                                                            break;
                                                        case "20.00":
                                                            Invoice["opzritenuta"] = 3;
                                                            break;

                                                        default:
                                                            Invoice["opzritenuta"] = 1;
                                                            break;
                                                    }


                                            }

                                            if (xFornitoreInfo.RegimeFiscale == "RF19")
                                                Invoice["opzritenuta"] = 5;

                                            x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/DatiBollo");
                                            if (x0 != null)
                                            {
                                                x1 = x0.SelectSingleNode("ImportoBollo");
                                                if (x1 != null)
                                                    TOTfci = IsDecimale(x1.InnerText.Replace(".", ","));
                                            }

                                            foreach (XmlNode xRighe in xnInv.SelectSingleNode("DatiBeniServizi").SelectNodes("DettaglioLinee"))
                                            {
                                                if (Invoice["descrizionesfattura"] == null)
                                                {

                                                    x1 = xRighe.SelectSingleNode("Descrizione");
                                                    if (x1 != null)
                                                        Invoice["descrizionesfattura"] = x1.InnerText;

                                                }
                                                if (xFornitoreInfo.PartitaIva.Contains("03819031208"))
                                                {

                                                    x1 = xRighe.SelectSingleNode("AltriDatiGestionali");
                                                    if (x1 != null)
                                                    {
                                                        x2 = x1.SelectSingleNode("RiferimentoTesto");
                                                        checkContratto = x2.InnerText;
                                                        if (w_apisql.CheckContratto(checkContratto, Convert.ToInt32(idcond)) != "OK")
                                                            Invoice["motivo"] = "Pod/PDR HERA errato";
                                                        else
                                                            Invoice["motivo"] = "";
                                                    }



                                                    if (checkContratto == "" & Invoice["motivo"].ToString() == "")
                                                        Invoice["motivo"] = "Pod/PDR HERA mancante";
                                                }
                                            }

                                            foreach (XmlNode xRiepil in xnInv.SelectSingleNode("DatiBeniServizi").SelectNodes("DatiRiepilogo"))
                                            {
                                                imponibile = 0;
                                                iva = 0;

                                                aliq = 0;
                                                x1 = xRiepil.SelectSingleNode("ImponibileImporto");
                                                if (x1 != null)
                                                {
                                                    imponibile = IsDecimale(x1.InnerText);
                                                    TOTimponibile += imponibile;
                                                }

                                                x1 = xRiepil.SelectSingleNode("AliquotaIVA");
                                                if (x1 != null)
                                                {
                                                    aliq = IsDecimale(x1.InnerText);
                                                }

                                                x1 = xRiepil.SelectSingleNode("Imposta");
                                                if (x1 != null)
                                                {
                                                    iva = IsDecimale(x1.InnerText);
                                                    TOTiva += iva;

                                                }
                                                switch (aliq)
                                                {
                                                    case 4:
                                                        Invoice["imponibile4"] = imponibile;
                                                        Invoice["iva4"] = iva;
                                                        break;
                                                    case 5:
                                                        Invoice["imponibile5"] = imponibile;
                                                        Invoice["iva5"] = iva;
                                                        break;
                                                    case 10:
                                                        Invoice["imponibile10"] = imponibile;
                                                        Invoice["iva10"] = iva;
                                                        break;
                                                    case 22:
                                                        Invoice["imponibile22"] = imponibile;
                                                        Invoice["iva22"] = iva;
                                                        break;
                                                    default:
                                                        //if (xFornitoreInfo.RegimeFiscale == "RF19")
                                                        //{
                                                        Invoice["imponibile22"] = imponibile;
                                                        Invoice["iva22"] = iva;
                                                        //}
                                                        //else
                                                        //{
                                                        //    TOTfci += imponibile;
                                                        //}
                                                        break;
                                                }

                                            }

                                            if (TOTiva == 0)
                                                Invoice["esenteiva"] = true;

                                            Invoice["imponibilefattura"] = TOTimponibile;
                                            Invoice["ivafattura"] = TOTiva;
                                            Invoice["imponibilefci"] = TOTfci;
                                            Invoice["ImponibileEsenteRitAcconto"] = 0;

                                            TOTdapagare = 0;
                                            string primascad = "";
                                            x0 = xnInv.SelectSingleNode("DatiPagamento");
                                            if (x0 != null)
                                                foreach (XmlNode xRiepil in x0.SelectNodes("DettaglioPagamento"))
                                                {
                                                    if (primascad == "")
                                                    {
                                                        x1 = xRiepil.SelectSingleNode("DataScadenzaPagamento");
                                                        if (x1 != null)
                                                            primascad = reversedata(x1.InnerText);
                                                    }

                                                    x1 = xRiepil.SelectSingleNode("ImportoPagamento");
                                                    if (x1 != null)
                                                        TOTdapagare += IsDecimale(x1.InnerText);
                                                }
                                            else
                                                TOTdapagare = TOTimponibile + TOTiva;

                                            if (xFornitoreInfo.PartitaIva.Contains("03819031208"))
                                            {
                                                Invoice["pagamento"] = "46";
                                                Invoice["datapagamentofissa"] = primascad;
                                            }
                                            else
                                            {
                                                Invoice["pagamento"] = "47";
                                                Invoice["datapagamentofissa"] = "";
                                            }

                                            Invoice["totaledapagare"] = TOTdapagare;

                                            Invoice["created_at"] = DateTime.Now;
                                        }
                                        if (Invoice["motivo"].ToString() == "")
                                        {
                                            errsql = w_apisql.SetRegistrazione(Invoice);

                                            if (errsql != "OK")
                                            {
                                                Invoice["motivo"] = errsql;
                                                errsql = w_apisql.InvoiceScartate(Invoice);
                                                if (errsql != "OK")
                                                    inv_ret = "";
                                                logfile.WriteLine("Condomino:" + nomecond + " - Import fattura del " + numfat + " del " + datafat.ToString("dd/MM/yyyy") + " SCARTATA");
                                                logfile_detailed.WriteLine("Condomino:" + nomecond + " - Import fattura del " + numfat + " del " + datafat.ToString("dd/MM/yyyy") + " SCARTATA");
                                            }
                                            else
                                                logfile.WriteLine("Condomino:" + nomecond + " - Import fattura del " + numfat + " del " + datafat.ToString("dd/MM/yyyy") + " avvenuta con successo");
                                                logfile_detailed.WriteLine("Condomino:" + nomecond + " - Import fattura del " + numfat + " del " + datafat.ToString("dd/MM/yyyy") + " avvenuta con successo");

                                        }

                                        else
                                        {
                                            errsql = w_apisql.InvoiceScartate(Invoice);
                                            if (errsql != "OK")
                                                inv_ret = "";
                                        }


                                    }
                                }

                            }

                            else if (stato == "KO")
                            {
                                inv_ret = "";
                            }
                            else
                            {
                                inv_ret = "";
                                //    errore = true;
                                logfile.WriteLine("Condominio " + Cond_partiva + " - " + nomecond + " - " + stato);
                                logfile_detailed.WriteLine("Condominio " + Cond_partiva + " - " + nomecond + " - " + stato);
                            }

                        } while (inv_ret != "");

                        //if (errore)
                        //{
                        //    logfile.WriteLine("Si è Verificato un errore di comunicazione con il portale");
                        //}

                    }
                }
                logfile.WriteLine("Oggi sono state processate " + contafatture + " fatture");
                logfile_detailed.WriteLine("Oggi sono state processate " + contafatture + " fatture");

            }



            catch (Exception e)
            {               
                logfile.WriteLine(FlattenException(e) + "");
            }
            logfile.Close();
            logfile_detailed.Close();
        }

        public static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();
            try
            {

                while (exception != null)
                {

                    stringBuilder.AppendLine(exception.Message);
                    stringBuilder.AppendLine(exception.StackTrace);

                    exception = exception.InnerException;
                }               
            }
            catch (Exception e) {
                stringBuilder.AppendLine(exception.Message);
            }

            return stringBuilder.ToString();

        }

        private string Pulisci(string str)
        {
            // return System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9a-zA-Z:,]+", "");
            return System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9a-zA-Z]+", "");

        }

        private decimal IsDecimale(string s)
        {
            if (s == string.Empty)
                return 0;
            if (s.Trim() == "")
                return 0;
            try
            {
                return Convert.ToDecimal(s.Replace(".", ","));
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        private string reversedata(string data)
        {
            return data.Substring(8, 2) + "/" + data.Substring(5, 2) + "/" + data.Substring(0, 4);
        }

        public string xmlfile(string namefile)
        {
            string percorso_pdf = "";
            string sPdf = "";
            // string condominiarchivi = "C:\\";

            string errsql = "";

            string nomecond = "";
            string idcond = "";
            string checkContratto = "";
            string numfat = "";
            decimal imponibile, iva, aliq;
            decimal TOTimponibile, TOTiva, TOTfci, TOTdapagare;
            System.IO.StreamWriter logfile = new System.IO.StreamWriter(@"D:\xml_file\log_manuale.txt", true);
            logfile.AutoFlush = true;
            //

            DateTime datafat;
            Boolean SonoResident = false;
            FornitoreInfo xFornitoreInfo;
            ClienteInfo xClienteInfo;
            XmlNode x0, x1, x2;
            APISQL w_apisql = new APISQL();
            logfile.WriteLine("inizio " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            try
            {
                w_apisql.testconnection();
                if (w_apisql.Ok == true)
                {
                    XmlDocument doc = new XmlDocument();

                    doc.Load(namefile);
                    Reobj wCond = new();
                    xClienteInfo = new ClienteInfo(doc);
                    wCond = w_apisql.GetCondominio(xClienteInfo.CodiceFiscale, logfile);
                    SonoResident = false;
                    if (wCond.Fields.Count != 0)
                    {
                        nomecond = wCond.GetNotNullString("condominio");
                        idcond = Convert.ToString(wCond.GetInteger("idcondominio"));
                        xFornitoreInfo = new FornitoreInfo(doc);
                        var n0 = doc.GetElementsByTagName("FatturaElettronicaBody");
                        numfat = n0[0].SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/Numero").InnerText;
                        datafat = Convert.ToDateTime(n0[0].SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/Data").InnerText);

                        Reobj Operatore = w_apisql.GetOperatore(xFornitoreInfo.PartitaIva, xFornitoreInfo.PartitaIva, idcond, datafat, logfile);
                        Reobj Invoice = new();

                        if ((Operatore != null) && (Operatore.GetInteger("idoperatori") == 3512 | Operatore.GetInteger("IdOperatori") == 225))
                        {
                            SonoResident = true;
                        }
                        logfile.WriteLine("operatore e invoice");

                        if (!SonoResident)
                        {
                            sPdf = "";
                            if (Operatore.Fields.Count != 0)
                            {
                                sPdf = Operatore.GetNotNullString("codicetributo");
                                if (sPdf != "")
                                {
                                    sPdf += "_";
                                }
                                string s = Pulisci(Operatore.GetNotNullString("deno"));
                                if (s.Length > 30)
                                    sPdf += s.Substring(0, 30);
                                else
                                    sPdf += s;

                            }
                            percorso_pdf = condominiarchivi + (nomecond.Substring(0, 1) + nomecond.Substring(1).ToLower()).Replace(" ","") + "=" + idcond + "\\Amministrazione\\Fatture in ingresso\\";
                            if (!System.IO.Directory.Exists(percorso_pdf))
                                System.IO.Directory.CreateDirectory(percorso_pdf);
                            foreach (XmlNode xPdf in doc.GetElementsByTagName("Allegati"))
                            {
                                x2 = xPdf.SelectSingleNode("NomeAttachment");
                                if (x2 != null)
                                {
                                    if (sPdf != "")
                                    {
                                        sPdf += "_" + Pulisci(numfat) + "_" + datafat.ToString("yyyy_MM_dd") + System.IO.Path.GetExtension(x2.InnerText);

                                    }
                                    else
                                    {
                                        sPdf = System.IO.Path.GetFileNameWithoutExtension(x2.InnerText) + "_" + Pulisci(numfat) + "_" + datafat.ToString("yyyy_MM_dd") + System.IO.Path.GetExtension(x2.InnerText);

                                    }
                                    System.IO.File.WriteAllBytes(percorso_pdf + sPdf, Convert.FromBase64String(xPdf.SelectSingleNode("Attachment").InnerText));
                                }
                            }
                            logfile.WriteLine("fine pdf");

                            Invoice["idcondominio"] = idcond;
                            Invoice["annobilancio"] = wCond.GetInteger("IdCdmAmministrazione");
                            Invoice["motivo"] = "";
                            if (Operatore.Fields.Count == 0)
                            {
                                Invoice["idoperatore"] = "";
                                Invoice["ragsocoperatore"] = xFornitoreInfo.RagioneSociale;
                                Invoice["codfiscoperatore"] = xFornitoreInfo.PartitaIva;
                                Invoice["pivaoperatore"] = xFornitoreInfo.PartitaIva;
                                Invoice["idpianodeiconti"] = "";
                                Invoice["created_at"] = DateTime.Now;
                                Invoice["motivo"] = "Operatore non trovato";
                                Invoice["allegato"] = percorso_pdf + sPdf;
                            }
                            else
                            {// deno, cFisc, pIva,pianodeiconti
                                Invoice["idoperatore"] = Operatore.GetInteger("IdOperatori");
                                Invoice["ragsocoperatore"] = Operatore.GetNotNullString("deno");
                                Invoice["codfiscoperatore"] = Operatore.GetNotNullString("cFisc");
                                Invoice["pivaoperatore"] = Operatore.GetNotNullString("pIva");
                                Invoice["idpianodeiconti"] = Operatore.GetNotNullString("pianodeiconti");
                                Invoice["allegato"] = percorso_pdf + sPdf;
                            }


                            Invoice["indirizzooperatore"] = xFornitoreInfo.Indirizzo;
                            Invoice["capoperatore"] = xFornitoreInfo.Cap;
                            Invoice["cittaoperatore"] = xFornitoreInfo.Comune;

                            Invoice["numerofattura"] = numfat;
                            Invoice["datafattura"] = datafat;
                            logfile.WriteLine("fine intestazione");
                            foreach (XmlNode xnInv in doc.GetElementsByTagName("FatturaElettronicaBody"))
                            {
                                TOTimponibile = 0;
                                TOTiva = 0;
                                TOTfci = 0;

                                x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/TipoDocumento");
                                switch (x0.InnerText)
                                {
                                    case "TD04":
                                    case "TD05":
                                    case "TD08":
                                    case "TD09":
                                        Invoice["tipodocumento"] = 2;
                                        break;

                                    default:
                                        Invoice["tipodocumento"] = 1;
                                        break;
                                }
                                logfile.WriteLine("tipodoc");
                                x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/ImportoTotaleDocumento");
                                if (x0 != null)
                                    Invoice["totalefattura"] = x0.InnerText.Replace(".", ",");
                                else
                                    Invoice["totalefattura"] = 0;
                                x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/Causale");
                                if (x0 != null)
                                    Invoice["descrizionesfattura"] = x0.InnerText;

                                if (xFornitoreInfo.PartitaIva.Contains("03819031208"))
                                {
                                    x0 = xnInv.SelectSingleNode("DatiGenerali/DatiContratto");
                                    if (x0 != null)
                                    {
                                        x1 = x0.SelectSingleNode("IdDocumento");
                                        if (x1 != null)
                                        {
                                            checkContratto = x1.InnerText;
                                            if (w_apisql.CheckContratto(checkContratto, Convert.ToInt32(idcond)) != "OK")
                                                Invoice["motivo"] = "Pod/PDR HERA errato";
                                        }

                                    }
                                }
                                logfile.WriteLine("finecheck");
                                Invoice["opzritenuta"] = 1;
                                x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/DatiRitenuta");
                                if (x0 != null)
                                {
                                    x1 = x0.SelectSingleNode("ImportoRitenuta");
                                    if (x1 != null)
                                        Invoice["ritenutaacconto"] = x1.InnerText.Replace(".", ",");

                                    x1 = x0.SelectSingleNode(" AliquotaRitenuta");

                                    if (x1 != null)
                                        switch (x1.InnerText)
                                        {

                                            case "4.00":
                                                Invoice["opzritenuta"] = 2;
                                                break;
                                            case "20.00":
                                                Invoice["opzritenuta"] = 3;
                                                break;

                                            default:
                                                Invoice["opzritenuta"] = 1;
                                                break;
                                        }


                                }

                                if (xFornitoreInfo.RegimeFiscale == "RF19")
                                    Invoice["opzritenuta"] = 5;

                                x0 = xnInv.SelectSingleNode("DatiGenerali/DatiGeneraliDocumento/DatiBollo");
                                if (x0 != null)
                                {
                                    x1 = x0.SelectSingleNode("ImportoBollo");
                                    if (x1 != null)
                                        TOTfci = IsDecimale(x1.InnerText.Replace(".", ","));
                                }
                                logfile.WriteLine("fine fiscalita");
                                foreach (XmlNode xRighe in xnInv.SelectSingleNode("DatiBeniServizi").SelectNodes("DettaglioLinee"))
                                {
                                    if (Invoice["descrizionesfattura"] == null)
                                    {

                                        x1 = xRighe.SelectSingleNode("Descrizione");
                                        if (x1 != null)
                                            Invoice["descrizionesfattura"] = x1.InnerText;

                                    }
                                    logfile.WriteLine("descriz");
                                    if (xFornitoreInfo.PartitaIva.Contains("03819031208"))
                                    {

                                        x1 = xRighe.SelectSingleNode("AltriDatiGestionali");
                                        if (x1 != null)
                                        {
                                           
                                           x2 = x1.SelectSingleNode("RiferimentoTesto");
                                            checkContratto = x2.InnerText;
                                            logfile.WriteLine("riferimento");

                                            if (w_apisql.CheckContratto(checkContratto, Convert.ToInt32(idcond)) != "OK")
                                                Invoice["motivo"] = "Pod/PDR HERA errato";
                                            else
                                                Invoice["motivo"] = "";
                                        }
                                        logfile.WriteLine("finerif");
                                        if (checkContratto == "" & Invoice["motivo"].ToString() == "")
                                            Invoice["motivo"] = "Pod/PDR HERA mancante";
                                    }
                                    logfile.WriteLine("fineriga");

                                }

                                logfile.WriteLine("finerighe");

                                foreach (XmlNode xRiepil in xnInv.SelectSingleNode("DatiBeniServizi").SelectNodes("DatiRiepilogo"))
                                {
                                    imponibile = 0;
                                    iva = 0;

                                    aliq = 0;
                                    x1 = xRiepil.SelectSingleNode("ImponibileImporto");
                                    if (x1 != null)
                                    {
                                        imponibile = IsDecimale(x1.InnerText);
                                        TOTimponibile += imponibile;
                                    }

                                    x1 = xRiepil.SelectSingleNode("AliquotaIVA");
                                    if (x1 != null)
                                    {
                                        aliq = IsDecimale(x1.InnerText);
                                    }
                                    x1 = xRiepil.SelectSingleNode("Imposta");
                                    if (x1 != null)
                                    {
                                        iva = IsDecimale(x1.InnerText);
                                        TOTiva += iva;
                                    }

                                    switch (aliq)
                                    {
                                        case 4:
                                            Invoice["imponibile4"] = imponibile;
                                            Invoice["iva4"] = iva;
                                            break;
                                        case 5:
                                            Invoice["imponibile5"] = imponibile;
                                            Invoice["iva5"] = iva;
                                            break;
                                        case 10:
                                            Invoice["imponibile10"] = imponibile;
                                            Invoice["iva10"] = iva;
                                            break;
                                        case 22:
                                            Invoice["imponibile22"] = imponibile;
                                            Invoice["iva22"] = iva;
                                            break;
                                        default:
                                            //if (xFornitoreInfo.RegimeFiscale == "RF19")
                                            //{
                                            Invoice["imponibile22"] = imponibile;
                                            Invoice["iva22"] = iva;
                                            //}
                                            //else
                                            //{
                                            //    TOTfci += imponibile;
                                            //}
                                            break;
                                    }

                                }

                                logfile.WriteLine("fineiva");

                                if (TOTiva == 0)
                                    Invoice["esenteiva"] = true;

                                Invoice["imponibilefattura"] = TOTimponibile;
                                Invoice["ivafattura"] = TOTiva;
                                Invoice["imponibilefci"] = TOTfci;
                                Invoice["ImponibileEsenteRitAcconto"] = 0;

                                TOTdapagare = 0;
                                string primascad = "";
                                x0 = xnInv.SelectSingleNode("DatiPagamento");
                                if (x0 != null)
                                    foreach (XmlNode xRiepil in x0.SelectNodes("DettaglioPagamento"))
                                    {
                                        if (primascad == "")
                                        {
                                            x1 = xRiepil.SelectSingleNode("DataScadenzaPagamento");
                                            if (x1 != null)
                                                primascad = reversedata(x1.InnerText);
                                        }

                                        x1 = xRiepil.SelectSingleNode("ImportoPagamento");
                                        if (x1 != null)
                                            TOTdapagare += IsDecimale(x1.InnerText);
                                    }
                                else
                                    TOTdapagare = TOTimponibile + TOTiva;

                                logfile.WriteLine("finepagamento");

                                if (xFornitoreInfo.PartitaIva.Contains("03819031208"))
                                {
                                    Invoice["pagamento"] = "46";
                                    Invoice["datapagamentofissa"] = primascad;
                                }
                                else
                                {
                                    Invoice["pagamento"] = "47";
                                    Invoice["datapagamentofissa"] = "";
                                }

                                Invoice["totaledapagare"] = TOTdapagare;

                                Invoice["created_at"] = DateTime.Now;

                            }
                            logfile.WriteLine("fine corpo");
                            if (Invoice["motivo"].ToString() == "")
                            {
                                errsql = w_apisql.SetRegistrazione(Invoice);

                                if (errsql != "OK")
                                {
                                    Invoice["motivo"] = errsql;
                                }
                                errsql = w_apisql.InvoiceScartate(Invoice);
                            }

                            else
                            {
                                errsql = w_apisql.InvoiceScartate(Invoice);
                                errsql = "Fattura scartata" + Invoice["motivo"].ToString();
                            }

                        }

                    }


                }
                logfile.Close();
                return errsql;
            }
            catch (Exception e)
            {
                logfile.WriteLine(FlattenException(e) + "");
                logfile.Close();
                return e.Message;
            }

        }

    }

    class FornitoreInfo : IDisposable
    {
        private bool disposedValue;

        [Category("1_DatiAnagrafici")] public string Conto { get; set; }
        [Category("1_DatiAnagrafici")] public string Paese { get; set; }
        [Category("1_DatiAnagrafici")] public string PartitaIva { get; set; }
        [Category("1_DatiAnagrafici")] public string RagioneSociale { get; set; }
        [Category("1_DatiAnagrafici")] public string RegimeFiscale { get; set; }
        [Category("2_Sede")] public string Indirizzo { get; set; }
        [Category("2_Sede")] public string Cap { get; set; }
        [Category("2_Sede")] public string Comune { get; set; }
        [Category("2_Sede")] public string Provincia { get; set; }
        [Category("2_Sede")] public string Nazione { get; set; }

        public FornitoreInfo(System.Xml.XmlDocument xmlin)
        {
            System.Xml.XmlNode n;
            var n0 = xmlin.GetElementsByTagName("CedentePrestatore");
            var n1 = n0[0].SelectSingleNode("DatiAnagrafici");
            var n3 = n1.SelectSingleNode("IdFiscaleIVA");
            n = n3.SelectSingleNode("IdPaese");
            Paese = n.InnerText;
            n = n3.SelectSingleNode("IdCodice");
            PartitaIva = n.InnerText;
            n3 = n1.SelectSingleNode("Anagrafica");
            if (n3 != null)
            {
                n = n3.SelectSingleNode("Denominazione");
                RagioneSociale = n.InnerText;
            }
            n = n1.SelectSingleNode("RegimeFiscale"); RegimeFiscale = n.InnerText;

            n1 = n0[0].SelectSingleNode("Sede");
            n = n1.SelectSingleNode("Indirizzo"); Indirizzo = n.InnerText;
            n = n1.SelectSingleNode("CAP"); Cap = n.InnerText;
            n = n1.SelectSingleNode("Comune"); Comune = n.InnerText;
            n = n1.SelectSingleNode("Provincia"); Provincia = n.InnerText;
            n = n1.SelectSingleNode("Nazione"); Nazione = n.InnerText;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminare lo stato gestito (oggetti gestiti)
                }

                // TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire l'override del finalizzatore
                // TODO: impostare campi di grandi dimensioni su Null
                disposedValue = true;
            }
        }

        // // TODO: eseguire l'override del finalizzatore solo se 'Dispose(bool disposing)' contiene codice per liberare risorse non gestite
        // ~FornitoreInfo()
        // {
        //     // Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(bool disposing)'
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    class ClienteInfo
    {
        [Category("1_DatiAnagrafici")] public string Conto { get; set; }
        [Category("1_DatiAnagrafici")] public string Paese { get; set; }
        [Category("1_DatiAnagrafici")] public string CodiceFiscale { get; set; }
        [Category("1_DatiAnagrafici")] public string RagioneSociale { get; set; }
        [Category("2_Sede")] public string Indirizzo { get; set; }
        [Category("2_Sede")] public string Cap { get; set; }
        [Category("2_Sede")] public string Comune { get; set; }
        [Category("2_Sede")] public string Provincia { get; set; }
        [Category("2_Sede")] public string Nazione { get; set; }

        public ClienteInfo(System.Xml.XmlDocument xmlin)
        {
            System.Xml.XmlNode n;
            var n0 = xmlin.GetElementsByTagName("CessionarioCommittente");
            var n1 = n0[0].SelectSingleNode("DatiAnagrafici");
            n = n1.SelectSingleNode("CodiceFiscale");
            CodiceFiscale = n.InnerText;
            var n3 = n1.SelectSingleNode("Anagrafica");
            if (n3 != null)
            {
                n = n3.SelectSingleNode("Denominazione");
                if (n == null)
                    n = n3.SelectSingleNode("Nome");
                if (n != null)
                    RagioneSociale = n.InnerText;
            }

            n1 = n0[0].SelectSingleNode("Sede");
            n = n1.SelectSingleNode("Indirizzo"); Indirizzo = n.InnerText;
            n = n1.SelectSingleNode("CAP"); Cap = n.InnerText;
            n = n1.SelectSingleNode("Comune"); Comune = n.InnerText;
            n = n1.SelectSingleNode("Provincia"); Provincia = n.InnerText;
            n = n1.SelectSingleNode("Nazione"); Nazione = n.InnerText;
        }
    }
}
