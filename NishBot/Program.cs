using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iris.Irc;
using System.Data.SQLite;
using System.IO;

namespace NishBot
{
    class Program
    {
        private static Dictionary<string, Client> ircClient = new Dictionary<string, Client>();
        private static Dictionary<Client, ircSettings> iSettings = new Dictionary<Client, ircSettings>();

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
                    case "a":
                        if (msg.Contains(' '))
                        {
                            string[] settings = msg.Split(' ');
                            if (settings.Length >= 3)
                            {
                                Client c = ircClient[settings[1]];
                                ircSettings i = iSettings[c];

                                i.auth.Add(settings[2]);
                                iSettings[c] = i;
                            }
                        }
                        break;
                    case "j":
                        if (msg.Contains(' '))
                        {
                            string[] settings = msg.Split(' ');
                            if (settings.Length >= 3)
                            {
                                Client c = ircClient[settings[1]];

                                c.Join(settings[2]);

                                Console.WriteLine("Joining channel " + settings[2] + " on " + settings[1]);
                            }
                            else
                            {
                                Console.WriteLine("Invalid parameters! eg: 'j client channel'");
                            }
                        }
                        break;

                    case "l":
                        if (msg.Contains(' '))
                        {
                            string[] settings = msg.Split(' ');
                            if (settings.Length >= 3)
                            {
                                Client c = ircClient[settings[1]];

                                c.Leave(settings[2]);

                                Console.WriteLine("Leaving channel " + settings[2] + " on " + settings[1]);
                            }
                            else
                            {
                                Console.WriteLine("Invalid parameters! eg: 'l client channel'");
                            }
                        }
                        break;
                    case "c":
                        if (msg.Contains(' '))
                        {
                            string server = msg.Split(' ')[1];
                            IrcConnection irc = new IrcConnection(new ServerDetails() { Port = 6667, Address = server, Name = server.Split('.')[1] });
                            Client c = new Client(irc, new ClientConfig() { Nickname = "Nish", Password = "", UserMode = 0, Username = "Nishtown" });
                            c.Message += c_Message;
                            c.PrivateMessage += c_PrivateMessage;
                            c.Connected += c_Connected;

                            ircSettings i = new ircSettings();
                            i.auth = new List<string>();
                            i.auth.Add("Nishtown");
                            i.server = irc.Server.Address;
                            i.log = false;
                            i.greet = false;

                            ircClient.Add(irc.Server.Name, c);
                            iSettings.Add(c, i);
                            
                            
                            Thread clientThread = new Thread((ParameterizedThreadStart)((object delay) => c.Run((Action)delay)));
                            clientThread.Name = irc.Server.Name;
                            clientThread.IsBackground = true;
                            clientThread.Start((Action)(() => Thread.Sleep(100)));
                        }
                        break;

                    case "d":
                        if (msg.Contains(' '))
                        {
                            string[] settings = msg.Split(' ');
                            Client c = ircClient[settings[1]];
                            c.Stop();
                        }
                        break;

