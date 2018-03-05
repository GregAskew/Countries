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
    public class CountryTimeZoneInfoTests {

        [TestMethod]
        public void Test_GetCountryTimeZoneInfo() {

            using (var entityManager = new CountriesEntityManager()) {
                var countryTimeZoneInfos = entityManager.CountryTimeZoneInfos
                    .ToList();

                Assert.IsNotNull(countryTimeZoneInfos);
                Assert.AreNotEqual(notExpected: 0, actual: countryTimeZoneInfos.Count);

                var countryInfosOrderedByCountryISO2 = countryTimeZoneInfos
                    .GroupBy(x =>  x.CountryISO2)
                    .OrderByDescending(x => x.Count())
                    .SelectMany(x => x)
                    .ToList();

                foreach (var countryTimeZoneInfo in countryInfosOrderedByCountryISO2) {
                    Debug.WriteLine(countryTimeZoneInfo.ToString());
                }

                foreach (var countryTimeZoneInfo in countryTimeZoneInfos) {
                    Debug.WriteLine(countryTimeZoneInfo.ToString());
                    Assert.IsNotNull(countryTimeZoneInfo.ToString());
                }

                Debug.WriteLine(CountryTimeZoneInfo.CSVString);
                foreach (var countryTimeZoneInfo in countryTimeZoneInfos) {
                    Debug.WriteLine(countryTimeZoneInfo.ToCSVString());
                    Assert.IsNotNull(countryTimeZoneInfo.ToCSVString());
                }
            }
        }
    }
}
