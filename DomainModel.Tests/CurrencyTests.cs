namespace Countries.DomainModel.Tests {

    #region Usings
    using Extensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    [TestClass]
    public class CurrencyTests : BaseTestClass {

        private string currencyCodeCreate = "ZZZ";
        private string currencyCodeUpdate = "ZZY";
        private string currencyCodeDelete = "ZZX";
        private string currencyNameCreate = "Fake Currency Name Create";
        private string currencyNameUpdate = "Fake Currency Name Update";
        private string currencyNameDelete = "Fake Currency Name Delete";


        #region Test Initialization and Cleanup
        [TestInitialize]
        public void Test_Initialize() {
            DeleteDatabaseObjects();
        }

        [TestCleanup]
        public void Test_Cleanup() {
            DeleteDatabaseObjects();
        }

        private void DeleteDatabaseObjects() {

            using (var entityManager = new CountriesEntityManager()) {

                try {
                    var sqlCommand = new SqlCommand($"SELECT [Id] FROM [Countries].[dbo].[Currency] WHERE ([Code] = '{currencyCodeCreate}');");
                    int currencyCodeCreateId = Convert.ToInt32(entityManager.ExecuteScalarSql(sqlCommand));
                    if (currencyCodeCreateId > 0) {
                        sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CountryCurrency] WHERE ([CurrencyId] = '{currencyCodeCreateId}');");
                        entityManager.ExecuteScalarSql(sqlCommand);
                    }
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Currency] WHERE ([Code] = '{currencyCodeCreate}');");
                    entityManager.ExecuteScalarSql(sqlCommand);

                    sqlCommand = new SqlCommand($"SELECT [Id] FROM [Countries].[dbo].[Currency] WHERE ([Code] = '{currencyCodeUpdate}');");
                    int currencyCodeUpdateId = Convert.ToInt32(entityManager.ExecuteScalarSql(sqlCommand));
                    if (currencyCodeUpdateId > 0) {
                        sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CountryCurrency] WHERE ([CurrencyId] = '{currencyCodeUpdateId}');");
                        entityManager.ExecuteScalarSql(sqlCommand);
                    }
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Currency] WHERE ([Code] = '{currencyCodeUpdate}');");
                    entityManager.ExecuteScalarSql(sqlCommand);

                    sqlCommand = new SqlCommand($"SELECT [Id] FROM [Countries].[dbo].[Currency] WHERE ([Code] = '{currencyCodeDelete}');");
                    int currencyCodeDeleteId = Convert.ToInt32(entityManager.ExecuteScalarSql(sqlCommand));
                    if (currencyCodeDeleteId > 0) {
                        sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CountryCurrency] WHERE ([CurrencyId] = '{currencyCodeDeleteId}');");
                        entityManager.ExecuteScalarSql(sqlCommand);
                    }
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Currency] WHERE ([Code] = '{currencyCodeDelete}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                }
                catch (Exception e) {
                    Log.Error($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} {e.VerboseExceptionString()}");
                }
            }
        }
        #endregion

        [TestMethod]
        public void Test_Currency_ChangeTracker() {
            using (var entityManager = new CountriesEntityManager()) {
                var currency = entityManager.Set<Currency>().Create();
                currency = entityManager.Set<Currency>().Add(currency);

                var changeTrackerEntry = entityManager.Entry<Currency>(currency);
                var changeTrackerEntries = entityManager.ChangeTracker.Entries().ToList();

                Assert.IsNotNull(changeTrackerEntry);
                Assert.AreEqual(expected: 1, actual: changeTrackerEntries.Count);

                var currencyHashCode = currency.GetHashCode();
                var changeTrackerEntryEntityHashCode = changeTrackerEntries.Single().Entity.GetHashCode();

                Assert.AreEqual(currencyHashCode, changeTrackerEntryEntityHashCode);

                currency = null;
            }
        }

        [TestMethod]
        public void Test_Currency_GetCurrencys() {
            using (var entityManager = new CountriesEntityManager()) {
                var currencys = entityManager.Set<Currency>()
                    .Include(x => x.Countries)
                    .ToList();

                Assert.IsNotNull(currencys);
                Assert.AreNotEqual(notExpected: 0, actual: currencys.Count);

                foreach (var currency in currencys) {
                    // all entity properties must be virtual for this to work
                    var proxy = currency as IEntityWithChangeTracker;
                    Assert.IsNotNull(proxy);

                    Assert.IsTrue(currency.StringPropertiesAreTrim());
                }
            }
        }

        /// <summary>
        /// If there is something wrong with the entity class/relationship configuration, 
        /// this will throw an exception.
        /// </summary>
        [TestMethod]
        public void Test_Currency_Schema() {
            using (var entityManager = new CountriesEntityManager()) {
                var continents = entityManager.Set<Currency>().Take(1).ToList();
            }
        }

        #region Entity Create/Update/Delete Tests
        [TestMethod]
        public void Test_Currency_Create() {

            var currency = new Currency() {
                DetachedState = DetachedState.Added,
                Code = currencyCodeCreate,
                DecimalDigits = 2,
                Name = currencyNameCreate
            };

            var validationResults = Get_Validation_Results(currency);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Currency>(currency);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var currencyFromDb = entityManager.Set<Currency>()
                    .Where(x => (x.Id == currency.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(currencyFromDb);
                Assert.AreEqual(expected: currency.ToCSVString(), actual: currencyFromDb.ToCSVString());

                // all entity properties must be virtual for this to work
                var proxy = currencyFromDb as IEntityWithChangeTracker;
                Assert.IsNotNull(proxy);
            }
        }

        [TestMethod]
        public void Test_Currency_Update() {

            var currency = new Currency() {
                DetachedState = DetachedState.Added,
                Code = currencyCodeUpdate,
                DecimalDigits = 2,
                Name = currencyNameUpdate
            };

            var validationResults = Get_Validation_Results(currency);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Currency>(currency);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            var newCurrencyName = currency.Name + "2";
            currency.Name = newCurrencyName;

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.Attach<Currency>(currency);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var currencyFromDb = entityManager.Set<Currency>()
                    .Where(x => (x.Id == currency.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(currencyFromDb);
                Assert.AreEqual(expected: newCurrencyName, actual: currencyFromDb.Name);
            }
        }

        [TestMethod]
        public void Test_Currency_Delete() {

            var currency = new Currency() {
                DetachedState = DetachedState.Added,
                Code = currencyCodeDelete,
                DecimalDigits = 2,
                Name = currencyNameDelete
            };

            var validationResults = Get_Validation_Results(currency);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Currency>(currency);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var currencyFromDb = entityManager.Set<Currency>()
                    .Where(x => (x.Id == currency.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(currencyFromDb);
            }

            var currencyToDelete = new Currency() {
                DetachedState = DetachedState.Deleted,
                Id = currency.Id,
                RowVersion = currency.RowVersion
            };
            using (var entityManager = new CountriesEntityManager()) {
                entityManager.RemoveEntity<Currency>(currencyToDelete);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var currencyFromDb = entityManager.Set<Currency>()
                    .Where(x => (x.Id == currency.Id))
                    .SingleOrDefault();

                Assert.IsNull(currencyFromDb);
            }
        }

        [TestMethod]
        public void Test_Currency_Validation() {
            var currency = new Currency();
            currency.Code = null;
            currency.Name = null;
            currency.DecimalDigits = -1;

            var validationResults = Get_Validation_Results(currency);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "EntityKey", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Id", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Code", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Name", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "DecimalDigits", StringComparison.OrdinalIgnoreCase))));

            currency.Code = "zzz";

            validationResults = Get_Validation_Results(currency);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Code", StringComparison.OrdinalIgnoreCase))));
        }
        #endregion
    }
}
