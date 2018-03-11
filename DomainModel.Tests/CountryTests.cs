namespace Countries.DomainModel.Tests {

    #region Usings
    using Extensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    [TestClass]
    public class CountryTests : BaseTestClass {

        private string countryISO2Create = "XZ";
        private string countryISO2Update = "XY";
        private string countryISO2Delete = "XW";
        private string countryISO3Create = "XZZ";
        private string countryISO3Update = "XYY";
        private string countryISO3Delete = "XWW";
        private string countryISONumericCreate = "999";
        private string countryISONumericUpdate = "998";
        private string countryISONumericDelete = "997";
        private string countryISONameCreate = "Fake ISO Name Create";
        private string countryISONameUpdate = "Fake ISO Name Update";
        private string countryISONameDelete = "Fake ISO Name Delete";
        private string countryCapitalCreate = "Fake Capital Create";
        private string countryCapitalUpdate = "Fake Capital Update";
        private string countryCapitalDelete = "Fake Capital Delete";
        private string countryOfficialNameCreate = "Fake Official Name Create";
        private string countryOfficialNameUpdate = "Fake Official Name Update";
        private string countryOfficialNameDelete = "Fake Official Name Delete";
        private string countryOfficialNameLocalCreate = "Fake Official Name Local Create";
        private string countryOfficialNameLocalUpdate = "Fake Official Name Local Update";
        private string countryOfficialNameLocalDelete = "Fake Official Name Local Delete";
        private string countryNameCreate = "Fake Name Create";
        private string countryNameUpdate = "Fake Name Update";
        private string countryNameDelete = "Fake Name Delete";
        private string continentName = "North America";
        private string currencyCode = "USD";

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
                    var sqlCommand = new SqlCommand($"SELECT [Id] FROM [Countries].[dbo].[Country] WHERE ([ISO2] = '{countryISO2Create}');");
                    int countryISO2CreateId = Convert.ToInt32(entityManager.ExecuteScalarSql(sqlCommand));
                    if (countryISO2CreateId > 0) {
                        sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CountryCurrency] WHERE ([CountryId] = '{countryISO2CreateId}');");
                        entityManager.ExecuteScalarSql(sqlCommand);
                    }
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Country] WHERE ([ISO2] = '{countryISO2Create}');");
                    entityManager.ExecuteScalarSql(sqlCommand);

                    sqlCommand = new SqlCommand($"SELECT [Id] FROM [Countries].[dbo].[Country] WHERE ([ISO2] = '{countryISO2Update}');");
                    int countryISO2UpdateId = Convert.ToInt32(entityManager.ExecuteScalarSql(sqlCommand));
                    if (countryISO2UpdateId > 0) {
                        sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CountryCurrency] WHERE ([CountryId] = '{countryISO2UpdateId}');");
                        entityManager.ExecuteScalarSql(sqlCommand);
                    }
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Country] WHERE ([ISO2] = '{countryISO2Update}');");
                    entityManager.ExecuteScalarSql(sqlCommand);

                    sqlCommand = new SqlCommand($"SELECT [Id] FROM [Countries].[dbo].[Country] WHERE ([ISO2] = '{countryISO2Delete}');");
                    int countryISO2DeleteId = Convert.ToInt32(entityManager.ExecuteScalarSql(sqlCommand));
                    if (countryISO2DeleteId > 0) {
                        sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CountryCurrency] WHERE ([CountryId] = '{countryISO2DeleteId}');");
                        entityManager.ExecuteScalarSql(sqlCommand);
                    }
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Country] WHERE ([ISO2] = '{countryISO2Delete}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                }
                catch (Exception e) {
                    Log.Error($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} {e.VerboseExceptionString()}");
                }
            }
        }
        #endregion

        [TestMethod]
        public void Test_Country_ChangeTracker() {
            using (var entityManager = new CountriesEntityManager()) {
                var country = entityManager.Set<Country>().Create();
                country = entityManager.Set<Country>().Add(country);

                var changeTrackerEntry = entityManager.Entry<Country>(country);
                var changeTrackerEntries = entityManager.ChangeTracker.Entries().ToList();

                Assert.IsNotNull(changeTrackerEntry);
                Assert.AreEqual(expected: 1, actual: changeTrackerEntries.Count);

                var countryHashCode = country.GetHashCode();
                var changeTrackerEntryEntityHashCode = changeTrackerEntries.Single().Entity.GetHashCode();

                Assert.AreEqual(countryHashCode, changeTrackerEntryEntityHashCode);

                country = null;
            }
        }

        [TestMethod]
        public void Test_Country_GetCountrys() {
            using (var entityManager = new CountriesEntityManager()) {
                var countries = entityManager.Set<Country>()
                    .Include(x => x.CallingCode)
                    .Include(x => x.Continent)
                    .Include(x => x.Currencies)
                    .ToList();

                Assert.IsNotNull(countries);
                Assert.AreNotEqual(notExpected: 0, actual: countries.Count);

                foreach (var country in countries) {
                    // all entity properties must be virtual for this to work
                    var proxy = country as IEntityWithChangeTracker;
                    Assert.IsNotNull(proxy);

                    try {
                        Assert.IsTrue(country.StringPropertiesAreTrim());
                    }
                    catch (Exception e) {
                        Debug.WriteLine($"Country: {country.ToCSVString()}");
                        throw;
                    }

                    try {
                        Assert.IsFalse(country.ToCSVString().Contains("ʻ"));
                    }
                    catch (Exception e) {
                        Debug.WriteLine($"Country: {country.ToCSVString()}");
                        throw;
                    }

                    try {
                        Assert.IsFalse(country.ToCSVString().Contains("’"));
                    }
                    catch (Exception e) {
                        Debug.WriteLine($"Country: {country.ToCSVString()}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// If there is something wrong with the entity class/relationship configuration, 
        /// this will throw an exception.
        /// </summary>
        [TestMethod]
        public void Test_Country_Schema() {
            using (var entityManager = new CountriesEntityManager()) {
                var countries = entityManager.Set<Country>().Take(1).ToList();
            }
        }

        #region Entity Create/Update/Delete Tests
        [TestMethod]
        public void Test_Country_Create() {

            var country = new Country() {
                DetachedState = DetachedState.Added,
                Capital = countryCapitalCreate,
                ISO2 = countryISO2Create,
                ISO3 = countryISO3Create,
                ISOName = countryISONameCreate,
                ISONumeric = countryISONumericCreate,
                Name = countryNameCreate,
                OfficialName = countryOfficialNameCreate,
                OfficialNameLocal = countryOfficialNameLocalCreate
            };

            using (var entityManager = new CountriesEntityManager()) {

                var continent = entityManager.Set<Continent>()
                    .Where(x => x.Name == continentName)
                    .Single();
                country.Continent = continent;

                var currency = entityManager.Set<Currency>()
                    .Where(x => x.Code == currencyCode)
                    .Single();
                country.Currencies.Add(currency);

                var validationResults = Get_Validation_Results(country);
                Assert.AreEqual(expected: 0, actual: validationResults.Count);

                entityManager.AddEntity<Country>(country);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var countryFromDb = entityManager.Set<Country>()
                    .Where(x => (x.Id == country.Id))
                    .Include(x => x.Currencies)
                    .Include(x => x.Continent)
                    .SingleOrDefault();

                Assert.IsNotNull(countryFromDb);
                Assert.AreEqual(expected: country.ToCSVString(), actual: countryFromDb.ToCSVString());

                // all entity properties must be virtual for this to work
                var proxy = countryFromDb as IEntityWithChangeTracker;
                Assert.IsNotNull(proxy);
            }
        }

        [TestMethod]
        public void Test_Country_Update() {

            var country = new Country() {
                DetachedState = DetachedState.Added,
                Capital = countryCapitalUpdate,
                ISO2 = countryISO2Update,
                ISO3 = countryISO3Update,
                ISOName = countryISONameUpdate,
                ISONumeric = countryISONumericUpdate,
                Name = countryNameUpdate,
                OfficialName = countryOfficialNameUpdate,
                OfficialNameLocal = countryOfficialNameLocalUpdate
            };

            using (var entityManager = new CountriesEntityManager()) {
                var continent = entityManager.Set<Continent>()
                    .Where(x => x.Name == continentName)
                    .Single();
                country.Continent = continent;

                var currency = entityManager.Set<Currency>()
                    .Where(x => x.Code == currencyCode)
                    .Single();
                country.Currencies.Add(currency);

                var validationResults = Get_Validation_Results(country);
                Assert.AreEqual(expected: 0, actual: validationResults.Count);

                entityManager.AddEntity<Country>(country);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            var newCountryName = country.Name + "2";
            country.Name = newCountryName;

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.Attach<Country>(country);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var countryFromDb = entityManager.Set<Country>()
                    .Where(x => (x.Id == country.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(countryFromDb);
                Assert.AreEqual(expected: newCountryName, actual: countryFromDb.Name);
            }
        }

        [TestMethod]
        public void Test_Country_Delete() {

            var country = new Country() {
                DetachedState = DetachedState.Added,
                Capital = countryCapitalDelete,
                ISO2 = countryISO2Delete,
                ISO3 = countryISO3Delete,
                ISOName = countryISONameDelete,
                ISONumeric = countryISONumericDelete,
                Name = countryNameDelete,
                OfficialName = countryOfficialNameDelete,
                OfficialNameLocal = countryOfficialNameLocalDelete
            };

            using (var entityManager = new CountriesEntityManager()) {
                var continent = entityManager.Set<Continent>()
                    .Where(x => x.Name == continentName)
                    .Single();
                country.Continent = continent;

                var currency = entityManager.Set<Currency>()
                    .Where(x => x.Code == currencyCode)
                    .Single();
                country.Currencies.Add(currency);

                var validationResults = Get_Validation_Results(country);
                Assert.AreEqual(expected: 0, actual: validationResults.Count);

                entityManager.AddEntity<Country>(country);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var countryFromDb = entityManager.Set<Country>()
                    .Where(x => (x.Id == country.Id))
                    // need to include related entities
                    .Include(x => x.Currencies)
                    .Include(x => x.Continent)
                    .SingleOrDefault();

                Assert.IsNotNull(countryFromDb);

                entityManager.RemoveEntity<Country>(countryFromDb);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);

                countryFromDb = entityManager.Set<Country>()
                    .Where(x => (x.Id == country.Id))
                    .SingleOrDefault();

                Assert.IsNull(countryFromDb);
            }
        }

        [TestMethod]
        public void Test_Country_Validation() {
            var country = new Country();
            country.Continent = null;
            country.ISO2 = null;
            country.ISO3 = null;
            country.ISOName = null;
            country.ISONumeric = null;
            country.Name = null;
            country.OfficialName = null;
            country.OfficialNameLocal = null;

             var validationResults = Get_Validation_Results(country);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "EntityKey", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Id", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Continent", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISO2", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISO3", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISOName", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISONumeric", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Name", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "OfficialName", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "OfficialNameLocal", StringComparison.OrdinalIgnoreCase))));

            country.ISO2 = "xx";
            country.ISO3 = "xxx";
            country.ISONumeric = "XXX";

            validationResults = Get_Validation_Results(country);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISO2", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISO3", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "ISONumeric", StringComparison.OrdinalIgnoreCase))));

        }
        #endregion
    }
}
