using System;

using System.Text;

using System.Net;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography.Pkcs;


namespace ResidentFE
{

    class invoice_ret
    {
        public mHUB_FC HUB_FC { get; set; }
        public mHUB_IE HUB_IE { get; set; }
        public mHUB_DC HUB_DC { get; set; }

    }

    class mHUB_FC
    {
        public string dateTime { get; set; }
        public string notificationFileData { get; set; }
        public string invoiceFileData { get; set; }
        public string notificationFileName { get; set; }
        public string additionalInfo { get; set; }
        public Boolean waitForHubDc { get; set; }
        public string invoiceFileName { get; set; }
        public string status { get; set; }
    }
    class mHUB_IE
    {
        public string dateTime { get; set; }
        public string additionalInfo { get; set; }
        public string invoiceFileName { get; set; }
        public string status { get; set; }
    }
    class mHUB_DC
    {
        public string dateTime { get; set; }
        public string additionalInfo { get; set; }
        public string invoiceRenderingFileName { get; set; }
        public string invoiceFileName { get; set; }
        public string status { get; set; }
        public string invoiceRenderingFileData { get; set; }
    }

    class login_return
    {
        public string resultCode { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("hub.sessionId")]
        public string hubsessionId { get; set; }
    }

    class amministraz_return
    {
        public string resultCode { get; set; }

    }

    public class GetInvoice
    {
        private const string username = "resident";
        private const string password = "w2pZ2*aX";
        private const string URL = "https://prod-satanetfe.resident.it/sdi-api/download-invoice";

        private string _pivacond;
        private string _invoice;
        public byte[] FilePdf { get; set; }

        public string jsonPath = null;



        public string Cond_partiva
        {
            //get { return _pivacond; }
            set
            {
                _pivacond = "IT" + value.PadLeft(11, '0');
            }
        }

        public string Invoice
        {
            get { return _invoice; }
            set
            {
                _invoice = value;
            }
        }

