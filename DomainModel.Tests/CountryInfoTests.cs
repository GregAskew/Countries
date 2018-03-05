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
    public class CountryInfoTests {

        [TestMethod]
        public void Test_GetCountryInfo() {

            using (var entityManager = new CountriesEntityManager()) {
                var countryInfos = entityManager.CountryInfos
                    .ToList();

                Assert.IsNotNull(countryInfos);
                Assert.AreNotEqual(notExpected: 0, actual: countryInfos.Count);

                var countryInfosOrderedByContinentCount = countryInfos
                    .GroupBy(x => x.ContinentName)
                    .OrderByDescending(x => x.Count())
                    .SelectMany(x => x)
                    .ToList();

                var countryInfosOrderedByCallingCodeNumberCount = countryInfos
                    .GroupBy(x => x.CallingCodeNumber)
                    .OrderByDescending(x => x.Count())
                    .SelectMany(x => x)
                    .ToList();

                var countryInfosOrderedByName = countryInfos
                    .OrderBy(x => x.Name)
                    .ToList();


                foreach (var countryInfo in countryInfosOrderedByCallingCodeNumberCount) {
                    Debug.WriteLine(countryInfo.ToString());
                }

                foreach (var countryInfo in countryInfosOrderedByName) {
                    Debug.WriteLine(countryInfo.Name);
                }

                foreach (var countryInfo in countryInfos) {
                    Debug.WriteLine(countryInfo.ToString());
                    Assert.IsNotNull(countryInfo.ToString());
                }

                Debug.WriteLine(Country.CSVString);
                foreach (var countryInfo in countryInfos) {
                    Debug.WriteLine(countryInfo.ToCSVString());
                    Assert.IsNotNull(countryInfo.ToCSVString());
                }
            }
        }
    }
}
