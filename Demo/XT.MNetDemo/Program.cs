// See https://aka.ms/new-console-template for more information
using System.Text;
using XT.MNet.Tcp;
using XT.MNet.Tcp.Options;
using XT.MNetDemo;

Console.WriteLine("Hello, World!");

//Parallel.For(4000, 4001, (i) =>
//{
//    ClientService clientService = new ClientService();
//    clientService.Start("127.0.0.1", (ushort)i, i);
//});

Parallel.For(4000, 4001, (i) =>
{
    try
    {
        ServerService serverService = new ServerService();
        serverService.Start("127.0.0.1", (ushort)i, i);
    }
    catch (Exception ex) { }
});





Console.ReadKey();



