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
    public class CountryCurrencyInfoTests {

        [TestMethod]
        public void Test_GetCountryCurrencyInfos() {

            using (var entityManager = new CountriesEntityManager()) {
                var countryCurrencyInfos = entityManager.CountryCurrencyInfos
                    .ToList();

                Assert.IsNotNull(countryCurrencyInfos);
                Assert.AreNotEqual(notExpected: 0, actual: countryCurrencyInfos.Count);

                var countryCurrencyInfosOrderedByCountryCount = countryCurrencyInfos
                    .GroupBy(x => x.CountryId)
                    .OrderByDescending(x => x.Count())
                    .SelectMany(x => x)
                    .ToList();

                foreach (var countryCurrencyInfo in countryCurrencyInfos) {
                    Debug.WriteLine(countryCurrencyInfo.ToString());
                    Assert.IsNotNull(countryCurrencyInfo.ToString());
                }

                Debug.WriteLine(CountryCurrencyInfo.CSVString);
                foreach (var countryCurrencyInfo in countryCurrencyInfos) {
                    Debug.WriteLine(countryCurrencyInfo.ToCSVString());
                    Assert.IsNotNull(countryCurrencyInfo.ToCSVString());
                }

                // Check for known countries that have multiple currencies to validate the View is
                // being keyed properly.
                Assert.IsTrue(countryCurrencyInfos.Any(x => string.Equals(x.ToCSVString(), "\"30249\",\"ZW\",\"ZWE\",\"716\",\"Zimbabwe\",\"40028\",\"Botswana pula\",\"BWP\"")));
                Assert.IsTrue(countryCurrencyInfos.Any(x => string.Equals(x.ToCSVString(), "\"30249\",\"ZW\",\"ZWE\",\"716\",\"Zimbabwe\",\"40054\",\"Euro\",\"EUR\"")));
                Assert.IsTrue(countryCurrencyInfos.Any(x => string.Equals(x.ToCSVString(), "\"30249\",\"ZW\",\"ZWE\",\"716\",\"Zimbabwe\",\"40057\",\"Pound Sterling\",\"GBP\"")));
                Assert.IsTrue(countryCurrencyInfos.Any(x => string.Equals(x.ToCSVString(), "\"30249\",\"ZW\",\"ZWE\",\"716\",\"Zimbabwe\",\"40158\",\"United States dollar\",\"USD\"")));
                Assert.IsTrue(countryCurrencyInfos.Any(x => string.Equals(x.ToCSVString(), "\"30249\",\"ZW\",\"ZWE\",\"716\",\"Zimbabwe\",\"40188\",\"South African rand\",\"ZAR\"")));

            }
        }
    }
}
