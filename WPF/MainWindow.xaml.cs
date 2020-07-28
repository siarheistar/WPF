using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using MySql.Data.MySqlClient;
using YahooFinanceApi;
using trading_WPF.Trading;
using System.Configuration;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections;
using WPFAuthentication;

namespace trading_WPF
{

    public partial class MainWindowScreen : Window
    {
        private readonly MySqlConnection connection;
        private readonly string connectionString;
        public string SelectedSymbol;
        string query;
        bool status = false;
        ArrayList mySymbols = new ArrayList();
        readonly string sproc = "call _sp_ACT();";
        private DateTime start;
        private DateTime end;

        public Auth0Window LoginForm;
        public About AboutForm;



        public MainWindowScreen()
        {
            InitializeComponent();
            StartDate.SelectedDate = DateTime.Today.AddDays(-366);
            EndDate.SelectedDate = DateTime.Today.AddDays(-1);
            start = StartDate.SelectedDate.Value;
            end = EndDate.SelectedDate.Value;
            connectionString = ConfigurationManager.ConnectionStrings["ALGOTRADE_Local"].ConnectionString;
            connection = new MySqlConnection(connectionString);

            ShowSymbolsList();

        }



        private volatile bool symbolListLoading;
        private void ShowSymbolsList()
        {
            try
            {
                if (symbolListLoading)
                    return;

                SymbolsList.IsEnabled = false;

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log(msg: $"...");

                        string query = "SELECT symbol_short FROM algotrade.static where status = 'A';";
                        MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                        MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                        using (MysqlDataAdapter)
                        {
                            try
                            {
                                sqlCommand.Parameters.AddWithValue("@symbol", symbol);
                                DataTable symbolsTable = new DataTable();
                                MysqlDataAdapter.Fill(symbolsTable);

                                Log(msg: "finished", guiUpdate: () =>
                                {
                                    SymbolsList.DisplayMemberPath = "symbol_short";
                                    SymbolsList.SelectedValuePath = "symbol_short";
                                    SymbolsList.ItemsSource = symbolsTable.DefaultView;
                                });
                            }
                            catch //(NullReferenceException e)
                            {
                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(null, $"Exception: {ex}");
                    }
                    finally
                    {
                        symbolListLoading = false;
                        Log($"All data for {symbol} loaded", guiUpdate: () => { SymbolsList.IsEnabled = true; });
                    }
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                symbolListLoading = false;
            }

        }

        private void ShowPosition()
        {
            try
            {
                Log(msg: "...");

                string query = "SELECT close_pos as POS FROM positions where symbol = @symbol and date = (SELECT max(date) FROM positions where symbol = @symbol) ;";

                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);
                    DataTable positionsTable = new DataTable();
                    MysqlDataAdapter.Fill(positionsTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        PositionsValue.DisplayMemberPath = "POS";
                        PositionsValue.SelectedValuePath = "POS";
                        PositionsValue.ItemsSource = positionsTable.DefaultView;
                    });
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }


        private void ShowCash()
        {
            try
            {
                Log(msg: "...");

                string query = "SELECT close_cash as CASH FROM cash where symbol = @symbol and date = (SELECT max(date) FROM cash where symbol = @symbol) ;";

                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable cashTable = new DataTable();
                    MysqlDataAdapter.Fill(cashTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        CashValue.DisplayMemberPath = "CASH";
                        CashValue.SelectedValuePath = "CASH";
                        CashValue.ItemsSource = cashTable.DefaultView;
                    });
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        private void ShowTodayTrades()
        {
            try
            {
                Log(msg: "...");

                string query = "SELECT gross_amount FROM algotrade.trades where symbol = @symbol and date = (SELECT max(date) FROM trades where symbol = @symbol);";

                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        TodayTradesValue.DisplayMemberPath = "gross_amount";
                        TodayTradesValue.SelectedValuePath = "gross_amount";
                        TodayTradesValue.ItemsSource = tradesTable.DefaultView;
                    });
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        private void ShowTodayQuantity()
        {
            try
            {
                Log(msg: "...");

                string query = "SELECT quantity FROM algotrade.trades where symbol = @symbol  and date = (SELECT max(date) FROM trades where symbol = @symbol);";

                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        QuantityValue.DisplayMemberPath = "quantity";
                        QuantityValue.SelectedValuePath = "quantity";
                        QuantityValue.ItemsSource = tradesTable.DefaultView;
                    });

                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        private void ShowTodayPrice()
        {
            try
            {
                Log(msg: "...");
                string query = "SELECT price FROM algotrade.trades where symbol = @symbol  and date = (SELECT max(date) FROM trades where symbol = @symbol);";
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        PriceValue.DisplayMemberPath = "price";
                        PriceValue.SelectedValuePath = "price";
                        PriceValue.ItemsSource = tradesTable.DefaultView;
                    });

                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }


