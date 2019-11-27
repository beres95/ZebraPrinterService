using Roys.Connect;
using Roys.Generic.SQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Zebra.Sdk.Comm;
using System.Text;


namespace ZebraPrintService
{
    class Program
    {
        

        public static Connection thePrinterConn;

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static void VerifyDir(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)
                {
                    dir.Create();
                }
            }
            catch (Exception e)
            {
                Logger(e.ToString());
            }
        }

        public static void Logger(string lines)
        {
            string path = "ZebraPrinter/Logs";
            VerifyDir(path);
            string fileName = "_.txt";
            try
            {
                StreamWriter file = new StreamWriter(path + fileName, true);
                file.WriteLine(DateTime.Now.ToString() + " - " + lines);                
                file.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        
        private static void PortListener()
        {
            

            TcpListener server = null;
            
            string printerName = "";

            try
            {

                int port = 9100;
                string localIP = GetLocalIPAddress();
                

                try
                {

                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                    {
                        Console.WriteLine(localIP);
                        server = new TcpListener(IPAddress.Parse(localIP), port);                                                                                           
                    }

                   
                }
                catch (SocketException e)
                {
                    Logger("Socket Exception - " + e.ToString());
                }

                // Start listening for client requests.                
                server.Start();
                
                
                // Buffer for reading data
                Byte[] bytes = new Byte[1024];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    
                    Console.Write("Waiting for a connection... ");


                    // Perform a blocking call to accept requests.  
                    
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();
                    int i;
                    StringBuilder sb = new StringBuilder();

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {

                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i); // message that gets sent over
                        
                        foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) //compares printer name that gets sent to ones connected to comp.
                        {
                            
                            if (data.Contains(printer))
                            {
                                printerName = printer;
                                
                            }
                        }

                        Console.WriteLine("printer name: " + printerName);

                        if (printerName == "")
                        {
                            Logger("No Printer Name Found");
                        }
                       
                        sb.Append(data.ToUpper()+"\n");
                        
                        Console.WriteLine(data+"\n");
                        Console.WriteLine("---------------------------------------------------------------");                                                  
                    }

                    if (sb.Length > 0)
                    {
                        
                        //byte[] msg = Encoding.ASCII.GetBytes(sb.ToString());

                        Console.WriteLine("Attempting To Print . . .");                                                

                        try
                        {
                            // Create file 
                            File.AppendAllText("Labels.zpl", sb.ToString());

                            // Copy file to printer
                            System.IO.File.Copy("Labels.zpl", $@"\\{localIP}\{printerName}");

                            // Clear file
                            string empty = "";
                            File.WriteAllText("Labels.zpl", empty);
                            

                            //thePrinterConn.Write(msg);

                        }
                        catch (ConnectionException e)
                        {
                            // Handle communications error here.
                            Logger("Connection Exception - " + e.ToString());
                            throw e;

                        }
                        finally
                        {
                            // Close the connection to release resources.
                            //thePrinterConn.Close();
                        }
                    }

                    client.Close();
                    
                                      
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                Logger("Socket Exception - " + e.ToString());
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }


            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }



        static void Main(string[] args)
        {
            PortListener();           
        }


    }

}
