﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Dragino.Radio;

namespace Dragino.Lora.Demo.P2P.Sender
{
    public sealed partial class SenderMainPage : Page
    {
        private readonly TimeSpan _sendInterval = TimeSpan.FromSeconds(5);
        private ITransceiver _transceiver;
        private Timer _sendMessageTimer;
        private byte _messageCounter;

        private static TransceiverSettings GetRadioSettings()
        {
            // *********************************************************************************************
            // #1/2. YOUR EDITING IS REQUIRED HERE!
            // 
            // Choose transeiver settings:
            // *********************************************************************************************

            return TransceiverSettings.Standard.Europe868;
        }

        private static TransceiverPinSettings GetPinSettings()
        {
            // *********************************************************************************************
            // #2/2. YOUR EDITING IS REQUIRED HERE!
            // 
            // Depending on the kind of Dragino expansion board you have, uncomment the right line below!
            // *********************************************************************************************


            // EITHER: I have a Dragino LoRa/GPS HAT attached on my Raspberry Pi:
            return TransceiverPinSettings.DraginoLoraGpsHat;


            // OR: I have a Dragino LoRa Arduino Shield connected via wires to the following GPIO pins on my Raspberry Pi:
            //return new TransceiverPinSettings(
            //    25,  // ChipSelect
            //    17,  // Reset
            //    4,   // Dio0
            //    23,  // Dio1 (Optional -- you may use null)
            //    24); // Dio2 (Optional -- you may use null)
        }

        public SenderMainPage()
        {
            InitializeComponent();

            bool initSuccessful = Task.Run(InitLoraTransceiver).ConfigureAwait(false).GetAwaiter().GetResult();

            if (initSuccessful)
            {
                WriteLog("The LoRa transceiver is initiated successfully.");
                _sendMessageTimer = new Timer(SendMessageTimerTick, null, TimeSpan.Zero, _sendInterval);
            }
            else
            {
                WriteLog("The LoRa transceiver failed initiating.");
            }
        }

        private async Task<bool> InitLoraTransceiver()
        {
            try
            {
                _transceiver = await TransceiverFactory.Create(GetRadioSettings(), GetPinSettings()).ConfigureAwait(false);

                _transceiver.OnMessageReceived += TransceiverOnMessageReceived;

                return true;
            }
            catch (Exception exception)
            {
                WriteLog("Failed initiating the LoRa transceiver:\r\n" + exception.Message);
                return false;
            }
        }

        private async void SendMessageTimerTick(object state)
        {
            // Create a random message (change this suiting your needs!):
            _messageCounter++;
            byte[] message = { 0x55, 0xFF, 0x00, 0xAA, _messageCounter };

            // Send it!
            await SendMessage(_transceiver, message, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        private void TransceiverOnMessageReceived(object sender, ReceivedMessageEventArgs e)
        {
            // A message is received -- handle it accordingly!
            ReceivedMessage message = e.Message;

            WriteLog("Message Received: " + message);
        }

        /// <summary>
        /// Send a message with a timeout.
        /// </summary>
        private async Task SendMessage(ITransceiver loraTransceiver, byte[] message, TimeSpan timeout)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                var stopwatch = new Stopwatch(); // Only out of pure interest...
                stopwatch.Start();

                bool successfullySent = await loraTransceiver.Transmit(message, cancellationTokenSource.Token).ConfigureAwait(false);

                stopwatch.Stop();

                WriteLog(successfullySent
                    ? $"Successfully sent in {stopwatch.ElapsedMilliseconds} milliseconds."
                    : $"Failed after {stopwatch.ElapsedMilliseconds} milliseconds.");
            }
        }

        /// <summary>
        /// Send a message with no timeout.
        /// </summary>
        private async Task SendMessage(ITransceiver loraTransceiver, byte[] message)
        {
            var stopwatch = new Stopwatch(); // Only out of pure interest...
            stopwatch.Start();

            bool successfullySent = await loraTransceiver.Transmit(message).ConfigureAwait(false);

            stopwatch.Stop();
            WriteLog(successfullySent
                ? $"Successfully sent in {stopwatch.ElapsedMilliseconds} milliseconds."
                : $"Failed after {stopwatch.ElapsedMilliseconds} milliseconds.");
        }

        private void WriteLog(string text)
        {
            // Simply writing to the Output window:
            Debug.WriteLine(text);
        }
    }
}
