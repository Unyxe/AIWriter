using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace AIWriter
{
    public partial class AIWriter : Form
    {
        public AIWriter()
        {
            InitializeComponent();
            TLSHandShake();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(query_txt_box.Text.Length <= 0)
            {
                return;
            }
            
            string message = "<bard_search>\\search\\?auth-fb1f92fce9539c1a723adabc3e9eb875&query-" + ToBase64(query_txt_box.Text);
            Console.WriteLine("[Request] " + message);
            button1.Enabled = false;
            Send(message, 10);
            button1.Enabled = true;
            response_txt_box.Text = last_msg.Replace("\n", Environment.NewLine);
        }










        static TLS_library tls_lib = new TLS_library();
        static byte[] symm_key = null;
        static bool handshake_done = false;

        static public string ips;

        static public string last_msg = "";
        static Random rand = new Random();
        static int connection_id = rand.Next();
        static int listening_port = rand.Next() % 30000 + 3000;

        static string binded_code = @"C:\binding.cs";

        static string url = "http://unyxe.mywire.org:8080";


        public static void TLSHandShake()
        {
            Console.WriteLine("[TLS handshake] Started...");
            Send(tls_lib.GetPublicKey(), 10);
            symm_key = tls_lib.DecryptAssymetric(FromBase64ToByte(last_msg), tls_lib.GetPrivateKey());
            handshake_done = true;
            //Console.WriteLine(ToBase64FromByte(symm_key));
            Console.WriteLine("[TLS handshake] Success!");
        }
        public static void Send(string str, int timeout)
        {
            string d = str;
            string sent = ToBase64(str);
            if (handshake_done)
            {
                sent = tls_lib.EncryptSymmetric(sent, symm_key);
            }
            SendHttp(sent, timeout, d);
        }
        public static void Ping()
        {
            while (true)
            {
                Send(@"<root>\log\", 1);
            }

        }
        public static void SendHttp(string str, int timeout, string display_str)
        {
            HttpContent content = new StringContent(str + "~");

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);
            client.DefaultRequestHeaders.Add("Connection_id", connection_id + "");
            DateTime now = DateTime.Now;
            HttpResponseMessage response;
            try
            {
                response = client.PostAsync(url, content).Result;
            }
            catch { Console.WriteLine("[Response] Failed to send, retry..."); SendHttp(str, timeout, display_str); return; }
            byte[] responseData = response.Content.ReadAsByteArrayAsync().Result;
            TimeSpan diff = DateTime.Now.Subtract(now);
            last_msg = Encoding.ASCII.GetString(responseData);
            if (last_msg.StartsWith("Encryption failed"))
            {
                handshake_done = false;
                Console.WriteLine("[TLS handshake] Key change...");
                TLSHandShake();
                Console.WriteLine("[TLS handshake] Key change finished!");


                Console.WriteLine("[Request] " + display_str);
                Send(display_str, timeout);
                return;
            }
            if (handshake_done)
            {
                last_msg = tls_lib.DecryptSymmetric(last_msg, symm_key);
                Console.Write("[Response] ");
                Console.WriteLine(last_msg);
                Console.Write("    Latency: " + diff.TotalMilliseconds + "ms \n");
            }
        }

        public static string ToBase64(string input)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(input));
        }

        public static string ToBase64FromByte(byte[] input)
        {
            return Convert.ToBase64String(input);
        }
        public static byte[] FromBase64ToByte(string input)
        {
            return Convert.FromBase64String(input);
        }

        public static string FromBase64(string input)
        {
            return Encoding.Default.GetString(Convert.FromBase64String(input));
        }
    }
}
