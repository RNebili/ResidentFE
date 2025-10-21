using System;

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
