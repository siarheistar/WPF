using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace trading_WPF.Trading
{
    class TradeProcessor : MainWindowScreen
    {
        public DateTime Date { get; set; }
        public new string Symbol { get; set; }
        public double Price { get; set; }
        public double DMA_50 { get; set; }
        public double DMA_200 { get; set; }
        public string ACT { get; set; }
        public double Open_pos { get; set; }
        public double Close_pos { get; set; }
        public double Open_cash { get; set; }
        public double Close_cash { get; set; }
        public double Day_trade { get; set; }
        public double Day_cash { get; set; }
        public DateTime Min_date { get; set; }
        public string Trade_date { get; set; }
        private readonly MySqlConnection connection;
        private readonly string connectionString;
       // public new string SelectedSymbol;
        string query;
        string query1;
        string QueryDates;
        string buy_update;
        string sell_update;
        ArrayList mySymbols = new ArrayList();
        ArrayList myDates = new ArrayList();

        public TradeProcessor()
        {
            connectionString = ConfigurationManager.ConnectionStrings["ALGOTRADE_Local"].ConnectionString;
            connection = new MySqlConnection(connectionString);
        }

        /// <summary>
        /// This method performs trading data analysis and trades execution
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task TradeExecute(DateTime start, DateTime end)
        {
            MainWindowScreen mw = new MainWindowScreen();
            MessageBox.Show("Click OK to start trades simulation run...");
                            

            /// SQL query below takes only active symbols in static table and only those symbols containing factdata entries
            query = @"SELECT distinct st.symbol_short FROM algotrade.static st, algotrade.factdata fd where st.status = 'A' 
                      and fd.symbol = st.symbol_short; ";
            DbCallSymbols(query);

            if (mySymbols.Count >= 1 && mySymbols.Count <= Int32.MaxValue) // This code is to check Integer overflow
            {
                foreach (string SelectedSymbol in mySymbols)
                {                                       
                    Symbol = SelectedSymbol;

                    CleanData(Symbol);

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
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
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
                            Trade_date = String.Format("{0:yyyy-MM-dd}", day);

                            query1 = @"SELECT date, symbol, lastPrice, DMA_50, DMA_200, ACT FROM algotrade.factdata where symbol = '" + Symbol + "' and date = '" + String.Format("{0:yyyy-MM-dd}", day) + "';";

                            DbCall(query1);

                            if (ACT.Equals("Buy"))
                            {
                                try
                                {
                                    buy_update = "call _sp_BuyUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(Price, 2) + "')";
                                    mw.DatabaseCalls(buy_update);
                                    Pos_and_cash_calc();
                                    string PosAndCashUpdate = "call _sp_BuyPosAndCashUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(Price, 2) + "', '" + Math.Round(Open_pos, 2) + "', '" + Math.Round(Day_trade, 2) + "', '" + Math.Round(Close_pos, 2) + "',  '" + Math.Round(Open_cash, 2) + "', '" + Math.Round(Day_cash, 2) + "', '" + Math.Round(Close_cash, 2) + "')";
                                    mw.DatabaseCalls(PosAndCashUpdate);
                                }
                                catch (NullReferenceException e)
                                {
                                    MessageBox.Show(e.ToString());
                                    throw;
                                }
                            }

                            else if (ACT.Equals("Sell"))
                            {
                                try
                                {
                                    Pos_and_cash_calc();
                                    if (Open_pos >= 1)
                                    {
                                        sell_update = "call _sp_SellUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(Price, 2) + "')";
                                        mw.DatabaseCalls(sell_update);
                                        Pos_and_cash_calc();
                                        string PosAndCashUpdate = "call _sp_SellPosAndCashUpdate('" + String.Format("{0:yyyy-MM-dd}", day) + "', '" + Symbol + "', '" + Math.Round(Price, 2) + "', '" + Math.Round(Open_pos, 2) + "', '" + Math.Round(Day_trade, 2) + "', '" + Math.Round(Close_pos, 2) + "',  '" + Math.Round(Open_cash, 2) + "', '" + Math.Round(Day_cash, 2) + "', '" + Math.Round(Close_cash, 2) + "')";
                                        mw.DatabaseCalls(PosAndCashUpdate);
                                    }
                                }
                                catch (NullReferenceException e)
                                {
                                    MessageBox.Show(e.ToString());
                                    throw;
                                }
                            }
                        }

                    }
                }
            }

            MessageBox.Show("Trade analysis completed!!!");
            await Task.CompletedTask;

        } // This is IEnumerable generating dates in range

        public IEnumerable<DateTime> EachCalendarDay(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date.Date <= endDate.Date; date = date.AddDays(1)) yield
            return date;
        } //This is IEnumerable generating dates in range

        public void Pos_and_cash_calc() //Method calculates open, day and close values for positions and cash. 
        {
            Open_pos = 0;

            string Open =
                "SELECT  close_pos FROM algotrade.positions where date =  " +
                "(SELECT distinct date FROM algotrade.factdata WHERE date < '" + Trade_date + "' and symbol = '" + Symbol + "' " +
                "ORDER BY date desc LIMIT 1)  and symbol = '" + Symbol + "'";

            string Opn_cash_query =
                "SELECT  close_cash FROM algotrade.cash where  date = (select max(date) FROM algotrade.cash where symbol = '" + Symbol + "') " +
                "and symbol = '" + Symbol + "'";

            string Day =
                "SELECT  quantity FROM algotrade.trades where date = '" + Trade_date + "' and symbol = '" + Symbol + "'";


            string Day_cash_query = 
                "SELECT settlement_amount  FROM algotrade.trades where date = '" + Trade_date + "' and symbol = '" + Symbol + "'";


            Open_pos = DBA(Open);
            Day_trade = DBA(Day);
            Close_pos = Open_pos + Day_trade;

            if (Close_pos < 0)
            {
                Open_pos = 0;
                Day_trade = 0;
                Close_pos = 0;
                Day_cash = 0;
            }

            Open_cash = DBA(Opn_cash_query);
            Day_cash = DBA(Day_cash_query);
            Close_cash = Open_cash + Day_cash;
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
                    Date = (DateTime)reader.GetValue(0);
                    string Sym = (string)reader.GetValue(1);
                    Price = Convert.ToDouble(reader.GetValue(2));
                    DMA_50 = (double)reader.GetValue(3);
                    DMA_200 = (double)reader.GetValue(4);
                    ACT = (string)reader.GetValue(5);

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
        }  //Method reads data from factdata table for TradeExecute() 

        public DateTime DbCallDate(string query)
        {

            try
            {
                MySqlCommand comm = new MySqlCommand(query, connection);
                connection.Open();

                MySqlDataReader reader = comm.ExecuteReader();

                while (reader.Read())
                {
                    Min_date = (DateTime)reader.GetValue(0);
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
            return Min_date;
        } //Method is used to calculate nearest dates to start date and end date based on dates in factdata for selected symbol

        public new void DbCallSymbols(string query)
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

        } //Method used for establishing active symbols in static and for which there are entries in factdata

        public double DBA(string query) // Method used for calculation for posi-tions and cash values for selected symbol
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
        } // Method validates End date value and assigns current date value if it is greater than current date
    }
}
