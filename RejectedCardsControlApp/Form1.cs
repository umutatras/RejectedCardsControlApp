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
                    if (readers.Length == 0)
                    {
                        MessageBox.Show(
                            "Hata: Kart okuyucu listesini alırken bir sorun oluştu. Lütfen takılı bir okuyucu var mı kontrol ediniz.",
                            "Hata",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                    if (readers.Length > 0)
                    {
                        portComboBox.SelectedIndex = 0; // Optional: Select the first reader by default
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                            "Hata: " + "Kart okuyucu listesini alırken bir sorun oluştu.",
                            "Hata",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
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

                        if (string.IsNullOrEmpty(atrHex))
                        {
                            MessageBox.Show(
                           "Hata: " + "ATR bilgisi alınamadı.",
                           "Hata",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error
                                );
                            return;
                        }
                        MessageBox.Show(
                      "ATR Bilgisi: " + atrHex,
                      "ATR Bilgisi",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Information
                           );
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                         "Hata: " + "Lütfen kartınızın çipini veya kart okuyucunuzu kontrol ediniz.",
                         "Hata",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error
                              );
            }
        }
        public void SendApduCommand(string readerName)
        {
            if (String.IsNullOrEmpty(okuyucuName))
            {
                MessageBox.Show(
                         "Hata: " + "Lütfen kart okuyucu seçiniz.",
                         "Hata",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error
                              );
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
                            MessageBox.Show(
                         "Hata: " + "Akıllı karta bağlanılamadı: ",
                         "Hata",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error
                              );
                            return;
                        }

                        // Define the APDU command
                        //içerisini sizin byte dizini ile dolduracaksınız.!!
                        byte[] apduCommand = { };

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
                            MessageBox.Show(
                         "Hata: " + "APDU komutu aktarılamadı.",
                         "Hata",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error
                              );
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
                                MessageBox.Show("Hata: " + "APDU komutu aktarılamadı.",
                                                       "Hata",
                                                       MessageBoxButtons.OK,
                                                       MessageBoxIcon.Error
                                                            );
                                return;
                            }
                        }

                        // Process the response
                        string responseHex = BitConverter.ToString(response).Replace("-", "");

                        switch (responseHex)
                        {
                            case "9000":
                                MessageBox.Show("Başarılı: " + "Kart dolu ve işlem başarılı." + responseHex,
                                                "Başarılı",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Information
                                                     );
                                break;
                            case "6A82":
                                MessageBox.Show("Hata: " + "Kart boş veya istenen veri mevcut değil." + responseHex,
                                                "Hata",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Error
                                                     );
                                break;
                            case "6700":
                                MessageBox.Show("Hata: " + "Yanlış uzunluk;daha fazla gösterge yok." + responseHex,
                                                       "Hata",
                                                       MessageBoxButtons.OK,
                                                       MessageBoxIcon.Error
                                                            );
                                break;
                            case "6982":
                                MessageBox.Show("Hata: " + "Güvenlik durumu karşılanmadı." + responseHex,
                                                      "Hata",
                                                      MessageBoxButtons.OK,
                                                      MessageBoxIcon.Error
                                                           );
                                break;
                            case "6A86":
                                MessageBox.Show("Hata: " + "Yanlış p1/p2 parametreleri." + responseHex,
                                                    "Hata",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Error
                                                         );
                                break;
                            case "6883":
                                MessageBox.Show("Hata: " + "Zincirdeki son komut bekleniyor." + responseHex,
                                               "Hata",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Error
                                                    );
                                break;
                            case "6884":
                                MessageBox.Show("Hata: " + "Bu apdu için komut zinciri desteklenmiyor." + responseHex,
                                               "Hata",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Error
                                                    );
                                break;
                            case "6A80":
                                MessageBox.Show("Hata: " + "Yanlış veri." + responseHex,
                                               "Hata",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Error
                                                    );
                                break;
                            case "6D00":
                                MessageBox.Show("Hata: " + "Talimat kodu desteklenmiyor veya geçersiz." + responseHex,
                                               "Hata",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Error
                                                    );
                                break;
                            case "6985":
                                MessageBox.Show("Hata: " + "Kullanım koşulları karşılanmadı (geçerli anahtar yok, özel anahtar zaten mevcut)." + responseHex,
                                               "Hata",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Error
                                                    );
                                break;
                            case "6A84":
                                MessageBox.Show("Hata: " + "Yeterli bellek alanı yok." + responseHex,
                                               "Hata",
                                               MessageBoxButtons.OK,
                                               MessageBoxIcon.Error
                                                    );
                                break;
                            case "6A89":
                                MessageBox.Show("Hata: " + "Dosya hazır mevcut." + responseHex,
                                              "Hata",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Error
                                                   );
                                break;
                            case "6986":
                                MessageBox.Show("Hata: " + "Komuta izin verilmiyor." + responseHex,
                                              "Hata",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Error
                                                   );
                                break;
                            case "6B00":
                                MessageBox.Show("Hata: " + "Yanlış parametreler P1-P2." + responseHex,
                                              "Hata",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Error
                                                   );
                                break;
                            default:
                                MessageBox.Show("Hata: " + "Bilinmeyen yanıt kodu: " + responseHex,
                                              "Hata",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Error
                                                   );
                                break;
                        }

                        reader.Disconnect(SCardReaderDisposition.Unpower);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            okuyucuName = comboBox1.SelectedItem.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ListSmartCardReaders(comboBox1);
        }
    }
}
