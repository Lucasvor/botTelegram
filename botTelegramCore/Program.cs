using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace botTelegramCore
{
    class Program
    {
        private static readonly TelegramBotClient bot =
            new TelegramBotClient("767214719:AAGJ1glfgnvEl8GVf32gv_jmwLj6dkb8kg0");

        private static ReplyKeyboardMarkup rkm;
        private static int numEst;
        private static StringBuilder sr;

        static List<string> opc = new List<string>
        {
            "AAM","ARS","CPE","CJM","DSA","EED","EXI","FPS","GSE","GTE","INT","JAR","JMG","MJG","MOI","MPA","MPM","MSG","NAX","NDE","NDS","PAS","PPI","PRT","PTU","RSD","SJD","SRA","TSD","VAC","VBA","VMA","VMI","VOA","VRE"
        };
        static void Main(string[] args)
        {
            //readCSV();
            var me = bot.GetMeAsync().Result;
            Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

            rkm = new ReplyKeyboardMarkup();
            var rows = new List<KeyboardButton[]>();
            var cols = new List<KeyboardButton>();

            int i = 1;
            for (var Index = 0; Index < opc.Count; Index++)
            {
                cols.Add(new KeyboardButton(opc[Index]));
                i++;
                if (i % 6 == 0)
                {
                    rows.Add(cols.ToArray());
                    cols = new List<KeyboardButton>();
                }
            }

            rkm.Keyboard = rows.ToArray();
            bot.OnMessage += Bot_OnMessage;
            bot.OnMessageEdited += Bot_OnMessage;
            bot.StartReceiving();
            Console.ReadLine();

        }

        private static void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {

            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                Console.WriteLine($"Message: {e.Message.Text}\nChat: {e.Message.Chat.Id} - Nome: {e.Message.Chat.FirstName}\nContato: {e.Message.Contact}\nData {e.Message.Date.ToLocalTime()}");

                if (e.Message.Text == "sair" &&
                    (DateTime.Now.ToString("hh:mm") == e.Message.Date.ToLocalTime().ToString("hh:mm")))
                {
                    bot.SendTextMessageAsync(e.Message.Chat, "Saindo....");
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                }

                using (var con = new SQLiteConnection("Data Source=banco.db;"))
                {
                    con.Open();

                    if (opc.Contains(e.Message.Text) || opc.Contains(e.Message.Text.Replace("/", "")))
                    {
                        using (var comm = new SQLiteCommand(con))
                        {
                            numEst = opc.IndexOf(e.Message.Text) + 1;
                            comm.CommandText = $"SELECT * from GRAM where codigo = '{numEst}'";
                            using (var reader = comm.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    bot.SendTextMessageAsync(e.Message.Chat, $"Você escolheu:\nEstação: {reader["Estacao"]}\nDescrição: {reader["Descricao"]}\nCABO: {reader["CABO"]}\nSS: {reader["SS"]}\nEndereço: {reader["Endereco"]}");
                                    bot.SendTextMessageAsync(e.Message.Chat, "*Digite o EQN:\nExemplo: 28161 ou 25145-26477*", parseMode: ParseMode.Markdown);
                                }
                            }

                        }
                    }
                    else if (Int32.TryParse(e.Message.Text, out int n))
                    {
                        if (numEst != 0)
                        {
                            if (e.Message.Text.Length == 5)
                            {
                                using (var comm = new SQLiteCommand(con))
                                {
                                    comm.CommandText =
                                        $"SELECT * from EQNS where CodigoGram = '{numEst}' and EQN = '{n}'";
                                    using (var reader = comm.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            while (reader.Read())
                                            {
                                                bot.SendTextMessageAsync(e.Message.Chat,
                                                    $"EQN Digitado: {reader["EQN"]}\nPRIMÁRIO REAL: {reader["PAR"]}");
                                            }
                                        }
                                        else
                                        {
                                            bot.SendTextMessageAsync(e.Message.Chat,
                                                $"EQN Digitado: {e.Message.Text}\nNão Encontrado!");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            bot.SendTextMessageAsync(e.Message.Chat,
                                $"Você não selecionou a estação!");
                        }
                    }
                    else if (e.Message.Text.Length == 11)
                    {
                        if (e.Message.Text.Contains("-"))
                        {
                            var range = e.Message.Text.Split('-');
                            if (Int32.TryParse(range[0], out int esq) &&
                                Int32.TryParse(range[1], out int dir))
                            {
                                sr = new StringBuilder();
                                using (var comm = new SQLiteCommand(con))
                                {
                                    comm.CommandText =
                                        $"SELECT * from EQNS where CodigoGram = '{numEst}' and EQN between '{esq}' and '{dir}'";
                                    using (var reader = comm.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            sr.AppendLine($"EQN Digitado: {e.Message.Text}");
                                            while (reader.Read())
                                            {
                                                sr.AppendLine($"EQN: {reader["EQN"]} - PRIMÁRIO REAL: {reader["PAR"]}");
                                            }

                                            bot.SendTextMessageAsync(e.Message.Chat, $"{sr.ToString()}");
                                        }
                                        else
                                        {
                                            bot.SendTextMessageAsync(e.Message.Chat,
                                                $"EQN Digitado: {e.Message.Text}\nNão Encontrado!");
                                        }

                                    }
                                }
                            }
                        }
                    }
                    else if (e.Message.Text == "/Start" || e.Message.Text == "/start")
                    {
                        //bot.SendTextMessageAsync(e.Message.Chat, "Você disse:\n " + e.Message.Text);
                        sr = new StringBuilder();
                        //foreach (var item in opc)
                        //{
                        //    sr.AppendLine($"/{item}");
                        //}
                        sr.AppendLine("Escolha uma das opções abaixo:");
                        bot.SendTextMessageAsync(e.Message.Chat.Id, sr.ToString(), replyMarkup: rkm);
                    }
                }

            }
        }

        private static void readCSV()
        {
            int cont = 0;
            List<string> ESTACAO = new List<string>()
            {
                "AAM","ARS","CPE","CJM","DSA","EED","EXI","FPS","GSE","GTE","INT","JAR","JMG","MJG","MOI","MPA","MPM","MSG","NAX","NDE","NDS","PAS","PPI","PRT","PTU","RSD","SJD","SRA","TSD","VAC","VBA","VMA","VMI","VOA","VRE"
            };
            using (var con = new SQLiteConnection("Data Source=banco.db;"))
            {
                con.Open();
                using (var reader = new StreamReader("PLANILHA CODIGOS GEORGE.csv"))
                {
                    StringBuilder sr = new StringBuilder();
                    string estacao = null;
                    int numEstacao = 0;
                    while (!reader.EndOfStream)
                    {
                        using (var comm = new SQLiteCommand(con))
                        {
                            var line = reader.ReadLine();
                            var values = line.Split(';');
                            Console.WriteLine(cont++);
                            if (values.Length > 2)
                            {
                                if (values[2] == "ESTAÇÃO")
                                {
                                    estacao = values[3].Substring(0, 3);
                                    numEstacao = ESTACAO.IndexOf(estacao) + 1;
                                }
                                else if (values[2] != "ENDEREÇO")
                                {
                                    if (values[0] != null && values[1] != null)
                                        if ((values[0] != string.Empty || values[0] != null) &&
                                            (values[1] != string.Empty || values[1] != null))
                                        {
                                            comm.CommandText =
                                                $"insert into EQNS(CodigoGram,EQN,PAR) VALUES({numEstacao},'{Convert.ToInt32(values[0])}','{values[1]}');";
                                            comm.ExecuteNonQuery();
                                            sr.AppendLine($"{estacao};{values[0]};{values[1]}");


                                        }

                                    if (values[2] != null && values[3] != null)
                                        if ((values[2] != string.Empty || values[2] != null) &&
                                            (values[3] != string.Empty || values[3] != null))
                                        {
                                            comm.CommandText =
                                                $"insert into EQNS(CodigoGram,EQN,PAR) VALUES({numEstacao},'{Convert.ToInt32(values[2])}','{values[3]}');";
                                            comm.ExecuteNonQuery();
                                            sr.AppendLine($"{estacao};{values[2]};{values[3]}");
                                        }

                                    if (values.Length > 5)
                                        if ((values[4] != string.Empty || values[4] != null) &&
                                            (values[5] != string.Empty || values[5] != null))
                                        {
                                            comm.CommandText =
                                                $"insert into EQNS(CodigoGram,EQN,PAR) VALUES({numEstacao},'{Convert.ToInt32(values[4])}','{values[5]}');";
                                            comm.ExecuteNonQuery();
                                            sr.AppendLine($"{estacao};{values[4]};{values[5]}");
                                        }
                                }
                            }
                            else
                            {
                                comm.CommandText =
                                    $"insert into EQNS(CodigoGram,EQN,PAR) VALUES({numEstacao},'{Convert.ToInt32(values[0])}','{values[1]}');";
                                comm.ExecuteNonQuery();
                                sr.AppendLine($"{estacao};{values[0]};{values[1]}");
                            }
                        }
                    }

                    Console.WriteLine(sr.ToString());
                }
            }

        }

    }
}
