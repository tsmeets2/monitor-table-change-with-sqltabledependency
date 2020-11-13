﻿using System;
using System.Configuration;
using System.Linq.Expressions;

using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Abstracts;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;
using TableDependency.SqlClient.Test.Client.Core.Models;
using TableDependency.SqlClient.Where;

namespace TableDependency.SqlClient.Test.Client.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var connectionString = string.Empty;
            ConsoleKeyInfo consoleKeyInfo;
            var originalForegroundColor = Console.ForegroundColor;

            do
            {
                Console.Clear();

                Console.Write("TableDependency, SqlTableDependency");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" (.NET Core)");
                Console.ForegroundColor = originalForegroundColor;
                Console.WriteLine("Copyright (c) 2015-2020 Christian Del Bianco.");
                Console.WriteLine("All rights reserved." + Environment.NewLine);
                Console.WriteLine("**********************************************************************************************");
                Console.WriteLine("Choose connection string:");
                Console.WriteLine(" - F4: tom_test");
                Console.WriteLine(" - ESC to exit");
                Console.WriteLine("**********************************************************************************************");

                consoleKeyInfo = Console.ReadKey();
                if (consoleKeyInfo.Key == ConsoleKey.Escape) Environment.Exit(0);

            } while (consoleKeyInfo.Key != ConsoleKey.F4);

            Console.ResetColor();
            if (consoleKeyInfo.Key == ConsoleKey.F4) connectionString = ConfigurationManager.ConnectionStrings["tom_test"].ConnectionString;
            if (consoleKeyInfo.Key == ConsoleKey.F5) connectionString = ConfigurationManager.ConnectionStrings["SqlServer2008 Test_User"].ConnectionString;

            // Mapper for DB columns not matching Model's columns
            var mapper = new ModelToTableMapper<Product>();
            mapper.AddMapping(c => c.Expiring, "ExpiringDate");

            // Define WHERE filter
            Expression<Func<Product, bool>> expression = p => (p.CategoryId == (int)CategorysEnum.Food || p.CategoryId == (int)CategorysEnum.Drink) && p.Quantity <= 10;
            ITableDependencyFilter whereCondition = new SqlTableDependencyFilter<Product>(expression, mapper);

            // As table name (Products) does not match model name (Product), its definition is needed.
            using (var dep = new SqlTableDependency<Product>(connectionString, "Products", mapper: mapper, includeOldValues: true, filter: whereCondition))
            {
                dep.OnChanged += Changed;
                dep.OnError += OnError;
                dep.OnStatusChanged += OnStatusChanged;

                dep.Start();

                Console.WriteLine();
                Console.WriteLine("Waiting for receiving notifications (db objects naming: " + dep.DataBaseObjectsNamingConvention + ")...");
                Console.WriteLine("Press a key to stop.");
                Console.ReadKey();
            }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.WriteLine(e.Error?.Message);
            Console.ResetColor();
        }

        private static void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            Console.WriteLine(Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"SqlTableDependency Status = {e.Status.ToString()}");
            Console.ResetColor();
        }

        private static void Changed(object sender, RecordChangedEventArgs<Product> e)
        {
            Console.WriteLine(Environment.NewLine);

            if (e.ChangeType != ChangeType.None)
            {
                var changedEntity = e.Entity;
                Console.WriteLine("Id: " + changedEntity.Id);
                Console.WriteLine("Name: " + changedEntity.Name);
                Console.WriteLine("Expiring: " + changedEntity.Expiring);
                Console.WriteLine("Quantity: " + changedEntity.Quantity);
                Console.WriteLine("Price: " + changedEntity.Price);
            }

            if (e.ChangeType == ChangeType.Update && e.EntityOldValues != null)
            {
                Console.WriteLine(Environment.NewLine);

                var changedEntity = e.EntityOldValues;
                Console.WriteLine("Id (OLD): " + changedEntity.Id);
                Console.WriteLine("Name (OLD): " + changedEntity.Name);
                Console.WriteLine("Expiring (OLD): " + changedEntity.Expiring);
                Console.WriteLine("Quantity (OLD): " + changedEntity.Quantity);
                Console.WriteLine("Price (OLD): " + changedEntity.Price);
            }
        }
    }
}