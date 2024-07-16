using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XT.MNet.Tcp;
using XT.MNet.Tcp.Options;

namespace XT.MNetDemo
{
    public class ClientService
    {
        int _flag = 0;
        public void Start(string ip , ushort port,int flag=0)
        {
            var client = new TcpClient(new TcpClientOptions()
            {
                Address = ip,
                Port = port,
                
                
                
            });
            _flag = flag;
            client.OnDisconnect += Client_OnDisconnect;
            client.OnConnect += Client_OnConnect;

            client.On(async(data) =>
            {
              
                    
                    var str = Encoding.UTF8.GetString(data.Span);

                    Console.WriteLine($"{DateTime.Now} {_flag} Get Data from Server {str},Total Length {str.Length}");

               

                 await Task.Delay(5000);
                string msg = $"{DateTime.Now}";

                for(int i = 0; i <= 2000; i++)
                {
                    msg += i;
                }
                msg += "#";
                var bytes=Encoding.UTF8.GetBytes(msg);
                client.Send(bytes);
                Console.WriteLine($"{DateTime.Now} {_flag} Send Data ,Total Length {msg.Length}");


            });

            client.Connect();
            var msg = "send from client#";
            var bytes = Encoding.UTF8.GetBytes(msg);
            client.Send(bytes);
        }

        void Client_OnConnect()
        {
           
            
            Console.WriteLine($"{DateTime.Now} {_flag} OnConnect");
        }

        void Client_OnDisconnect()
        {
            Console.WriteLine($"{DateTime.Now} {_flag} DisConnect");
        }

    }
  

}