                    default:
                        Console.WriteLine("Invalid chat command");
                        break;
                }
            }
        }

        static void c_Connected(Client sender)
        {
            Console.WriteLine(iSettings[sender].server + " connected as '" + iSettings[sender].server.Split('.')[1] + "'!");
            Console.Write("> ");
        }


        static void c_PrivateMessage(Client sender, Iris.Irc.ServerMessages.PrivateMessage privateMessage)
        {
            ircSettings i = iSettings[sender];
            string user = privateMessage.User.Split('!')[0];
            string rcpt = privateMessage.Recipient;
            if (privateMessage.Recipient == sender.Config.Nickname)
            {
                rcpt = user;
            }

            string db = i.server.Split('.')[1] + ".db";
            string tbl = @"CREATE TABLE IF NOT EXISTS seen (
                            sDATE TEXT  NULL,
                            sCHANNEL TEXT   NULL,
                            sUSER TEXT  NULL,
                            sMSG TEXT   NULL)";
            if (File.Exists(db))
            {
                SQLiteConnection sql = new SQLiteConnection("data source=" + db);
                
                sql.Open();
                using(SQLiteCommand com = new SQLiteCommand(sql))
                {
                    string message = privateMessage.Message;
                    if (message.Contains('\''))
                    {
                        message= message.Replace("\'", "");
                    }
                    com.CommandText = tbl;
                    com.ExecuteNonQuery();
                    com.CommandText = "INSERT INTO seen (sDATE, sCHANNEL, sUSER, sMSG) SELECT '" + DateTime.Now.ToString() + "','" +
                        rcpt + "','" + user + "','" + message + "' WHERE NOT EXISTS(SELECT 1 FROM seen WHERE sUSER='" + user + "')";
                    com.ExecuteNonQuery();
                    com.CommandText = "UPDATE seen SET sDATE='" + DateTime.Now.ToString() + "',sCHANNEL='" + rcpt + "',sUSER='" + user + "',sMSG='" + message + "' WHERE sUSER='" + user + "'";
                    com.ExecuteNonQuery();
                }
                sql.Close();
                sql.Dispose();
            }
            else
            {
                SQLiteConnection.CreateFile(db);
                SQLiteConnection sql = new SQLiteConnection("data source=" + db);
                
                sql.Open();
                using (SQLiteCommand com = new SQLiteCommand(sql))
                {
                    com.CommandText = tbl;
                    com.ExecuteNonQuery();
                }
                sql.Close();
                sql.Dispose();
            }


            if (privateMessage.Message.StartsWith("."))
            {

                string cmd = privateMessage.Message;
                string msg = "";

                if (privateMessage.Message.Contains(' '))
                {
                    cmd = privateMessage.Message.Substring(0, privateMessage.Message.IndexOf(' '));
                    msg = privateMessage.Message.Substring(cmd.Length + 1, privateMessage.Message.Length - cmd.Length - 1);
                }

                switch (cmd.ToUpper())
                {
                    case ".JOIN":
                        if (msg.Contains(' '))
                        {
                            msg = msg.Split(' ')[0];
                        }
                        sender.Join(msg);
                        break;
                    case ".PART":
                        if (msg.Contains(' '))
                        {
                            sender.Leave(msg.Split(' ')[0]);
                        }
                        else
                        {
                            if (rcpt.StartsWith("#"))
                            {
                                sender.Leave(rcpt);
                            }
                        }
                        break;
                    case ".WAVE":
                        sender.Send(rcpt, "o/");
                        break;

                    case ".SEEN":
                        if (check_auth(sender, user))
                        {
                            if (File.Exists(db))
                            {
                                if (msg.Contains(' '))
                                {
                                    msg = msg.Split(' ')[0];
                                }
                                SQLiteConnection sql = new SQLiteConnection("data source=" + db);
                                sql.Open();
                                using (SQLiteCommand com = new SQLiteCommand(sql))
                                {
                                    com.CommandText = "SELECT * FROM seen WHERE sUSER='" + msg + "'";
                                    using(SQLiteDataReader res = com.ExecuteReader())
                                    {
                                        if (res.HasRows)
                                        {
                                            while (res.Read())
                                            {
                                                string sUser = res["sUSER"].ToString();
                                                string sChannel = res["sCHANNEL"].ToString();
                                                string sMsg = res["sMSG"].ToString();
                                                string sDate = res["sDATE"].ToString();
                                                if (sChannel.StartsWith("#") == false)
                                                {
                                                    sChannel = "PRIVATE";
                                                }
                                                sender.Send(rcpt, "Last seen " + sUser + " in " + sChannel + " at " + sDate + " saying: '" + sMsg + "'");
                                            }
                                        }
                                        else
                                        {
                                            sender.Send(rcpt, msg + " has not been seen :(");
                                        }
                                    }
                                }
                                sql.Close();
                                sql.Dispose();
                            }
                            else
                            {
                                sender.Send(rcpt, msg + " has not been seen :(");
                            }

                        }
                        break;
                    case ".INFO":
                        sender.Send(rcpt, "An IRC BOT Written by Nishtown, Based off IRIS by Banane9");
                        break;

                    case ".ADDAUTH":
                        if (check_auth(sender, user))
                        {
                            
                            if (msg.Contains(' '))
                            {
                                msg = msg.Split(' ')[0];
                            }
                            i.auth.Add(msg);
                            iSettings[sender] = i;
                        }
                        break;

                }
            }
        }

        static void c_Message(Client sender, Iris.Irc.ServerMessages.Message message)
        {
            ircSettings i = iSettings[sender];
            
            if (i.log == true)
            {
                
            }
            Console.WriteLine(message.Line);
            Console.Write("> ");
        }

        static bool check_auth(Client sender, string user)
        {
            foreach (string s in iSettings[sender].auth)
            {
                if (s == user)
                {
                    return true;
                }
            }
            return false;

        }
    }

    class ircSettings
    {
        public bool greet { get; set; }
        public bool log { get; set; }
        public List<string> auth { get; set; }
        public string server { get; set; }
    }
}
