namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics;
    using System.Text;
    #endregion

    /// <summary>
    /// A custom view for detailed Country information.
    /// </summary>
    [Table("CountryInfo")]
    public class CountryInfo : EntityBase, ICountryInfo {

        #region Members
        #region Fields
        public const string CSVString = "CountryId,CountryISO2,CountryISO3,CountryISONumeric,CountryName,CurrencyId,CurrencyName,CurrencyCode";
        #endregion

        [Key]
        public int Id { get; protected set; }
        public string ISO2 { get; protected set; }
        public string ISO3 { get; protected set; }
        public string ISONumeric { get; protected set; }
        public string Name { get; protected set; }
        public string ISOName { get; protected set; }
        public string OfficialName { get; protected set; }
        public string Capital { get; protected set; }
        public string ContinentName { get; protected set; }
        public int? CallingCodeNumber { get; protected set; }
        #endregion

        #region Methods
        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.Id);
            info.AppendFormat("\"{0}\",", this.ISO2 ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ISO3 ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ISONumeric ?? "NULL");
            info.AppendFormat("\"{0}\",", this.Name ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ISOName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.OfficialName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.Capital ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ContinentName ?? "NULL");
            info.AppendFormat("\"{0}\"", this.CallingCodeNumber.HasValue ? this.CallingCodeNumber.ToString() : "NULL");

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("Id: {0}; ", this.Id);
            info.AppendFormat("ISO2: {0}; ", this.ISO2 ?? "NULL");
            info.AppendFormat("ISO3: {0}; ", this.ISO3 ?? "NULL");
            info.AppendFormat("ISONumeric: {0}; ", this.ISONumeric ?? "NULL");
            info.AppendFormat("Name: {0}; ", this.Name ?? "NULL");
            info.AppendFormat("OfficialName: {0}; ", this.OfficialName ?? "NULL");
            info.AppendFormat("Capital: {0}; ", this.Capital ?? "NULL");
            info.AppendFormat("ContinentName: {0}; ", this.ContinentName ?? "NULL");
            info.AppendFormat("CallingCodeNumber: {0}; ", this.CallingCodeNumber.HasValue ? this.CallingCodeNumber.ToString() : "NULL");

            return info.ToString();
        }
        #endregion
    }
}