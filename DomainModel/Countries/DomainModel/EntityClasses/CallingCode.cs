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
    /// The international calling code for a country.
    /// </summary>
    [Table("CallingCode")]
    [DataContract(IsReference = true)]
    public class CallingCode : EntityBase, ICallingCode {

        #region Members

        #region Fields
        private const int DatabaseIdentitySeedStart = 20001;
        public const string CSVString = "Id,CallingCodeNumber";
        #endregion

        /// <summary>
        /// The international calling code numer.
        /// </summary>
        [Range(1, 1000)]
        public virtual int CallingCodeNumber { get; set; }

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
        public CallingCode() {

        }
        #endregion

        #region Methods

        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.Id);
            info.AppendFormat("\"{0}\"", this.CallingCodeNumber);

            return info.ToString();
        }
        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("CallingCodeNumber: {0}; ", this.CallingCodeNumber);

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

                foreach (var validationResult in base.ValidateProperty(this.CallingCodeNumber, "CallingCodeNumber")) {
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