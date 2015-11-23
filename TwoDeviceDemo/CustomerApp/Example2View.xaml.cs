using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CustomerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Example2View : Page
    {
        public string ConnectionString = "Endpoint=sb://fiservdemo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=L57YMOEKXBrKEitd5zpOwYypLJmdHtXBz+7PrSQABiw=";
        public ServiceBusConnectionStringBuilder builder;
        public MessagingFactory factory;
        public TopicClient topicClient;
        public SubscriptionClient subClient;
        public Example2View()
        {
            this.InitializeComponent();

            builder = new ServiceBusConnectionStringBuilder(this.ConnectionString);
            builder.TransportType = TransportType.Amqp;
            factory = MessagingFactory.CreateFromConnectionString(this.ConnectionString);
            topicClient = factory.CreateTopicClient("topic3");
            subClient = factory.CreateSubscriptionClient("topic3", "sub1", ReceiveMode.PeekLock);


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

            topicClient.Send(message);
        }

        private void TopicReceiveClick(object sender, RoutedEventArgs e)
        {



        }

        public async Task RunAsync()
        {
            Debug.WriteLine("RunAsync Called");
            BrokeredMessage message = new BrokeredMessage();
            while (true)
            {
                try
                {
                    message = subClient.Receive();

                    Debug.WriteLine("\t message = " + (string) message.Properties["message"]);

                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        this.TopicReceived.Text = (string) message.Properties["message"];
                    });

                    message.Complete();
                }
                catch
                {
                    message.Complete();
                    Debug.WriteLine("Caught Exception");
                }
            }
        }

        private void TwoDeviceClick(object sender, RoutedEventArgs e)
        {
            topicClient.Close();
            subClient.Close();
            this.Frame.Navigate(typeof (Example3));
        }

        private void Example1Click(object sender, RoutedEventArgs e)
        {
            topicClient.Close();
            subClient.Close();
            this.Frame.Navigate(typeof (MainPage));
        }
    }
}