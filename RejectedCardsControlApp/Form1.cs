using PCSC;
using PCSC.Utils;


using System;
using System.Linq;
using System.Windows.Forms;

namespace RejectedCardsControlApp
{
    public partial class Form1 : Form
    {

        public Form1()
        {

            InitializeComponent();
            ListSmartCardReaders(comboBox1);

        }
        string okuyucuName = "";
        private void button1_Click(object sender, EventArgs e)
        {
            GetCardAtr(okuyucuName);
            SendApduCommand(okuyucuName);
        }
        public void ListSmartCardReaders(ComboBox portComboBox)
        {
            try
            {
                using (var context = new SCardContext())
                {
                    context.Establish(SCardScope.System);

                    // Get the list of readers
                    var readers = context.GetReaders();

                    portComboBox.Items.Clear();
                    portComboBox.Items.AddRange(readers.ToArray());
                    if (readers.Length > 0)
                    {
                        portComboBox.SelectedIndex = 0; // Optional: Select the first reader by default
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + "Kart okuyucu listesini alırken bir sorun oluştu.");
            }
        }

        public void GetCardAtr(string readerName)
        {
            try
            {
                using (var context = new SCardContext())
                {
                    context.Establish(SCardScope.System);

                    using (var reader = new SCardReader(context))
                    {
                        reader.Connect(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                        var atr = reader.GetAttrib(SCardAttribute.AtrString, out byte[] atrout);
                        var atrHex = BitConverter.ToString(atrout).Replace("-", "");

                        if(string.IsNullOrEmpty(atrHex))
                        {
                            MessageBox.Show("ATR bilgisi alınamadı.");
                            return;
                        }
                        MessageBox.Show("ATR Bilgisi: " + atrHex);
                    }
                 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + "Lütfen kartınızı veya kart okuyucunuzu kontrol ediniz.");
            }
        }
        public void SendApduCommand(string readerName)
        {
            if(String.IsNullOrEmpty(okuyucuName))
            {
                MessageBox.Show("Lütfen kart okuyucu seçiniz.");                
            }
            try
            {
                using (var context = new SCardContext())
                {
                    context.Establish(SCardScope.System);

                    using (var reader = new SCardReader(context))
                    {
                        var rc = reader.Connect(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                        if (rc != SCardError.Success)
                        {
                            MessageBox.Show("Akıllı karta bağlanılamadı: " + SCardHelper.StringifyError(rc));
                            return;
                        }

                        // Define the APDU command
                        byte[] apduCommand = { 0x00, 0xA4, 0x04, 0x0C, 0x06, 0xFF, 0x54, 0x41, 0x43, 0x48, 0x4F };

                        // Use a smaller initial buffer
                        byte[] response = new byte[256];
                        uint responseLength = (uint)response.Length;

                        var sendPci = SCardPCI.GetPci(reader.ActiveProtocol);
                        var receivePci = new SCardPCI();

                        rc = reader.Transmit(
                            sendPci,    // Protocol Control Information (T0, T1 or Raw)
                            apduCommand, // command APDU
                            receivePci, // returning Protocol Control Information
                            ref response); // data buffer for the answer

                        if (rc != SCardError.Success)
                        {
                            MessageBox.Show("APDU komutu aktarılamadı: " + SCardHelper.StringifyError(rc));
                            return;
                        }

                        // Check if response buffer was large enough
                        if (responseLength > response.Length)
                        {
                            // Resize the buffer to fit the actual response length
                            response = new byte[responseLength];

                            // Transmit the command again
                            rc = reader.Transmit(
                                sendPci,    // Protocol Control Information (T0, T1 or Raw)
                                apduCommand, // command APDU
                                receivePci, // returning Protocol Control Information
                                ref response); // data buffer for the answer

                            if (rc != SCardError.Success)
                            {
                                MessageBox.Show("APDU komutu aktarılamadı: " + SCardHelper.StringifyError(rc));
                                return;
                            }
                        }

                        // Process the response
                        string responseHex = BitConverter.ToString(response).Replace("-", "");
                        MessageBox.Show("APDU Yanıtı: " + responseHex);

                     

                            if (responseHex == "9000")
                            {
                                MessageBox.Show("Kart dolu ve işlem başarılı.");
                            }
                            else if (responseHex == "6A82")
                            {
                                MessageBox.Show("Kart boş veya istenen veri mevcut değil.");
                            }
                            else
                            {
                                MessageBox.Show("Bilinmeyen yanıt kodu: " + responseHex);
                            }
                      

                        reader.Disconnect(SCardReaderDisposition.Unpower);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            okuyucuName = comboBox1.SelectedItem.ToString();
        }
    }
}