        public string Run()
        {
            string stato = "";
            _invoice = "";
            //BEGIN TODO MODE
            bool todo = import.IsTodoMode();
            if (todo) {
                string[] fileEntries = Directory.GetFiles(import.pathRisposteSataTodo);
                foreach (string fileName in fileEntries) {
                    String[] s =fileName.Split("\\");
                    Console.WriteLine("Elaborazione " + fileName);
                    if (s.Length > 0) s = s[s.Length - 1].Split(".");                    
                    s = s[0].Split("_");
                    if (s.Length > 0 && s[1] == _pivacond.Trim())
                    {
                        stato = this.SetByFile(fileName);
                        Console.WriteLine("stato=" + stato);
                        return stato;
                    }
                    else {
                        Console.WriteLine("Situazione non previsto con la partita iva, aggiornare hDoc.Add(\"codiceFiscale\", \"94035300360\");");
                        Console.WriteLine((s.Length>1?"s[1]=\"" + s[1] + "\"":"-"));
                        Console.WriteLine("_pivacond.Trim()=\"" + _pivacond.Trim() + "\"");
                        return "KO";
                    }
                    
                }                                
            }
            if (import.IsDebugMode()) {                
                Console.WriteLine("** END DEBUG MODE **");
                System.Environment.Exit(0);
            }
            //END TODO MODE
            string nomefile = "";
            string DATA = @"{""object"":{""name"":""Name""}}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "POST";
            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                               .GetBytes(username + ":" + password));
            request.Headers.Add("Authorization", "Basic " + encoded);
            request.PreAuthenticate = true;
            request.Headers.Add("Content-Type", "application/json");
            DATA = @"{" + "\n" + @"""identifierCode"": """ + _pivacond.Trim() + @""" " + "\n" + @"}";
            request.ContentLength = DATA.Length;

            using (Stream webStream = request.GetRequestStream())
            using (StreamWriter requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
            {
                requestWriter.Write(DATA);
            }

            //nomefile = @"D:\xml_file\" +  DateTime.Now.ToString("yyyyMMddHHmmssffff") + "_"+ _pivacond.Trim()  + ".txt";

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();


                    if (response != "")
                    {
                        nomefile = @"D:\xml_file\risposteSATA\" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + "_" + _pivacond.Trim() + ".json";
                        System.IO.File.WriteAllText(nomefile, response);

                        stato = this.SetByFile(nomefile);

                    }
                    else
                    {
                        stato = "KO";
                    }


                }
                if (stato != "OK")
                {
                    _invoice = "";
                }


            }
            catch (Exception e)

            {
                _invoice = "";
                return e.Message;

            }
            return stato;
        }


        public void moveJsonToDoneFolder() {
            string donePath = import.pathRisposteSataDone;
            if (!System.IO.Directory.Exists(donePath))
                System.IO.Directory.CreateDirectory(donePath);
            System.IO.File.Move(this.jsonPath, donePath + System.IO.Path.GetFileName(this.jsonPath));
        }

        public void moveJsonToDiscardedFolder() {
            string discardedPath = import.pathRisposteSataDiscarded;
            if (!System.IO.Directory.Exists(discardedPath))
                System.IO.Directory.CreateDirectory(discardedPath);
            System.IO.File.Move(this.jsonPath, discardedPath + System.IO.Path.GetFileName(this.jsonPath));
        }

        public string SetByFile(string nomeFile)
        {
            this.jsonPath = nomeFile;
            string stato = "";
            string response = System.IO.File.ReadAllText(nomeFile);
            invoice_ret w_invoice = JsonSerializer.Deserialize<invoice_ret>(response);
            FilePdf = Convert.FromBase64String(w_invoice.HUB_DC.invoiceRenderingFileData);
            byte[] FileFirmatoP7m = Convert.FromBase64String(w_invoice.HUB_FC.invoiceFileData);

            if (System.IO.Path.GetExtension(w_invoice.HUB_FC.invoiceFileName) == ".p7m")
            {
                if (FileFirmatoP7m != null)
                {
                    SignedCms cmsFirmato = new();
                    try
                    {
                        cmsFirmato.Decode(FileFirmatoP7m);
                    } catch (Exception e) {
                        stato = "Errore decodifica pm7 (2) " + nomeFile + " " + e.Message;
                    }
                    
                    if (stato == "" && !cmsFirmato.Detached)
                    {
                        byte[] str = cmsFirmato.ContentInfo.Content;
                        _invoice = Encoding.UTF8.GetString(str);

                        stato = "OK";
                    }
                    else if (stato == "")
                    {
                        stato = "Errore decodifica pm7 (1) " + nomeFile;
                    }


                }
                else
                {
                    stato = "Errore decodifica pm7 (0) " + nomeFile;
                }

            }
            else
            {
                _invoice = Encoding.UTF8.GetString(FileFirmatoP7m);
                stato = "OK";
            }
            return stato;

        }
    }



    public class Amministrazione
    {
        private const string username = "api-resident.it";
        private const string password = "KqK])Rszfmk4&Pzv*eE0o9yo?GWWCI";
        private const string URL = "https://prod-satanetfe.resident.it/admin-api";
        private string _session_id;
        //public static void AppendUrlEncoded(this StringBuilder sb, string name, string value)
        //   {
        //       if (sb.Length != 0)
        //           sb.Append("&");
        //       sb.Append(System.Web.HttpUtility.UrlEncode(name));
        //       sb.Append("=");
        //       sb.Append(System.Web.HttpUtility.UrlEncode(value));
        //   }
        public string Login()
        {
            string stato = "";

            var postData = System.Web.HttpUtility.UrlEncode("username") + "=" + System.Web.HttpUtility.UrlEncode(username);
            postData += "&" + System.Web.HttpUtility.UrlEncode("password") + "=" + System.Web.HttpUtility.UrlEncode(password);
            var data = Encoding.ASCII.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL + "/login");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            _session_id = "";

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();

                    if (response != "")
                    {
                        login_return w_login = JsonSerializer.Deserialize<login_return>(response);
                        stato = w_login.resultCode;

                        if (stato == "SUCCESS")
                            _session_id = w_login.hubsessionId;
                        else
                            _session_id = "";

                    }
                }

            }
            catch (Exception e)

            {
                _session_id = "";
                return e.Message;

            }
            return stato;
        }
        public string CaricaAnagrafica(Reobj Condominio)
        {
            string stato = "";

            var postData = System.Web.HttpUtility.UrlEncode("vatCode") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("codiceFiscale"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("identifierCode") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("codiceFiscale"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("legalName") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("condominio"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("taxCode") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("codiceFiscale"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("streetName") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("indirizzo"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("streetNumber") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("civ"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("city") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("comune"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("zipCode") + "=" + System.Web.HttpUtility.UrlEncode(Condominio.GetNotNullString("cap"));
            postData += "&" + System.Web.HttpUtility.UrlEncode("country") + "=" + System.Web.HttpUtility.UrlEncode("ITA");
            postData += "&" + System.Web.HttpUtility.UrlEncode("issuerType") + "=" + System.Web.HttpUtility.UrlEncode("COMPANY");
            var data = Encoding.ASCII.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL + "/save-company");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("hub.sessionId", System.Web.HttpUtility.UrlEncode(_session_id));

            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();
                    if (response != "")
                    {
                        amministraz_return w_stato = JsonSerializer.Deserialize<amministraz_return>(response);
                        stato = w_stato.resultCode;
                    }
                }

            }
            catch (Exception e)
            {
                return e.Message;

            }

            return stato;
        }

        public string AssegnaUtente(string partitaiva, string nomeutente)
        {
            string stato = "";

            var postData = System.Web.HttpUtility.UrlEncode("identifierCode") + "=" + System.Web.HttpUtility.UrlEncode(partitaiva);
            postData += "&" + System.Web.HttpUtility.UrlEncode("username") + "=" + System.Web.HttpUtility.UrlEncode(nomeutente);
            postData += "&" + System.Web.HttpUtility.UrlEncode("enabled") + "=" + System.Web.HttpUtility.UrlEncode("true");

            var data = Encoding.ASCII.GetBytes(postData);


            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL + "/enable-or-disable-user-company");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("hub.sessionId", _session_id);

            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();
                    if (response != "")
                    {
                        amministraz_return w_stato = JsonSerializer.Deserialize<amministraz_return>(response);
                        stato = w_stato.resultCode;
                    }
                }

            }
            catch (Exception e)
            {
                return e.Message;

            }
            return stato;
        }
    }
}

