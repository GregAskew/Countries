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
    /// A Continent entity.
    /// </summary>
    [Table("Continent")]
    [DataContract(IsReference = true)]
    public class Continent : EntityBase, IContinent {

        #region Members

        #region Fields
        private const int DatabaseIdentitySeedStart = 1;
        public const string CSVString = "Id,Abbreviation,Name";
        #endregion

        /// <summary>
        /// The continent two-character ISO abbreviation.
        /// https://en.wikipedia.org/wiki/List_of_sovereign_states_and_dependent_territories_by_continent_(data_file)
        /// </summary>
        [Column(TypeName = "char")]
        [StringLength(maximumLength: 2, MinimumLength = 2)]
        [Required]
        [RegularExpression(@"^[A-Z]+$", ErrorMessage = "Only uppercase alpha characters are allowed.")]
        public virtual string Abbreviation { get; set; }

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
        /// The continent name.
        /// </summary>
        [Column(TypeName = "varchar")]
        [StringLength(255)]
        [Required]
        public virtual string Name { get; set; }

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
        public Continent() {
            this.Abbreviation = string.Empty;
            this.Name = string.Empty;
        }
        #endregion

        #region Methods

        [DebuggerStepThroughAttribute]
        public string ToCSVString() {
            var info = new StringBuilder();

            info.AppendFormat("\"{0}\",", this.Id);
            info.AppendFormat("\"{0}\",", this.Abbreviation ?? "NULL");
            info.AppendFormat("\"{0}\"", this.Name ?? "NULL");

            return info.ToString();
        }

        [DebuggerStepThroughAttribute]
        public override string ToString() {
            var info = new StringBuilder();

            info.AppendFormat("Name: {0}; ", this.Name ?? "NULL");
            info.AppendFormat("Abbreviation: {0}; ", this.Abbreviation ?? "NULL");

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

                foreach (var validationResult in base.ValidateProperty(this.Abbreviation, "Abbreviation")) {
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