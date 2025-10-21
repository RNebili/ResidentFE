using System;

//v1.0 gestione dei pod/pdr in caso di errore
//v1.1 per cercare operatore uso solo la partita iva


namespace runresidentFE
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Import fatture passive !");
            ResidentFE.import app_res = new ResidentFE.import();
            app_res.Run();
            Environment.Exit(0);


        }
    }
}
