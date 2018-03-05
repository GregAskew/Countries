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
    /// Custom view for Country+Currency Information
    /// </summary>
    [Table("CountryCurrencyInfo")]
    public class CountryCurrencyInfo : EntityBase, ICountryCurrencyInfo {

        #region Members

        #region Fields
        public const string CSVString = "CountryId,CountryISO2,CountryISO3,CountryISONumeric,CountryName,CurrencyId,CurrencyName,CurrencyCode";
        #endregion

        [Key, Column(Order = 0)]
        public int CountryId { get; protected set; }
        public string CountryISO2 { get; protected set; }
        public string CountryISO3 { get; protected set; }
        public string CountryISONumeric { get; protected set; }
        public string CountryName { get; protected set; }
        [Key, Column(Order = 1)]
        public int? CurrencyId { get; protected set; }
        public string CurrencyName { get; protected set; }
        public string CurrencyCode { get; protected set; }
        #endregion

        #region Methods
        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.CountryId);
            info.AppendFormat("\"{0}\",", this.CountryISO2 ?? "NULL");
            info.AppendFormat("\"{0}\",", this.CountryISO3 ?? "NULL");
            info.AppendFormat("\"{0}\",", this.CountryISONumeric ?? "NULL");
            info.AppendFormat("\"{0}\",", this.CountryName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.CurrencyId.HasValue ? this.CurrencyId.ToString() : "NULL");
            info.AppendFormat("\"{0}\",", this.CurrencyName ?? "NULL");
            info.AppendFormat("\"{0}\"", this.CurrencyCode ?? "NULL");

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("CountryId: {0}; ", this.CountryId);
            info.AppendFormat("CountryISO2: {0}; ", this.CountryISO2 ?? "NULL");
            info.AppendFormat("CountryISO3: {0}; ", this.CountryISO3 ?? "NULL");
            info.AppendFormat("CountryISONumeric: {0}; ", this.CountryISONumeric ?? "NULL");
            info.AppendFormat("CountryName: {0}; ", this.CountryName ?? "NULL");
            info.AppendFormat("CurrencyId: {0}; ", this.CurrencyId.HasValue ? this.CurrencyId.ToString() : "NULL");
            info.AppendFormat("CurrencyName: {0}; ", this.CurrencyName ?? "NULL");
            info.AppendFormat("CurrencyCode: {0}; ", this.CurrencyCode ?? "NULL");

            return info.ToString();
        }

            #endregion
        }
}