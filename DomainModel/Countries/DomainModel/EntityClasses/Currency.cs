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
    /// A currency entity. 
    /// A country may have multiple currencies, and a currency may be used in multiple countries.
    /// </summary>
    [Table("Currency")]
    [DataContract(IsReference = true)]
    public class Currency : EntityBase, ICurrency {

        #region Members

        #region Fields
        private const int DatabaseIdentitySeedStart = 40001;
        public const string CSVString = "Id,Code,Name,DecimalDigits";
        #endregion

        /// <summary>
        /// The currency code abbreviation
        /// </summary>
        [Column(TypeName = "char")]
        [StringLength(maximumLength: 3, MinimumLength = 3)]
        [Required]
        [RegularExpression(@"^[A-Z]+$", ErrorMessage = "Only uppercase alpha characters are allowed.")]
        public virtual string Code { get; set; }

        /// <summary>
        /// The number of digits to the right of the decimal point.
        /// </summary>
        [Range(0, 3)]
        public virtual int DecimalDigits { get; set; }

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
        /// The currency name.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(50)]
        [Required]
        public virtual string Name { get; set; }

        public virtual byte[] RowVersion { get; set; }

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
        public Currency() {
            this.Code = string.Empty;
            this.Name = string.Empty;
        }
        #endregion

        #region Methods

        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.Id);
            info.AppendFormat("\"{0}\",", this.Code ?? "NULL");
            info.AppendFormat("\"{0}\",", this.Name ?? "NULL");
            info.AppendFormat("\"{0}\"", this.DecimalDigits);

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("Code: {0}; ", this.Code ?? "NULL");
            info.AppendFormat("Name: {0}; ", this.Name ?? "NULL");
            info.AppendFormat("DecimalDigits: {0}; ", this.DecimalDigits);

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

                foreach (var validationResult in base.ValidateProperty(this.Code, "Code")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.DecimalDigits, "DecimalDigits")) {
                    yield return validationResult;
                }

                foreach (var validationResult in base.ValidateProperty(this.Name, "Name")) {
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