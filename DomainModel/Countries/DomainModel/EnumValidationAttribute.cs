namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    #endregion

    public class EnumValidationAttribute : ValidationAttribute {

        private readonly Type EnumType;

        public EnumValidationAttribute(Type enumType) {
            this.EnumType = enumType;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {

            if (!this.EnumType.IsEnum) {
                return new ValidationResult(
                    $"Type: {this.EnumType.Name} is not an enum Type.", new[] { validationContext.MemberName });
            }
            // does not work for Flags with multiple values
            else if (Enum.IsDefined(this.EnumType, value)) {
                return null;
            }
            else {
                // for flags, converting to a string will always be a friendly text. If a number, it isn't valid.
                char firstDigit = value.ToString()[0];
                if (!char.IsDigit(firstDigit) && (firstDigit != '-')) {
                    return null;
                }
            }

            return new ValidationResult(
                $"{value.ToString()} is not valid for Type: {this.EnumType.Name}", new[] { validationContext.MemberName });
        }

    }
}
