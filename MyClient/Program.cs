using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;

namespace MyClient
{
    class Program
    {
        // Note this client should be run as a console app from the command prompt
        // Or create a seperate Console App Solution and paste the code
        static IHubProxy proxy;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting.....");
            // We use a Hub Connection as it is more functional than the connection it is a subclass of connection
            HubConnection connection = new HubConnection("http://localhost:52037");

            // A local proxy represenatation of the Hub in Question. 
            // Note the server in this example has two hubs
            // better control can be exhibited by using a Proxy for the server
            proxy = connection.CreateHubProxy("ChatHub");

            // Fires whenever a json package is sent from the Hub Server
            connection.Received += connection_Received;
            connection.StateChanged += Connection_StateChanged;

            connection.Start().Wait();
            connection.ConnectionId = "Client 1";



            // subscribe to the send message of the Chat Hub
            // We define an action with the paramater types that are expected formt the hub method
            Action<string, string> MessageRecieved = recieved_a_message;
            // We link the Method name on the Server to the Local Method that responds to the message 
            // recieved from the hub
            // The result is that recieved_a_message will get called when the hub issues a Send message to
            // This client. NOTE the name of the message that the client 
            proxy.On("broadcastMessage", MessageRecieved);
            


            string input = null;
            //as long as something can be read from console
            //it will be sent to the server
            while((input = Console.ReadLine()) != null)
            {
                // Note this envokes the Send method defined on the ChatHub
                // Which leads to a connection_recieved method being fired See below
                proxy.Invoke("Send", new object[] { connection.ConnectionId, input});
                
            }
        }

        private static void recieved_a_message(string sender, string message)
        {
            Console.WriteLine("recieved from {0} message {1}", sender, message);
        }

        // This method just checks the connection state
        private static void Connection_StateChanged(StateChange state)
        {
            Console.WriteLine("State Changed {0}", state.NewState.ToString());
            if (state.NewState == ConnectionState.Disconnected)
            {
                System.Environment.Exit(0);
            }

        }
        // This method just displays raw data returned from the connection object as a json package
        static void connection_Received(string data)
        {
            Console.WriteLine(data);
        }
    }
}
