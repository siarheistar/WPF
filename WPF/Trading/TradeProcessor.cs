using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;

namespace trading_WPF.Trading
{
    class TradeProcessor : MainWindow
    {
        public DateTime date { get; set; }
        public string Symbol { get; set; }
        public double price { get; set; }
        public double dma_50 { get; set; }
        public double dma_200 { get; set; }
        public string act { get; set; }
        public double open_pos { get; set; }
        public double close_pos { get; set; }
        public double open_cash { get; set; }
        public double close_cash { get; set; }
        public double day_trade { get; set; }
        public double day_cash { get; set; }
        public DateTime min_date { get; set; }
        public string trade_date { get; set; }
        private MySqlConnection connection;
        private string connectionString;
        public string SelectedSymbol;
        string query;
        string query1;
        string QueryDates;
        string buy_update;
        string sell_update;
        ArrayList mySymbols = new ArrayList();
        ArrayList myDates = new ArrayList();

        public TradeProcessor()
        {
            connectionString = ConfigurationManager.ConnectionStrings["ALGOTRADE"].ConnectionString;
            connection = new MySqlConnection(connectionString);
        }


        public void TradeExecute(DateTime start, DateTime end)
        {
            MainWindow mw = new MainWindow();

            query = @"SELECT symbol_short FROM algotrade.static where status = 'A'; ";
            DbCallSymbols(query);

            if (mySymbols.Count >= 1 && mySymbols.Count <= Int32.MaxValue) // This code is to check Integer overflow
            {

                // Loop for symbols to frocess
                for (int i = 0; i < mySymbols.Count; i++)
                {

                    Symbol = (string)mySymbols[i]; // setting property for currently processing symbol
                                                   //CleanData(Symbol);
                    string CleanData = "call _sp_CleanData('" + Symbol + "')";

                    Console.WriteLine(CleanData);

                    //        mw.DatabaseCalls(CleanData);

                    QueryDates = $"select date from algotrade.factdata where date between '{start:yyyy-MM-dd}' and '{end:yyyy-MM-dd}'";

                    try
                    {
                        MySqlCommand comm = new MySqlCommand(QueryDates, connection);
                        connection.Open();

                        MySqlDataReader reader = comm.ExecuteReader();

                        while (reader.Read())
                        {
                            myDates.Insert(0, reader.GetValue(0));
                        }

                        //reader.Close();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        connection.Close();
                    }

                    // Define nearest start and end dates
                    string query_sdate = $"SELECT date FROM algotrade.factdata WHERE date >= '{start:yyyy-MM-dd}' and date <= NOW() and Symbol = '" + Symbol + "' ORDER BY date LIMIT 1;";
                    string query_ed = "SELECT date FROM algotrade.factdata WHERE date >= (select max(date) from algotrade.factdata where Symbol = '" + Symbol + "') and date <= NOW() and Symbol = '" + Symbol + "' ORDER BY date LIMIT 1;";
                    var startdate = Convert.ToDateTime(DbCallDate(query_sdate));
                    var enddate = Convert.ToDateTime(DbCallDate(query_ed));



                    foreach (DateTime day in EachCalendarDay(startdate, enddate))
                    {
                        if (myDates.Contains(day))
                        {

                            trade_date = String.Format("{0:yyyy-MM-dd}", day);

                            query1 = @"SELECT date, symbol, lastPrice, DMA_50, DMA_200, ACT FROM algotrade.factdata where symbol = '" + Symbol + "' and date = '" + String.Format("{0:yyyy-MM-dd}", day) + "';";

                            DbCall(query1);


                            if (act.Equals("Buy"))
                            {
                                try
                                {
                                    buy_update = "call _sp_BuyUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(price, 2) + "')";
                                    mw.DatabaseCalls(buy_update);
                                    pos_and_cash_calc();
                                    string PosAndCashUpdate = "call _sp_BuyPosAndCashUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(price, 2) + "', '" + Math.Round(open_pos, 2) + "', '" + Math.Round(day_trade, 2) + "', '" + Math.Round(close_pos, 2) + "',  '" + Math.Round(open_cash, 2) + "', '" + Math.Round(day_cash, 2) + "', '" + Math.Round(close_cash, 2) + "')";
                                    mw.DatabaseCalls(PosAndCashUpdate);
                                }
                                catch //(NullReferenceException e)
                                {

                                }
                                finally
                                {


                                }


                            }

                            else if (act.Equals("Sell"))
                            {
                                try
                                {


                                    pos_and_cash_calc();
                                    if (open_pos >= 1)
                                    {
                                        sell_update = "call _sp_SellUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(price, 2) + "')";
                                        mw.DatabaseCalls(sell_update);
                                        pos_and_cash_calc();
                                        string PosAndCashUpdate = "call _sp_SellPosAndCashUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(price, 2) + "', '" + Math.Round(open_pos, 2) + "', '" + Math.Round(day_trade, 2) + "', '" + Math.Round(close_pos, 2) + "',  '" + Math.Round(open_cash, 2) + "', '" + Math.Round(day_cash, 2) + "', '" + Math.Round(close_cash, 2) + "')";
                                        mw.DatabaseCalls(PosAndCashUpdate);

                                    }

                                }
                                catch //(NullReferenceException e)
                                {

                                }
                                finally
                                {
                                }
                            }
                        }

                    }
                }
            }

            MessageBox.Show("Trade analysis completed!!!");

        }


