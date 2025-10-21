using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Data.SqlClient;


namespace ResidentFE
{
    public class APISQL
    {

        private const string username = "Resident";
        private const string password = "R3s!d3nt";
        private const string istanza = @"SRVAPPRES\SQLEXPRESS";
        private const string dbname = "Resident_Dati";
        private const string dbname_com = "Resident_DatiComuni";
        private Boolean m_ok = false;

        public Boolean Ok
        {
            get { return m_ok; }

        }

        //TODO da usare per evitare codice duplicato e debug
        private SqlConnection getSqlConnection(){
            SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();
            con_string.DataSource = istanza;
            con_string.UserID = username;
            con_string.Password = password;
            con_string.InitialCatalog = dbname;
            return new SqlConnection(con_string.ConnectionString);
        }


        public void testconnection()
        {
            if (import.IsDebugMode()) {
                Console.WriteLine("** SQL EMULATED IN DEBUG MODE **");
                m_ok = true;
                return;
            }
            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();
                m_ok = false;
                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    sql_con.Open();
                    sql_con.Close();
                    m_ok = true;
                }

            }
            catch (SqlException e)
            {
                m_ok = false;
                Console.Out.WriteLine(e.Message);
            }

        }
        public IList<Reobj> GetCondomini()
        {
            IList<Reobj> result = new List<Reobj>();
            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();

                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    sql_con.Open();

                    String sql = "select distinct c.IDCondominio, c.codiceFiscale, c.condominio, a.IdCdmAmministrazione	 from condomini c ";
                    sql += " join [Condomini Amministrazione]  a on c.IDCondominio = a.IdCondominio where c.escludifat <> 1 or c.escludifat is null";

                    using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                    {
                        sql_comm.CommandType = CommandType.Text;
                        using (SqlDataReader sql_reader = sql_comm.ExecuteReader())
                        {
                            while (sql_reader.Read())
                            {
                                Reobj doc = new Reobj();
                                for (int i = 0; i < sql_reader.FieldCount; i++)
                                {
                                    doc[sql_reader.GetName(i).ToLower()] = sql_reader[i].ToString().Trim();
                                }

                                result.Add(doc);
                            }
                        }
                    }
                    sql_con.Close();
                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return result;
        }

        public Reobj GetCondominio(string codicefiscale, System.IO.StreamWriter logfile)
        {
            //logfile.WriteLine("GetCondominio(string codicefiscale = " + codicefiscale + ")");

            Reobj result = new Reobj();
            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();

                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    sql_con.Open();

                    String sql = "select distinct c.IDCondominio, c.codiceFiscale, c.condominio, a.IdCdmAmministrazione	 from condomini c ";
                    sql += " join [Condomini Amministrazione]  a on c.IDCondominio = a.IdCondominio where c.IDCondominio <> 326 and  c.codiceFiscale ='" + codicefiscale + "'";

                    using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                    {
                        sql_comm.CommandType = CommandType.Text;
                        using (SqlDataReader sql_reader = sql_comm.ExecuteReader())
                        {
                            
                            sql_reader.Read();
                            if (sql_reader.HasRows)
                            {

                                for (int i = 0; i < sql_reader.FieldCount; i++)
                                {
                                    result[sql_reader.GetName(i).ToLower()] = sql_reader[i];
                                }


                            }
                        }
                    }
                    sql_con.Close();
                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return result;
        }
        public Reobj GetOperatore(string cfisc, string piva, string condominio, DateTime datafat, System.IO.StreamWriter logfile)
        {
            bool log = false;
            //TODO impostare un log_lvl nel config , per debug direttamente in prod
            //logfile.WriteLine("GetOperatore(string cfisc = " + cfisc + ", string piva = " + piva + ", string condominio = " + condominio + ", DateTime datafat = " + datafat.ToString() + ")");

            Reobj result = new Reobj();

            SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();
            SqlConnectionStringBuilder con_stringpiano = new SqlConnectionStringBuilder();

            con_string.DataSource = istanza;
            con_string.UserID = username;
            con_string.Password = password;
            con_string.InitialCatalog = dbname_com;
            con_stringpiano.DataSource = istanza;
            con_stringpiano.UserID = username;
            con_stringpiano.Password = password;
            con_stringpiano.InitialCatalog = dbname;

            //v1.1 String sql = "select IdOperatori, deno, cFisc, pIva, codiceTributo from Operatori  where cFisc = @CFISC or pIva =@PIVA and (dataFine is null or dataFine > '";
            String sql = "select IdOperatori, deno, cFisc, pIva, codiceTributo from Operatori  where cFisc = @CFISC or pIva =@PIVA and (dataFine is null or dataFine > '";
            //sql += datafat.ToShortDateString() + "')";
            sql += datafat.ToString("yyyy-dd-MM hh:mm tt") + "')";
            String sqlpiano = "select StrIdPianoDeiConti from Tbl_DettaglioRipartizioni  ";
            sqlpiano += "where (IdOperatore = @IDoper or IdOperatore is null) and IdCondominio = @IdCond and Abilita = 1 and TipoRipartizione = 1 ";
            string IDoper = "";

            using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
            {
                using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                {
                    sql_comm.Parameters.AddWithValue("@CFISC", cfisc);
                    sql_comm.Parameters.AddWithValue("@PIVA", piva);

                    sql_con.Open();
                    using (SqlDataReader sql_reader = sql_comm.ExecuteReader())
                    {
                        if(log) logfile.Write("+");
                        sql_reader.Read();
                        if(log) logfile.Write("x");
                        if (sql_reader.HasRows)
                        {

                            for (int i = 0; i < sql_reader.FieldCount; i++)
                            {
                                result[sql_reader.GetName(i).ToLower()] = sql_reader[i];
                            }
                            using (SqlConnection sql_conpiano = new SqlConnection(con_stringpiano.ConnectionString))
                            {
                                using (SqlCommand sql_commpiano = new SqlCommand(sqlpiano, sql_conpiano))
                                {
                                    IDoper = sql_reader[0].ToString();
                                    sql_commpiano.Parameters.AddWithValue("@IDoper", sql_reader[0]);
                                    sql_commpiano.Parameters.AddWithValue("@IdCond", condominio);
                                    sql_comm.CommandType = CommandType.Text;
                                    sql_conpiano.Open();
                                    using (SqlDataReader sql_readerpiano = sql_commpiano.ExecuteReader())
                                    {
                                        int conta = 0;
                                        string idpiano = "";
                                        string[] piano;
                                        while (sql_readerpiano.Read())
                                        {
                                            if (log) logfile.WriteLine("-");
                                            conta += 1;
                                            idpiano = Convert.ToString(sql_readerpiano[0]);
                                        }
                                        result["pianodeiconti"] = "";
                                        if (conta == 1)
                                        {
                                            piano = idpiano.Split(",");
                                            if (piano.Length == 1)
                                                result["pianodeiconti"] = idpiano;
                                        }

                                    }
                                    sql_conpiano.Close();
                                }
                            }

                        }
                    }
                    if (log) logfile.WriteLine("");
                    sql_con.Close();

                }
            }
            if (result.Fields.Count == 0) {
                logfile.WriteLine(" GetOperatore() KO sql=" + sql.Replace("@CFISC", "'" + cfisc + "'").Replace("@PIVA", "'" + piva + "'") + " sqlpiano=" + sqlpiano.Replace("@IDoper", "'" + IDoper + "'").Replace("@IdCond", "'" + condominio + "'"));
            }
            return result;
        }
        public string SetRegistrazione(Reobj invoice)
        {
            string result = "OK";

            string log = "";

            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();


                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;


                String sql = "INSERT INTO [dbo].[RegistrazioneFattureCondomini] ";
                sql += "(IDCondominio ,NumeroFattura,DataFattura,AnnoBilancio,RiferimentoFornitore,ModalitaPagamento,DataPagamentoFissa,IdPianoDeiConti ";
                sql += ",DescrizionesFattura,TipoDocumento,ImponibileFattura,IvaFattura,Imponibile22,Iva22,Imponibile21,Iva21,Imponibile20,Iva20 ";
                sql += ",Imponibile10,Iva10,Imponibile4,Iva4,ImponibileFCI,Commissioni,TotaleScontrino,TotaleDaPagare,OpzRitenuta,RitenutaAcconto ";
                sql += ",IDOperatori,RagSocOperatore,IndirizzoOperatore,CAPOperatore,CittaOperatore,CodFiscOperatore,PIvaOperatore,FatturaRegistrata ";
                sql += ",ImportoInteressi,ImportoSanzione,ImponibileEsenteRitAcconto,ImportoF24 ,TipoRipartizione,EsenteIva,Idf24T,IdRavvedimento ";
                sql += ", ImponibileNonOnorario, fat_cessione, fat_centodieci,Imponibile5,Iva5,import) ";
                sql += " VALUES (@idcondominio,@numerofattura,@datafattura,@annobilancio,'123456',@pagamento,@datapagamentofissa,@idpianodeiconti ";
                sql += ",@descrizionesfattura,@tipodocumento,@imponibilefattura,@ivafattura,@imponibile22,@iva22,0,0,0,0,@imponibile10,@iva10 ";
                sql += ",@imponibile4,@iva4,@imponibilefci,0,@totalefattura,@totaledapagare,@opzritenuta,@ritenutaacconto,@idoperatore ";
                sql += ",@ragsocoperatore,@indirizzooperatore,@capoperatore,@cittaoperatore,@codfiscoperatore,@pivaoperatore,0,0,0,@ImponibileEsenteRitAcconto ";
                sql += ",0,1,@esenteiva,0,0,0,0,0,@Imponibile5,@Iva5,1) ";

                //    "INSERT INTO [dbo].[RegistrazioneFattureCondomini] ";
                //sql += "(IDCondominio ,NumeroFattura,DataFattura,AnnoBilancio,RiferimentoFornitore,ModalitaPagamento,DataPagamentoFissa,IdPianoDeiConti ";
                //sql += ",DescrizionesFattura,TipoDocumento,ImponibileFattura,IvaFattura,Imponibile22,Iva22,Imponibile21,Iva21,Imponibile20,Iva20 ";
                //sql += ",Imponibile10,Iva10,Imponibile4,Iva4,ImponibileFCI,Commissioni,TotaleScontrino,TotaleDaPagare,OpzRitenuta,RitenutaAcconto ";
                //sql += ",IDOperatori,RagSocOperatore,IndirizzoOperatore,CAPOperatore,CittaOperatore,CodFiscOperatore,PIvaOperatore,FatturaRegistrata ";
                //sql += ",DataRegistrazione,ImportoInteressi,ImportoSanzione,DataPagamentoF24,IdDom,ImponibileEsenteRitAcconto,ImportoF24,DataRavvedimento ";
                //sql += ",TipoRipartizione,CodiceBarre,EsenteIva,Idf24T,IdRavvedimento,ImponibileNonOnorario) ";

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                    {
                        sql_comm.Parameters.AddWithValue("@idcondominio", invoice.GetInteger("idcondominio"));
                        sql_comm.Parameters.AddWithValue("@numerofattura", invoice.GetNotNullString("numerofattura"));
                        sql_comm.Parameters.AddWithValue("@datafattura", invoice.GetDateTime("datafattura"));
                        sql_comm.Parameters.AddWithValue("@annobilancio", invoice.GetInteger("annobilancio"));
                        sql_comm.Parameters.AddWithValue("@pagamento", invoice.GetInteger("pagamento"));
                        //gestire null
                        if (invoice.isnull("datapagamentofissa"))
                            sql_comm.Parameters.AddWithValue("@datapagamentofissa", DBNull.Value);
                        else
                            sql_comm.Parameters.AddWithValue("@datapagamentofissa", invoice.GetDateTime("datapagamentofissa"));

                        sql_comm.Parameters.AddWithValue("@idpianodeiconti", invoice.GetNotNullString("idpianodeiconti"));
                        string df = invoice.GetNotNullString("descrizionesfattura");
                        df = df.Length <= 250 ? df : df.Substring(0, 250);
                        sql_comm.Parameters.AddWithValue("@descrizionesfattura", df);
                        sql_comm.Parameters.AddWithValue("@tipodocumento", invoice.GetInteger("tipodocumento"));
                        sql_comm.Parameters.AddWithValue("@imponibilefattura", invoice.GetDecimal("imponibilefattura"));
                        sql_comm.Parameters.AddWithValue("@ivafattura", invoice.GetDecimal("ivafattura"));
                        sql_comm.Parameters.AddWithValue("@imponibile22", invoice.GetDecimal("imponibile22"));
                        sql_comm.Parameters.AddWithValue("@iva22", invoice.GetDecimal("iva22"));
                        sql_comm.Parameters.AddWithValue("@imponibile10", invoice.GetDecimal("imponibile10"));
                        sql_comm.Parameters.AddWithValue("@iva10", invoice.GetDecimal("iva10"));
                        sql_comm.Parameters.AddWithValue("@imponibile4", invoice.GetDecimal("imponibile4"));
                        sql_comm.Parameters.AddWithValue("@iva4", invoice.GetDecimal("iva4"));
                        sql_comm.Parameters.AddWithValue("@imponibilefci", invoice.GetDecimal("imponibilefci"));
                        sql_comm.Parameters.AddWithValue("@totalefattura", invoice.GetDecimal("totalefattura"));
                        sql_comm.Parameters.AddWithValue("@totaledapagare", invoice.GetDecimal("totaledapagare"));
                        sql_comm.Parameters.AddWithValue("@opzritenuta", invoice.GetInteger("opzritenuta"));
                        sql_comm.Parameters.AddWithValue("@ritenutaacconto", invoice.GetDecimal("ritenutaacconto"));
                        sql_comm.Parameters.AddWithValue("@idoperatore", invoice.GetInteger("idoperatore"));
                        sql_comm.Parameters.AddWithValue("@ragsocoperatore", invoice.GetNotNullString("ragsocoperatore"));
                        sql_comm.Parameters.AddWithValue("@indirizzooperatore", invoice.GetNotNullString("indirizzooperatore"));
                        sql_comm.Parameters.AddWithValue("@capoperatore", invoice.GetInteger("capoperatore"));
                        sql_comm.Parameters.AddWithValue("@cittaoperatore", invoice.GetNotNullString("cittaoperatore"));
                        sql_comm.Parameters.AddWithValue("@codfiscoperatore", invoice.GetNotNullString("codfiscoperatore"));
                        sql_comm.Parameters.AddWithValue("@pivaoperatore", invoice.GetNotNullString("pivaoperatore"));
                        sql_comm.Parameters.AddWithValue("@ImponibileEsenteRitAcconto", invoice.GetDecimal("ImponibileEsenteRitAcconto"));
                        sql_comm.Parameters.AddWithValue("@esenteiva", invoice.GetDecimal("esenteiva"));
                        sql_comm.Parameters.AddWithValue("@imponibile5", invoice.GetDecimal("imponibile5"));
                        sql_comm.Parameters.AddWithValue("@iva5", invoice.GetDecimal("iva5"));
                        log += sql + " >>>";

                        foreach (SqlParameter sp in sql_comm.Parameters)
                        {
                            log += " " + sp.ParameterName + "=" + sp.Value.ToString();
                        }
                        sql_comm.CommandType = CommandType.Text;
                        sql_con.Open();
                        sql_comm.ExecuteNonQuery();
                    }
                    sql_con.Close();
                }

            }
            catch (SqlException e)
            {
                result = e.Message.ToString() + " >>> " + log;
            }

            return result;
        }


        public string InvoiceScartate(Reobj invoice)
        {

            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();

                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;

                String sql = "INSERT INTO [dbo].[Fatture_scartate] ";
                sql += "(created_at, idcondominio ,codicefiscale, numerofattura, datafattura, modalitapagamento,datapagamentofissa ";
                sql += ",descrizionesfattura,tipodocumento,imponibilefattura,ivafattura,totalefattura, idoperatore, ragsocoperatore,indirizzooperatore,capoperatore,cittaoperatore ";
                sql += ",codfiscoperatore,pivaoperatore, motivo, allegato) ";
                sql += " VALUES (@created_at, @idcondominio,@codicefiscale,@numerofattura,@datafattura,@pagamento,@datapagamentofissa,@descrizionesfattura,@tipodocumento ";
                sql += ",@imponibilefattura,@ivafattura,@totalefattura,@idoperatore, @ragsocoperatore,@indirizzooperatore,@capoperatore,@cittaoperatore ";
                sql += " ,@codfiscoperatore,@pivaoperatore, @motivo, @allegato) ";

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                    {
                        sql_comm.Parameters.AddWithValue("@created_at", invoice.GetDateTime("created_at"));
                        sql_comm.Parameters.AddWithValue("@idcondominio", invoice.GetInteger("idcondominio"));
                        sql_comm.Parameters.AddWithValue("@codicefiscale", invoice.GetNotNullString("codicefiscale"));
                        sql_comm.Parameters.AddWithValue("@numerofattura", invoice.GetNotNullString("numerofattura"));
                        sql_comm.Parameters.AddWithValue("@datafattura", invoice.GetDateTime("datafattura"));
                        sql_comm.Parameters.AddWithValue("@totalefattura", invoice.GetDecimal("totalefattura"));
                        sql_comm.Parameters.AddWithValue("@pagamento", invoice.GetInteger("pagamento"));
                        //gestire null
                        if (invoice.isnull("datapagamentofissa"))
                            sql_comm.Parameters.AddWithValue("@datapagamentofissa", DBNull.Value);
                        else
                            sql_comm.Parameters.AddWithValue("@datapagamentofissa", invoice.GetDateTime("datapagamentofissa"));

                        sql_comm.Parameters.AddWithValue("@descrizionesfattura", invoice.GetNotNullString("descrizionesfattura"));
                        sql_comm.Parameters.AddWithValue("@tipodocumento", invoice.GetInteger("tipodocumento"));
                        sql_comm.Parameters.AddWithValue("@imponibilefattura", invoice.GetDecimal("imponibilefattura"));
                        sql_comm.Parameters.AddWithValue("@ivafattura", invoice.GetDecimal("ivafattura"));
                        sql_comm.Parameters.AddWithValue("@idoperatore", invoice.GetInteger("idoperatore"));
                        sql_comm.Parameters.AddWithValue("@ragsocoperatore", invoice.GetNotNullString("ragsocoperatore"));
                        sql_comm.Parameters.AddWithValue("@indirizzooperatore", invoice.GetNotNullString("indirizzooperatore"));
                        sql_comm.Parameters.AddWithValue("@capoperatore", invoice.GetInteger("capoperatore"));
                        sql_comm.Parameters.AddWithValue("@cittaoperatore", invoice.GetNotNullString("cittaoperatore"));
                        sql_comm.Parameters.AddWithValue("@codfiscoperatore", invoice.GetNotNullString("codfiscoperatore"));
                        sql_comm.Parameters.AddWithValue("@pivaoperatore", invoice.GetNotNullString("pivaoperatore"));
                        sql_comm.Parameters.AddWithValue("@motivo", invoice.GetNotNullString("motivo"));
                        sql_comm.Parameters.AddWithValue("@allegato", invoice.GetNotNullString("allegato"));

                        sql_comm.CommandType = CommandType.Text;

                        sql_con.Open();
                        sql_comm.ExecuteNonQuery();
                    }
                    sql_con.Close();
                }

            }
            catch (SqlException e)
            {
                return e.ToString();
            }

            return "OK";
        }
        public string CheckContratto(string contratto, int idcondominio, System.IO.StreamWriter logfile, string logkey)
        {
            string result = "KO";
            string sql = "";
            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();

                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    sql_con.Open();

                    sql = "SELECT IdCondominio, Anno, CodiceCliente, Contratto, Consumo, DsTipoConsumo, dtag, Gestore FROM [Condomini ConsumiPresunti] ";
                    sql += " where Contratto = '" + contratto.Trim() + "' and IdCondominio =" + idcondominio;

                    using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                    {
                        sql_comm.CommandType = CommandType.Text;
                        using (SqlDataReader sql_reader = sql_comm.ExecuteReader())
                        {
                            sql_reader.Read();
                            if (sql_reader.HasRows)
                            {
                                result = "OK";
                            }
                        }
                    }
                    sql_con.Close();
                }

            }
            catch (SqlException e)
            {
                result = e.ToString();
            }
            //if (result != "OK")
            //{
                logfile.WriteLine("CheckContratto: result=" + result + " key=" + logkey  + " sql=" + sql);
            //}
            return result;
        }

        public IList<Reobj> Datiammnistrativi()
        {
            IList<Reobj> result = new List<Reobj>();
            try
            {
                SqlConnectionStringBuilder con_string = new SqlConnectionStringBuilder();

                con_string.DataSource = istanza;
                con_string.UserID = username;
                con_string.Password = password;
                con_string.InitialCatalog = dbname;

                using (SqlConnection sql_con = new SqlConnection(con_string.ConnectionString))
                {
                    sql_con.Open();
                    //String sql = "select distinct c.IDCondominio ,c.codiceFiscale, c.condominio, isnull(c.ViaPerCap, t.DENOMINAZIONE) as indirizzo, ";
                    //sql += " isnull(c.Cap, isnull(tc.VALORE, cc.Cap)) as cap,  civ, cc.Comune ";
                    //sql += "from condomini c ";
                    //sql += "left join Resident_DatiComuni.dbo.Tbl_Vie t on c.COD_VIA = t.COD_VIA ";
                    //sql += "left join Resident_DatiComuni.dbo.Comuni cc on c.IDComuni = cc.IdComuni ";
                    //sql += "left join Resident_DatiComuni.dbo.Tbl_Civici tc on tc.CIVICO = c.civ and tc.COD_VIA = c.COD_VIA ";
                    //sql += "where c.esce is null ";
                    string sql = "SELECT IDCondominio, codiceFiscale, condominio, indirizzo, cap, civ, Comune, import ";
                    sql += "FROM zz_Condomini WHERE (import = N'0') ORDER BY IDCondominio";

                    using (SqlCommand sql_comm = new SqlCommand(sql, sql_con))
                    {
                        sql_comm.CommandType = CommandType.Text;
                        using (SqlDataReader sql_reader = sql_comm.ExecuteReader())
                        {
                            while (sql_reader.Read())
                            {
                                Reobj doc = new Reobj();
                                for (int i = 0; i < sql_reader.FieldCount; i++)
                                {
                                    doc[sql_reader.GetName(i).ToLower()] = sql_reader[i];
                                }                                
                                doc["codicefiscale"]= "IT" + sql_reader["codicefiscale"].ToString().PadLeft(11, '0');

                                result.Add(doc);
                            }
                        }
                    }

                    sql_con.Close();
                }
            }

            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return result;
        }

    }
}
