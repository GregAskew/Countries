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

    /// <summary>
    /// A TimeZone entity.
    /// </summary>
    [Table("TimeZone")]
    [DataContract(IsReference = true)]
    public class TimeZone : EntityBase, ITimeZone {

        #region Members

        #region Fields
        private const int DatabaseIdentitySeedStart = 50001;
        public const string CSVString = "Id,TimeZoneAcronym,TimeZoneName,UTCOffset,DST";
        #endregion

        /// <summary>
        /// Daylight Saving Time designator.
        /// </summary>
        public virtual bool DST { get; set; }

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

        [Timestamp]
        public virtual byte[] RowVersion { get; set; }

        /// <summary>
        /// The time zone acronym.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(10)]
        [Required]
        [RegularExpression(@"^[A-Z]+$", ErrorMessage = "Only uppercase alpha characters are allowed.")]
        public virtual string TimeZoneAcronym { get; set; }

        /// <summary>
        /// The time zone name.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(255)]
        [Required]
        public virtual string TimeZoneName { get; set; }

        /// <summary>
        /// The amount of time in minutes offset from Universal Time Coordinated (UTC).
        /// </summary>
        [Range(typeof(decimal), "-14", "14")]
        public virtual decimal UTCOffset { get; set; }

        #region Related Entities
        public virtual ICollection<Country> Countries {
            get {
                if (countries == null) countries = new List<Country>();
                return countries;
            }
            set { countries = value; }
        }
        private ICollection<Country> countries;
        #endregion
        #endregion

        #region Constructor
        public TimeZone() {
            this.TimeZoneAcronym = string.Empty;
            this.TimeZoneName = string.Empty;
        }
        #endregion

        #region Methods

        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.Id);
            info.AppendFormat("\"{0}\",", this.TimeZoneAcronym ?? "NULL");
            info.AppendFormat("\"{0}\",", this.TimeZoneName ?? "NULL");
            info.AppendFormat("\"{0}\",", this.UTCOffset.ToString("00.00"));
            info.AppendFormat("\"{0}\"", this.DST);

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("TimeZoneAcronym: {0}; ", this.TimeZoneAcronym ?? "NULL");
            info.AppendFormat("TimeZoneName: {0}; ", this.TimeZoneName ?? "NULL");
            info.AppendFormat("UTCOffset: {0}; ", this.UTCOffset.ToString("00.00"));
            info.AppendFormat("DST: {0}; ", this.DST);

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

                foreach (var validationResult in base.ValidateProperty(this.TimeZoneAcronym, "TimeZoneAcronym")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.TimeZoneName, "TimeZoneName")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.UTCOffset, "UTCOffset")) {
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