        public IEnumerable<DateTime> EachCalendarDay(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date.Date <= endDate.Date; date = date.AddDays(1)) yield
            return date;
        }


        public void pos_and_cash_calc()
        {
            open_pos = 0;


            string open = "";
            string opn_cash_query = "";

            open =
            //"call _sp_OpenPosition('" + trade_date + "', '" + Symbol + "', ClosePosition);";
            "SELECT  close_pos FROM algotrade.positions where date =  (SELECT distinct date FROM algotrade.factdata WHERE date < '" + trade_date + "' and symbol = '" + Symbol + "' ORDER BY date desc LIMIT 1)  and symbol = '" + Symbol + "'";
            opn_cash_query =
            //"call _sp_OpenCash('" + trade_date + "',  '" + Symbol + "', CloseCash);";

            "SELECT  close_cash FROM algotrade.cash where date =  (SELECT distinct date FROM algotrade.factdata WHERE date < '" + trade_date + "' and symbol = '" + Symbol + "' ORDER BY date desc LIMIT 1)  and symbol = '" + Symbol + "'";

            string day =
            //"call _sp_TradeQuantity('" + trade_date + "', '" + Symbol + "', TradeQuantity);";

            "SELECT  quantity FROM algotrade.trades where date = '" + trade_date + "' and symbol = '" + Symbol + "'";


            string day_cash_query =
                //"_sp_Day_Cash_Query('" + trade_date + "', '" + Symbol + "', DayCash)";

                "SELECT settlement_amount  FROM algotrade.trades where date = '" + trade_date + "' and symbol = '" + Symbol + "'";


            open_pos = DBA(open);
            day_trade = DBA(day);
            close_pos = open_pos + day_trade;

            if (close_pos < 0)
            {
                open_pos = 0;
                day_trade = 0;
                close_pos = 0;
                day_cash = 0;
            }

            open_cash = DBA(opn_cash_query);
            day_cash = DBA(day_cash_query);
            close_cash = open_cash + day_cash;
        }


        public void DbCall(string query)
        {

            try
            {
                MySqlCommand comm = new MySqlCommand(query, connection);
                connection.Open();

                MySqlDataReader reader = comm.ExecuteReader();

                while (reader.Read())
                {
                    date = (DateTime)reader.GetValue(0);
                    string Sym = (string)reader.GetValue(1);
                    price = Convert.ToDouble(reader.GetValue(2));
                    dma_50 = (double)reader.GetValue(3);
                    dma_200 = (double)reader.GetValue(4);
                    act = (string)reader.GetValue(5);

                }
                reader.Close();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }
        }


        public DateTime DbCallDate(string query)
        {

            try
            {
                MySqlCommand comm = new MySqlCommand(query, connection);
                connection.Open();

                MySqlDataReader reader = comm.ExecuteReader();

                while (reader.Read())
                {
                    min_date = (DateTime)reader.GetValue(0);
                }
                reader.Close();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }
            return min_date;
        }





        public void DbCallSymbols(string query)
        {

            try
            {
                MySqlCommand comm = new MySqlCommand(query, connection);
                connection.Open();

                MySqlDataReader reader = comm.ExecuteReader();

                while (reader.Read())
                {
                    mySymbols.Insert(0, reader.GetValue(0));
                }
                reader.Close();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }

        }

        public double DBA(string query)
        {
            double some_value = 0;

            try
            {
                MySqlCommand comm = new MySqlCommand(query, connection);
                comm.Parameters.Add("@SelectedSymbol", (MySqlDbType)SqlDbType.NVarChar,5);

                connection.Open();

                MySqlDataReader reader = comm.ExecuteReader();

                while (reader.Read())
                {
                    some_value = (double)reader.GetDouble(0);
                }
                reader.Close();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection.Close();

            }
            return some_value;
        }

        public void EndDateValidator(string ed)
        {
            DateTime today = new DateTime();

            if (Convert.ToDateTime(ed) > today)
            {
                ed = String.Format("{0:yyyy-MM-dd}", today);
                MessageBox.Show(ed);
            }
            else
            {
                MessageBox.Show(ed);
            }
        }
    }
}
