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
    public class TimeZoneTests : BaseTestClass {

        private string timeZoneAbbreviationCreate = "ZZZZ";
        private string timeZoneNameCreate = "Fake TimeZone Create";
        private decimal timeZoneUTCOffsetCreate = 5.0m;
        private string timeZoneAbbreviationUpdate = "ZZZY";
        private string timeZoneNameUpdate = "Fake TimeZone Create";
        private decimal timeZoneUTCOffsetUpdate = 5.0m;
        private string timeZoneAbbreviationDelete = "ZZZX";
        private string timeZoneNameDelete = "Fake TimeZone Delete";
        private decimal timeZoneUTCOffsetDelete = 5.0m;

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
                    var sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[TimeZone] WHERE ([TimeZoneAcronym] = '{timeZoneAbbreviationCreate}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[TimeZone] WHERE ([TimeZoneAcronym] = '{timeZoneAbbreviationUpdate}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[TimeZone] WHERE ([TimeZoneAcronym] = '{timeZoneAbbreviationDelete}');");
                    entityManager.ExecuteScalarSql(sqlCommand);
                }
                catch (Exception e) {
                    Log.Error($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} {e.VerboseExceptionString()}");
                }
            }
        }
        #endregion

        [TestMethod]
        public void Test_TimeZone_ChangeTracker() {
            using (var entityManager = new CountriesEntityManager()) {
                var timeZone = entityManager.Set<Countries.DomainModel.TimeZone>().Create();
                timeZone = entityManager.Set<Countries.DomainModel.TimeZone>().Add(timeZone);

                var changeTrackerEntry = entityManager.Entry<Countries.DomainModel.TimeZone>(timeZone);
                var changeTrackerEntries = entityManager.ChangeTracker.Entries().ToList();

                Assert.IsNotNull(changeTrackerEntry);
                Assert.AreEqual(expected: 1, actual: changeTrackerEntries.Count);

                var timeZoneHashCode = timeZone.GetHashCode();
                var changeTrackerEntryEntityHashCode = changeTrackerEntries.Single().Entity.GetHashCode();

                Assert.AreEqual(timeZoneHashCode, changeTrackerEntryEntityHashCode);

                timeZone = null;
            }
        }

        [TestMethod]
        public void Test_TimeZone_GetTimeZones() {
            using (var entityManager = new CountriesEntityManager()) {
                var timeZones = entityManager.Set<Countries.DomainModel.TimeZone>()
                    .Include(x => x.Countries)
                    .ToList();

                Assert.IsNotNull(timeZones);
                Assert.AreNotEqual(notExpected: 0, actual: timeZones.Count);

                foreach (var timeZone in timeZones) {
                    // all entity properties must be virtual for this to work
                    var proxy = timeZone as IEntityWithChangeTracker;
                    Assert.IsNotNull(proxy);

                    Assert.IsTrue(timeZone.StringPropertiesAreTrim());
                }
            }
        }

        /// <summary>
        /// If there is something wrong with the entity class/relationship configuration, 
        /// this will throw an exception.
        /// </summary>
        [TestMethod]
        public void Test_TimeZone_Schema() {
            using (var entityManager = new CountriesEntityManager()) {
                var continents = entityManager.Set<Countries.DomainModel.TimeZone>().Take(1).ToList();
            }
        }

        #region Entity Create/Update/Delete Tests
        [TestMethod]
        public void Test_TimeZone_Create() {

            var timeZone = new Countries.DomainModel.TimeZone() {
                DetachedState = DetachedState.Added,
                TimeZoneAcronym = timeZoneAbbreviationCreate,
                TimeZoneName = timeZoneNameCreate,
                UTCOffset = timeZoneUTCOffsetCreate
            };

            var validationResults = Get_Validation_Results(timeZone);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Countries.DomainModel.TimeZone>(timeZone);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var timeZoneFromDb = entityManager.Set<Countries.DomainModel.TimeZone>()
                    .Where(x => (x.Id == timeZone.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(timeZoneFromDb);
                Assert.AreEqual(expected: timeZone.ToCSVString(), actual: timeZoneFromDb.ToCSVString());

                // all entity properties must be virtual for this to work
                var proxy = timeZoneFromDb as IEntityWithChangeTracker;
                Assert.IsNotNull(proxy);
            }
        }

        [TestMethod]
        public void Test_TimeZone_Update() {

            var timeZone = new Countries.DomainModel.TimeZone() {
                DetachedState = DetachedState.Added,
                TimeZoneAcronym = timeZoneAbbreviationUpdate,
                TimeZoneName = timeZoneNameUpdate,
                UTCOffset = timeZoneUTCOffsetUpdate
            };

            var validationResults = Get_Validation_Results(timeZone);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Countries.DomainModel.TimeZone>(timeZone);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            var newTimeZoneName = timeZone.TimeZoneName + "2";
            timeZone.TimeZoneName = newTimeZoneName;

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.Attach<Countries.DomainModel.TimeZone>(timeZone);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var timeZoneFromDb = entityManager.Set<Countries.DomainModel.TimeZone>()
                    .Where(x => (x.Id == timeZone.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(timeZoneFromDb);
                Assert.AreEqual(expected: newTimeZoneName, actual: timeZoneFromDb.TimeZoneName);
            }
        }

        [TestMethod]
        public void Test_TimeZone_Delete() {

            var timeZone = new Countries.DomainModel.TimeZone() {
                DetachedState = DetachedState.Added,
                TimeZoneAcronym = timeZoneAbbreviationDelete,
                TimeZoneName = timeZoneNameDelete,
                UTCOffset = timeZoneUTCOffsetDelete
            };

            var validationResults = Get_Validation_Results(timeZone);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<Countries.DomainModel.TimeZone>(timeZone);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var timeZoneFromDb = entityManager.Set<Countries.DomainModel.TimeZone>()
                    .Where(x => (x.Id == timeZone.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(timeZoneFromDb);
            }

            var timeZoneToDelete = new Countries.DomainModel.TimeZone() {
                DetachedState = DetachedState.Deleted,
                Id = timeZone.Id,
                RowVersion = timeZone.RowVersion
            };
            using (var entityManager = new CountriesEntityManager()) {
                entityManager.RemoveEntity<Countries.DomainModel.TimeZone>(timeZoneToDelete);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var timeZoneFromDb = entityManager.Set<Countries.DomainModel.TimeZone>()
                    .Where(x => (x.Id == timeZone.Id))
                    .SingleOrDefault();

                Assert.IsNull(timeZoneFromDb);
            }
        }

        [TestMethod]
        public void Test_TimeZone_Validation() {
            var timeZone = new Countries.DomainModel.TimeZone();
            timeZone.TimeZoneAcronym = null;
            timeZone.TimeZoneName = null;
            timeZone.UTCOffset = -15m;

            var validationResults = Get_Validation_Results(timeZone);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "EntityKey", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Id", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "TimeZoneAcronym", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "TimeZoneName", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "UTCOffset", StringComparison.OrdinalIgnoreCase))));

            timeZone.TimeZoneAcronym = "xx";

            validationResults = Get_Validation_Results(timeZone);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "TimeZoneAcronym", StringComparison.OrdinalIgnoreCase))));
        }
        #endregion
    }
}
