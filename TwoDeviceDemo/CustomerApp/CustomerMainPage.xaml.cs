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
using Amqp;
using ppatierno.AzureSBLite;
using ppatierno.AzureSBLite.Messaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CustomerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Example3 : Page
    {
        public string ConnectionString = "Endpoint=sb://fiservdemo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=L57YMOEKXBrKEitd5zpOwYypLJmdHtXBz+7PrSQABiw=";
        public ServiceBusConnectionStringBuilder builder;
        public MessagingFactory factory;
        public TopicClient topicClient;
        public SubscriptionClient subClient;
        public Example3()
        {
            this.InitializeComponent();

            //Amqp.Trace.TraceLevel = Amqp.TraceLevel.Frame | Amqp.TraceLevel.Verbose;
            //Amqp.Trace.TraceListener = (f, a) => Debug.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + Fx.Format(f, a));

            builder = new ServiceBusConnectionStringBuilder(this.ConnectionString);
            builder.TransportType = TransportType.Amqp;
            factory = MessagingFactory.CreateFromConnectionString(this.ConnectionString);
            topicClient = factory.CreateTopicClient("topic1");
            
            subClient = factory.CreateSubscriptionClient("topic1", "CustomerChannel", ReceiveMode.PeekLock);

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
            message.Properties["recipient"] = "Banker";

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
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("Customer: Timeout Exception: " + ex.Message);
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
                    Debug.WriteLine("Customer: try message recieve");
                    message = subClient.Receive();

                    if(message != null) {
                        Debug.WriteLine("Customer: message received");

                        try
                        {
                            if (message.Properties["recipient"].Equals("Customer"))
                            {
                                Debug.WriteLine("Customer: message = " + (string) message.Properties["message"]);

                                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                                {
                                    this.TopicReceived.Text = (string) message.Properties["message"];
                                });
                            }
                        }
                        catch
                        {
                            Debug.WriteLine("Customer: checking message.Properties for key failed");
                        }


                        Debug.WriteLine("Customer: try message complete");
                        message.Complete();
                        Debug.WriteLine("Customer: message complete successful");
                    }
                    else
                    {
                        Debug.WriteLine("Customer: message was null");
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine("Customer: Caught Exception: " + e.Message);
                    //message.Complete();
                }
            }
        }

        private void BasicExampleClick(object sender, RoutedEventArgs e)
        {
            topicClient.Close();
            subClient.Close();
            this.Frame.Navigate(typeof (MainPage));
        }

        private void ContinuousClick(object sender, RoutedEventArgs e)
        {
            topicClient.Close();
            subClient.Close();
            this.Frame.Navigate(typeof (Example2View));
        }
    }
}