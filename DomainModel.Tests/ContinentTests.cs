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
    public class ContinentTests : BaseTestClass {

        private string continentAbbreviationCreate = "FC";
        private string continentNameCreate = "Fake Continent Create";
        private string continentAbbreviationUpdate = "FU";
        private string continentNameUpdate = "Fake Continent Update";
        private string continentAbbreviationDelete = "FD";
        private string continentNameDelete = "Fake Continent Delete";

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
                    var sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Continent] WHERE ([Abbreviation] = '{continentAbbreviationCreate}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Continent] WHERE ([Abbreviation] = '{continentAbbreviationUpdate}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[Continent] WHERE ([Abbreviation] = '{continentAbbreviationDelete}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                }
                catch (Exception e) {
                    Log.Error($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} {e.VerboseExceptionString()}");
                }
            }
        } 
        #endregion

        [TestMethod]
        public void Test_Continent_ChangeTracker() {
            using (var entityManager = new CountriesEntityManager()) {
                var continent = entityManager.Set<Continent>().Create();
                continent = entityManager.Set<Continent>().Add(continent);

                var changeTrackerEntry = entityManager.Entry<Continent>(continent);
                var changeTrackerEntries = entityManager.ChangeTracker.Entries().ToList();

                Assert.IsNotNull(changeTrackerEntry);
                Assert.AreEqual(expected: 1, actual: changeTrackerEntries.Count);

                var continentHashCode = continent.GetHashCode();
                var changeTrackerEntryEntityHashCode = changeTrackerEntries.Single().Entity.GetHashCode();

                Assert.AreEqual(continentHashCode, changeTrackerEntryEntityHashCode);

                continent = null;
            }
        }

        [TestMethod]
        public void Test_Continent_GetContinents() {
            using (var entityManager = new CountriesEntityManager()) {
                var continents = entityManager.Set<Continent>()
                    .Include(x => x.Countries)
                    .ToList();

                Assert.IsNotNull(continents);
                Assert.AreNotEqual(notExpected: 0, actual: continents.Count);

                foreach (var continent in continents) {
                    // all entity properties must be virtual for this to work
                    var proxy = continent as IEntityWithChangeTracker;
                    Assert.IsNotNull(proxy);

                    Assert.IsTrue(continent.StringPropertiesAreTrim());
                }
            }
        }

        /// <summary>
        /// If there is something wrong with the entity class/relationship configuration, 
        /// this will throw an exception.
        /// </summary>
        [TestMethod]
        public void Test_Continent_Schema() {
            using (var entityManager = new CountriesEntityManager()) {
                var continents = entityManager.Set<Continent>().Take(1).ToList();
            }
        }

        #region Entity Create/Update/Delete Tests
        [TestMethod]
        public void Test_Continent_Create() {

            var continent = new Continent() {
                DetachedState = DetachedState.Added,
                Abbreviation = continentAbbreviationCreate,
                Name = continentNameCreate
            };

            var validationResults = Get_Validation_Results(continent);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Continent>(continent);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var continentFromDb = entityManager.Set<Continent>()
                    .Where(x => (x.Id == continent.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(continentFromDb);
                Assert.AreEqual(expected: continent.ToCSVString(), actual: continentFromDb.ToCSVString());

                // all entity properties must be virtual for this to work
                var proxy = continentFromDb as IEntityWithChangeTracker;
                Assert.IsNotNull(proxy);
            }
        }

        [TestMethod]
        public void Test_Continent_Update() {

            var continent = new Continent() {
                DetachedState = DetachedState.Added,
                Abbreviation = continentAbbreviationUpdate,
                Name = continentNameUpdate
            };

            var validationResults = Get_Validation_Results(continent);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Continent>(continent);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            var newContinentName = continent.Name + "2";
            continent.Name = newContinentName;

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.Attach<Continent>(continent);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var continentFromDb = entityManager.Set<Continent>()
                    .Where(x => (x.Id == continent.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(continentFromDb);
                Assert.AreEqual(expected: newContinentName, actual: continentFromDb.Name);
            }
        }

        [TestMethod]
        public void Test_Continent_Delete() {

            var continent = new Continent() {
                DetachedState = DetachedState.Added,
                Abbreviation = continentAbbreviationDelete,
                Name = continentNameDelete
            };

            var validationResults = Get_Validation_Results(continent);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Continent>(continent);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var continentFromDb = entityManager.Set<Continent>()
                    .Where(x => (x.Id == continent.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(continentFromDb);
            }

            var continentToDelete = new Continent() { DetachedState = DetachedState.Deleted, Id = continent.Id };
            using (var entityManager = new CountriesEntityManager()) {
                entityManager.RemoveEntity<Continent>(continentToDelete);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var continentFromDb = entityManager.Set<Continent>()
                    .Where(x => (x.Id == continent.Id))
                    .SingleOrDefault();

                Assert.IsNull(continentFromDb);
            }
        }

        [TestMethod]
        public void Test_Continent_Validation() {
            var continent = new Continent();
            continent.Abbreviation = null;
            continent.Name = null;

            var validationResults = Get_Validation_Results(continent);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "EntityKey", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Id", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Abbreviation", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Name", StringComparison.OrdinalIgnoreCase))));

            continent.Abbreviation = "xx";

            validationResults = Get_Validation_Results(continent);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Abbreviation", StringComparison.OrdinalIgnoreCase))));
        }
        #endregion
    }
}
