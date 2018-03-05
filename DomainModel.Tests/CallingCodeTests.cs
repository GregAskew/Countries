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
    public class CallingCodeTests : BaseTestClass {

        private int callingCodeNumberCreate = 10;
        private int callingCodeNumberUpdate = 11;
        private int callingCodeNumberUpdate2 = 12;
        private int callingCodeNumberDelete = 13;

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
                    var sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CallingCode] WHERE ([CallingCodeNumber] = {callingCodeNumberCreate});");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CallingCode] WHERE ([CallingCodeNumber] = {callingCodeNumberUpdate});");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CallingCode] WHERE ([CallingCodeNumber] = {callingCodeNumberUpdate2});");
                    entityManager.ExecuteScalarSql(sqlCommand);
                    sqlCommand = new SqlCommand($"DELETE [Countries].[dbo].[CallingCode] WHERE ([CallingCodeNumber] = {callingCodeNumberDelete});");
                    entityManager.ExecuteScalarSql(sqlCommand);
                }
                catch (Exception e) {
                    Log.Error($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} {e.VerboseExceptionString()}");
                }
            }
        }
        #endregion

        [TestMethod]
        public void Test_CallingCode_ChangeTracker() {
            using (var entityManager = new CountriesEntityManager()) {
                var callingCode = entityManager.Set<CallingCode>().Create();
                callingCode = entityManager.Set<CallingCode>().Add(callingCode);

                var changeTrackerEntry = entityManager.Entry<CallingCode>(callingCode);
                var changeTrackerEntries = entityManager.ChangeTracker.Entries().ToList();

                Assert.IsNotNull(changeTrackerEntry);
                Assert.AreEqual(expected: 1, actual: changeTrackerEntries.Count);

                var callingCodeHashCode = callingCode.GetHashCode();
                var changeTrackerEntryEntityHashCode = changeTrackerEntries.Single().Entity.GetHashCode();

                Assert.AreEqual(callingCodeHashCode, changeTrackerEntryEntityHashCode);

                callingCode = null;
            }
        }

        [TestMethod]
        public void Test_CallingCode_GetCallingCodes() {
            using (var entityManager = new CountriesEntityManager()) {
                var callingCodes = entityManager.Set<CallingCode>().ToList();

                Assert.IsNotNull(callingCodes);
                Assert.AreNotEqual(notExpected: 0, actual: callingCodes.Count);

                foreach (var callingCode in callingCodes) {
                    // all entity properties must be virtual for this to work
                    var proxy = callingCode as IEntityWithChangeTracker;
                    Assert.IsNotNull(proxy);
                }
            }
        }

        /// <summary>
        /// If there is something wrong with the entity class/relationship configuration, 
        /// this will throw an exception.
        /// </summary>
        [TestMethod]
        public void Test_CallingCode_Schema() {
            using (var entityManager = new CountriesEntityManager()) {
                var callingCodes = entityManager.Set<CallingCode>().Take(1).ToList();
            }
        }

        #region Entity Create/Update/Delete Tests
        [TestMethod]
        public void Test_CallingCode_Create() {

            var callingCode = new CallingCode() {
                DetachedState = DetachedState.Added,
                CallingCodeNumber = callingCodeNumberCreate
            };

            var validationResults = Get_Validation_Results(callingCode);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<CallingCode>(callingCode);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var dallingCodeFromDb = entityManager.Set<CallingCode>()
                    .Where(x => (x.Id == callingCode.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(dallingCodeFromDb);
                Assert.AreEqual(expected: callingCode.ToCSVString(), actual: dallingCodeFromDb.ToCSVString());

                // all entity properties must be virtual for this to work
                var proxy = dallingCodeFromDb as IEntityWithChangeTracker;
                Assert.IsNotNull(proxy);
            }
        }

        [TestMethod]
        public void Test_CallingCode_Update() {

            var callingCode = new CallingCode() {
                DetachedState = DetachedState.Added,
                CallingCodeNumber = callingCodeNumberUpdate
            };

            var validationResults = Get_Validation_Results(callingCode);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<CallingCode>(callingCode);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            callingCode.CallingCodeNumber= callingCodeNumberUpdate2;

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.Attach<CallingCode>(callingCode);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var callingCodeFromDb = entityManager.Set<CallingCode>()
                    .Where(x => (x.Id == callingCode.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(callingCodeFromDb);
                Assert.AreEqual(expected: callingCodeNumberUpdate2, actual: callingCodeFromDb.CallingCodeNumber);
            }
        }

        [TestMethod]
        public void Test_CallingCode_Delete() {

            var callingCode = new CallingCode() {
                DetachedState = DetachedState.Added,
                CallingCodeNumber = callingCodeNumberDelete
            };

            var validationResults = Get_Validation_Results(callingCode);
            Assert.AreEqual(expected: 0, actual: validationResults.Count);

            using (var entityManager = new CountriesEntityManager()) {
                entityManager.AddEntity<CallingCode>(callingCode);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var callingCodeFromDb = entityManager.Set<CallingCode>()
                    .Where(x => (x.Id == callingCode.Id))
                    .SingleOrDefault();

                Assert.IsNotNull(callingCodeFromDb);
            }

            var callingCodeToDelete = new CallingCode() { DetachedState = DetachedState.Deleted, Id = callingCode.Id };
            using (var entityManager = new CountriesEntityManager()) {
                entityManager.RemoveEntity<CallingCode>(callingCodeToDelete);
                int updateCount = entityManager.SaveChanges();
                Assert.AreNotEqual(notExpected: 0, actual: updateCount);
            }

            using (var entityManager = new CountriesEntityManager()) {
                var callingCodeFromDb = entityManager.Set<CallingCode>()
                    .Where(x => (x.Id == callingCode.Id))
                    .SingleOrDefault();

                Assert.IsNull(callingCodeFromDb);
            }
        }

        [TestMethod]
        public void Test_CallingCode_Validation() {
            var callingCode = new CallingCode();
            callingCode.CallingCodeNumber = 1001;

            var validationResults = Get_Validation_Results(callingCode);

            Assert.IsTrue(validationResults.Count > 0);
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "EntityKey", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "Id", StringComparison.OrdinalIgnoreCase))));
            Assert.IsTrue(validationResults.Any(
                x => x.MemberNames.Any(y => string.Equals(y, "CallingCodeNumber", StringComparison.OrdinalIgnoreCase))));

        }
        #endregion
    }
}