        private void ShowTrades()
        {
            try
            {
                Log(msg: "...");
                string query = "SELECT date, Symbol, quantity, price, settlement_amount as amount FROM algotrade.trades where symbol = @symbol order by date desc;";
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () => { Show_Trades.ItemsSource = tradesTable.DefaultView; });

                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        private void ShowPositions()
        {
            try
            {
                Log(msg: "...");

                string query = "SELECT  Date, Symbol, open_pos, day_trade, close_pos FROM algotrade.positions where symbol = @symbol order by date desc;";
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () => { Show_POS.ItemsSource = tradesTable.DefaultView; });

                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        private void ShowBuy()
        {
            try
            {
                Log(msg: "...");
                string query = "select count(*) as Buy from algotrade.trades where quantity > 0 and symbol = @symbol;";
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        BuyValue.DisplayMemberPath = "Buy";
                        BuyValue.SelectedValuePath = "Buy";
                        BuyValue.ItemsSource = tradesTable.DefaultView;
                    });

                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        private void ShowSell()
        {
            try
            {
                Log(msg: "...");
                string query = "select count(*) as Sell from algotrade.trades where quantity < 0 and symbol = @symbol;";
               
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () =>
                    {
                        SellValue.DisplayMemberPath = "Sell";
                        SellValue.SelectedValuePath = "Sell";
                        SellValue.ItemsSource = tradesTable.DefaultView;
                    });
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        //private void ShowFactData()
        //{

        //    try
        //    {
        //        Log(msg: "...");
        //        string query = "SELECT date, Symbol, lastprice, ACT FROM algotrade.factdata where symbol = @symbol and date between'"+ sd +"' and '"+ ed +"' order by date desc;";
        //        MySqlCommand sqlCommand = new MySqlCommand(query, connection);
        //        MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

        //        using (MysqlDataAdapter)
        //        {
        //            sqlCommand.Parameters.AddWithValue("@symbol", symbol);

        //            DataTable tradesTable = new DataTable();
        //            MysqlDataAdapter.Fill(tradesTable);

        //            Log(msg: "finished", guiUpdate: () => { Show_FactData.ItemsSource = tradesTable.DefaultView; });

        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.ToString());
        //    }

        //}

        private void ShowDailyCash()
        {
            try
            {
                Log(msg: "...");

                string query = "SELECT  date as Date, Symbol, open_cash, day_cash, close_cash FROM algotrade.cash where symbol = @symbol order by Date desc;";
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

                using (MysqlDataAdapter)
                {
                    sqlCommand.Parameters.AddWithValue("@symbol", symbol);

                    DataTable tradesTable = new DataTable();
                    MysqlDataAdapter.Fill(tradesTable);

                    Log(msg: "finished", guiUpdate: () => { Show_Daily_Cash.ItemsSource = tradesTable.DefaultView; });

                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }


        private void AddSymbol()
        {

            ValidateSymbol();

            if (status)
            {
                try
                {
                    string query = "call _sp_AddSymbol(@symbol_short)";

                    MySqlCommand sqlCommand1 = new MySqlCommand(query, connection);
                    MySqlDataAdapter MysqlDataAdapter1 = new MySqlDataAdapter(sqlCommand1);

                    using (MysqlDataAdapter1)
                    {
                        sqlCommand1.Parameters.AddWithValue("@symbol_short", Symbol.Text.ToUpper());

                        DataTable symbolsTable = new DataTable();
                        MysqlDataAdapter1.Fill(symbolsTable);
                        SymbolsList.DisplayMemberPath = "symbol_short";
                        SymbolsList.SelectedValuePath = "symbol_short";
                        SymbolsList.ItemsSource = symbolsTable.DefaultView;

                    }
                    Symbol.Clear();
                }
                catch //(MySqlException e)
                {
                    MessageBox.Show("Entry already exists!");
                    Symbol.Clear();
                }
            }

            else
            {
                MessageBox.Show("Enter valid symbol!");
                Symbol.Clear();
            }
        }


        private bool ValidateSymbol()
        {
            status = false;
            TradeProcessor tp = new TradeProcessor();

            MySqlCommand sqlCommand = new MySqlCommand(query, connection);
            MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);
            using (MysqlDataAdapter)
            {
                sqlCommand.Parameters.AddWithValue("@symbol", Symbol.Text.ToString());

                try
                {
                    SelectedSymbol = Symbol.Text.ToString();
                }
                catch  //NullReferenceException e
                {
                    MessageBox.Show("Please select Symbol");
                }

                if (SelectedSymbol.Length <= 5)
                {

                    if (!checkForSQLInjection(SelectedSymbol))
                    {
                        string query = "SELECT count(distinct `SecurityName`) FROM `algotrade`.`symbols`" +
                        //" where `Symbol` = '@SelectedSymbol' ;";
                        " where `Symbol` = '" + SelectedSymbol + "' ;";


                        if (tp.DBA(query) > 0)
                        {
                            status = true;
                        }
                    }
                    else
                    {
                        status = false;
                        Symbol.Clear();
                    }
                }
                else
                {
                    status = false;
                    MessageBox.Show("Please enter Symbol with correct number of characters!");
                    Symbol.Clear();
                }

            }
            return status;
        }

        /// <summary>
        /// SQL injection code sniplet was taken from https://www.c-sharpcorner.com/blogs/check-string-against-sql-injection-in-c-sharp1
        /// added some more checks to close vulnerability possibilities
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns></returns>
        public static Boolean checkForSQLInjection(string userInput)

        {

            bool isSQLInjection = false;

            string[] sqlCheckList = { "--", ";--", ";", "/*", "*/", "@@", "@", "char", "nchar", "varchar", "nvarchar", "alter", "begin", "cast",
                                     "create", "cursor", "declare", "delete", "drop", "end", "exec", "execute", "fetch", "insert", "kill", "select",
                                      "sys", "sysobjects", "syscolumns", "table", "update", "' or 1=1#", "' or 1=1--", "TRUE", "FALSE"};

            string CheckString = userInput.Replace("'", "''");

            for (int i = 0; i <= sqlCheckList.Length - 1; i++)

            {

                if ((CheckString.IndexOf(sqlCheckList[i],

                    StringComparison.OrdinalIgnoreCase) >= 0))

                { isSQLInjection = true; }
            }

            return isSQLInjection;
        }
    



    private void RemoveSymbol()
        {
            try
            {
                string query = "call _sp_RemoveSymbol('" + @symbol + "')";
                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                connection.Open();
                sqlCommand.Parameters.AddWithValue("@symbol", SymbolsList.SelectedValue);
                sqlCommand.ExecuteScalar();
                connection.Close();
                ShowSymbolsList();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                MessageBox.Show("Symbol " + @symbol + " removed OK");
            }

        }

        public void CleanData(string SelectedSymbol)
        {
            try
            {               
                string query = "call _sp_CleanData('" + SelectedSymbol + "')";

                MySqlCommand sqlCommand = new MySqlCommand(query, connection);
                connection.Open();
                sqlCommand.ExecuteScalar();
                connection.Close();
                ShowSymbolsList();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                connection.Close();
            }

        }



        public async Task YFinance()

        {
           MessageBox.Show("Click OK to start Historical data download...");
           query = @"SELECT symbol_short FROM algotrade.static where status = 'A'; ";
            DbCallSymbols(query);

            Yahoo.IgnoreEmptyRows = true;
            MySqlCommand sqlCommand = new MySqlCommand(query, connection);
            MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);


            if (mySymbols.Count >= 1 && mySymbols.Count <= Int32.MaxValue) // This code is to check Integer overflow
            {
                for (int i = 0; i < mySymbols.Count; i++) 
                {

                    SelectedSymbol = (string)mySymbols[i]; // setting property for currently processing symbol
                                        
                    var history = await Yahoo.GetHistoricalAsync(SelectedSymbol, start, end, Period.Daily);
                    string query1 = "delete from algotrade.factdata where symbol = '" + SelectedSymbol + "'; ";



                    DatabaseCalls(query1);

                    try
                    {

                        foreach (var candle in history)
                        {
                            string date = String.Format("{0:yyyy-MM-dd}", candle.DateTime);
                            query = "call _sp_FactDataHydrate('" + date + "' ,'" + SelectedSymbol.ToString() + "', " + candle.Open + ", " + candle.High + " , " + candle.Low + " ," + candle.Close + " , " + candle.Volume + ")";

                            DatabaseCalls(query);
                        }

                        string query2 = "create or replace view DMA_50 as SELECT date, symbol,AVG(lastPrice) OVER(ORDER BY Date ROWS BETWEEN 50 PRECEDING AND 0 FOLLOWING) DMA_50 FROM algotrade.factdata where Symbol = '" + SelectedSymbol.ToString() + "' and Date <= current_date() order by date desc; " +
                                 "create or replace view DMA_200 as SELECT date, symbol, AVG(lastPrice) OVER(ORDER BY Date ROWS BETWEEN 200 PRECEDING AND 0 FOLLOWING ) DMA_200 FROM algotrade.factdata where Symbol = '" + SelectedSymbol.ToString() + "' and Date <= current_date() order by date desc;";


                        string query3 = "call _sp_DMACalculation()";

                        DatabaseCalls(query2);
                        DatabaseCalls(query3);

                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                    finally
                    {
                        DatabaseCalls(sproc);
                        CleanData(SelectedSymbol);
                    }

                }
                MessageBox.Show("Static Data for refreshed!!!");
            }
            else
            {
                MessageBox.Show("No Symbols added in list. Please Add more symbols");
            }
        }

        public void DatabaseCalls(string query)
        {
            try
            {


                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteScalar();
                    }

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {

            }

        }

        private volatile bool loading;
        private string symbol;

        private void SymbolsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading || SymbolsList.SelectedValue == null)
                return;

            loading = true;
            SymbolsList.IsEnabled = false;

            symbol = SymbolsList.SelectedValue.ToString();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Log($"Loading data for {symbol}...");
                    ShowPosition();
                    ShowCash();
                    ShowTodayTrades();
                    ShowTodayQuantity();
                    ShowTodayPrice();
                    ShowTrades();
                    ShowPositions();
                    ShowDailyCash();
                    //ShowFactData();
                    ShowBuy();
                    ShowSell();
                }
                catch (Exception ex)
                {
                    Log(null, $"Exception loading data for {symbol}: {ex}");
                }
                finally
                {
                    loading = false;
                    Log($"All data for {symbol} loaded", guiUpdate: () => { SymbolsList.IsEnabled = true; });
                }
            });
        }

        private void Log([System.Runtime.CompilerServices.CallerMemberName] string callerName = "",
            string msg = null,
            Action guiUpdate = null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LogList.Items.Add($"{DateTime.Now:hh:mm:ss}: {callerName} {msg}");
                guiUpdate?.Invoke();
                try
                {
                    var border = (Border)VisualTreeHelper.GetChild(LogList, 0);
                    var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
                catch
                {

                }

            }), DispatcherPriority.Background);
        }

        private void RemoveSymbol(object sender, RoutedEventArgs e)
        {
            RemoveSymbol();
            ShowSymbolsList();
        }

        private void RefreshSymbols(object sender, RoutedEventArgs e)
        {
            ShowSymbolsList();
        }

        private async void RefreshData(object sender, RoutedEventArgs e)
        {
            if(start < end)
            {            
                    try
                    {
                        await YFinance();
                    }
                    catch (Exception ae)
                    {
                        MessageBox.Show(ae.ToString());
                        MessageBox.Show("Select symbol to refresh");
                    }
            
            }
            else
            {
                MessageBox.Show("Select START date and END date correctly!");
            }

        }

        private void Add_Symbol(object sender, RoutedEventArgs e)
        {
            try
            {
                AddSymbol();

            }
            catch
            {
                MessageBox.Show("Incorrect symbol");
            }
            finally
            {
                ShowSymbolsList();
            }
        }

        private async void Process_Trade(object sender, RoutedEventArgs e)
        {
            if (start < end)
            {
                TradeProcessor tradeProcessor = new TradeProcessor();
                tradeProcessor.TradeExecute(start, end);
                ShowSymbolsList();
            }
            else
            {
                MessageBox.Show("Select START date and END date correctly!");
            }
        }

        public void DateValidator(string ed)
        {
            DateTime today = new DateTime();

            if (Convert.ToDateTime(ed) > today)
            {
                ed = String.Format("{0:yyyy-MM-dd}", today);
            }
            else
            {
            }
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

        private void StartDate_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!StartDate.SelectedDate.HasValue)
                return;

            start = StartDate.SelectedDate.Value;
        }

        private void EndDate_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!EndDate.SelectedDate.HasValue)
                return;

            end = EndDate.SelectedDate.Value;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            LoginForm.Logout();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            About AboutScreen = new About();
            AboutScreen.Show();
        }


    }
}