namespace Countries.DomainModel {

    #region Usings
    using Extensions;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    #endregion

    /// <summary>
    /// Base class for entity objects
    /// </summary>
    public abstract class EntityBase : IEntityBase {

        #region Members

        #region NotMapped properties

        [NotMapped]
        [EnumValidation(typeof(DetachedState))]
        public DetachedState DetachedState { get; set; }

        /// <summary>
        /// Generic property to access the database key
        /// </summary>
        [NotMapped]
        public object EntityKey { get; set; }

        /// <summary>
        /// Contains the starting original values of the entity, for using during SaveChanges() to determine the properties that are modified
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> StartingOriginalValues { get; set; }

        #endregion 
        #endregion

        #region Constructor       
        protected EntityBase() {
        }
        #endregion

        #region Methods

        /// <summary>
        /// Gets the maximum length value for a specified string property/column.
        /// </summary>
        /// <param name="propertyName">The name of the property/column.</param>
        /// <returns>The max length.</returns>
        public int GetMaxLength(string propertyName) {

            #region Validation
            if (string.IsNullOrWhiteSpace(propertyName)) {
                throw new ArgumentNullException("propertyName");
            }
            #endregion

            int maxLength = -1;

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(this)) {
                if (propertyDescriptor.Name == propertyName) {
                    foreach (var attribute in propertyDescriptor.Attributes) {
                        StringLengthAttribute stringLengthAttribute = attribute as StringLengthAttribute;
                        if (stringLengthAttribute != null) {
                            maxLength = stringLengthAttribute.MaximumLength;
                            break;
                        }
                    }

                    break;
                }
            }

            return maxLength;
        }

        /// <summary>
        /// Helper method to check string properties of this object for leading or trailing spaces
        /// </summary>
        [DebuggerStepThroughAttribute]
        public bool StringPropertiesAreTrim() {
            PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var stringPropertiesAreTrim = true;

            foreach (PropertyInfo propertyInfo in properties) {
                // Only work with strings
                if (propertyInfo.PropertyType != typeof(string)) { continue; }

                // If not writable then cannot null it; if not readable then cannot check it's value
                if (!propertyInfo.CanWrite || !propertyInfo.CanRead) { continue; }

                MethodInfo getMethod = propertyInfo.GetGetMethod(false);
                MethodInfo setMethod = propertyInfo.GetSetMethod(false);

                // Get and set methods need to be public
                if (getMethod == null) { continue; }
                if (setMethod == null) { continue; }

                string currentValue = propertyInfo.GetValue(this, new string[] { }) as string;
                if (currentValue != null) {
                    if (currentValue.Length > 0) {
                        if (char.IsWhiteSpace(currentValue[0])) {
                            stringPropertiesAreTrim = false;
                            break;
                        }
                    }
                    if (currentValue.Length > 1) {
                        if (char.IsWhiteSpace(currentValue[currentValue.Length - 1])) {
                            stringPropertiesAreTrim = false;
                            break;
                        }
                    }
                }
            }

            return stringPropertiesAreTrim;
        }

        /// <summary>
        /// Helper method to trim string properties of this object
        /// </summary>
        [DebuggerStepThroughAttribute]
        public void TrimStringProperties() {
            PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo propertyInfo in properties) {
                // Only work with strings
                if (propertyInfo.PropertyType != typeof(string)) { continue; }

                // If not writable then cannot null it; if not readable then cannot check it's value
                if (!propertyInfo.CanWrite || !propertyInfo.CanRead) { continue; }

                MethodInfo getMethod = propertyInfo.GetGetMethod(false);
                MethodInfo setMethod = propertyInfo.GetSetMethod(false);

                // Get and set methods need to be public
                if (getMethod == null) { continue; }
                if (setMethod == null) { continue; }

                string currentValue = propertyInfo.GetValue(this, new string[] { }) as string;
                if (currentValue != null) {
                    propertyInfo.SetValue(this, currentValue.Trim(), new object[] { });
                }
            }
        }

        /// <summary>
        /// Performs custom validation of the object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns>A list of validation failures</returns>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext = null) {

            if (this.DetachedState != DetachedState.Deleted) {

                #region Check for untrimmed string properties
                PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propertyInfo in properties) {
                    // Only work with strings
                    if (propertyInfo.PropertyType != typeof(string)) { continue; }

                    // If not writable then cannot null it; if not readable then cannot check it's value
                    if (!propertyInfo.CanWrite || !propertyInfo.CanRead) { continue; }

                    MethodInfo getMethod = propertyInfo.GetGetMethod(false);
                    MethodInfo setMethod = propertyInfo.GetSetMethod(false);

                    // Get and set methods need to be public
                    if (getMethod == null) { continue; }
                    if (setMethod == null) { continue; }

                    string currentValue = propertyInfo.GetValue(this, new string[] { }) as string;
                    if (currentValue != null) {
                        if (currentValue.Length > 0) {
                            if (char.IsWhiteSpace(currentValue[0])) {
                                yield return new ValidationResult(
                                    $"{propertyInfo.Name} is not trimmed. (Leading whitespace)", new[] { propertyInfo.Name });
                            }
                        }
                        if (currentValue.Length > 1) {
                            if (char.IsWhiteSpace(currentValue[currentValue.Length - 1])) {
                                yield return new ValidationResult(
                                    $"{propertyInfo.Name} is not trimmed. (Trailing whitespace)", new[] { propertyInfo.Name });
                            }
                        }
                    }
                }
                #endregion

                foreach (var validationResult in this.ValidateProperty(this.DetachedState, "DetachedState")) {
                    yield return validationResult;
                }

            } // if (this.DetachedState != DetachedState.Deleted) {
        }

        /// <summary>
        /// Validates the Data Annotations attributes of a property.
        /// </summary>
        /// <param name="propertyToValidate">The property to validate</param>
        /// <param name="memberName">The property name</param>
        /// <returns>The validation results.</returns>
        protected IEnumerable<ValidationResult> ValidateProperty(object propertyToValidate, string memberName) {
            var results = new List<ValidationResult>();

            Validator.TryValidateProperty(
                propertyToValidate, new ValidationContext(this, null, null) { MemberName = memberName }, results);

            return results;
        }
        #endregion
    }
}
