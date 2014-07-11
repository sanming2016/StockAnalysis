﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingStrategy
{
    public sealed class StatisticalCounter
    {
        public TradingHistory History { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public ITradingDataProvider DataProvider { get; private set; }

        public StatisticalCounter(TradingHistory history, ITradingDataProvider provider, DateTime startDate, DateTime endDate)
        {
            if (history == null)
            {
                throw new ArgumentNullException("history");
            }

            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            startDate = startDate.Date;
            endDate = endDate.Date;

            if (startDate >= endDate)
            {
                throw new ArgumentException("startDate must be earlier than endDate");
            }

            if (history.MinTransactionTime < startDate)
            {
                throw new ArgumentOutOfRangeException("startDate is not smaller than the minimum transaction time in trading history");
            }

            if (history.MaxTransactionTime >= endDate)
            {
                throw new ArgumentOutOfRangeException("endDate is not larger than the maximum transaction time in trading history");
            }

            // copy history object
            History = history.Clone();
            DataProvider = provider;

            StartDate = startDate;
            EndDate = endDate;
        }

        public IEnumerable<DateTime> GetDateScale()
        {
            DateTime date = StartDate;

            while (date <= EndDate)
            {
                yield return date;

                // add one day each time
                date.AddDays(1.0);
            }
        }

        public IEnumerable<double> GetEquityCurve()
        {
            // order the transaction histories firstly.
            Transaction[] transactions = History.History.OrderBy(t => t, new Transaction.DefaultComparer()).ToArray();

            EquityManager manager = new EquityManager(History.InitialCapital);

            DateTime date = StartDate;
            int transactionIndex = 0;
            double currentEquity = manager.InitialCapital;

            while (date <= EndDate)
            {
                bool equityChanged = false;
                while (transactionIndex < transactions.Length)
                {
                    string error;
                    if (transactions[transactionIndex].ExecutionTime < date)
                    {
                        if (!manager.ExecuteTransaction(transactions[transactionIndex], out error))
                        {
                            throw new InvalidOperationException("Replay transaction failed: " + error);
                        }

                        ++transactionIndex;

                        equityChanged = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (equityChanged)
                {
                    // if any transaction is executed, update the total equity.
                    currentEquity = manager.GetTotalEquityBasedOnMarketValue(DataProvider, date);
                }

                // add one day each time
                date.AddDays(1.0);

                yield return currentEquity;
            }
        }
    }
}