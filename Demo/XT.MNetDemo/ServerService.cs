using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XT.MNet.Tcp;
using XT.MNet.Tcp.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace XT.MNetDemo
{
    public class ServerService
    {
        int _flag = 0;
        public void Start(string ip, ushort port, int flag = 0)
        {
            var server = new TcpServer(new TcpServerOptions()
            {
                Address = ip,
                Port = port
                
                


            });
            _flag = flag;
            server.OnDisconnect += Server_OnDisconnect;
            server.OnConnect += Server_OnConnect; 

           

            server.On(async (data,a) =>
            {

               
                var str = Encoding.UTF8.GetString(data.Span);

                Console.WriteLine($"{DateTime.Now} {_flag} Get Data from client {str},Total Length {str.Length}");

                await Task.Delay(5000);

            
                var send= $"{DateTime.Now} ";
                for (int i = 0; i <= 2000; i++)
                {
                    send += i;
                }
                send += "#";
                var bytes = Encoding.UTF8.GetBytes(send);
                a.Send(bytes);

                Console.WriteLine($"{DateTime.Now} {_flag} Send Data ,Total Length {send.Length}");

            });

            server.Start();
        }

        private void Server_OnConnect(TcpServerConnection connection)
        {
            Console.WriteLine($"{DateTime.Now} {_flag} OnConnect");
        }

        private void Server_OnDisconnect(TcpServerConnection connection)
        {
            Console.WriteLine($"{DateTime.Now} {_flag} OnDisConnect");
        }


        CancellationTokenSource _cancel = new CancellationTokenSource();
        public void StartServerHandle(string ip,int port,int flag = 0)
        {
            LgTcpServerHandle lgTcpServer = new LgTcpServerHandle();
            lgTcpServer.StartListen(ip, port,_cancel.Token);

            lgTcpServer.ConnectionRequestEvent += LgTcpServer_ConnectionRequestEvent;
            lgTcpServer.RecievedEvent += LgTcpServer_RecievedEvent;
            lgTcpServer.StateChangeEvent += LgTcpServer_StateChangeEvent;
        }

        private void LgTcpServer_StateChangeEvent(object? sender, LgTcpState e)
        {
            if(e==LgTcpState.Connected)
            {
                Console.WriteLine("Connected");

            }else if (e == LgTcpState.Closed)
            {
                Console.WriteLine("DisConnect");
            }
        }

        private async void LgTcpServer_RecievedEvent(object? sender, byte[] e)
        {
            var str = Encoding.UTF8.GetString(e);
            Console.WriteLine($"{DateTime.Now} b Get Data from client {str},Total Length {str.Length}");

            var server = sender as LgTcpServerHandle;
            if (server != null)
            {

                await Task.Delay(5000);


                var send = $"{DateTime.Now} ";
                for (int i = 0; i <= 2000; i++)
                {
                    send += i;
                }
                send += "#";
                var bytes = Encoding.UTF8.GetBytes(send);
                await server.Send(bytes);

                Console.WriteLine($"{DateTime.Now} {_flag} Send Data ,Total Length {send.Length}");
            }
        }

        private void LgTcpServer_ConnectionRequestEvent(object? sender, System.Net.Sockets.Socket e)
        {
            var server = sender as LgTcpServerHandle; 
            if (server != null)
            {
                
                server.AddAccept(e);
            }
        }
    }
}
