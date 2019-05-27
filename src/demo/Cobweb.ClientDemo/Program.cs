using Cobweb.Client;
using System;

namespace Cobweb.ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        static void StartMain()
        {
            var user = new CobClientFactory(null).CreateProxy<IDemo>().GetUserInfo("jackie");
        }
    }
}
