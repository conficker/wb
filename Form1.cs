using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using System.Windows.Controls;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Threading;
using PuppeteerSharp;
using System.Drawing;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;

namespace wb_scan
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class Form1 : Form
    {
        private static readonly Encoding LocalEncoding = Encoding.UTF8;
        

        int count = 0;
        string[] proxys = new string[] {}; //System.IO.File.ReadAllLines("proxy.txt");
        static string id_token = "";
        string admin_id = "";

        public Form1()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
            

            checkbox_proxy.Checked = Properties.Settings.Default.enable_proxy;
            token_api.Text = Properties.Settings.Default.token_api;
            id_send.Text = Properties.Settings.Default.id_send;
            procent_user.Value = Properties.Settings.Default.procent_user;
            time_out_ui.Value = Properties.Settings.Default.time_out;
            all_sels.Checked = Properties.Settings.Default.filtr_all;
            only_wb.Checked = Properties.Settings.Default.filtr_wb;
            check_threads.Checked = Properties.Settings.Default.enable_threads;
            if (Properties.Settings.Default.list_catalog == null)
                Properties.Settings.Default.list_catalog = new System.Collections.Specialized.StringCollection();
            foreach (var i in Properties.Settings.Default.list_catalog)
                listbox_category.Items.Add(i);


            //CheckForIllegalCrossThreadCalls = false;
            load_items();
            load_proxy();
            load_base();
            load_cookie();
            set_headers();
            set_headers_ios();
        }

        db db = new db();

        public void load_items()
        {

            var user_array = db.Execute("SELECT * FROM items", 6);

            foreach (var i in user_array)
            {
                if (items.Keys.Contains(i[0]) == false)
                {
                    items.Add(i[0], new string[5]);

                    items[i[0]][0] = i[1];
                    items[i[0]][1] = i[2];
                    items[i[0]][2] = i[3];
                    items[i[0]][3] = i[4];
                    items[i[0]][4] = i[5];
                }
            }
        }
        public void load_base()
        {

            var user_array = db.Execute("SELECT * FROM users", 4);

            foreach (var i in user_array)
            {
                if (Users.Keys.Contains(i[0]) == false)
                {
                    Users.Add(i[0], new User());

                    Users[i[0]].telegram_id = i[0];
                    Users[i[0]].articul = i[1];
                    Users[i[0]].proxy_text = i[2];
                    Users[i[0]].phone = i[3];
                }
            }
        }
        public void load_proxy()
        {
            List<string> result = new List<string>();

            var user_array = db.Execute("SELECT * FROM proxy", 4);

            foreach (var i in user_array)
            {
                result.Add(i[0]+":"+ i[1]+":"+i[2]+":"+ i[3]);
            }

            proxys = result.ToArray();
        }

        public Dictionary<string, User> Users = new Dictionary<string, User>();

  
        //private static byte[] Decompress(byte[] compressed)
        //{
        //    using var from = new MemoryStream(compressed);
        //    using var to = new MemoryStream();
        //    using var gZipStream = new GZipStream(from, CompressionMode.Decompress);
        //    gZipStream.CopyTo(to);
        //    return to.ToArray();
        //}
        public static Telegram.Bot.TelegramBotClient bot;
        xNetStandard.CookieDictionary Cookie_ = new xNetStandard.CookieDictionary();
        GetPost get_post = new GetPost();
        xNetStandard.RequestParams data = new xNetStandard.RequestParams();
        Match BDC_VCID_smsCodeRequest;
        Dictionary<string, string> Headers = new Dictionary<string, string>();
        Dictionary<string, string> Headers_ios = new Dictionary<string, string>();


        public async void start_tg_bot()
        {
            admin_id = id_send.Text;
            ManualResetEvent manual = new ManualResetEvent(false);
            string token = "5874482564:AAHh2DxnHhYC4uqgVG-shDFWlArsLTUdDKk";// Console.ReadLine();
            bot = new TelegramBotClient(token);
            var me = await bot.GetMeAsync();
            // Console.WriteLine(
            //    "Hello, World! I am user {" + me.Id + "} and my name is {" + me.FirstName + "}."
            // );

            timer1_enable = true;
            timer2_enable = true;

            timer1_Tick(null, null);
            timer2_Tick(null, null);

            var cts = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            bot.StartReceiving(
           new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
           cts.Token);

                while (true)
                {
                    bool ee = cts.Token.IsCancellationRequested;

                    if (ee == true)
                    {
                        cts = new CancellationTokenSource();
                    bot.StartReceiving(
                        new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                        cts.Token);
                        //System.IO.File.AppendAllText("1.txt","1\r\n");
                    }

                    Thread.Sleep(1000);
                }
                //Console.WriteLine("Start listening for @{" + me.Username + "}");
                //manual.WaitOne();
                cts.Cancel();
        }

        string process_admin = "";

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            if (update.Type != UpdateType.Message)
                return;
            if (update.Message.Type != MessageType.Text && update.Message.Type != MessageType.Contact)
                return;

            if(update.Message.Type == MessageType.Contact)
            {
                update.Message.Text = "contact";
            }

            //try
            //{

                var chatId = update.Message.Chat.Id;

            string text = "";

            if (update.Message.Text != null)
            {                    
                text = update.Message.Text;


                if (chatId.ToString() == admin_id)
                {
                    if (text == "/start")
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Нажмите на одну из кнопок:", replyMarkup: greate_buttons(new string[] { "Добавить список прокси", "Удалить весь список прокси" }, 1));
                    }
                    else if (text == "Добавить список прокси")
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Пришлите прокси в формате ip:port:user:pass пример ниже:\r\n127.0.0.1:88:user:pass\r\n127.0.0.1:88:user:pass");
                        process_admin = "add_proxy";
                    }
                    else if (text == "Удалить весь список прокси")
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Прокси успешно удалены.");
                        proxys = new string[] { };
                        db.Insert(
                                       "DELETE FROM proxy"
                                 );
                    }
                    else if (process_admin == "add_proxy")
                    {
                        if (text.IndexOf("\r\n") > -1)
                        {
                            proxys = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        else if (text.IndexOf("\n") > -1)
                        {
                            proxys = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        else if (text.IndexOf("\r") > -1)
                        {
                            proxys = text.Split(new string[] { "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        }

                        db.Insert(
                                       "DELETE FROM proxy"
                                 );

                        for (int i = 0; i < proxys.Length; i++)
                        {
                            db.Insert("INSERT INTO proxy (ip,port,user,pass)" +
                                      "SELECT '" + proxys[i].Split(':')[0] + "','" + proxys[i].Split(':')[1] + "','" + proxys[i].Split(':')[2] + "','" + proxys[i].Split(':')[3] + "'");
                        }

                        process_admin = "";

                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Прокси успешно добавлены.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Не знаю такой вариант.");
                    }
                }
                else
                {
                    if (Users.Keys.Contains(chatId.ToString()) == false)
                    {
                        Users.Add(chatId.ToString(), new User());
                        db.Insert("INSERT INTO users (telegram_id_users)" +
                                  "SELECT '" + chatId.ToString() + "' WHERE NOT EXISTS(SELECT 1 FROM users WHERE telegram_id_users = '" + chatId.ToString() + "')");
                    }

                    if (text == "/start")
                    {
                        var keyboard = new ReplyKeyboardMarkup(new[]
                                                              {
                                                                new []
                                                                {
                                                                    new KeyboardButton("Поделиться телефоном")
                                                                     {
                                                                        RequestContact = true
                                                                     }
                                                                }
                                                         });
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Пройдите авторизацию отправив номер телефона", replyMarkup: keyboard);
                    }
                    else if (text == "contact")
                    {
                        Users[chatId.ToString()].phone = update.Message.Contact.PhoneNumber;
                        db.Insert(
                                     "UPDATE users " +
                                     "SET phone = '" + Users[chatId.ToString()].phone + "'" +
                                     " WHERE telegram_id_users = '" + chatId.ToString() + "'"
                                     );


                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Нажмите на одну из кнопок:", replyMarkup: greate_buttons(new string[] { "Добавить артикул", "Удалить артикул" }, 1));
                    }
                    else if (text == "Добавить артикул")
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Введите номер артикула:");
                        Users[chatId.ToString()].process = "add_art";

                    }
                    else if (text == "Удалить артикул")
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Введите номер артикула:");
                        Users[chatId.ToString()].process = "remove_art";
                    }
                    else if (Users[chatId.ToString()].process == "add_art")
                    {
                        Users[chatId.ToString()].articul += "|" + text + "|";
                        db.Insert(
                                        "UPDATE users " +
                                        "SET articul = '" + Users[chatId.ToString()].articul + "'" +
                                        " WHERE telegram_id_users = '" + chatId.ToString() + "'"
                                  );

                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Артикул поставлен в очередь на отслеживание.");
                        Users[chatId.ToString()].process = "";
                    }
                    else if (Users[chatId.ToString()].process == "remove_art")
                    {
                        if (Users[chatId.ToString()].articul.IndexOf("|" + text + "|") > -1)
                        {
                            Users[chatId.ToString()].articul = Users[chatId.ToString()].articul.Replace("|" + text + "|", "");
                            db.Insert(
                                            "UPDATE users " +
                                            "SET articul = '" + Users[chatId.ToString()].articul + "'" +
                                            " WHERE telegram_id_users = '" + chatId.ToString() + "'"
                                      );

                            await botClient.SendTextMessageAsync(update.Message.From.Id, "Артикул удален.");
                            Users[chatId.ToString()].process = "";
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(update.Message.From.Id, "Артикул не найден, повторите ввод.");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(update.Message.From.Id, "Не знаю такой вариант.");
                    }
                }


            }
            //}
            //catch (Exception x)
            //{

            //}
        }

        public ReplyKeyboardMarkup greate_buttons(string[] bt, int row)
        {
            List<KeyboardButton[]> hh = new List<KeyboardButton[]>();

            for (int i = 0; i < bt.Length;)
            {
                List<KeyboardButton> ff = new List<KeyboardButton>();

                for (int y = 0; y < row; y++)
                {
                    if (i < bt.Length)
                    {
                        ff.Add(bt[i]);
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                hh.Add(ff.ToArray());
            }

            var replyKeyboardMarkup = new ReplyKeyboardMarkup(hh) { ResizeKeyboard = true };

            return replyKeyboardMarkup;
        }
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                Task ErrorMessage = new Task(null);
                //var ErrorMessage = exception switch
                //{
                //    ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                //    _ => exception.ToString()
                //};

                //Console.WriteLine(ErrorMessage);
            }
            catch { }
            return Task.CompletedTask;
        }
        public void set_headers()
        {

            Headers = new Dictionary<string, string>();
            Headers.Add("Accept", "*/*");
            Headers.Add("Accept-Language", "en-US");
            Headers.Add("x-o3-app-name", "dweb_client");
            Headers.Add("Referer", "https://www.wildberries.ru");
            Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36");
            Headers.Add("x-o3-app-version", "release_1-1'-'2022_c21f5039");
            Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            Headers.Add("sec-ch-ua-mobile", "?0");
            Headers.Add("sec-ch-ua", "\" Not;A Brand\";v=\"99\", \"Google Chrome\";v=\"97\", \"Chromium\";v=\"97\"");
        }

        public void set_headers_ios()
        {

            Headers_ios = new Dictionary<string, string>();
            Headers_ios.Add("Accept", "*/*");
            Headers_ios.Add("Accept-Language", "ru-RU;q=1.0, en-RU;q=0.9");
            Headers_ios.Add("devicename", "iOS, iPhone8,1");
            Headers_ios.Add("wb-appstate", "1");
            Headers_ios.Add("User-Agent", "Wildberries/5.6.6000 (RU.WILDBERRIES.MOBILEAPP; build:5045395; iOS 14.6.0) Alamofire/5.4.3");
            Headers_ios.Add("WB-AppLanguage", "ru");
            Headers_ios.Add("XPosition", "0");
            Headers_ios.Add("Wb-AppType", "ios");
            Headers_ios.Add("Wb-AppVersion", "566");
            Headers_ios.Add("Site-Locale", "ru");
            
        }
        private void button5_Click(object sender, EventArgs e)
        {

            string result = "";

            xNetStandard.RequestParams Params;



            string proxy_server = "";
            string user = "";
            string pass = "";

            Cookie_ = new xNetStandard.CookieDictionary();
            //string result28 = get_post.GET_string("https://www.wildberries.ru/security/login?",ref Cookie_, proxy_server, true, false,"", user, pass, Headers);
            //Thread.Sleep(5000);
            data = new xNetStandard.RequestParams();
            data.Add(new KeyValuePair<string, string>("phoneMobile", phone.Text.Replace("+", "")));


            string result29 = get_post.Post_response3("https://www.wildberries.ru/webapi/security/spa/checkcatpcharequirements?forAction=EasyLogin", data, proxy_server, false, ref Cookie_, true, user, pass, Headers);

            if (result29.IndexOf("showCaptcha\":false") > -1)
            {
                MessageBox.Show("Нажмите ок .и ждите 60 сек...");
                data = new xNetStandard.RequestParams();
                data.Add(new KeyValuePair<string, string>("phoneInput.AgreeToReceiveSmsSpam", "true"));
                data.Add(new KeyValuePair<string, string>("phoneInput.ConfirmCode", ""));
                data.Add(new KeyValuePair<string, string>("phoneInput.FullPhoneMobile", phone.Text.Replace("+", "")));
                data.Add(new KeyValuePair<string, string>("returnUrl", "https%3A%2F%2Fwww.wildberries.ru%2F"));
                data.Add(new KeyValuePair<string, string>("phonemobile", phone.Text.Replace("+", "")));
                data.Add(new KeyValuePair<string, string>("agreeToReceiveSms", "true"));
                data.Add(new KeyValuePair<string, string>("shortSession", "false"));
                data.Add(new KeyValuePair<string, string>("period", "ru"));


                string result31 = get_post.Post_response3("https://www.wildberries.ru/webapi/lk/mobile/requestconfirmcode?forAction=EasyLogin", data, proxy_server, false, ref Cookie_, true, user, pass, Headers);

                Thread.Sleep(65000);
                result29 = get_post.Post_response3("https://www.wildberries.ru/webapi/security/spa/checkcatpcharequirements?forAction=EasyLogin", data, proxy_server, false, ref Cookie_, true, user, pass, Headers);
            }

            Match Url = Regex.Match(result29, "\"imageSrc\":\"(.+?)\"");
            BDC_VCID_smsCodeRequest = Regex.Match(result29, "\"instanceId\":\"(.+?)\"");

            if (Url.Success == true)
            {
                MemoryStream result30 = new MemoryStream();
                MemoryStream outputStream = new MemoryStream();

                for (int i = 0; i < 1; i++)
                {
                    DateTimeOffset dto = new DateTimeOffset(DateTime.UtcNow);
                    string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();
                    data = new xNetStandard.RequestParams();
                    result30 = get_post.Post_response_steam("https://www.wildberries.ru/" + Url.Groups[1].Value + "&_=" + unixTimeMilliSeconds, data, proxy_server, false, ref Cookie_, true, user, pass, Headers);
                }



                //File.WriteAllBytes("ca.jpg",result30.ToArray());

                var bitmap = new Bitmap(result30);

                //using (var stream = result30)
                //{

                //    bitmap.BeginInit();
                //    bitmap.StreamSource = stream;
                //    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //    bitmap.EndInit();
                //    bitmap.Freeze();
                //}

                var bitmap2 = new Bitmap(bitmap, 143, 56);
                capcha.Image = bitmap2;
                //capcha.BackgroundImageLayout = ImageLayout.Stretch;

                Match Message = Regex.Match(result29, "\"Value\":\"(.+?)\"");

                if (Message.Success == true)
                {
                    MessageBox.Show(Message.Groups[1].Value);
                }

                tab_control.SelectedIndex = 1;
                // var dsd = Encoding.UTF8.GetBytes(MemoryStream);

                //var sad = Encoding.UTF8.GetString(Decompress(result30.ToArray()));
                //var fdsf = UnZip(result30);

                //var settingsString = LocalEncoding.GetString(result30.ToArray());

                //System.IO.File.WriteAllText("c.jpeg",result30);
            }
            else
            {
                MessageBox.Show("Ошибка при авторизации");
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (BDC_VCID_smsCodeRequest != null && BDC_VCID_smsCodeRequest.Success == true)
            {
                data = new xNetStandard.RequestParams();
                data.Add(new KeyValuePair<string, string>("phoneInput.AgreeToReceiveSmsSpam", "true"));
                data.Add(new KeyValuePair<string, string>("phoneInput.ConfirmCode", ""));
                data.Add(new KeyValuePair<string, string>("phoneInput.FullPhoneMobile", phone.Text.Replace("+", "")));
                data.Add(new KeyValuePair<string, string>("returnUrl", "https%3A%2F%2Fwww.wildberries.ru%2F"));
                data.Add(new KeyValuePair<string, string>("phonemobile", phone.Text.Replace("+", "")));
                data.Add(new KeyValuePair<string, string>("agreeToReceiveSms", "true"));
                data.Add(new KeyValuePair<string, string>("shortSession", "false"));
                data.Add(new KeyValuePair<string, string>("period", "ru"));
                data.Add(new KeyValuePair<string, string>("smsCaptchaCode", input_capcha.Text));
                data.Add(new KeyValuePair<string, string>("BDC_VCID_smsCodeRequest", BDC_VCID_smsCodeRequest.Groups[1].Value));

                string result31 = get_post.Post_response3("https://www.wildberries.ru/webapi/lk/mobile/requestconfirmcode?forAction=EasyLogin", data, "", false, ref Cookie_, true, "", "", Headers);
                Match Message = Regex.Match(result31, "\"Value\":\"(.+?)\"");

                if (Message.Success == true)
                {
                    MessageBox.Show(Message.Groups[1].Value);
                    tab_control.SelectedIndex = 2;
                }
                else
                {
                    MessageBox.Show("Ошибка");
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {

            if (BDC_VCID_smsCodeRequest != null && BDC_VCID_smsCodeRequest.Success == true)
            {
                data = new xNetStandard.RequestParams();
                data.Add(new KeyValuePair<string, string>("confirmCode", pinbox.Text));
                data.Add(new KeyValuePair<string, string>("phonemobile", phone.Text.Replace("+", "")));

                string result31 = get_post.Post_response3("https://www.wildberries.ru/webapi/lk/user/checkconfirmcode?forAction=EasyLogin", data, "", false, ref Cookie_, true, "", "", Headers);

                Match Message = Regex.Match(result31, "\"Value\":\"(.+?)\"");

                if (Message.Success == true)
                {
                    MessageBox.Show(Message.Groups[1].Value);
                }
                if (result31.IndexOf("\"ResultState\":0") > -1)
                {
                    data = new xNetStandard.RequestParams();
                    data.Add(new KeyValuePair<string, string>("phoneInput.AgreeToReceiveSmsSpam", "true"));
                    data.Add(new KeyValuePair<string, string>("phoneInput.ConfirmCode", pinbox.Text));
                    data.Add(new KeyValuePair<string, string>("phoneInput.FullPhoneMobile", phone.Text.Replace("+", "")));
                    data.Add(new KeyValuePair<string, string>("returnUrl", "https%3A%2F%2Fwww.wildberries.ru%2F"));
                    data.Add(new KeyValuePair<string, string>("phonemobile", phone.Text.Replace("+", "")));
                    data.Add(new KeyValuePair<string, string>("agreeToReceiveSms", "true"));
                    data.Add(new KeyValuePair<string, string>("shortSession", "false"));
                    data.Add(new KeyValuePair<string, string>("period", "ru"));

                    string result40 = get_post.Post_response3("https://www.wildberries.ru/webapi/security/spa/signinsignup", data, "", false, ref Cookie_, true, "", "", Headers);


                    var keys_cookie = Cookie_.Select(x => String.Format("{0}={1}", x.Key, x.Value));
                    var cookie_str = String.Join(";", keys_cookie);
                    System.IO.File.WriteAllText("cookie.txt", cookie_str);
                    System.Threading.Thread.Sleep(500);
                    load_cookie();
                    MessageBox.Show("Авторизация успешно!");
                }

            }
        }

        object proxy_lock = new Object();
        Dictionary<string, string> all_items = new Dictionary<string, string>();
        List<Thread> threads = new List<Thread>();
        List<string> check = new List<string>();

        public void prev_proxy(ref string proxy_server, ref string user, ref string pass)
        {
            lock (proxy_lock)
            {
                proxy_server = "";
                user = "";
                pass = "";

                bool? proxy_check = false;

                proxy_check = checkbox_proxy.Checked;

                if (proxy_check == true)
                {
                    if (proxys.Length > 0)
                    {
                        count = count - 2;

                        if (count >= proxys.Length)
                            count = 0;
                        if (count < 0)
                            count = 0;

                        string pr_ar = proxys[count];

                        string[] pr = pr_ar.Split(':');

                        if (pr.Length > 2)
                        {
                            proxy_server = pr_ar.Split(':')[0] + ":" + pr_ar.Split(':')[1];
                            user = pr_ar.Split(':')[2];
                            pass = pr_ar.Split(':')[3];
                        }
                        else
                        {
                            proxy_server = pr_ar.Split(':')[0] + ":" + pr_ar.Split(':')[1];
                        }

                        count++;

                    }
                }
            }
        }
        public void next_proxy(ref string proxy_server, ref string user, ref string pass)
        {
            lock (proxy_lock)
            {
                proxy_server = "";
                user = "";
                pass = "";

                try
                {
                    if (proxys.Length > 0)
                    {
                        if (count >= proxys.Length)
                            count = 0;
                        string pr_ar = proxys[count];

                        string[] pr = pr_ar.Split(':');

                        if (pr.Length > 2)
                        {
                            proxy_server = pr_ar.Split(':')[0] + ":" + pr_ar.Split(':')[1];
                            user = pr_ar.Split(':')[2];
                            pass = pr_ar.Split(':')[3];
                        }
                        else
                        {
                            proxy_server = pr_ar.Split(':')[0] + ":" + pr_ar.Split(':')[1];
                        }


                        count++;
                    }
                }
                catch
                {

                }
            }
        }

        public void load_cookie()
        {
            string file_name = "cookie.txt";

            if (System.IO.File.Exists(file_name))
            {
                string text = System.IO.File.ReadAllText(file_name);

                string[] key_value_line = text.Split(';');

                Cookie_ = new xNetStandard.CookieDictionary();

                foreach (var key_value_current in key_value_line)
                {
                    string[] key_value = key_value_current.Split('=');

                    if (key_value.Length > 1)
                    {
                        string key = key_value[0];
                        string value = key_value[1];

                        Cookie_.Add(key, value);
                    }
                }
            }
            else
            {

            }
        }

        public string get_filtr()
        {
            string filtr_url = "";

            bool? only_wb_ = false;
            only_wb_ = only_wb.Checked;

            if (only_wb_ == true)
            {
                filtr_url = "&fsupplier=-100";
            }

            return filtr_url;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.list_catalog.Remove(listbox_category.SelectedItem.ToString());
                Properties.Settings.Default.Save();
                listbox_category.Items.Remove(listbox_category.SelectedItem);
            }
            catch { }
        }



        Thread main_thread; 
        bool processed = false;

        private void button2_Click(object sender, EventArgs e)
        {
            timer1_Tick(null, null);
            timer1.Start();

            if (check_log.Checked == true)
            {
                log.Text += "Идет скачивание браузера. \r\n";
            }


            // timer.Start();
            // timer_Tick(null, null);

            if (main_thread != null)
            {
                main_thread.Abort();
                main_thread.Join();
            }

            main_thread = new Thread((new ThreadStart(async () =>
            {
                //await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                string token_api_ = "";
                token_api_ = token_api.Text;

                bot = new Telegram.Bot.TelegramBotClient(token_api_);

                log.Text += "Запуск..\r\n";

                stop = false;
                var category_urls = listbox_category.Items;

                bool? check_threads_ = false;
                check_threads_ = check_threads.Checked;

                processed = false;

                do
                {
                    if (stop == true)
                    {
                        break;
                    }

                    foreach (var url in category_urls)
                    {
                        if (stop == true)
                        {
                            break;
                        }

                        int page = 1;
                        bool null_item_page = false;
                        string last_url = "";

                        string start_url = "";
                        string subject_url = "";
                        //&subject=(.\S+)
                        //Match url_regex = Regex.Match(url.ToString(), @"subject=(.\S+)&|subject=(.\S+)");
                        Match url_regex = Regex.Match(url.ToString(), @"&subject=(.\S+)");
                        Match url_regex2 = Regex.Match(url.ToString(), @"https:(.+?)\?");

                        if (url_regex.Success)
                        {
                            subject_url = url_regex.Value;
                            //if(url_regex.Groups[2].Value.Length > 1)
                            //{
                            //    subject_url = "&subject=" + url_regex.Groups[2].Value;
                            //}
                            //if (url_regex.Groups[1].Value.Length > 1)
                            //{
                            //    subject_url = "&subject=" + url_regex.Groups[1].Value;
                            //}
                        }

                        if (url_regex2.Success)
                        {
                            start_url = url_regex2.Value;
                        }

                        do
                        {

                            try
                            {
                                //throw new Exception("Длина имени меньше 2 символов");
                                string proxy_server = "";
                                string user = "";
                                string pass = "";

                                next_proxy(ref proxy_server, ref user, ref pass);

                                string str_page = "";

                                if (url.ToString().IndexOf("?") == -1)
                                {
                                    str_page = "?page=";
                                }
                                else
                                {
                                    str_page = "&page=";
                                }

                                string url_full = start_url+spp+subject_url + str_page + page + get_filtr();
                                //Match spp_match = Regex.Match(url_full, @"spp=(\d+)");

                                //if (spp_match.Success == true)
                                //{
                                //    url_full = url_full.Replace(spp_match.Value, "spp=" + spp);
                                //}


                                //string result = get_post.GET_string(url_full, ref Cookie_, proxy_server, true, false, "", user, pass, Headers);
                                string result = get_content(url_full, proxy_server, user, pass, Cookie_);

                                page++;
                                Data_thread data = new Data_thread() { proxy_server = proxy_server, user = user, pass = pass };


                                if (result.IndexOf("\"id\"") > -1 && result.IndexOf("\"salePriceU\"") > -1)
                                {



                                    if (check_threads_ == true)
                                    {
                                        if (last_url != "")
                                        {
                                            data.last_url = last_url;
                                            data.end = false;

                                            run_thread(data);

                                            last_url = url_full;
                                        }
                                        else
                                        {
                                            last_url = url_full;
                                        }
                                    }
                                    else
                                    {
                                        update_category(url_full, data.proxy_server, data.user, data.pass, Cookie_, data.end, false, result);
                                    }
                                }
                                else
                                {
                                    if (check_threads_ == true)
                                    {
                                        prev_proxy(ref proxy_server, ref user, ref pass);
                                        data = new Data_thread() { proxy_server = proxy_server, user = user, pass = pass };
                                        data.last_url = last_url;
                                        data.end = true;

                                        run_thread(data);
                                    }

                                    null_item_page = true;
                                }



                                if (check_threads_ == false)
                                {
                                    double time_out_ = 0;
                                    time_out_ = Convert.ToDouble(time_out_ui.Value);
                                    Thread.Sleep(TimeSpan.FromSeconds(Convert.ToInt32(time_out_)));
                                }
                            }
                            catch (Exception x)
                            {
                                try
                                {
                                    //System.IO.File.AppendAllText("error.txt", x.Message + "           " + x.StackTrace + "\r\n\r\n");
                                }
                                catch { }

                            }
                        }
                        while (!null_item_page);

                    }

                    processed = true;
                } while (check_threads_ == false);//

            })));
            main_thread.IsBackground = true;
            main_thread.Start();
        }

        public void run_thread(Data_thread data1)
        {
            Data_thread data = new Data_thread() { last_url = data1.last_url, proxy_server = data1.proxy_server, user = data1.user, pass = data1.pass, end = data1.end };
            threads.Add(new Thread(new ThreadStart(() => { update_category(data.last_url, data.proxy_server, data.user, data.pass, Cookie_, data.end); })));
            threads.Last().IsBackground = true;
            threads.Last().Start();
        }

        bool stop = false;
        string text = "";

        List<Browser> all_browsers = new List<Browser>();
        List<Browser> main_browser = new List<Browser>();


        public async Task<string> pupp_get_content(string url, string proxy_server, string user, string pass, xNetStandard.CookieDictionary cookie, Browser brow = null)
        {
            string result = "";
            Browser br;

            if (brow == null)
            {
                main_browser = new List<Browser>();

                br = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = false,
                    Args = proxy_server == "" ? new[] { "" } : new[] { "--proxy-server=" + proxy_server }
                });
                main_browser.Add(br);
            }
            else
            {
                br = brow;
            }

            // var all_page = await br.PagesAsync();
            var page = await br.NewPageAsync();

            if (user != "" && pass != "")
            {
                await page.AuthenticateAsync(new Credentials() { Username = user, Password = pass });
            }
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.54 Safari/537.36");

            CookieParam[] hhh = new CookieParam[cookie.Count];
            int yy = 0;

            foreach (var o in cookie)
            {
                hhh[yy] = new CookieParam() { Domain = ".wildberries.ru", Name = o.Key, Value = o.Value };
                yy++;
            }

            await page.SetCookieAsync(hhh);
            //await page.WaitForSelectorAsync("div[class=edit-profile]");
            int count_error = 0;

            await page.SetRequestInterceptionAsync(true);
            page.Request += (s, er) =>
            {
                er.Request.ContinueAsync();
                return;
            };
            page.Response += async (s, er) =>
            {
                try
                {
                    throw new Exception("Длина имени меньше 2 символов");
                    if (er.Response.Url.IndexOf("catalog.wb.ru/catalog/") > -1 && er.Response.Url.IndexOf("/filters") == -1)// 
                    {
                        result = await er.Response.TextAsync();
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                catch
                {
                    return;
                }
            };
        gg:
            count_error++;
            await page.GoToAsync(url);

            //Thread.Sleep(1000);

            Response wait = null;
            int count_wait = 0;





            while (wait == null)
            {
                if (result.Length > 0)
                {
                    break;
                }
                //wait = await page.WaitForNavigationAsync(new NavigationOptions() { Timeout = 10000, WaitUntil = new WaitUntilNavigation[] { WaitUntilNavigation.Load } });
                Thread.Sleep(1000);
                count_wait++;

                if (count_wait > 10)
                {
                    goto gg;
                }
                if (count_error > 2)
                {
                    break;
                }
            }


            await page.CloseAsync();

            if (brow == null)
            {
                await page.CloseAsync();
                //await br.CloseAsync();
            }
            else
            {

            }


            return result;

        }



        public string get_content(string url, string proxy_server, string user, string pass, xNetStandard.CookieDictionary cookie, Browser brow = null)
        {
            string result = "";

            data = new xNetStandard.RequestParams();
            string result40 = get_post.GET_string(url, ref Cookie_, "", false , false, "", "","", Headers_ios);
            return result40;

            return result;
        }
        public async void update_category(string url, string proxy_server, string user, string pass, xNetStandard.CookieDictionary cookie, bool end, bool endlessly = true, string html = "")
        {
            Browser browser = null;

            if (html == "")
            {

                //browser = await Puppeteer.LaunchAsync(new LaunchOptions
                //{
                //    Headless = false,
                //    Args = proxy_server == "" ? new[] { "" } : new[] { "--proxy-server=" + proxy_server }
                //});
                //all_browsers.Add(browser);


            }

            while (true)
            {
                try
                {
                   
                    string result = "";
                    Match page_match = Regex.Match(url, @"page=(\d+)");
                    string page_number = "";

                    if (page_match.Success == true)
                    {
                        page_number = page_match.Groups[1].Value.Replace("&", "");
                    }

                    if (html != "")//
                    {
                        result = html;
                    }
                    else
                    {

                        //await page.WaitForSelectorAsync("div[class=edit-profile]");

                        string start_url = "";
                        string subject_url = "";

                        Match url_regex = Regex.Match(url.ToString(), @"&subject=(.\S+)");
                        Match url_regex2 = Regex.Match(url.ToString(), @"https:(.+?)\?");

                        if (url_regex.Success)
                        {
                            subject_url = url_regex.Value;
                        }

                        if (url_regex2.Success)
                        {
                            start_url = url_regex2.Value;
                        }

                        url = start_url + spp + subject_url;

                        result = get_content(url, proxy_server, user, pass, cookie, browser);
                        
                        //Match spp_match = Regex.Match(url, @"spp=(\d+)");

                        //if (spp_match.Success == true)
                        //{
                        //    url = url.Replace(spp_match.Value, "spp=" + spp);
                        //}
                        //result = get_post.GET_string(url, ref cookie, proxy_server, true, false, "", user, pass, Headers);
                    }

                    string[] items = result.Split('{');
                    bool null_item_page = false;
                    string count_item_log = "";

                    MatchCollection item_price_log = Regex.Matches(result, "\"salePriceU\":(.+?),");

                    if (item_price_log != null && item_price_log.Count != null)
                    {
                        count_item_log = item_price_log.Count.ToString();
                    }

                    //string text = "Проверяем  " + url.Split('?')[0] + ", страница = " + page_number + ", Найдено = " + count_item_log + " шт. \r\n";


                            if (check_log.Checked == true)
                            {
                                log.Text += "Проверяем  " + url + ", страница = " + page_number + ", Найдено = " + count_item_log + " шт. \r\n";
                            }


                    //if (text.Length > 800)
                    //{
                    //    text = "";
                    //}

                    //text += "Проверяем  " + url.Split('?')[0] + ", страница = " + page_number + ", Найдено = " + count_item_log + " шт. \r\n";

                    //log.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new
                    //                     Action(() =>
                    //                     {
                    //                         log.Text = text
                    //                     ;
                    //                     }));

                    foreach (var item in items)
                    {
                        Match item_id = Regex.Match(item, "\"id\":(.+?),");
                        Match item_price = Regex.Match(item, "\"salePriceU\":(.+?),");
                        Match item_name = Regex.Match(item, "\"name\":\"(.+?)\"");
                        string new_item = "";

                        if (item_id.Success == true && item_price.Success == true)
                        {
                            if (all_items.Keys.Contains(item_id.Groups[1].Value) == false)
                            {
                                all_items.Add(item_id.Groups[1].Value, item_price.Groups[1].Value);

                                if (processed && checkBox1.Checked)
                                {
                                    new_item = "Новый товар!\r\n";
                                }

                            }
                            else
                            {
                                long last_price = 0;
                                long new_price = 0;
                                long srednee_chislo = 0;
                                double user_procent_ = 0;

                                long.TryParse(all_items[item_id.Groups[1].Value], out last_price);
                                long.TryParse(item_price.Groups[1].Value, out new_price);

                                if (last_price > 0 && new_price > 0)
                                {
                                    long aas = new_price * 100;

                                    double procent_up = aas / last_price;
                                    double procent_down = 100 - procent_up;

                                    // Or use this.Dispatcher if this method is in a window class.

                                    user_procent_ = Convert.ToDouble(procent_user.Value);

                                    if (check.Contains(item_id.Groups[1].Value + ":" + new_price.ToString()) == false)
                                    {
                                        if (procent_down >= user_procent_)
                                        {
                                            check.Add(item_id.Groups[1].Value + ":" + new_price.ToString());
                                            string name_item = "";

                                            if (item_name.Success == true)
                                            {
                                                name_item = item_name.Groups[1].Value;
                                            }

                                            string id_send_bot = "";
                                            id_send_bot = id_send.Text;

                                            //try
                                            //{
                                            //await bot.SendTextMessageAsync(666895677,
                                            //    "Название: " + name_item + "\r\nЦена сейчас/была: " + new_price + "/" + last_price + "\r\nСсылка: https://www.wildberries.ru/catalog/" + item_id.Groups[1].Value + "/detail.aspx?targetUrl=GP");
                                            string new_price_string = new_price.ToString();
                                            string last_price_string = last_price.ToString();

                                            if (new_price_string.Length > 1)
                                            {
                                                new_price_string = new_price_string.Substring(0, new_price_string.Length - 2);
                                            }

                                            if (last_price_string.Length > 1)
                                            {
                                                last_price_string = last_price_string.Substring(0, last_price_string.Length - 2);
                                            }

                                            await bot.SendTextMessageAsync(id_send_bot,new_item+
                                                name_item + "\r\n" + "https://www.wildberries.ru/catalog/" + item_id.Groups[1].Value + "/detail.aspx?targetUrl=GP \r\n" + new_price_string + "₽ / " + last_price_string + "₽ (" + procent_down + "%)");

                                            //}
                                            //catch(Exception x)
                                            //{

                                            //}

                                            //


                                        }
                                        
                                        srednee_chislo = ((last_price + new_price) / 2);
                                            all_items[item_id.Groups[1].Value] = srednee_chislo.ToString();
                                        //
                                    }


                                }


                                //;
                            }
                        }
                    }

                    int time_out = 0;
                    time_out = Convert.ToInt32(time_out_ui.Value);

                    if (endlessly == true)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(time_out));
                    }

                    if (!endlessly || stop == true)
                    {
                        break;
                    }
                }
                catch (Exception x)
                {
                    try
                    {
                        //System.IO.File.AppendAllText("error.txt", x.Message + "           " + x.StackTrace + "\r\n\r\n");
                    }
                    catch
                    {

                    }
                }
            }

            if (browser != null)
                await browser.CloseAsync();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1_enable = false;
            timer2_enable = false;
            label13.Text = "Остановлено.";
            stop = true;
            log.Text += "Остановка.. \r\n";
            all_browsers = new List<Browser>();
            main_browser = new List<Browser>();
            // timer.Stop();
        }


        private void check_threads_Checked(object sender, EventArgs e)
        {
            Properties.Settings.Default.enable_threads = check_threads.Checked == true ? true : false;
            Properties.Settings.Default.Save();
        }

        private void checkbox_proxy_Unchecked(object sender, EventArgs e)
        {
            Properties.Settings.Default.enable_proxy = checkbox_proxy.Checked == true ? true : false;
            Properties.Settings.Default.Save();
        }


        private void log_TextChanged(object sender, EventArgs e)
        {
        }

        private async void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
          ////  new Thread(new ThreadStart(async () =>
          // // {
          //      stop = true;
          //      for (int y = 0; y < all_browsers.Count; y++)
          //      {
          //          try
          //          {
          //              await all_browsers[y].CloseAsync();
          //          }
          //          catch { }
          //      }
          //      for (int y = 0; y < main_browser.Count; y++)
          //      {
          //          try
          //          {
          //              await main_browser[y].CloseAsync();
          //          }
          //          catch
          //          {

          //          }
          //      }

          //      System.Diagnostics.Process.GetCurrentProcess().Kill();
          //  //})).Start();
        }

        //public string get_personal_info()
        //{
        //    data = new xNetStandard.RequestParams();
        //    string result40 = get_post.GET_string("https://marketing-info.wildberries.ru/marketing-info/api/v4/info?curr=rub", ref Cookie_, "", false, false, "", "", "", Headers_ios);
        //    // get_post.Post_response3("https://marketing-info.wildberries.ru/marketing-info/api/v4/info?curr=rub", data, "", false, ref Cookie_, true, "", "", Headers);
        //    return result40;
        //}

        public string get_personal_info()
        {
            data = new xNetStandard.RequestParams();
            string result40 = get_post.Post_response3("https://www.wildberries.ru/webapi/personalinfo", data, "", false, ref Cookie_, true, "", "", Headers);
            return result40;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string result40 = get_personal_info();
            Match FullPhone = Regex.Match(result40, "\"formattedPhoneMobile\":\"(.+?)\"");

            if (FullPhone.Success)
            {
                MessageBox.Show("Номер: " + FullPhone.Groups[1].Value);
            }
            else
            {
                MessageBox.Show("Ошибка!");
            }

        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {


            //System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void checkbox_proxy_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.enable_proxy = checkbox_proxy.Checked == true ? true : false;
            Properties.Settings.Default.Save();
        }

        private void token_api_TextChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.token_api = token_api.Text;
            Properties.Settings.Default.Save();
        }

        private void id_send_TextChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.id_send = id_send.Text;
            Properties.Settings.Default.Save();
        }

        private void procent_user_ValueChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.procent_user = Convert.ToInt32(procent_user.Value);
            Properties.Settings.Default.Save();
        }

        private void time_out_ui_ValueChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.time_out = Convert.ToInt32(time_out_ui.Value);
            Properties.Settings.Default.Save();
        }

        private void all_sels_CheckedChanged(object sender, EventArgs e)
        {
            if (all_sels.Checked)
            {
                Properties.Settings.Default.filtr_all = true;
                Properties.Settings.Default.filtr_wb = false;
                Properties.Settings.Default.Save();
            }
        }

        private void only_wb_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.filtr_all = false;
            Properties.Settings.Default.filtr_wb = true;
            Properties.Settings.Default.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listbox_category.Items.Add(text_category.Text);
            Properties.Settings.Default.list_catalog.Add(text_category.Text);
            Properties.Settings.Default.Save();
            text_category.Text = "";
        }

        private void check_log_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void check_threads_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.enable_threads = check_threads.Checked == true ? true : false;
            Properties.Settings.Default.Save();
        }

        private void log_TextChanged_1(object sender, EventArgs e)
        {
            if (log.Text.Length > 30000)
            {
                log.Text = log.Text.Remove(0, 25000);

            }


            log.SelectionStart = log.TextLength;
            log.ScrollToCaret();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           Thread ddf = new Thread(new ThreadStart(async () =>
             {
            stop = true;
            for (int y = 0; y < all_browsers.Count; y++)
            {
                try
                {
                    await all_browsers[y].CloseAsync();
                }
                catch { }
            }
            for (int y = 0; y < main_browser.Count; y++)
            {
                try
                {
                    await main_browser[y].CloseAsync();
                }
                catch
                {

                }
            }

            System.Diagnostics.Process.GetCurrentProcess().Kill();
            }));

            ddf.IsBackground = false;
            ddf.Start();
        }

        bool timer1_enable = false;
        bool timer2_enable = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timer1_enable)
            {
                string result40 = get_post.GET_string("https://marketing-info.wildberries.ru/marketing-info/api/v4/info?curr=rub", ref Cookie_, "", false, false, "", "", "", Headers_ios);
                //string result40 = get_personal_info();
                Match FullSpp = Regex.Match(result40, "xClientInfo\":\"(.+?)\"");

                if (FullSpp.Success)
                {

                    spp = FullSpp.Groups[1].Value.Replace("\\u0026", "&");
                }
                else
                {
                    //MessageBox.Show("Ошибка!");
                }
            }
        }
        public string Get_Money(string txt, string name, string raz)
        {
            string value = "";

            int index = txt.IndexOf(name);

            if (index > -1)
            {
                int index2 = txt.IndexOf(raz, index) - index;

                if (index2 > -1 && ((index2 - name.Length) - 1) > -1)
                {
                    value = txt.Substring(index + name.Length, ((index2 - name.Length)));
                    value = value.Replace("\"", "");
                }
            }

            return value;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            label13.Text = "В работе.";
            new Thread(new ThreadStart(() =>
           {              

               start_tg_bot();
           })).Start();
        }

        public Dictionary<string, string[]> items = new Dictionary<string, string[]>();
        static string spp = "0";
        private async void timer2_Tick(object sender, EventArgs e)
        {
            if (timer2_enable)
            {                        
                try
                {
                    for (int i = 0; i < Users.Count; i++)
                    {
                        string art_text = Users.ElementAt(i).Value.articul;
                        string[] art = art_text.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var articul in art)
                        {

                            string proxy_server = "";
                            string user = "";
                            string pass = "";

                            next_proxy(ref proxy_server, ref user, ref pass);

                            string result = get_post.GET_string("https://card.wb.ru/cards/detail?" + spp + "&nm=" + articul, ref Cookie_, proxy_server, false, false, "", user, pass, Headers);

                            string price = Get_Money(result, "\"priceU\":", ",");
                            string price_spp = Get_Money(result, "\"clientPriceU\":", "}");
                            int price_ = 0;
                            int price_spp_ = 0;

                            if (price != "" || price_spp != "")
                            {
                                try
                                {
                                    price_ = int.Parse(price);
                                    price_spp_ = int.Parse(price_spp);
                                }
                                catch
                                {

                                }

                                string price_without_discount = Get_Money(result, "\"priceU\":", ",");//(price_ - price_spp_).ToString();//
                                string discount = Get_Money(result, "\"basicSale\":", ",");
                                string discount_price = Get_Money(result, "\"basicPriceU\":", ",");
                                string spp_ = Get_Money(result, "\"clientSale\":", ",");//token_api.Text;

                                if (items.Keys.Contains(articul) == false)
                                {
                                    items.Add(articul, new string[] { price_without_discount, discount, discount_price, spp_, price_spp });
                                    db.Insert("INSERT INTO items (articul,price_without_discount,discount,discount_price,spp,price_spp,user_tg_id)" +
                                              "SELECT '" + articul + "','" + price_without_discount + "','" + discount + "','" + discount_price + "','" + spp_ + "','" + price_spp + "','" + Users.ElementAt(i).Key.ToString() + "'");
                                }
                                else
                                {
                                    if (items[articul][0] != price_without_discount || items[articul][1] != discount ||
                                        items[articul][2] != discount_price || items[articul][3] != spp_ || items[articul][4] != price_spp)
                                    {
                                        string price_without_discount__ = "0";
                                        string discount__ = "0";
                                        string discount_price__ = "0";
                                        string spp__ = "0";
                                        string price_spp__ = "0";

                                        string price_without_discount_no_null_new = "";
                                        string discount_price_no_null_new = "";
                                        string price_spp_no_null_new = "";

                                        string price_without_discount_no_null_last = "";
                                        string discount_price_no_null_last = "";
                                        string price_spp_no_null_last = "";

                                        if (price_without_discount.Length > 1)
                                        {
                                            price_without_discount_no_null_new = price_without_discount.Substring(0, price_without_discount.Length - 2);
                                        }

                                        if (discount_price.Length > 1)
                                        {
                                            discount_price_no_null_new = discount_price.Substring(0, discount_price.Length - 2);
                                        }

                                        if (price_spp.Length > 1)
                                        {
                                            price_spp_no_null_new = price_spp.Substring(0, price_spp.Length - 2);
                                        }
                                        //--------

                                        if (items[articul][0].Length > 1)
                                        {
                                            price_without_discount_no_null_last = items[articul][0].Substring(0, items[articul][0].Length - 2);
                                        }

                                        if (items[articul][2].Length > 1)
                                        {
                                            discount_price_no_null_last = items[articul][2].Substring(0, items[articul][2].Length - 2);
                                        }

                                        if (items[articul][4].Length > 1)
                                        {
                                            price_spp_no_null_last = items[articul][4].Substring(0, items[articul][4].Length - 2);
                                        }

                                        //******************************************
                                        if (items[articul][0] != price_without_discount)
                                        {
                                            price_without_discount__ = price_without_discount_no_null_last + " -> " + price_without_discount_no_null_new;
                                        }
                                        else
                                        {
                                            price_without_discount__ = price_without_discount_no_null_last;
                                        }

                                        if (items[articul][1] != discount)
                                        {
                                            discount__ = items[articul][1] + " -> " + discount;
                                        }
                                        else
                                        {
                                            discount__ = discount;
                                        }

                                        if (items[articul][2] != discount_price)
                                        {
                                            discount_price__ = discount_price_no_null_last + " -> " + discount_price_no_null_new;
                                        }
                                        else
                                        {
                                            discount_price__ = discount_price_no_null_last;
                                        }

                                        if (items[articul][3] != spp_)
                                        {
                                            spp__ = items[articul][3] + " -> " + spp_;
                                        }
                                        else
                                        {
                                            spp__ = spp_;
                                        }

                                        if (items[articul][4] != price_spp)
                                        {
                                            price_spp__ = price_spp_no_null_last + " -> " + price_spp_no_null_new;
                                        }
                                        else
                                        {
                                            price_spp__ = price_spp_no_null_last;
                                        }


                                        await bot.SendTextMessageAsync(Users.ElementAt(i).Key.ToString(),
                                            "https://www.wildberries.ru/catalog/" + articul + "/detail.aspx\r\nЦена без скидки: " + price_without_discount__ + " р.\r\n" +
                                            "Скидка: " + discount__ + " %\r\n" +
                                            "Цена со скидкой: " + discount_price__ + " р.\r\n" +
                                            "СПП: " + spp__ + " %\r\n" +
                                            "Цена с СПП: " + price_spp__ + " р.\r\n");

                                        items[articul][0] = price_without_discount;
                                        items[articul][1] = discount;
                                        items[articul][2] = discount_price;
                                        items[articul][3] = spp_;
                                        items[articul][4] = price_spp;

                                        db.Insert(
                                                    "UPDATE items " +
                                                    "SET price_without_discount = '" + price_without_discount + "',discount = '" + discount + "', discount_price = '" + discount_price + "', spp = '" + spp_ + "', price_spp = '" + price_spp + "'" +
                                                    " WHERE articul = '" + articul + "'"
                                                 );
                                    }
                                    else
                                    {

                                    }
                                }
                            }

                        }
                    }

                }
                catch (Exception x)
                {
                    System.IO.File.AppendAllText("Error.txt", x.Message + "\r\n\r\n" + x.StackTrace + "\r\n\r\n\r\n\r\n");
                }



            }
        }
    }
    public class User
    {
        public string telegram_id { get; set; }
        public string articul { get; set; }
        public string proxy_text { get; set; }
        public string phone { get; set; }
        public string process { get; set; }
    }
    public class Data_thread
    {
        public string last_url { get; set; }
        public string proxy_server { get; set; }
        public string user { get; set; }
        public string pass { get; set; }
        public bool end { get; set; }
    }

}
