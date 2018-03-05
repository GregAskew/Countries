namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Text;
    #endregion

    [Table("Country")]
    [DataContract(IsReference = true)]
    public class Country : EntityBase, ICountry {

        #region Members

        #region Fields
        private const int DatabaseIdentitySeedStart = 30001;
        public const string CSVString = "Id,ISO2,ISO3,ISONumeric,ISOName,Name,OfficialName,Capital,Continent,CallingCode";
        #endregion

        /// <summary>
        /// The country's capital city.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(255)]
        [Required(AllowEmptyStrings = true)]
        public virtual string Capital { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id {
            get {
                return id;
            }
            set {
                id = value;
                this.EntityKey = value;
            }
        }
        private int id;

        /// <summary>
        /// The country ISO two-character code.
        /// https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// </summary>
        [Column(TypeName = "char")]
        [StringLength(maximumLength: 2, MinimumLength = 2)]
        [Required]
        [RegularExpression(@"^[A-Z]+$", ErrorMessage = "Only uppercase alpha characters are allowed.")]
        public virtual string ISO2 { get; set; }

        /// <summary>
        /// The country ISO three-character code.
        /// https://en.wikipedia.org/wiki/ISO_3166-1_alpha-3
        /// </summary>
        [Column(TypeName = "char")]
        [StringLength(maximumLength: 3, MinimumLength = 3)]
        [Required]
        [RegularExpression(@"^[A-Z]+$", ErrorMessage = "Only uppercase alpha characters are allowed.")]
        public virtual string ISO3 { get; set; }

        /// <summary>
        /// The country ISO short name.
        /// https://www.iso.org/obp/ui/#search
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(255)]
        [Required]
        public virtual string ISOName { get; set; }

        /// <summary>
        /// The country ISO three-character numeric code, padded with leading zeros.
        /// https://en.wikipedia.org/wiki/ISO_3166-1_numeric
        /// </summary>
        [Column(TypeName = "char")]
        [StringLength(maximumLength: 3, MinimumLength = 3)]
        [Required]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Only uppercase numeric characters are allowed.")]
        public virtual string ISONumeric { get; set; }

        /// <summary>
        /// The country short name, as customized/expected/used by the calling application.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(255)]
        [Required]
        public virtual string Name { get; set; }

        /// <summary>
        /// The country official name.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(255)]
        [Required]
        public virtual string OfficialName { get; set; }

        [Timestamp]
        public virtual byte[] RowVersion { get; set; }

        #region Related Entities
        [ForeignKey("CallingCode")]
        public virtual int? CallingCodeId {
            get {
                if (this.CallingCode != null) return this.CallingCode.Id;
                return callingCodeId;
            }
            set { callingCodeId = value; }
        }
        private int? callingCodeId;
        public virtual CallingCode CallingCode {
            get { return callingCode; }
            set {
                callingCode = value;
                this.CallingCodeId = callingCode?.Id;
            }
        }
        private CallingCode callingCode;

        /// <summary>
        /// The Continent.
        /// </summary>
        /// <remarks>
        /// Assignment of some countries to continents may be arbitrary due to some countries
        /// may not reside in a single continent. Examples would include
        /// Armenia, Azerbaijan, Cyprus, Georgia, Kazakhstan, Russian Federation, and Turkey
        /// which may be considered "Eurasia".
        /// </remarks>
        [ForeignKey("Continent")]
        public virtual int ContinentId {
            get {
                if (this.Continent != null) return this.Continent.Id;
                return continentId;
            }
            set { continentId = value; }
        }
        private int continentId;
        public virtual Continent Continent {
            get { return continent; }
            set {
                continent = value;
                if (continent != null) {
                    this.ContinentId = continent.Id;
                }
                else {
                    this.ContinentId = 0;
                }
            }
        }
        private Continent continent;

        public virtual ICollection<Currency> Currencies {
            get {
                if (currencies == null) currencies = new List<Currency>();
                return currencies;
            }
            set { currencies = value; }
        }
        private ICollection<Currency> currencies;

        public virtual ICollection<TimeZone> TimeZones {
            get {
                if (timeZones == null) timeZones = new List<TimeZone>();
                return timeZones;
            }
            set { timeZones = value; }
        }
        private ICollection<TimeZone> timeZones;
        #endregion
        #endregion

        #region Constructor
        public Country() {
            this.Capital = string.Empty;
            this.ISO2 = string.Empty;
            this.ISO3 = string.Empty;
            this.ISOName = string.Empty;
            this.ISONumeric = string.Empty;
            this.Name = string.Empty;
            this.OfficialName = string.Empty;
        }
        #endregion

        #region Methods

        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.Id);
            info.AppendFormat("\"{0}\",", this.ISO2 ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ISO3 ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ISONumeric ?? "NULL");
            info.AppendFormat("\"{0}\",", this.ISOName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.Name ?? "NULL");
            info.AppendFormat("\"{0}\",", this.OfficialName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.Capital ?? "NULL");
            info.AppendFormat("\"{0}\",", this.Continent != null ? this.Continent.Name ?? "NULL" : "NULL");
            info.AppendFormat("\"{0}\"", this.CallingCode != null ? this.CallingCode.CallingCodeNumber.ToString() : "NULL");

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("Name: {0}; ", this.Name ?? "NULL");
            info.AppendFormat("Capital: {0}; ", this.Capital ?? "NULL");
            info.AppendFormat("ISO2: {0}; ", this.ISO2 ?? "NULL");
            info.AppendFormat("ISO3: {0}; ", this.ISO3 ?? "NULL");
            info.AppendFormat("ISONumeric: {0}; ", this.ISONumeric ?? "NULL");
            info.AppendFormat("ISOName: {0}; ", this.ISOName ?? "NULL");
            info.AppendFormat("OfficialName: {0}; ", this.OfficialName ?? "NULL");
            info.AppendFormat("CallingCodeNumber: {0}; ", this.CallingCode != null ? this.CallingCode.CallingCodeNumber.ToString() : "NULL");
            info.AppendFormat("Continent: {0}; ", this.Continent != null ? this.Continent.Name ?? "NULL" : "NULL");

            return info.ToString();
        }

        /// <summary>
        /// Performs custom validation of the object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>A list of validation failures</returns>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext = null) {

            if (this.DetachedState != DetachedState.Added) {

                if (this.Id < DatabaseIdentitySeedStart) {
                    yield return new ValidationResult(
                    $"Id is invalid: {this.Id}. Must be equal or greater than: {DatabaseIdentitySeedStart}", new[] { "Id" });
                }

                if ((this.EntityKey == null) || (this.Id.ToString() != this.EntityKey.ToString())) {
                    yield return new ValidationResult(
                        $"EntityKey: {this.EntityKey ?? "NULL"} is required/must match Id: {this.Id}.", new[] { "EntityKey" });
                }
            }

            if (this.DetachedState != DetachedState.Deleted) {

                foreach (var validationResult in base.ValidateProperty(this.Capital, "Capital")) {
                    yield return validationResult;
                }

                if (this.Continent == null) {
                    yield return new ValidationResult(
                    $"Continent is required.", new[] { "Continent" });
                }

                foreach (var validationResult in base.ValidateProperty(this.ISO2, "ISO2")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.ISO3, "ISO3")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.ISOName, "ISOName")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.ISONumeric, "ISONumeric")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.Name, "Name")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.OfficialName, "OfficialName")) {
                    yield return validationResult;
                }
            }

            foreach (var result in base.Validate(validationContext)) {
                yield return result;
            }

        }
        #endregion
    }
}