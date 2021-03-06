﻿using System.Collections.Generic;
using System.Data.SqlClient;
using BetterModules.Core.DataAccess.DataContext.Migrations;
using BetterModules.Core.Modules;
using BetterModules.Sample.Module;
using Moq;
using NUnit.Framework;

namespace BetterModules.Core.Database.Tests.TestHelpers.Migrations
{
    public class TestDatabaseInitializer
    {
        public static LocalDatabase RunDatabaseMigrationTests()
        {
            var database = new LocalDatabase();
            
            var versionUpdateCount = 0;
            var versionChecker = new Mock<IVersionChecker>();
            versionChecker
                .Setup(vc => vc.VersionExists(It.IsAny<string>(), It.IsAny<long>()))
                .Returns<string, long>((s, l) => false);
            versionChecker
                .Setup(vc => vc.AddVersion(It.IsAny<string>(), It.IsAny<long>()))
                .Callback<string, long>((s, l) => versionUpdateCount++);
            var descriptors = new List<ModuleDescriptor> { new SampleModuleDescriptor() };

            database.RunMigrations(descriptors, versionChecker.Object);

            // Should run 2 migration scripts
            Assert.AreEqual(versionUpdateCount, 2);

            // Check, if tables are created
            CheckIfTablesExist(database.SqlConnection);

            // Shouldn't run the same migrations second time
            versionUpdateCount = 0;
            versionChecker
                .Setup(vc => vc.VersionExists(It.IsAny<string>(), It.IsAny<long>()))
                .Returns<string, long>((s, l) => true);

            database.RunMigrations(descriptors);

            // Should run 0 migration scripts
            Assert.AreEqual(versionUpdateCount, 0);

            return database;
        }

        private static void CheckIfTablesExist(SqlConnection connection)
        {
            var command = new SqlCommand("SELECT TABLE_NAME FROM information_schema.tables", connection);
            var tables = new List<string>();

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var table = reader.GetString(0);
                        tables.Add(table);
                    }
                    reader.Close();
                }
            }

            Assert.GreaterOrEqual(tables.Count, 5);
            Assert.IsTrue(tables.Contains("VersionInfo"));
            Assert.IsTrue(tables.Contains("TestItems"));
            Assert.IsTrue(tables.Contains("TestItemCategories"));
            Assert.IsTrue(tables.Contains("TestItemChildren"));
            Assert.IsTrue(tables.Contains("InheritedTestItems"));
        }
    }
}
