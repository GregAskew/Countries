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
    /// A custom view for detailed Country+TimeZone information.
    /// </summary>
    [Table("CountryTimeZoneInfo")]
    public class CountryTimeZoneInfo : EntityBase, ICountryTimeZoneInfo {

        #region Members
        #region Fields
        public const string CSVString = "TimeZoneAcronym,TimeZoneName,UTCOffset,DST,CountryISO2,CountryName";
        #endregion

        [Key, Column(Order = 0)]
        public string TimeZoneAcronym { get; protected set; }
        public string TimeZoneName { get; protected set; }
        public decimal UTCOffset { get; protected set; }
        public bool DST { get; protected set; }
        [Key, Column(Order = 1)]
        public string CountryISO2 { get; protected set; }
        public string CountryName { get; protected set; }
        #endregion

        #region Methods
        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.TimeZoneAcronym ?? "NULL");
            info.AppendFormat("\"{0}\",", this.TimeZoneName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.UTCOffset.ToString("00.00"));
            info.AppendFormat("\"{0}\",", this.DST.ToString().ToUpperInvariant());
            info.AppendFormat("\"{0}\",", this.CountryISO2 ?? "NULL");
            info.AppendFormat("\"{0}\"", this.CountryName ?? "NULL");

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("TimeZoneAcronym: {0}; ", this.TimeZoneAcronym ?? "NULL");
            info.AppendFormat("TimeZoneName: {0}; ", this.TimeZoneName ?? "NULL");
            info.AppendFormat("UTCOffset: {0}; ", this.UTCOffset.ToString("00.00"));
            info.AppendFormat("DST: {0}; ", this.DST.ToString().ToUpperInvariant());
            info.AppendFormat("CountryISO2: {0}; ", this.CountryISO2 ?? "NULL");
            info.AppendFormat("CountryName: {0}; ", this.CountryName ?? "NULL");

            return info.ToString();
        }
        #endregion
    }
}