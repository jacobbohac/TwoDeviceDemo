using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


using ppatierno.AzureSBLite;
using ppatierno.AzureSBLite.Messaging;
using Amqp;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BankerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public string ConnectionString = "Endpoint=sb://fiservdemo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=L57YMOEKXBrKEitd5zpOwYypLJmdHtXBz+7PrSQABiw=";
        public ServiceBusConnectionStringBuilder builder;
        public MessagingFactory factory;
        public TopicClient topicClient;
        public SubscriptionClient subClient;
        public MainPage()
        {
            this.InitializeComponent();

            //Amqp.Trace.TraceLevel = Amqp.TraceLevel.Frame | Amqp.TraceLevel.Verbose;
            //Amqp.Trace.TraceListener = (f, a) => Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + Fx.Format(f, a));

            builder = new ServiceBusConnectionStringBuilder(this.ConnectionString);
            builder.TransportType = TransportType.Amqp;
            factory = MessagingFactory.CreateFromConnectionString(this.ConnectionString);
            topicClient = factory.CreateTopicClient("topic1");
            subClient = factory.CreateSubscriptionClient("topic1", "BankerChannel", ReceiveMode.PeekLock);

            factory.Close();

            Task.Run(async () =>
            {
                await RunAsync();
            });
        }

        private void TopicSendClick(object sender, RoutedEventArgs e)
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("Body"));
            BrokeredMessage message = new BrokeredMessage(stream);
            message.Properties["time"] = DateTime.UtcNow;
            message.Properties["message"] = TopicMessage.Text;
            message.Properties["recipient"] = "Customer";

            /// TRYING TO CUSTOM TIMEOUT
            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            int timeOut = 1000; // 1000 ms

            try
            {
                var task = Task.Factory.StartNew(() => topicClient.Send(message), token);
                if (!task.Wait(timeOut, token))
                {
                    Debug.WriteLine("The Task timed out!");
                }
            }
            catch
            {
                Debug.WriteLine("Some kind of exception");
            }



            //try
            //{
            //    topicClient.Send(message);
            //}
            //catch
            //{

            //}

            TopicMessage.Text = String.Empty;
        }

        public async Task RunAsync()
        {
            Debug.WriteLine("RunAsync Called");
            BrokeredMessage message = new BrokeredMessage();
            while (true)
            {
                try
                {
                    Debug.WriteLine("Banker: try message recieve");
                    message = subClient.Receive();

                    if (message != null)
                    {
                        Debug.WriteLine("Banker: message received");

                        try
                        {
                            if (message.Properties["recipient"].Equals("Banker"))
                            {
                                Debug.WriteLine("Banker: message = " + (string)message.Properties["message"]);

                                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                                {
                                    this.TopicReceived.Text = (string)message.Properties["message"];
                                });
                            }
                        }
                        catch
                        {
                            Debug.WriteLine("Banker: checking message.Properties for key failed");
                        }


                        Debug.WriteLine("Banker: try message complete");
                        message.Complete();
                        Debug.WriteLine("Banker: message complete successful");
                    }
                    else
                    {
                        Debug.WriteLine("Banker: message was null");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Banker: Caught Exception: " + e.Message);
                    //message.Complete();
                }
            }
        }
    }
}