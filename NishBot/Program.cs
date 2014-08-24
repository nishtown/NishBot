using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iris.Irc;

namespace NishBot
{
    class Program
    {
        private static Dictionary<string, Client> ircClient = new Dictionary<string, Client>();

        static void Main(string[] args)
        {
            bool bRunning = true;

            while(bRunning)
            {
                Console.Write("> ");
                string msg = Console.ReadLine();
                if (msg == "exit")
                {
                    bRunning = false;
                }
                else
                {
                    processCommand(msg);
                }
            }

        }

        static void processCommand(string msg)
        {
            if (msg.Length > 0)
            {
                switch (msg.Substring(0, 1).ToLower())
                {
                    case "j":
                        if (msg.Contains(' '))
                        {
                            string[] settings = msg.Split(' ');
                            if (settings.Length >= 3)
                            {
                                Client c = ircClient[settings[1]];

                                c.Join(settings[2]);
                            }
                            else
                            {
                                Console.WriteLine("Invalid parameters! eg: 'j client channel'");
                            }
                        }
                        break;

                    case "l":

                        break;
                    case "c":
                        if (msg.Contains(' '))
                        {
                            string server = msg.Split(' ')[1];
                            IrcConnection irc = new IrcConnection(new ServerDetails() { Port = 6667, Address = server, Name = server.Split('.')[1] });
                            Client c = new Client(irc, new ClientConfig() { Nickname = "Nish", Password = "", UserMode = 0, Username = "Nishtown" });
                            c.Message += c_Message;
                            ircClient.Add(irc.Server.Name, c);
                            Thread clientThread = new Thread((ParameterizedThreadStart)((object delay) => c.Run((Action)delay)));
                            clientThread.Name = irc.Server.Name;
                            clientThread.IsBackground = true;
                            clientThread.Start((Action)(() => Thread.Sleep(100)));
                        }
                        break;

                    case "d":

                        break;

                    default:
                        Console.WriteLine("Invalid chat command");
                        break;
                }
            }
        }

        static void c_Message(Client sender, Iris.Irc.ServerMessages.Message message)
        {
            Console.WriteLine(message.Line);
        }
    }
}
